using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Gives a fighter its 3D look at runtime, from its FortressCharacterDefinition:
    //  - modelPrefab (any FBX/prefab; feet at the origin, facing +X), or a placeholder capsule coloured by modelStyle;
    //  - weaponModelPrefab held at the aim pivot, or the built-in bow (bows only; spears are the loaded projectile);
    //  - projectileModelPrefab for the loaded/flying/fallen shot, or the built-in arrow or spear.
    // The prefab hierarchy (Motion / Body / AimPivot / WeaponMotion) and its Animator are unchanged. With
    // useBuiltInBodyMotion the model sits under Body, so the stock clips lean, squash and topple it; otherwise it
    // sits under Motion and only follows facing. The hidden Body SpriteRenderer still receives the clips' colour
    // keys, which are copied to the meshes as a tint (hit flash, death fade). If the model has its own Animator,
    // every gameplay parameter it also declares (Speed, Grounded, BowAttack, Hit, Dead ...) is forwarded to it.
    [DisallowMultipleComponent]
    public sealed class FortressFighterModel : MonoBehaviour
    {
        FortressFighterView view;
        Transform figure;
        // Every tintable renderer with each material slot's own colour (skinned and multi-material models too).
        readonly List<(Renderer renderer, Color[] colors)> parts = new List<(Renderer, Color[])>();
        readonly HashSet<int> modelParameters = new HashSet<int>();
        Animator modelAnimator;
        MaterialPropertyBlock block;
        Color appliedTint = Color.white;
        Vector3 figureScale = Vector3.one;

        public static FortressModelStyle Resolve(FortressCharacterDefinition definition)
        {
            if (definition.modelStyle != FortressModelStyle.Auto) return definition.modelStyle;
            string id = definition.name + " " + (definition.prefab ? definition.prefab.name : "");
            if (id.Contains("Goblin")) return FortressModelStyle.Goblin;
            if (id.Contains("Captain")) return FortressModelStyle.Captain;
            if (id.Contains("Spear") || definition.weapon == FortressWeapon.Spear) return FortressModelStyle.Spearman;
            return FortressModelStyle.Archer;
        }

        public static FortressFighterModel Attach(FortressFighterView view, FortressCharacterDefinition definition)
        {
            var model = view.GetComponent<FortressFighterModel>();
            if (!model) model = view.gameObject.AddComponent<FortressFighterModel>();
            model.Build(view, definition);
            return model;
        }

        // Animator forwarding for models with their own Animator; parameters the model lacks are skipped.
        public void SetFloat(int id, float value) { if (Forwards(id)) modelAnimator.SetFloat(id, value); }
        public void SetBool(int id, bool value) { if (Forwards(id)) modelAnimator.SetBool(id, value); }
        public void SetTrigger(int id) { if (Forwards(id)) modelAnimator.SetTrigger(id); }
        public void ResetTrigger(int id) { if (Forwards(id)) modelAnimator.ResetTrigger(id); }
        public void ResetAnimation() { if (modelAnimator && modelAnimator.runtimeAnimatorController) { modelAnimator.Rebind(); modelAnimator.Update(0); } }
        bool Forwards(int id) => modelAnimator && modelParameters.Contains(id);

        void Build(FortressFighterView target, FortressCharacterDefinition definition)
        {
            if (figure) return;
            view = target;
            view.body.enabled = false;
            foreach (var sprite in view.weapon.GetComponentsInChildren<SpriteRenderer>(true)) sprite.enabled = false;
            LayShadowFlat();

            var parent = definition.useBuiltInBodyMotion ? view.body.transform : view.motion;
            figure = FortressModel3D.Group(parent, "Figure 3D", definition.modelOffset, definition.modelRotation);
            if (definition.modelPrefab) BuildCustom(definition);
            else
            {
                BuildPlaceholder(definition);
                figure.localScale = Vector3.one * definition.modelScale;
            }
            figureScale = figure.localScale;

            if (definition.weaponModelPrefab)
            {
                var weapon = Instantiate(definition.weaponModelPrefab, view.weapon).transform;
                weapon.name = "Weapon 3D"; weapon.localPosition = Vector3.zero;
                Collect(weapon);
            }
            else if (definition.weapon == FortressWeapon.Bow)
            {
                // Matches the prefab's Bow sprite position; the loaded arrow's tip is at the loaded projectile.
                var bowSprite = view.weapon.Find("Bow");
                Collect(FortressModel3D.Bow(view.weapon, bowSprite ? bowSprite.localPosition : new Vector3(1.1f, 0, 0)));
            }
            Collect(FortressModel3D.Projectile(view.loadedProjectile, definition));
            Collect(figure);
        }

        void BuildCustom(FortressCharacterDefinition definition)
        {
            var model = Instantiate(definition.modelPrefab, figure).transform;
            model.name = "Model"; model.localPosition = Vector3.zero;
            figure.localScale = Vector3.one * definition.modelScale;
            if (definition.fitModelToHeight && FortressModel3D.TryGetBounds(model, out var bounds) && bounds.size.y > .01f)
            {
                // Match the hitbox height and stand the model's lowest point on the feet.
                float worldHeight = definition.height * view.transform.lossyScale.y;
                figure.localScale *= worldHeight / bounds.size.y;
                FortressModel3D.TryGetBounds(model, out bounds);
                model.position += Vector3.up * (view.transform.position.y - bounds.min.y);
            }
            modelAnimator = model.GetComponentInChildren<Animator>();
            if (modelAnimator && modelAnimator.runtimeAnimatorController)
            {
                modelAnimator.applyRootMotion = false;
                foreach (var parameter in modelAnimator.parameters) modelParameters.Add(parameter.nameHash);
            }
        }

        void Collect(Transform root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || parts.Exists(p => p.renderer == renderer)) continue;
                var materials = renderer.sharedMaterials;
                var colors = new Color[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                    colors[i] = !materials[i] ? Color.white : materials[i].HasProperty("_BaseColor") ? materials[i].GetColor("_BaseColor")
                        : materials[i].HasProperty("_Color") ? materials[i].GetColor("_Color") : Color.white;
                parts.Add((renderer, colors));
            }
        }

        // The prefab's ground shadow is a sprite facing the camera; lay it on the platform's top face instead.
        void LayShadowFlat()
        {
            var shadow = view.transform.Find("Ground shadow");
            if (!shadow) return;
            shadow.localRotation = Quaternion.Euler(90, 0, 0);
            shadow.localPosition = new Vector3(0, .04f, 0);
            shadow.localScale = new Vector3(2.6f, 1.7f, 1);
            var renderer = shadow.GetComponent<SpriteRenderer>();
            if (renderer) renderer.color = new Color(0, 0, 0, .55f);
        }

        void LateUpdate()
        {
            if (!view || !figure) return;
            Color tint = view.body.color;
            if (tint == appliedTint) return;
            appliedTint = tint;
            block ??= new MaterialPropertyBlock();
            foreach (var (renderer, colors) in parts)
            {
                if (!renderer) continue;
                for (int i = 0; i < colors.Length; i++)
                {
                    Color color = colors[i];
                    Color tinted = new Color(color.r * tint.r, color.g * tint.g, color.b * tint.b, color.a);
                    block.Clear(); block.SetColor("_BaseColor", tinted); block.SetColor("_Color", tinted);
                    renderer.SetPropertyBlock(block, i);
                }
            }
            // Opaque meshes cannot fade, so the death clip's alpha shrinks the figure into the ground instead.
            figure.localScale = figureScale * Mathf.Lerp(.15f, 1, tint.a);
        }

        // Placeholder until real models arrive: one plain capsule filling the hitbox, told apart only by colour.
        static Color PlaceholderColor(FortressModelStyle style)
        {
            switch (style)
            {
                case FortressModelStyle.Spearman: return new Color(.95f, .55f, .18f);
                case FortressModelStyle.Goblin: return new Color(.55f, .35f, .75f);
                case FortressModelStyle.Captain: return new Color(.85f, .2f, .22f);
                default: return new Color(.3f, .78f, .4f);
            }
        }

        void BuildPlaceholder(FortressCharacterDefinition definition)
        {
            float width = definition.halfWidth * 2, height = definition.height;
            FortressModel3D.Part(figure, "Body", PrimitiveType.Capsule, new Vector3(0, height * .5f, 0),
                new Vector3(width, height * .5f, width), PlaceholderColor(Resolve(definition)), default, .4f);
        }
    }
}
