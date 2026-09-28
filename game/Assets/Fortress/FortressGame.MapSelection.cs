using System.Collections.Generic;
using UnityEngine;

namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        // Maps are FortressMapDefinition assets: the Arena's list if set, otherwise every asset in
        // Resources/FortressMaps, otherwise the built-in seeds. Platform and enemy counts may differ per map;
        // scene terrain and spawn objects are added or hidden to match.
        const string MapResourceFolder = "FortressMaps";

        FortressMapDefinition[] maps;
        readonly List<FortressTerrain> mapTerrain = new List<FortressTerrain>();
        readonly List<FortressSpawnPoint> mapEnemySpawns = new List<FortressSpawnPoint>();
        readonly List<FortressCharacterDefinition> sceneEnemyCharacters = new List<FortressCharacterDefinition>();
        readonly List<Transform> platformVisuals = new List<Transform>();
        readonly Dictionary<Texture2D, Sprite> wrappedSprites = new Dictionary<Texture2D, Sprite>();
        int selectedMap;

        public int MapCount => maps.Length;
        public int SelectedMap => selectedMap;
        public string MapName(int index) => maps[index].Title;
        public string MapDescription(int index) => maps[index].description;
        public string MapMenuTagline(int index) => maps[index].menuTagline;
        public string MapMenuDescription(int index) => maps[index].menuDescription;
        public Sprite MapPreviewSprite(int index) => index >= 0 && index < maps.Length ? maps[index].PreviewImage : null;
        FortressMapDefinition CurrentMap => maps[selectedMap];

        void LoadMaps()
        {
            var found = new List<FortressMapDefinition>();
            if (arena.maps != null) foreach (var map in arena.maps) if (map) found.Add(map);
            if (found.Count == 0) found.AddRange(Resources.LoadAll<FortressMapDefinition>(MapResourceFolder));
            if (found.Count == 0)
                foreach (var seed in FortressBuiltInMaps.Seeds)
                {
                    var map = FortressBuiltInMaps.Create(seed, LoadBackground);
                    ownedAssets.Add(map); found.Add(map);
                }
            found.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.name, b.name));
            maps = found.ToArray();
        }

        // Resources path → Sprite; images imported as plain textures are wrapped once.
        Sprite LoadBackground(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite) return sprite;
            var texture = Resources.Load<Texture2D>(path);
            if (!texture) return null;
            if (!wrappedSprites.TryGetValue(texture, out sprite))
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
                ownedAssets.Add(sprite); wrappedSprites[texture] = sprite;
            }
            return sprite;
        }

        void InitializeMapSelection()
        {
            LoadMaps();
            mapTerrain.AddRange(arena.terrainRoot.GetComponentsInChildren<FortressTerrain>(true));
            mapEnemySpawns.AddRange(arena.enemySpawns.GetComponentsInChildren<FortressSpawnPoint>(true));
            foreach (var spawn in mapEnemySpawns) sceneEnemyCharacters.Add(spawn.character);
            // 코덱스orig: 궤도 추적 카메라가 이동·확대되어도 선택한 맵의 배경이 화면을 계속 덮도록 연결합니다.
            var backdrop = arena.background.GetComponent<FortressCameraBackdrop>();
            if (!backdrop) backdrop = arena.background.gameObject.AddComponent<FortressCameraBackdrop>();
            backdrop.Follow(worldCamera);
        }

        public void SelectMap(int index)
        {
            if (phase != Phase.Selecting || index < 0 || index >= maps.Length || index == selectedMap) return;
            selectedMap = index;
            ApplyMap(CurrentMap);

            foreach (Fighter fighter in fighters)
                if (fighter.root) { fighter.root.gameObject.SetActive(false); Destroy(fighter.root.gameObject); }
            fighters.Clear();
            LoadArenaLayout();
            AddFighter(arena.playerClasses[playerClass], starts[0], null);
            var spawns = ActiveEnemySpawns();
            for (int i = 0; i < spawns.Length; i++) AddFighter(spawns[i].character, starts[i + 1], spawns[i].displayName);
            Restart();
            OpenSelection();
        }

        FortressSpawnPoint[] ActiveEnemySpawns() => arena.enemySpawns.GetComponentsInChildren<FortressSpawnPoint>();

        void ApplyMap(FortressMapDefinition map)
        {
            ValidateMapSpawns(map);
            for (int i = 0; i < Mathf.Max(map.platforms.Count, mapTerrain.Count); i++)
            {
                bool used = i < map.platforms.Count;
                if (used && i >= mapTerrain.Count) mapTerrain.Add(NewTerrain(i));
                mapTerrain[i].gameObject.SetActive(used);
                if (i < platformVisuals.Count) platformVisuals[i].gameObject.SetActive(used);
                if (!used) continue;

                var platform = map.platforms[i];
                Rect rect = platform.Rect;
                mapTerrain[i].name = string.IsNullOrWhiteSpace(platform.name) ? "Platform " + (i + 1) : platform.name;
                mapTerrain[i].transform.position = new Vector3(rect.center.x, rect.center.y, 0);
                var collider = mapTerrain[i].GetComponent<BoxCollider2D>();
                collider.offset = Vector2.zero; collider.size = rect.size;
                mapTerrain[i].allowDropThrough = platform.dropThrough;
                if (i >= platformVisuals.Count) platformVisuals.Add(null);
                platformVisuals[i] = BuildPlatformVisual(platformVisuals[i], i, platform, map);
            }

            arena.playerSpawn.position = new Vector3(map.playerSpawn.x, map.playerSpawn.y, 0);
            for (int i = 0; i < Mathf.Max(map.enemies.Count, mapEnemySpawns.Count); i++)
            {
                bool used = i < map.enemies.Count;
                if (used && i >= mapEnemySpawns.Count) mapEnemySpawns.Add(NewEnemySpawn(i));
                mapEnemySpawns[i].gameObject.SetActive(used);
                if (!used) continue;
                var enemy = map.enemies[i];
                var spawn = mapEnemySpawns[i];
                spawn.transform.position = new Vector3(enemy.position.x, enemy.position.y, 0);
                spawn.displayName = enemy.displayName;
                // Without a character, keep the scene's spawn at the same slot (slot 2 is the captain), else the first.
                spawn.character = enemy.character ? enemy.character
                    : i < sceneEnemyCharacters.Count && sceneEnemyCharacters[i] ? sceneEnemyCharacters[i] : sceneEnemyCharacters[0];
            }

            if (map.background) arena.background.sprite = map.background;
            else Debug.LogWarning("FortressGame: 맵 '" + map.Title + "'의 배경 이미지가 비어 있습니다.", map);
            arena.background.color = Color.white;
            worldCamera.backgroundColor = new Color(.035f, .055f, .09f);
        }

        FortressTerrain NewTerrain(int index)
        {
            var item = new GameObject("Platform " + (index + 1));
            item.transform.SetParent(arena.terrainRoot, false);
            item.AddComponent<BoxCollider2D>().isTrigger = true;
            return item.AddComponent<FortressTerrain>();
        }

        FortressSpawnPoint NewEnemySpawn(int index)
        {
            var item = new GameObject("Enemy " + (index + 1));
            item.transform.SetParent(arena.enemySpawns, false);
            return item.AddComponent<FortressSpawnPoint>();
        }

        // 코덱스orig: 맵을 추가하거나 발판 높이를 바꾸면 플레이어와 적의 발밑을 검사해 낙하 시작 배치를 미리 찾습니다.
        void ValidateMapSpawns(FortressMapDefinition map)
        {
            if (map.platforms.Count == 0) Debug.LogWarning("FortressGame: 맵 '" + map.Title + "'에 발판이 없습니다.", map);
            if (map.enemies.Count == 0) Debug.LogWarning("FortressGame: 맵 '" + map.Title + "'에 적이 없습니다.", map);
            ValidateSpawnSupport(map, map.playerSpawn, "플레이어");
            for (int i = 0; i < map.enemies.Count; i++)
                ValidateSpawnSupport(map, map.enemies[i].position, "적 " + (i + 1));
        }

        void ValidateSpawnSupport(FortressMapDefinition map, Vector2 spawn, string actorName)
        {
            const float actorHalfWidth = .75f;
            // Claude: 머리 위 발판과 겹치는 스폰은 캐릭터가 발판에 파묻혀 보이고 이동도 막힙니다.
            const float actorHeight = 4.2f;
            foreach (var platform in map.platforms)
                if (spawn.x + actorHalfWidth > platform.left && spawn.x - actorHalfWidth < platform.right &&
                    spawn.y + .05f < platform.top && spawn.y + actorHeight > platform.bottom)
                    Debug.LogWarning("FortressGame: 맵 '" + map.Title + "'에서 " + actorName + " 스폰이 위쪽 발판과 겹칩니다.", map);
            foreach (var platform in map.platforms)
                if (spawn.x >= platform.left + actorHalfWidth && spawn.x <= platform.right - actorHalfWidth &&
                    Mathf.Abs(spawn.y - platform.top) <= .05f)
                    return;
            Debug.LogWarning("FortressGame: 맵 '" + map.Title + "'에서 " + actorName + " 스폰을 받치는 발판을 찾지 못했습니다.", map);
        }
    }
}
