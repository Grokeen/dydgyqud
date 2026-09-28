using UnityEngine;
using UnityEngine.Rendering;

namespace MiniFortress
{
    // 2.5D presentation: fighters, weapons, projectiles and platforms are lit 3D meshes, while every gameplay rule
    // (movement, collision rectangles, ballistics) stays on the z = 0 plane. The painted map backdrop is unchanged
    // and pushed far behind the meshes.
    public sealed partial class FortressGame
    {
        // Platforms sit entirely behind the plane fighters walk on (z = 0), like the old sprite platforms,
        // so a fighter under or beside a platform is never hidden by it. Fighters reach about 0.7 m deep
        // (spearman shield, captain scale), and the cap overhangs the block by 0.09 m.
        const float PlatformFrontZ = 1f, PlatformBackZ = 5.4f, PlatformCapHeight = .32f;
        const float BackdropDepth = 60;

        void SetUpWorld3D()
        {
            if (!worldCamera.GetComponent<FortressObliqueCamera>()) worldCamera.gameObject.AddComponent<FortressObliqueCamera>();
            var backdropPosition = arena.background.transform.position;
            arena.background.transform.position = new Vector3(backdropPosition.x, backdropPosition.y, BackdropDepth);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.33f, .37f, .48f);
            // Moonlight from the upper left, in front of the plane, so shadows fall back onto platform tops.
            var moon = SceneLight("Moonlight", new Vector3(48, 28, 0), new Color(.86f, .9f, 1f), 1.35f, LightShadows.Soft);
            moon.shadowStrength = .65f;
            // Warm torch-like fill from the right rim.
            SceneLight("Torch fill", new Vector3(18, -125, 0), new Color(1f, .62f, .34f), .45f, LightShadows.None);
        }

        Light SceneLight(string name, Vector3 euler, Color color, float intensity, LightShadows shadows)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.transform.rotation = Quaternion.Euler(euler);
            light.type = LightType.Directional; light.color = color; light.intensity = intensity; light.shadows = shadows;
            return light;
        }

        // Replaces a projectile sprite holder's look with the character's projectile model: its
        // projectileModelPrefab if set (tip at the origin, pointing +X), else the built-in 3D arrow or spear.
        static void ShowProjectileModel(Transform holder, FortressCharacterDefinition definition)
        {
            var sprite = holder.GetComponent<SpriteRenderer>();
            if (sprite) sprite.enabled = false;
            string wanted = FortressModel3D.ProjectileName(definition);
            bool found = false;
            foreach (Transform child in holder)
            {
                bool match = child.name == wanted;
                child.gameObject.SetActive(match); found |= match;
            }
            if (!found) FortressModel3D.Projectile(holder, definition);
        }

        Transform ProjectileObject(string name, FortressCharacterDefinition definition, Vector2 position)
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(transform, false);
            holder.localPosition = position;
            ShowProjectileModel(holder, definition);
            return holder;
        }

        // One platform's look, rebuilt whenever a map is applied. In order of preference: the platform's own model,
        // the map's look model, or a generated block (stone, or planks for drop-through bridges) with a slightly
        // wider cap. The top of the look is exactly the collision rectangle's top, where fighters' feet rest.
        Transform BuildPlatformVisual(Transform previous, int index, FortressMapPlatform platform, FortressMapDefinition map)
        {
            if (previous) DestroyPlatformVisual(previous);
            var root = new GameObject("Platform visual " + (index + 1)).transform;
            root.SetParent(transform, false);
            Rect rect = platform.Rect;
            float thickness = Mathf.Clamp(rect.height, .9f, 3.2f);
            var look = (platform.dropThrough ? map.dropThroughLook : map.groundLook) ?? new FortressPlatformLook();
            var model = platform.model ? platform.model : look.model;
            if (model) { FitPlatformModel(root, model, rect, thickness, platform.model ? FortressModelFit.Stretch : look.fit); return root; }

            float depth = PlatformBackZ - PlatformFrontZ, centreZ = (PlatformFrontZ + PlatformBackZ) * .5f;
            Texture texture = look.texture ? look.texture : platform.dropThrough ? FortressModel3D.PlankTexture : FortressModel3D.StoneTexture;
            Color body = look.overrideColors ? look.bodyColor : platform.dropThrough ? new Color(.48f, .34f, .22f) : new Color(.5f, .54f, .6f);
            Color cap = look.overrideColors ? look.capColor : platform.dropThrough ? new Color(.66f, .5f, .33f) : new Color(.74f, .72f, .66f);
            float tile = Mathf.Max(.1f, look.textureTile);

            var block = GeneratedBox(root, "Block",
                new Vector3(rect.center.x, rect.yMax - PlatformCapHeight - (thickness - PlatformCapHeight) * .5f, centreZ),
                new Vector3(rect.width, thickness - PlatformCapHeight, depth), tile);
            block.sharedMaterial = look.bodyMaterial ? look.bodyMaterial : FortressModel3D.Lit(body * map.tint, .15f, 0, texture);
            var top = GeneratedBox(root, "Cap", new Vector3(rect.center.x, rect.yMax - PlatformCapHeight * .5f, centreZ),
                new Vector3(rect.width + .18f, PlatformCapHeight, depth + .18f), tile * (platform.dropThrough ? 1 : .75f));
            top.sharedMaterial = look.capMaterial ? look.capMaterial : FortressModel3D.Lit(cap * map.tint, .25f, 0, texture);
            return root;
        }

        MeshRenderer GeneratedBox(Transform parent, string name, Vector3 position, Vector3 size, float tile)
        {
            var box = new GameObject(name).transform;
            box.SetParent(parent, false);
            box.position = position;
            var mesh = new Mesh { name = "Fortress platform " + name };
            ownedAssets.Add(mesh);
            FortressModel3D.BuildBox(mesh, size, position, tile);
            box.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = box.gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            return renderer;
        }

        void DestroyPlatformVisual(Transform visual)
        {
            foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh && ownedAssets.Remove(filter.sharedMesh)) Destroy(filter.sharedMesh);
            visual.gameObject.SetActive(false);
            Destroy(visual.gameObject);
        }

        // Stretch fits the model's rendered bounds to the platform: full width, the block's thickness, and the
        // platform depth band behind the fighters. PivotAtTopCentre just puts the model's origin on the top edge.
        void FitPlatformModel(Transform root, GameObject prefab, Rect rect, float thickness, FortressModelFit fit)
        {
            var instance = Instantiate(prefab, root).transform;
            if (fit == FortressModelFit.PivotAtTopCentre)
            {
                instance.position = new Vector3(rect.center.x, rect.yMax, PlatformFrontZ);
                return;
            }
            if (!FortressModel3D.TryGetBounds(instance, out var bounds)) return;
            float depth = PlatformBackZ - PlatformFrontZ;
            Vector3 scale = instance.localScale;
            instance.localScale = new Vector3(scale.x * rect.width / Mathf.Max(bounds.size.x, .01f),
                scale.y * thickness / Mathf.Max(bounds.size.y, .01f), scale.z * depth / Mathf.Max(bounds.size.z, .01f));
            FortressModel3D.TryGetBounds(instance, out bounds);
            instance.position += new Vector3(rect.center.x - bounds.center.x, rect.yMax - bounds.max.y, PlatformFrontZ - bounds.min.z);
        }
    }
}
