using UnityEngine;
using UnityEngine.Rendering;

namespace MiniFortress
{
    // 2.5D presentation: fighters, weapons, projectiles and platforms are lit 3D meshes, while every gameplay rule
    // (movement, collision rectangles, ballistics) stays on the z = 0 plane. The painted map backdrop is unchanged
    // and pushed far behind the meshes.
    public sealed partial class FortressGame
    {
        // Platforms span this depth range around the plane fighters walk on (z = 0).
        const float PlatformFrontZ = -1.8f, PlatformBackZ = 2.6f, PlatformCapHeight = .32f;
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

        // Replaces a projectile sprite holder's look with the 3D arrow or spear for the given weapon.
        static void ShowProjectileModel(Transform holder, FortressWeapon weapon)
        {
            var sprite = holder.GetComponent<SpriteRenderer>();
            if (sprite) sprite.enabled = false;
            string wanted = weapon == FortressWeapon.Spear ? "Spear 3D" : "Arrow 3D";
            bool found = false;
            foreach (Transform child in holder)
            {
                bool match = child.name == wanted;
                child.gameObject.SetActive(match); found |= match;
            }
            if (!found) FortressModel3D.Projectile(holder, weapon);
        }

        Transform ProjectileObject(string name, FortressWeapon weapon, Vector2 position)
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(transform, false);
            holder.localPosition = position;
            ShowProjectileModel(holder, weapon);
            return holder;
        }

        // A platform is a stone (or plank, for drop-through bridges) block with a slightly wider cap on top.
        // The cap's top face is exactly the collision rectangle's top, where fighters' feet rest.
        Transform PlatformBlock(string name)
        {
            var block = new GameObject(name).transform;
            block.SetParent(transform, false);
            var mesh = new Mesh { name = name };
            ownedAssets.Add(mesh);
            block.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = block.gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
            return block;
        }

        void ShapePlatform(int index, Rect rect, bool dropThrough, Color tint)
        {
            float depth = PlatformBackZ - PlatformFrontZ, centreZ = (PlatformFrontZ + PlatformBackZ) * .5f;
            float thickness = Mathf.Clamp(rect.height, .9f, 3.2f);
            Texture texture = dropThrough ? FortressModel3D.PlankTexture : FortressModel3D.StoneTexture;
            Color body = dropThrough ? new Color(.48f, .34f, .22f) : new Color(.5f, .54f, .6f);
            Color cap = dropThrough ? new Color(.66f, .5f, .33f) : new Color(.74f, .72f, .66f);

            var block = mapPlatformVisuals[index];
            block.position = new Vector3(rect.center.x, rect.yMax - PlatformCapHeight - (thickness - PlatformCapHeight) * .5f, centreZ);
            FortressModel3D.BuildBox(block.GetComponent<MeshFilter>().sharedMesh,
                new Vector3(rect.width, thickness - PlatformCapHeight, depth), block.position, 2);
            block.GetComponent<MeshRenderer>().sharedMaterial = FortressModel3D.Lit(body * tint, .15f, 0, texture);

            var top = mapPlatformTopVisuals[index];
            top.position = new Vector3(rect.center.x, rect.yMax - PlatformCapHeight * .5f, centreZ);
            FortressModel3D.BuildBox(top.GetComponent<MeshFilter>().sharedMesh,
                new Vector3(rect.width + .18f, PlatformCapHeight, depth + .18f), top.position, dropThrough ? 2 : 1.5f);
            top.GetComponent<MeshRenderer>().sharedMaterial = FortressModel3D.Lit(cap * tint, .25f, 0, texture);
        }

        // Spawn markers are flat sprites; lay them on the platform's top face.
        static void LayFlat(Transform marker, Vector2 depthScale)
        {
            marker.rotation = Quaternion.Euler(90, 0, 0);
            marker.localScale = new Vector3(depthScale.x, depthScale.y, 1);
        }
    }
}
