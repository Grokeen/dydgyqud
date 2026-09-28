using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    // Replaces a fighter prefab's sprite body and weapon with a low-poly 3D figure at runtime.
    // The prefab hierarchy (Motion / Body / AimPivot / WeaponMotion) and its Animator are unchanged: the figure is
    // parented under Body and the weapon, so the existing clips still lean, squash, recoil and topple it.
    // The hidden Body SpriteRenderer keeps receiving the clips' colour keys; they are copied to the meshes as a tint.
    [DisallowMultipleComponent]
    public sealed class FortressFighterModel : MonoBehaviour
    {
        // Turns the figure toward the camera so the side-on view reads as three-quarter.
        const float TurnTowardCamera = 28;

        struct Palette
        {
            public Color skin, tunic, trim, legs, boots, hair, metal, accent, eyes;
        }

        FortressFighterView view;
        Transform figure;
        readonly List<(Renderer renderer, Color color)> parts = new List<(Renderer, Color)>();
        MaterialPropertyBlock block;
        Color appliedTint = Color.white;

        public static FortressModelStyle Resolve(FortressCharacterDefinition definition)
        {
            if (definition.modelStyle != FortressModelStyle.Auto) return definition.modelStyle;
            string id = definition.name + " " + (definition.prefab ? definition.prefab.name : "");
            if (id.Contains("Goblin")) return FortressModelStyle.Goblin;
            if (id.Contains("Captain")) return FortressModelStyle.Captain;
            if (id.Contains("Spear") || definition.weapon == FortressWeapon.Spear) return FortressModelStyle.Spearman;
            return FortressModelStyle.Archer;
        }

        public static void Attach(FortressFighterView view, FortressCharacterDefinition definition)
        {
            var model = view.GetComponent<FortressFighterModel>();
            if (!model) model = view.gameObject.AddComponent<FortressFighterModel>();
            model.Build(view, Resolve(definition), definition.weapon);
        }

        void Build(FortressFighterView target, FortressModelStyle style, FortressWeapon weapon)
        {
            if (figure) return;
            view = target;
            view.body.enabled = false;
            foreach (var sprite in view.weapon.GetComponentsInChildren<SpriteRenderer>(true)) sprite.enabled = false;
            LayShadowFlat();

            Palette p = PaletteFor(style);
            figure = FortressModel3D.Group(view.body.transform, "Figure 3D", Vector3.zero, new Vector3(0, TurnTowardCamera, 0));
            BuildBody(p);
            switch (style)
            {
                case FortressModelStyle.Archer: BuildArcher(p); break;
                case FortressModelStyle.Spearman: BuildSpearman(p); break;
                case FortressModelStyle.Goblin: BuildGoblin(p); break;
                case FortressModelStyle.Captain: BuildCaptain(p); break;
            }
            BuildArms(p, weapon);
            if (weapon == FortressWeapon.Bow)
            {
                // Matches the prefab's Bow sprite position; the loaded arrow's tip is at the loaded projectile.
                var bowSprite = view.weapon.Find("Bow");
                Collect(FortressModel3D.Bow(view.weapon, bowSprite ? bowSprite.localPosition : new Vector3(1.1f, 0, 0)));
            }
            Collect(FortressModel3D.Projectile(view.loadedProjectile, weapon));
            Collect(figure);
        }

        void Collect(Transform root)
        {
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                if (!parts.Exists(p => p.renderer == renderer)) parts.Add((renderer, renderer.sharedMaterial.color));
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
            foreach (var (renderer, color) in parts)
            {
                if (!renderer) continue;
                Color tinted = new Color(color.r * tint.r, color.g * tint.g, color.b * tint.b, color.a);
                block.Clear(); block.SetColor("_BaseColor", tinted); block.SetColor("_Color", tinted);
                renderer.SetPropertyBlock(block);
            }
            // Opaque meshes cannot fade, so the death clip's alpha shrinks the figure into the ground instead.
            figure.localScale = Vector3.one * Mathf.Lerp(.15f, 1, tint.a);
        }

        static Palette PaletteFor(FortressModelStyle style)
        {
            switch (style)
            {
                case FortressModelStyle.Spearman:
                    return new Palette { skin = new Color(.93f, .74f, .6f), tunic = new Color(.92f, .45f, .12f), trim = new Color(.45f, .28f, .14f),
                        legs = new Color(.3f, .25f, .22f), boots = new Color(.22f, .15f, .1f), hair = new Color(.3f, .2f, .12f),
                        metal = new Color(.7f, .73f, .78f), accent = new Color(.95f, .6f, .15f), eyes = new Color(.08f, .08f, .1f) };
                case FortressModelStyle.Goblin:
                    return new Palette { skin = new Color(.42f, .6f, .28f), tunic = new Color(.3f, .27f, .36f), trim = new Color(.3f, .2f, .12f),
                        legs = new Color(.25f, .22f, .2f), boots = new Color(.16f, .12f, .09f), hair = new Color(.18f, .15f, .12f),
                        metal = new Color(.45f, .45f, .48f), accent = new Color(.5f, .18f, .15f), eyes = new Color(.95f, .2f, .1f) };
                case FortressModelStyle.Captain:
                    return new Palette { skin = new Color(.8f, .62f, .52f), tunic = new Color(.45f, .08f, .1f), trim = new Color(.85f, .66f, .25f),
                        legs = new Color(.2f, .2f, .24f), boots = new Color(.12f, .1f, .1f), hair = new Color(.1f, .08f, .08f),
                        metal = new Color(.32f, .34f, .4f), accent = new Color(.7f, .1f, .12f), eyes = new Color(.95f, .75f, .2f) };
                default:
                    return new Palette { skin = new Color(.94f, .76f, .62f), tunic = new Color(.5f, .34f, .2f), trim = new Color(.3f, .2f, .12f),
                        legs = new Color(.32f, .3f, .24f), boots = new Color(.25f, .16f, .1f), hair = new Color(.55f, .32f, .15f),
                        metal = new Color(.7f, .72f, .75f), accent = new Color(.2f, .45f, .28f), eyes = new Color(.08f, .12f, .1f) };
            }
        }

        // Shared humanoid: faces +X, feet at the origin, shoulders at the prefab's 2.5 m aim pivot, head top near 3.5 m.
        void BuildBody(Palette p)
        {
            const PrimitiveType Cube = PrimitiveType.Cube, Sphere = PrimitiveType.Sphere, Cylinder = PrimitiveType.Cylinder, Capsule = PrimitiveType.Capsule;
            foreach (float side in new[] { -1f, 1f })
            {
                FortressModel3D.Part(figure, "Boot", Cube, new Vector3(.07f, .15f, .2f * side), new Vector3(.46f, .3f, .27f), p.boots);
                FortressModel3D.Part(figure, "Leg", Capsule, new Vector3(0, .78f, .2f * side), new Vector3(.3f, .5f, .3f), p.legs);
                FortressModel3D.Part(figure, "Shoulder", Sphere, new Vector3(0, 2.48f, .38f * side), Vector3.one * .38f, p.tunic);
                FortressModel3D.Part(figure, "Eye", Sphere, new Vector3(.27f, 3.22f, .11f * side), Vector3.one * .075f, p.eyes, default, .8f);
            }
            FortressModel3D.Part(figure, "Skirt", Cylinder, new Vector3(0, 1.36f, 0), new Vector3(.86f, .3f, .76f), p.tunic);
            FortressModel3D.Part(figure, "Torso", Capsule, new Vector3(0, 2.05f, 0), new Vector3(.8f, .62f, .68f), p.tunic);
            FortressModel3D.Part(figure, "Belt", Cylinder, new Vector3(0, 1.64f, 0), new Vector3(.84f, .06f, .72f), p.trim);
            FortressModel3D.Part(figure, "Buckle", Cube, new Vector3(.41f, 1.64f, 0), new Vector3(.06f, .14f, .16f), new Color(.85f, .7f, .3f), default, .7f, .8f);
            FortressModel3D.Part(figure, "Neck", Cylinder, new Vector3(0, 2.76f, 0), new Vector3(.26f, .1f, .26f), p.skin);
            FortressModel3D.Part(figure, "Head", Sphere, new Vector3(.02f, 3.18f, 0), Vector3.one * .62f, p.skin, default, .3f);
            FortressModel3D.Part(figure, "Nose", Cube, new Vector3(.32f, 3.14f, 0), new Vector3(.09f, .11f, .08f), p.skin * .92f);
        }

        void BuildArcher(Palette p)
        {
            Color hood = p.accent, cloak = p.accent * .8f;
            FortressModel3D.Part(figure, "Hood", PrimitiveType.Sphere, new Vector3(-.12f, 3.28f, 0), new Vector3(.68f, .72f, .74f), hood);
            FortressModel3D.Part(figure, "Hood tip", PrimitiveType.Cube, new Vector3(-.42f, 3.3f, 0), new Vector3(.26f, .26f, .26f), hood, new Vector3(0, 0, 40));
            FortressModel3D.Part(figure, "Fringe", PrimitiveType.Sphere, new Vector3(.12f, 3.4f, 0), new Vector3(.42f, .16f, .5f), p.hair);
            FortressModel3D.Part(figure, "Cloak", PrimitiveType.Cube, new Vector3(-.38f, 1.95f, 0), new Vector3(.12f, 1.5f, .82f), cloak, new Vector3(0, 0, -7));
            FortressModel3D.Part(figure, "Collar", PrimitiveType.Cylinder, new Vector3(0, 2.68f, 0), new Vector3(.62f, .08f, .62f), hood);
            FortressModel3D.Part(figure, "Quiver", PrimitiveType.Cylinder, new Vector3(-.46f, 2.35f, .2f), new Vector3(.24f, .42f, .24f), p.trim, new Vector3(0, 0, -24));
            for (int i = 0; i < 3; i++)
                FortressModel3D.Part(figure, "Quiver feather", PrimitiveType.Cube, new Vector3(-.26f + i * .05f, 2.8f + i * .03f, .14f + i * .06f),
                    new Vector3(.06f, .22f, .06f), new Color(.85f, .22f, .18f), new Vector3(0, 0, -24));
        }

        void BuildSpearman(Palette p)
        {
            FortressModel3D.Part(figure, "Helmet", PrimitiveType.Sphere, new Vector3(0, 3.3f, 0), new Vector3(.7f, .56f, .7f), p.metal, default, .7f, .8f);
            FortressModel3D.Part(figure, "Helmet brim", PrimitiveType.Cylinder, new Vector3(0, 3.18f, 0), new Vector3(.8f, .03f, .8f), p.metal, default, .7f, .8f);
            FortressModel3D.Part(figure, "Nasal guard", PrimitiveType.Cube, new Vector3(.34f, 3.2f, 0), new Vector3(.05f, .3f, .08f), p.metal, default, .7f, .8f);
            FortressModel3D.Part(figure, "Plume", PrimitiveType.Capsule, new Vector3(-.12f, 3.68f, 0), new Vector3(.14f, .26f, .14f), p.accent, new Vector3(0, 0, 35));
            FortressModel3D.Part(figure, "Breastplate", PrimitiveType.Capsule, new Vector3(.08f, 2.12f, 0), new Vector3(.72f, .5f, .66f), p.metal, default, .65f, .8f);
            FortressModel3D.Part(figure, "Shield", PrimitiveType.Cylinder, new Vector3(-.48f, 2.05f, 0), new Vector3(1f, .06f, 1f), new Color(.5f, .3f, .15f), new Vector3(0, 0, 90));
            FortressModel3D.Part(figure, "Shield rim", PrimitiveType.Cylinder, new Vector3(-.5f, 2.05f, 0), new Vector3(1.06f, .03f, 1.06f), p.metal, new Vector3(0, 0, 90), .6f, .8f);
            FortressModel3D.Part(figure, "Shield boss", PrimitiveType.Sphere, new Vector3(-.55f, 2.05f, 0), new Vector3(.12f, .26f, .26f), p.metal, default, .7f, .8f);
        }

        void BuildGoblin(Palette p)
        {
            foreach (float side in new[] { -1f, 1f })
                FortressModel3D.Part(figure, "Ear", PrimitiveType.Cube, new Vector3(-.02f, 3.26f, .4f * side), new Vector3(.08f, .12f, .42f), p.skin, new Vector3(22 * side, 0, 12));
            FortressModel3D.Part(figure, "Big nose", PrimitiveType.Sphere, new Vector3(.34f, 3.12f, 0), new Vector3(.2f, .14f, .14f), p.skin * .9f);
            FortressModel3D.Part(figure, "Leather cap", PrimitiveType.Sphere, new Vector3(-.02f, 3.34f, 0), new Vector3(.64f, .4f, .64f), p.trim);
            FortressModel3D.Part(figure, "Pauldron", PrimitiveType.Sphere, new Vector3(0, 2.55f, -.4f), new Vector3(.46f, .3f, .46f), p.metal, default, .5f, .6f);
            FortressModel3D.Part(figure, "Sash", PrimitiveType.Cube, new Vector3(0, 2.05f, 0), new Vector3(.82f, .14f, .7f), p.accent, new Vector3(0, 0, 32));
            FortressModel3D.Part(figure, "Quiver", PrimitiveType.Cylinder, new Vector3(-.44f, 2.3f, .18f), new Vector3(.22f, .38f, .22f), p.trim, new Vector3(0, 0, -20));
        }

        void BuildCaptain(Palette p)
        {
            FortressModel3D.Part(figure, "Helmet", PrimitiveType.Sphere, new Vector3(0, 3.3f, 0), new Vector3(.72f, .6f, .72f), p.metal, default, .7f, .85f);
            FortressModel3D.Part(figure, "Visor", PrimitiveType.Cube, new Vector3(.3f, 3.2f, 0), new Vector3(.1f, .12f, .5f), p.metal * .7f, default, .7f, .85f);
            FortressModel3D.Part(figure, "Crest", PrimitiveType.Cube, new Vector3(-.05f, 3.62f, 0), new Vector3(.62f, .2f, .06f), p.trim, default, .6f, .8f);
            foreach (float side in new[] { -1f, 1f })
            {
                FortressModel3D.Limb(figure, "Horn", new Vector3(0, 3.42f, .3f * side), new Vector3(-.1f, 3.9f, .52f * side), .1f, new Color(.9f, .85f, .7f));
                FortressModel3D.Part(figure, "Pauldron", PrimitiveType.Sphere, new Vector3(0, 2.55f, .42f * side), new Vector3(.5f, .32f, .5f), p.metal, default, .65f, .85f);
            }
            FortressModel3D.Part(figure, "Breastplate", PrimitiveType.Capsule, new Vector3(.08f, 2.12f, 0), new Vector3(.74f, .52f, .68f), p.metal, default, .65f, .85f);
            FortressModel3D.Part(figure, "Gold trim", PrimitiveType.Cylinder, new Vector3(0, 2.62f, 0), new Vector3(.7f, .04f, .7f), p.trim, default, .7f, .9f);
            FortressModel3D.Part(figure, "Cape", PrimitiveType.Cube, new Vector3(-.4f, 1.6f, 0), new Vector3(.1f, 2f, .9f), p.accent, new Vector3(0, 0, -8));
        }

        // Arms live under WeaponMotion so they follow the aim angle and the attack/recoil clips with the weapon.
        void BuildArms(Palette p, FortressWeapon weapon)
        {
            var arms = FortressModel3D.Group(view.weaponMotion, "Arms 3D");
            Color sleeve = p.tunic * .9f;
            Vector3 front = new Vector3(0, 0, -.3f), back = new Vector3(0, 0, .3f);
            Vector3 frontHand, backHand;
            if (weapon == FortressWeapon.Spear) { frontHand = new Vector3(.62f, -.08f, -.12f); backHand = new Vector3(-.32f, -.12f, .1f); }
            else { frontHand = new Vector3(1.38f, 0, -.04f); backHand = new Vector3(-.12f, .02f, .06f); }
            FortressModel3D.Limb(arms, "Front arm", front, frontHand, .2f, sleeve);
            FortressModel3D.Limb(arms, "Back arm", back, backHand, .2f, sleeve);
            FortressModel3D.Part(arms, "Front hand", PrimitiveType.Sphere, frontHand, Vector3.one * .19f, p.skin);
            FortressModel3D.Part(arms, "Back hand", PrimitiveType.Sphere, backHand, Vector3.one * .19f, p.skin);
            Collect(arms);
        }
    }
}
