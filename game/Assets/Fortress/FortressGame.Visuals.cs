using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        Transform Shape(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var obj = new GameObject(name); obj.transform.SetParent(transform);
            obj.transform.position = position; obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var sprite = obj.AddComponent<SpriteRenderer>(); sprite.sprite = square; sprite.color = color; sprite.sortingOrder = order;
            return obj.transform;
        }
        Transform Part(Transform parent, string name, Vector2 point, Vector2 size, Color color, int order)
        {
            Transform part = Shape(name, Vector2.zero, size, color, order); part.SetParent(parent, false); part.localPosition = point; return part;
        }
        void Line(Transform parent, string name, Vector2 a, Vector2 b, float width, Color color, int order)
        {
            Vector2 d = b - a;
            Transform line = Part(parent, name, (a + b) / 2, new Vector2(d.magnitude, width), color, order);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
        void BuildWorld()
        {
            var rng = new System.Random(42);
            Color distant = new Color(0.09f, 0.15f, 0.23f);
            for (int i = 0; i < 65; i++) Shape("Stars", new Vector2((float)rng.NextDouble() * 110 - 5, 28 + (float)rng.NextDouble() * 22), Vector2.one * 0.12f, new Color(0.38f, 0.48f, 0.61f), -30);
            for (int y = -8; y <= 8; y++)
            {
                int width = Mathf.FloorToInt(Mathf.Sqrt(64 - y * y));
                Shape("Pixel moon", new Vector2(76, 36 + y * 0.36f), new Vector2(width * 0.72f, 0.37f), new Color(0.57f, 0.66f, 0.75f), -25);
            }
            for (int i = 0; i < 17; i++)
            {
                float x = i * 7 - 5, h = rng.Next(9, 25);
                Shape("Distant keep", new Vector2(x, 14 + h / 2), new Vector2(4, h), distant, -20);
                for (int j = 0; j < 3; j++) Shape("Battlement", new Vector2(x - 1.4f + j * 1.4f, 14 + h), new Vector2(0.7f, 1.1f), distant, -20);
                Shape("Spire", new Vector2(x, 15 + h), new Vector2(0.55f, 3), distant, -20);
                for (int y = 17; y < 14 + h - 1; y += 3) Shape("Distant window", new Vector2(x, y), new Vector2(0.3f, 0.7f), new Color(0.48f, 0.4f, 0.27f), -19);
            }
            for (int i = 0; i < 7; i++)
            {
                float x = 27 + i * 7;
                Shape("Waterfall", new Vector2(x, 4), new Vector2(2.4f, 37), new Color(0.16f, 0.28f, 0.38f), -15);
                for (int j = 0; j < 3; j++) Shape("Waterfall streak", new Vector2(x - 0.8f + j * 0.7f, rng.Next(-2, 8)), new Vector2(0.16f, rng.Next(15, 32)), new Color(0.24f, 0.39f, 0.49f), -14);
            }
            for (int p = 0; p < terrain.Length; p++)
            {
                Platform land = terrain[p]; bool bridge = p == 3;
                Shape(bridge ? "Bridge deck" : "Cliff", new Vector2((land.left + land.right) / 2, (land.top + land.bottom) / 2), new Vector2(land.right - land.left, land.top - land.bottom), bridge ? new Color(0.26f, 0.2f, 0.15f) : new Color(0.12f, 0.16f, 0.19f), 0);
                Shape("Moss edge", new Vector2((land.left + land.right) / 2, land.top - 0.15f), new Vector2(land.right - land.left, 0.3f), new Color(0.42f, 0.42f, 0.25f), 2);
                if (!bridge)
                    for (float x = land.left; x < land.right; x += 1.6f)
                        for (float y = land.bottom + 1; y < land.top - 0.6f; y += 2.1f)
                            Shape("Rock pixel", new Vector2(x + (float)rng.NextDouble(), y), new Vector2(0.4f + (float)rng.NextDouble(), 0.8f), new Color(0.16f, 0.2f, 0.23f), 1);
                for (float x = land.left + 0.5f; x < land.right; x += 2)
                {
                    Shape("Grass pixel", new Vector2(x, land.top + 0.1f), new Vector2(0.6f, 0.2f), new Color(0.51f, 0.48f, 0.28f), 3);
                    if (bridge)
                    {
                        Shape("Bridge post", new Vector2(x, land.top + 0.8f), new Vector2(0.2f, 1.6f), new Color(0.3f, 0.25f, 0.2f), 3);
                        Shape("Bridge rail", new Vector2(x, land.top + 1.2f), new Vector2(2, 0.14f), new Color(0.3f, 0.25f, 0.2f), 3);
                    }
                }
            }
            for (int i = 0; i < 5; i++) Shape("Bridge support", new Vector2(25 + i * 8, 6), new Vector2(0.5f, 12.6f), new Color(0.2f, 0.18f, 0.16f), -1);
            Torch(3, 13); Torch(20, 13); Torch(39, 6); Torch(65, 22); Torch(79, 22); Torch(99, 13); Torch(98, 5);
            Banner(17, 16); Banner(67, 27); Banner(98, 19);
        }
        void Torch(float x, float y)
        {
            Shape("Torch post", new Vector2(x, y + 1), new Vector2(0.22f, 2), new Color(0.3f, 0.21f, 0.14f), 4);
            Shape("Flame amber", new Vector2(x, y + 2.1f), new Vector2(0.55f, 0.8f), new Color(1, 0.42f, 0.12f), 5);
            Shape("Flame core", new Vector2(x, y + 2.3f), new Vector2(0.22f, 0.65f), new Color(1, 0.85f, 0.4f), 6);
        }
        void Banner(float x, float y)
        {
            Shape("Banner pole", new Vector2(x, y), new Vector2(0.2f, 8), new Color(0.28f, 0.24f, 0.21f), 4);
            Shape("Crimson banner", new Vector2(x + 1, y + 0.5f), new Vector2(2, 4), new Color(0.35f, 0.12f, 0.16f), 4);
            Shape("Banner emblem", new Vector2(x + 1, y + 0.7f), new Vector2(0.3f, 1.8f), gold, 5);
        }
        Sprite CharacterSprite(Color cloth, bool enemy, bool helmet = false)
        {
            string[] pixels = {
                "......HHHH......", ".....HHHHHH.....", "....HHHHHHHH....", "...HHHHHHHHHH...",
                "...HHSSSSSHHH...", "...HSSSSKSSHH...", "....SSSSSSSH....", ".....SSSSSS.....",
                "......SSSS......", "....CCCCCCCC....", "...CCCCCCCCCC...", "..LCCCCCCCCCC...",
                "..LCCCCCCCCCC...", "..LCCCCCCCCCC...", "..LCCCCCCCCCC...", "...CCCCCCCCCC...",
                "....LLLLLLLL....", "....CCCCCCCC....", "....CCCCCCCC....", ".....DDDDDD.....",
                ".....DD.DDD.....", "....DDD.DDD.....", "....DDD..DDD....", "....DDD..DDD....",
                "...LLLL..LLLL...", "...LLLL..LLLL..."
            };
            var texture = new Texture2D(16, pixels.Length, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var colors = new Color[16 * pixels.Length];
            for (int y = 0; y < pixels.Length; y++)
                for (int x = 0; x < 16; x++)
                {
                    char c = pixels[y][x]; Color color = Color.clear;
                    if (c == 'H') color = helmet ? new Color(0.66f, 0.72f, 0.77f) : cloth * 0.7f;
                    if (c == 'C') color = cloth;
                    if (c == 'S') color = enemy ? new Color(0.59f, 0.64f, 0.37f) : new Color(0.95f, 0.72f, 0.49f);
                    if (c == 'K') color = Color.black;
                    if (c == 'L') color = new Color(0.27f, 0.18f, 0.12f);
                    if (c == 'D') color = new Color(0.19f, 0.24f, 0.29f);
                    if (c != '.') color.a = 1;
                    colors[(pixels.Length - y - 1) * 16 + x] = color;
                }
            texture.SetPixels(colors); texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 16, pixels.Length), new Vector2(0.5f, 0), 8);
            ownedAssets.Add(sprite); ownedAssets.Add(texture); return sprite;
        }
        void AddFighter(string name, Vector2 feet, int hp, Color cloth, bool large)
        {
            var f = new Fighter { name = name, feet = feet, maxHp = hp, hp = hp };
            f.root = new GameObject(name).transform; f.root.SetParent(transform); f.root.position = feet;
            f.body = f.root.gameObject.AddComponent<SpriteRenderer>();
            f.body.sprite = CharacterSprite(cloth, fighters.Count > 0); f.body.sortingOrder = 12;
            if (large) f.root.localScale = Vector3.one * 1.25f;
            f.weapon = new GameObject("Bow and arms").transform; f.weapon.SetParent(f.root, false);
            Color skin = new Color(0.9f, 0.69f, 0.43f);
            Line(f.weapon, "Arm", Vector2.zero, new Vector2(1, 0), 0.2f, skin, 13);
            Line(f.weapon, "Drawing arm", new Vector2(-0.2f, -0.3f), new Vector2(0.25f, 0), 0.19f, skin, 13);
            Vector2 previous = new Vector2(0.9f, -0.85f);
            for (int i = 1; i <= 8; i++)
            {
                float t = i / 8f;
                Vector2 next = new Vector2(0.9f + 0.4f * Mathf.Sin(t * Mathf.PI), Mathf.Lerp(-0.85f, 0.85f, t));
                Line(f.weapon, "Bow", previous, next, 0.12f, gold, 15); previous = next;
            }
            Line(f.weapon, "String", new Vector2(0.9f, -0.85f), new Vector2(0.25f, 0), 0.045f, pale, 14);
            Line(f.weapon, "String", new Vector2(0.25f, 0), new Vector2(0.9f, 0.85f), 0.045f, pale, 14);
            f.loadedArrow = BuildArrow(f.weapon, "Ready arrow", 16); f.loadedArrow.localPosition = Vector3.right * 1.6f;
            fighters.Add(f);
        }
        void InitializeClasses()
        {
            Fighter player = fighters[0];
            classSprites[0] = player.body.sprite;
            classSprites[1] = CharacterSprite(new Color(0.77f, 0.4f, 0.22f), false, true);
            for (int i = 0; i < 2; i++) classPortraits[i] = BuildClassPortrait(classSprites[i], i == 1);
            bowWeapon = player.weapon; bowLoaded = player.loadedArrow;
            spearWeapon = new GameObject("Spear throwing arm").transform;
            spearWeapon.SetParent(player.root, false);
            Line(spearWeapon, "Throwing arm", new Vector2(-0.1f, -0.25f), new Vector2(0.65f, 0), 0.23f, new Color(0.95f, 0.72f, 0.49f), 13);
            spearLoaded = BuildSpear(spearWeapon, "Held spear", 16);
            spearLoaded.localPosition = Vector3.right * 1.6f;
            spearWeapon.gameObject.SetActive(false);
            spearProjectile = BuildSpear(transform, "Thrown spear", 30);
            spearProjectile.gameObject.SetActive(false);
        }
        Texture2D BuildClassPortrait(Sprite body, bool spear)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[32 * 32];
            Color[] source = body.texture.GetPixels();
            for (int y = 0; y < 26; y++)
                for (int x = 0; x < 16; x++) pixels[(y + 2) * 32 + x + 4] = source[y * 16 + x];
            if (spear)
            {
                for (int y = 1; y < 27; y++) pixels[y * 32 + 25] = gold;
                for (int y = 25; y < 31; y++)
                    for (int x = 25 - (30 - y) / 2; x <= 25 + (30 - y) / 2; x++) pixels[y * 32 + x] = pale;
            }
            else
            {
                for (int y = 7; y <= 25; y++)
                {
                    int x = 23 + Mathf.RoundToInt(4 * Mathf.Sin((y - 7) / 18f * Mathf.PI));
                    pixels[y * 32 + x] = gold;
                    pixels[y * 32 + 23] = pale;
                }
                for (int x = 17; x < 31; x++) pixels[16 * 32 + x] = gold;
                pixels[17 * 32 + 29] = pixels[15 * 32 + 29] = pale;
            }
            texture.SetPixels(pixels); texture.Apply(); ownedAssets.Add(texture); return texture;
        }
        Transform BuildSpear(Transform parent, string name, int order)
        {
            Transform root = new GameObject(name).transform; root.SetParent(parent, false);
            Line(root, "Long wooden shaft", new Vector2(-2.5f, 0), new Vector2(-0.35f, 0), 0.13f, gold, order);
            Line(root, "Steel spearhead", new Vector2(-0.5f, 0), Vector2.zero, 0.19f, pale, order);
            Line(root, "Blade upper", new Vector2(-0.45f, 0.23f), Vector2.zero, 0.12f, pale, order);
            Line(root, "Blade lower", new Vector2(-0.45f, -0.23f), Vector2.zero, 0.12f, pale, order);
            Part(root, "Red binding", new Vector2(-0.65f, 0), new Vector2(0.28f, 0.19f), new Color(0.8f, 0.22f, 0.17f), order + 1);
            return root;
        }
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
        Transform BuildArrow(Transform parent, string name, int order)
        {
            Transform root = new GameObject(name).transform; root.SetParent(parent, false);
            Line(root, "Shaft", new Vector2(-1.35f, 0), Vector2.zero, 0.09f, gold, order);
            Line(root, "Tip", new Vector2(-0.25f, 0.16f), Vector2.zero, 0.12f, pale, order);
            Line(root, "Tip", new Vector2(-0.25f, -0.16f), Vector2.zero, 0.12f, pale, order);
            Line(root, "Feather", new Vector2(-1.35f, 0.2f), new Vector2(-1.05f, 0), 0.13f, blue, order);
            Line(root, "Feather", new Vector2(-1.35f, -0.2f), new Vector2(-1.05f, 0), 0.13f, blue, order);
            return root;
        }
    }
}
