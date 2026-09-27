using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        struct MapPreset
        {
            public string name, description, backgroundResource;
            public Color tint;
            public Rect[] platforms;
            public bool[] dropThrough;
            public Vector2 player;
            public Vector2[] enemies;
            public string[] enemyNames;

            public MapPreset(string name, string description, string backgroundResource, Color tint,
                Rect[] platforms, bool[] dropThrough, Vector2 player, Vector2[] enemies, string[] enemyNames)
            {
                this.name = name;
                this.description = description;
                this.backgroundResource = backgroundResource;
                this.tint = tint;
                this.platforms = platforms;
                this.dropThrough = dropThrough;
                this.player = player;
                this.enemies = enemies;
                this.enemyNames = enemyNames;
            }
        }

        // 코덱스code(CodexCode) 작업 메모: 맵 이름, 실제 배경 리소스, 충돌 발판, 적 출전 위치를 한 프리셋으로 관리합니다.
        static readonly MapPreset[] MapPresets =
        {
            new MapPreset("월하 요새", "넓은 대교와 양쪽 성루", "FortressMapPreviews/MoonlitFortress", new Color(.9f, .94f, 1f),
                new[]
                {
                    PlatformRect(0, 17, 24, 19), PlatformRect(17, 34, 19, 17.8f),
                    PlatformRect(34, 51, 19, 17.8f), PlatformRect(51, 68, 19, 17.8f),
                    PlatformRect(68, 85, 19, 17.8f), PlatformRect(85, 102, 24, 19),
                    PlatformRect(27, 38, 22.2f, 21.4f), PlatformRect(64, 75, 22.2f, 21.4f),
                    PlatformRect(43, 59, 13, 11.8f)
                },
                new[] { false, false, false, false, false, false, false, false, true },
                new Vector2(8, 24),
                new[] { new Vector2(25, 19), new Vector2(46, 19), new Vector2(73, 19), new Vector2(94, 24) },
                new[] { "서문 파수꾼", "대교 경비병", "성채 대장", "동문 파수꾼" }),
            new MapPreset("달빛 협곡", "절벽 사이를 오르는 계단길", "FortressMapPreviews/MoonlitRavine", new Color(.82f, .9f, 1f),
                new[]
                {
                    PlatformRect(0, 18, 25, 20), PlatformRect(18, 34, 21, 19.8f),
                    PlatformRect(34, 51, 19, 17.8f), PlatformRect(53, 69, 22, 20.8f),
                    PlatformRect(69, 85, 26, 24.8f), PlatformRect(85, 102, 30, 28.8f),
                    PlatformRect(30, 42, 24, 23.2f), PlatformRect(55, 67, 27, 26.2f),
                    PlatformRect(75, 87, 32, 31.2f)
                },
                new[] { false, true, false, true, false, false, false, false, false },
                new Vector2(8, 25),
                new[] { new Vector2(27, 21), new Vector2(46, 19), new Vector2(64, 22), new Vector2(94, 30) },
                new[] { "서쪽 절벽 사수", "협곡 다리 파수꾼", "상단 경비병", "봉우리 대장" }),
            new MapPreset("갈라진 성벽", "무너진 틈을 건너는 부유 발판", "FortressMapPreviews/BrokenRamparts", new Color(1f, .88f, .76f),
                new[]
                {
                    PlatformRect(0, 19, 25, 23.8f), PlatformRect(19, 34, 17, 15.8f),
                    PlatformRect(35, 49, 21, 19.8f), PlatformRect(51, 66, 18, 16.8f),
                    PlatformRect(68, 84, 22, 20.8f), PlatformRect(85, 102, 27, 25.8f),
                    PlatformRect(24, 37, 12, 10.8f), PlatformRect(62, 74, 12, 10.8f),
                    PlatformRect(43, 56, 27, 25.8f)
                },
                new[] { false, true, false, true, false, false, true, true, false },
                new Vector2(8, 25),
                new[] { new Vector2(27, 17), new Vector2(43, 21), new Vector2(74, 22), new Vector2(94, 27) },
                new[] { "서쪽 성벽 경비병", "무너진 탑 수비병", "동쪽 균열 사수", "폐허의 대장" }),
            // 코덱스orig: 기존 FortressBattle 씬의 지형·스폰과 CitadelArena 배경을 독립된 선택지로 복원합니다.
            new MapPreset("기존 성채 전장", "처음 전장의 성채·협곡·대교 배치", "FortressArt/CitadelArena", new Color(.9f, .94f, 1f),
                new[]
                {
                    PlatformRect(-3, 18.3f, 24.7f, -14), PlatformRect(18.3f, 32.7f, 6.9f, -14),
                    PlatformRect(38.3f, 52f, 8.6f, -14), PlatformRect(18.3f, 63f, 18.4f, 17.7f),
                    PlatformRect(63f, 81.5f, 28.5f, -14), PlatformRect(81.5f, 103f, 6.9f, -14),
                    PlatformRect(92f, 103f, 17f, 16f), PlatformRect(54f, 58f, 22f, 18.4f),
                    PlatformRect(58f, 63f, 24.5f, 18.4f)
                },
                new[] { false, false, false, true, false, false, true, false, false },
                new Vector2(9, 24.7f),
                new[] { new Vector2(46, 18.4f), new Vector2(72, 28.5f), new Vector2(97, 17), new Vector2(85, 6.9f) },
                new[] { "다리 파수꾼", "성채 대장", "절벽 사수", "하단 경비병" })
        };

        FortressTerrain[] mapTerrain;
        FortressSpawnPoint[] mapEnemySpawns;
        Transform[] mapPlatformVisuals, mapPlatformTopVisuals;
        Transform[] spawnShadows, spawnFootprints;
        Sprite[] mapPreviewCache;
        int selectedMap;

        static Rect PlatformRect(float left, float right, float top, float bottom)
            => new Rect(left, bottom, right - left, top - bottom);

        public int MapCount => MapPresets.Length;
        public int SelectedMap => selectedMap;
        public string MapName(int index) => MapPresets[index].name;
        public string MapDescription(int index) => MapPresets[index].description;

        public Sprite MapPreviewSprite(int index)
        {
            if (index < 0 || index >= MapPresets.Length) return null;
            if (mapPreviewCache == null) mapPreviewCache = new Sprite[MapPresets.Length];
            if (mapPreviewCache[index]) return mapPreviewCache[index];

            string resource = MapPresets[index].backgroundResource;
            mapPreviewCache[index] = Resources.Load<Sprite>(resource);
            if (mapPreviewCache[index]) return mapPreviewCache[index];

            // CodexCode: the restored Citadel background imports as Texture2D, so wrap it for both the HUD and battlefield.
            Texture2D texture = Resources.Load<Texture2D>(resource);
            if (!texture) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            ownedAssets.Add(sprite);
            mapPreviewCache[index] = sprite;
            return sprite;
        }

        void InitializeMapSelection()
        {
            mapTerrain = arena.terrainRoot.GetComponentsInChildren<FortressTerrain>();
            mapEnemySpawns = arena.enemySpawns.GetComponentsInChildren<FortressSpawnPoint>();
            // 코덱스orig: 궤도 추적 카메라가 이동·확대되어도 선택한 맵의 배경이 화면을 계속 덮도록 연결합니다.
            var backdrop = arena.background.GetComponent<FortressCameraBackdrop>();
            if (!backdrop) backdrop = arena.background.gameObject.AddComponent<FortressCameraBackdrop>();
            backdrop.Follow(worldCamera);
            mapPlatformVisuals = new Transform[mapTerrain.Length];
            mapPlatformTopVisuals = new Transform[mapTerrain.Length];
            for (int i = 0; i < mapTerrain.Length; i++)
            {
                mapPlatformVisuals[i] = Shape("Map platform " + mapTerrain[i].name, Vector2.zero, Vector2.one,
                    new Color(.12f, .17f, .2f, .98f), -19);
                mapPlatformTopVisuals[i] = Shape("Map platform top " + mapTerrain[i].name, Vector2.zero, Vector2.one,
                    Color.white, -18);
            }

            // CodexCode: show a soft contact shadow and color-coded footprint at every map spawn so feet read as grounded.
            int spawnCount = mapEnemySpawns.Length + 1;
            spawnShadows = new Transform[spawnCount];
            spawnFootprints = new Transform[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                spawnShadows[i] = ArtObject(transform, "Spawn Contact Shadow " + i, arena.effectSprite, Vector2.zero, -17);
                spawnShadows[i].localScale = new Vector3(2.8f, .66f, 1);
                spawnShadows[i].GetComponent<SpriteRenderer>().color = new Color(.015f, .025f, .035f, .88f);
                Color footprintColor = i == 0 ? new Color(1f, .63f, .2f, .95f) : new Color(.32f, .8f, .94f, .92f);
                spawnFootprints[i] = Shape("Spawn Footprint " + i, Vector2.zero, new Vector2(1.65f, .1f), footprintColor, -16);
            }
        }

        public void SelectMap(int index)
        {
            if (phase != Phase.Selecting || index < 0 || index >= MapPresets.Length || index == selectedMap) return;
            selectedMap = index;
            ApplyMapPreset(MapPresets[index]);

            foreach (Fighter fighter in fighters)
                if (fighter.root) { fighter.root.gameObject.SetActive(false); Destroy(fighter.root.gameObject); }
            fighters.Clear();
            LoadArenaLayout();
            AddFighter(arena.playerClasses[playerClass], starts[0], null);
            for (int i = 0; i < mapEnemySpawns.Length; i++)
                AddFighter(mapEnemySpawns[i].character, starts[i + 1], mapEnemySpawns[i].displayName);
            Restart();
            OpenSelection();
        }

        void ApplyMapPreset(MapPreset preset)
        {
            ValidateMapPresetSpawns(preset);
            if (mapTerrain.Length != preset.platforms.Length || mapEnemySpawns.Length != preset.enemies.Length)
                Debug.LogWarning("FortressGame: 맵 프리셋의 발판 또는 적 수와 씬 구성이 다릅니다.", this);

            int platformCount = Mathf.Min(mapTerrain.Length, preset.platforms.Length);
            for (int i = 0; i < platformCount; i++)
            {
                Rect rect = preset.platforms[i];
                mapTerrain[i].transform.position = new Vector3(rect.center.x, rect.center.y, 0);
                var collider = mapTerrain[i].GetComponent<BoxCollider2D>();
                collider.offset = Vector2.zero;
                collider.size = rect.size;
                mapTerrain[i].allowDropThrough = i < preset.dropThrough.Length && preset.dropThrough[i];

                mapPlatformVisuals[i].position = new Vector3(rect.center.x, rect.yMax - .25f, 0);
                mapPlatformVisuals[i].localScale = new Vector3(rect.width, .5f, 1);
                mapPlatformVisuals[i].GetComponent<SpriteRenderer>().color = preset.tint * new Color(.3f, .34f, .4f, .98f);
                mapPlatformTopVisuals[i].position = new Vector3(rect.center.x, rect.yMax - .055f, -.01f);
                mapPlatformTopVisuals[i].localScale = new Vector3(rect.width, .11f, 1);
                mapPlatformTopVisuals[i].GetComponent<SpriteRenderer>().color = preset.tint * new Color(.88f, .82f, .68f, 1f);
            }

            arena.playerSpawn.position = new Vector3(preset.player.x, preset.player.y, 0);
            SetSpawnFootprint(0, preset.player);
            int enemyCount = Mathf.Min(mapEnemySpawns.Length, preset.enemies.Length);
            for (int i = 0; i < enemyCount; i++)
            {
                mapEnemySpawns[i].transform.position = new Vector3(preset.enemies[i].x, preset.enemies[i].y, 0);
                SetSpawnFootprint(i + 1, preset.enemies[i]);
                if (i < preset.enemyNames.Length) mapEnemySpawns[i].displayName = preset.enemyNames[i];
            }

            int presetIndex = System.Array.IndexOf(MapPresets, preset);
            Sprite mapBackground = MapPreviewSprite(presetIndex);
            if (mapBackground)
            {
                arena.background.sprite = mapBackground;
            }
            else Debug.LogWarning("FortressGame: 맵 배경 이미지를 찾을 수 없습니다: " + preset.backgroundResource, this);
            arena.background.color = Color.white;
            worldCamera.backgroundColor = new Color(.035f, .055f, .09f);
        }

        void SetSpawnFootprint(int index, Vector2 feet)
        {
            if (spawnShadows == null || index < 0 || index >= spawnShadows.Length) return;
            Vector3 position = new Vector3(feet.x, feet.y + .025f, -.02f);
            spawnShadows[index].position = position;
            spawnFootprints[index].position = new Vector3(feet.x, feet.y + .045f, -.03f);
        }

        // 코덱스orig: 맵을 추가하거나 발판 높이를 바꾸면 플레이어와 적의 발밑을 검사해 낙하 시작 배치를 미리 찾습니다.
        void ValidateMapPresetSpawns(MapPreset preset)
        {
            ValidateSpawnSupport(preset, preset.player, "플레이어");
            for (int i = 0; i < preset.enemies.Length; i++)
                ValidateSpawnSupport(preset, preset.enemies[i], "적 " + (i + 1));
            if (preset.enemyNames.Length != preset.enemies.Length)
                Debug.LogWarning("FortressGame: 맵 '" + preset.name + "'의 적 이름과 스폰 수가 다릅니다.", this);
        }

        void ValidateSpawnSupport(MapPreset preset, Vector2 spawn, string actorName)
        {
            const float actorHalfWidth = .75f;
            for (int i = 0; i < preset.platforms.Length; i++)
            {
                Rect platform = preset.platforms[i];
                if (spawn.x >= platform.xMin + actorHalfWidth && spawn.x <= platform.xMax - actorHalfWidth &&
                    Mathf.Abs(spawn.y - platform.yMax) <= .05f)
                    return;
            }

            Debug.LogWarning("FortressGame: 맵 '" + preset.name + "'에서 " + actorName + " 스폰을 받치는 발판을 찾지 못했습니다.", this);
        }
    }
}
