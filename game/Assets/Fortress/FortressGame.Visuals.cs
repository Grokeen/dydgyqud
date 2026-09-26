using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        Texture2D arenaArt;
        readonly Texture2D[] illustratedCharacters = new Texture2D[4];
        Sprite bowArt, arrowArt, spearArt, softCircle;
        readonly Sprite[] illustratedBodies = new Sprite[4];

        Transform Shape(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var obj = new GameObject(name); obj.transform.SetParent(transform);
            obj.transform.position = position; obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = square; renderer.color = color; renderer.sortingOrder = order;
            return obj.transform;
        }
        Transform ArtObject(Transform parent, string name, Sprite sprite, Vector2 localPosition, int order)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.transform.localPosition = localPosition;
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
            return obj.transform;
        }
        Sprite MakeSprite(Texture2D texture, float units, Vector2 pivot, bool useWidth = false)
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, (useWidth ? texture.width : texture.height) / units);
            ownedAssets.Add(sprite); return sprite;
        }
        Texture2D SliceArt(Texture2D atlas, Rect normalized)
        {
            int x = Mathf.RoundToInt(normalized.x * atlas.width), y = Mathf.RoundToInt(normalized.y * atlas.height);
            int width = Mathf.Min(Mathf.RoundToInt(normalized.width * atlas.width), atlas.width - x);
            int height = Mathf.Min(Mathf.RoundToInt(normalized.height * atlas.height), atlas.height - y);
            Color[] source = atlas.GetPixels(x, y, width, height);
            int minX = width, minY = height, maxX = 0, maxY = 0;
            for (int py = 0; py < height; py++)
                for (int px = 0; px < width; px++)
                    if (source[py * width + px].a > 0.08f)
                    { minX = Mathf.Min(minX, px); maxX = Mathf.Max(maxX, px); minY = Mathf.Min(minY, py); maxY = Mathf.Max(maxY, py); }
            if (minX > maxX) throw new System.InvalidOperationException("Empty artwork atlas region.");
            int cropWidth = maxX - minX + 1, cropHeight = maxY - minY + 1;
            var pixels = new Color[cropWidth * cropHeight];
            for (int row = 0; row < cropHeight; row++)
                System.Array.Copy(source, (minY + row) * width + minX, pixels, row * cropWidth, cropWidth);
            var texture = new Texture2D(cropWidth, cropHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels); texture.Apply(); ownedAssets.Add(texture); return texture;
        }
        void LoadIllustrations()
        {
            arenaArt = Resources.Load<Texture2D>("FortressArt/CitadelArena");
            var people = Resources.Load<Texture2D>("FortressArt/Characters");
            var weapons = Resources.Load<Texture2D>("FortressArt/Weapons");
            if (arenaArt == null || people == null || weapons == null)
                throw new System.InvalidOperationException("FortressArt resources are missing. Reimport Assets/Resources/FortressArt.");
            // Regions are authored for the generated atlas; crops preserve its original alpha channel.
            illustratedCharacters[0] = SliceArt(people, new Rect(0, 0.50f, 0.5f, 0.5f));
            illustratedCharacters[1] = SliceArt(people, new Rect(0.5f, 0.50f, 0.5f, 0.5f));
            illustratedCharacters[2] = SliceArt(people, new Rect(0, 0, 0.5f, 0.50f));
            illustratedCharacters[3] = SliceArt(people, new Rect(0.5f, 0, 0.5f, 0.50f));
            for (int i = 0; i < 4; i++) illustratedBodies[i] = MakeSprite(illustratedCharacters[i], ActorHeight, new Vector2(0.5f, 0));
            bowArt = MakeSprite(SliceArt(weapons, new Rect(0.16f, 0.40f, 0.23f, 0.6f)), 2.45f, Vector2.one * 0.5f);
            arrowArt = MakeSprite(SliceArt(weapons, new Rect(0.5f, 0.60f, 0.5f, 0.2f)), 1.9f, new Vector2(1, 0.5f), true);
            spearArt = MakeSprite(SliceArt(weapons, new Rect(0, 0.17f, 0.65f, 0.15f)), 3.5f, new Vector2(1, 0.5f), true);
            var glow = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[4096];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), Vector2.one * 31.5f) / 31.5f;
                    pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - d), 1.7f));
                }
            glow.SetPixels(pixels); glow.Apply(); ownedAssets.Add(glow);
            softCircle = MakeSprite(glow, 1, Vector2.one * 0.5f);
        }
        void BuildWorld()
        {
            LoadIllustrations();
            LoadFighterAnimations();
            Sprite background = MakeSprite(arenaArt, 60, Vector2.one * 0.5f);
            Transform painting = ArtObject(transform, "Painted citadel battlefield", background, new Vector2(50, 18), -20);
            painting.localScale = new Vector3((106.66667f / 60) / (arenaArt.width / (float)arenaArt.height), 1, 1);
        }
        void AddFighter(string name, Vector2 feet, int hp, Color cloth, bool large)
        {
            var f = new Fighter { name = name, feet = feet, maxHp = hp, hp = hp };
            f.root = new GameObject(name).transform; f.root.SetParent(transform); f.root.position = feet;
            f.motion = new GameObject("Motion").transform; f.motion.SetParent(f.root, false);
            f.body = ArtObject(f.motion, "Body", illustratedBodies[0], Vector2.zero, 12).GetComponent<SpriteRenderer>();
            f.body.sprite = illustratedBodies[fighters.Count == 0 ? 0 : large ? 3 : 2]; f.body.sortingOrder = 12;
            if (large) f.root.localScale = Vector3.one * 1.25f;
            var shadow = ArtObject(f.root, "Ground shadow", softCircle, new Vector2(0, 0.05f), 10);
            shadow.localScale = new Vector3(2.9f, 0.4f, 1); shadow.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0.8f);
            f.aimPivot = new GameObject("AimPivot").transform; f.aimPivot.SetParent(f.motion, false);
            f.weaponMotion = new GameObject("WeaponMotion").transform; f.weaponMotion.SetParent(f.aimPivot, false);
            f.weapon = new GameObject("Aimed longbow").transform; f.weapon.SetParent(f.weaponMotion, false);
            ArtObject(f.weapon, "Painted bow", bowArt, new Vector2(1.1f, 0), 15);
            f.loadedArrow = BuildArrow(f.weapon, "Ready arrow", 16); f.loadedArrow.localPosition = Vector3.right * 1.6f;
            InitializeFighterAnimator(f);
            fighters.Add(f);
        }
        void InitializeClasses()
        {
            Fighter player = fighters[0];
            for (int i = 0; i < 2; i++) { classSprites[i] = illustratedBodies[i]; classPortraits[i] = illustratedCharacters[i]; }
            bowWeapon = player.weapon; bowLoaded = player.loadedArrow;
            spearWeapon = new GameObject("Aimed throwing spear").transform; spearWeapon.SetParent(player.weaponMotion, false);
            spearLoaded = BuildSpear(spearWeapon, "Held spear", 16); spearLoaded.localPosition = Vector3.right * 1.6f;
            spearWeapon.gameObject.SetActive(false);
            spearProjectile = BuildSpear(transform, "Thrown spear", 30); spearProjectile.gameObject.SetActive(false);
        }
        Transform BuildSpear(Transform parent, string name, int order) => ArtObject(parent, name, spearArt, Vector2.zero, order);
        Transform BuildArrow(Transform parent, string name, int order) => ArtObject(parent, name, arrowArt, Vector2.zero, order);
        void OpenSelection()
        {
            phase = Phase.Selecting; highlightedClass = playerClass; mouseMove = 0;
            arrowProjectile.gameObject.SetActive(false); spearProjectile.gameObject.SetActive(false); burst.gameObject.SetActive(false);
            foreach (Fighter fighter in fighters) fighter.root.gameObject.SetActive(false);
            foreach (Transform dot in guide) dot.gameObject.SetActive(false);
        }
        void BeginBattle()
        {
            if (phase != Phase.Selecting) return;
            playerClass = highlightedClass;
            Fighter player = fighters[0];
            player.name = IsSpearman ? "창병" : "궁수";
            player.maxHp = IsSpearman ? 140 : 120;
            player.body.sprite = classSprites[(int)playerClass];
            bowWeapon.gameObject.SetActive(!IsSpearman); spearWeapon.gameObject.SetActive(IsSpearman);
            player.weapon = IsSpearman ? spearWeapon : bowWeapon;
            player.loadedArrow = IsSpearman ? spearLoaded : bowLoaded;
            Restart();
        }
    }
}
