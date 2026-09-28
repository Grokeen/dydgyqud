using System;
using UnityEngine;

namespace MiniFortress
{
    // The four maps that shipped in code before maps became assets. They seed Assets/Resources/FortressMaps
    // (see the editor's FortressMapAssets) and are the runtime fallback when no map asset exists.
    // Edit the map assets, not this file; it only matters for fresh projects.
    public static class FortressBuiltInMaps
    {
        public struct Seed
        {
            public int order;
            public string name, description, backgroundResource, menuTagline, menuDescription;
            public Color tint;
            public Vector2 player;
            public FortressMapPlatform[] platforms;
            public FortressMapEnemy[] enemies;
        }

        static Seed Map(int order, string name, string description, string backgroundResource, Color tint, Vector2 player,
            FortressMapPlatform[] platforms, FortressMapEnemy[] enemies)
            => new Seed { order = order, name = name, description = description, backgroundResource = backgroundResource,
                tint = tint, player = player, platforms = platforms, enemies = enemies };

        // FortressMapPlatform(name, left, right, top, bottom, dropThrough). Spawns avoid standing under a higher
        // platform, which would bury the 3D figure's head in it (월하 요새's third enemy moved 73 → 79).
        public static Seed[] Seeds => new[]
        {
            Map(0, "월하 요새", "넓은 대교와 양쪽 성루", "FortressMapPreviews/MoonlitFortress", new Color(.9f, .94f, 1f), new Vector2(8, 24),
                new[]
                {
                    new FortressMapPlatform("Platform 1", 0, 17, 24, 19, false),
                    new FortressMapPlatform("Platform 2", 17, 34, 19, 17.8f, false),
                    new FortressMapPlatform("Platform 3", 34, 51, 19, 17.8f, false),
                    new FortressMapPlatform("Platform 4", 51, 68, 19, 17.8f, false),
                    new FortressMapPlatform("Platform 5", 68, 85, 19, 17.8f, false),
                    new FortressMapPlatform("Platform 6", 85, 102, 24, 19, false),
                    new FortressMapPlatform("Platform 7", 27, 38, 22.2f, 21.4f, false),
                    new FortressMapPlatform("Platform 8", 64, 75, 22.2f, 21.4f, false),
                    new FortressMapPlatform("Platform 9", 43, 59, 13, 11.8f, true),
                },
                new[]
                {
                    new FortressMapEnemy("서문 파수꾼", new Vector2(25, 19)),
                    new FortressMapEnemy("대교 경비병", new Vector2(46, 19)),
                    new FortressMapEnemy("성채 대장", new Vector2(79, 19)),
                    new FortressMapEnemy("동문 파수꾼", new Vector2(94, 24)),
                }).Menu("달빛 아래 대교, 양쪽 성루에서 화살이 쏟아진다",
                "넓은 대교를 사이에 두고 성루의 파수꾼들을 쓰러뜨려라. 대교 아래 발판으로 우회할 수 있다."),
            Map(1, "달빛 협곡", "절벽 사이를 오르는 계단길", "FortressMapPreviews/MoonlitRavine", new Color(.82f, .9f, 1f), new Vector2(8, 25),
                new[]
                {
                    new FortressMapPlatform("Platform 1", 0, 18, 25, 20, false),
                    new FortressMapPlatform("Platform 2", 18, 34, 21, 19.8f, true),
                    new FortressMapPlatform("Platform 3", 34, 51, 19, 17.8f, false),
                    new FortressMapPlatform("Platform 4", 53, 69, 22, 20.8f, true),
                    new FortressMapPlatform("Platform 5", 69, 85, 26, 24.8f, false),
                    new FortressMapPlatform("Platform 6", 85, 102, 30, 28.8f, false),
                    new FortressMapPlatform("Platform 7", 30, 42, 24, 23.2f, false),
                    new FortressMapPlatform("Platform 8", 55, 67, 27, 26.2f, false),
                    new FortressMapPlatform("Platform 9", 75, 87, 32, 31.2f, false),
                },
                new[]
                {
                    new FortressMapEnemy("서쪽 절벽 사수", new Vector2(27, 21)),
                    new FortressMapEnemy("협곡 다리 파수꾼", new Vector2(46, 19)),
                    new FortressMapEnemy("상단 경비병", new Vector2(64, 22)),
                    new FortressMapEnemy("봉우리 대장", new Vector2(94, 30)),
                }).Menu("절벽을 오를수록 적은 더 높은 곳에서 기다린다",
                "계단처럼 이어진 절벽길을 올라 봉우리의 대장을 노려라. 높이 차를 계산한 곡사가 승부를 가른다."),
            Map(2, "갈라진 성벽", "무너진 틈을 건너는 부유 발판", "FortressMapPreviews/BrokenRamparts", new Color(1f, .88f, .76f), new Vector2(8, 25),
                new[]
                {
                    new FortressMapPlatform("Platform 1", 0, 19, 25, 23.8f, false),
                    new FortressMapPlatform("Platform 2", 19, 34, 17, 15.8f, true),
                    new FortressMapPlatform("Platform 3", 35, 49, 21, 19.8f, false),
                    new FortressMapPlatform("Platform 4", 51, 66, 18, 16.8f, true),
                    new FortressMapPlatform("Platform 5", 68, 84, 22, 20.8f, false),
                    new FortressMapPlatform("Platform 6", 85, 102, 27, 25.8f, false),
                    new FortressMapPlatform("Platform 7", 24, 37, 12, 10.8f, true),
                    new FortressMapPlatform("Platform 8", 62, 74, 12, 10.8f, true),
                    new FortressMapPlatform("Platform 9", 43, 56, 27, 25.8f, false),
                },
                new[]
                {
                    new FortressMapEnemy("서쪽 성벽 경비병", new Vector2(27, 17)),
                    new FortressMapEnemy("무너진 탑 수비병", new Vector2(43, 21)),
                    new FortressMapEnemy("동쪽 균열 사수", new Vector2(74, 22)),
                    new FortressMapEnemy("폐허의 대장", new Vector2(94, 27)),
                }).Menu("무너진 틈 사이, 한 걸음이 곧 추락이다",
                "부유 발판과 큰 틈을 건너며 폐허의 수비병을 제압하라. 떨어지면 체력을 잃는다."),
            Map(3, "기존 성채 전장", "처음 전장의 성채·협곡·대교 배치", "FortressArt/CitadelArena", new Color(.9f, .94f, 1f), new Vector2(9, 24.7f),
                new[]
                {
                    new FortressMapPlatform("Platform 1", -3, 18.3f, 24.7f, -14, false),
                    new FortressMapPlatform("Platform 2", 18.3f, 32.7f, 6.9f, -14, false),
                    new FortressMapPlatform("Platform 3", 38.3f, 52f, 8.6f, -14, false),
                    new FortressMapPlatform("Platform 4", 18.3f, 63f, 18.4f, 17.7f, true),
                    new FortressMapPlatform("Platform 5", 63f, 81.5f, 28.5f, -14, false),
                    new FortressMapPlatform("Platform 6", 81.5f, 103f, 6.9f, -14, false),
                    new FortressMapPlatform("Platform 7", 92f, 103f, 17f, 16f, true),
                    new FortressMapPlatform("Platform 8", 54f, 58f, 22f, 18.4f, false),
                    new FortressMapPlatform("Platform 9", 58f, 63f, 24.5f, 18.4f, false),
                },
                new[]
                {
                    new FortressMapEnemy("다리 파수꾼", new Vector2(46, 18.4f)),
                    new FortressMapEnemy("성채 대장", new Vector2(72, 28.5f)),
                    new FortressMapEnemy("절벽 사수", new Vector2(97, 17)),
                    new FortressMapEnemy("하단 경비병", new Vector2(85, 6.9f)),
                }).Menu("성채와 협곡, 대교가 얽힌 처음의 전장",
                "다리 위 파수꾼부터 절벽 위 대장까지, 높낮이가 다른 적을 차례로 격파하라."),
        };

        static Seed Menu(this Seed seed, string tagline, string description)
        { seed.menuTagline = tagline; seed.menuDescription = description; return seed; }

        // Builds an unsaved map asset; `loadBackground` resolves a Resources path to a sprite.
        public static FortressMapDefinition Create(Seed seed, Func<string, Sprite> loadBackground)
        {
            var map = ScriptableObject.CreateInstance<FortressMapDefinition>();
            map.name = seed.name; map.order = seed.order; map.displayName = seed.name; map.description = seed.description;
            map.menuTagline = seed.menuTagline; map.menuDescription = seed.menuDescription;
            map.background = loadBackground(seed.backgroundResource); map.tint = seed.tint; map.playerSpawn = seed.player;
            map.platforms.AddRange(seed.platforms); map.enemies.AddRange(seed.enemies);
            return map;
        }
    }
}
