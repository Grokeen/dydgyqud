using System.Collections.Generic;
using MiniFortress;
using UnityEditor;
using UnityEngine;

public static class FortressPlacement
{
    public struct Issue
    {
        public Transform target;
        public string message;
    }

    public static FortressCharacterDefinition Definition(FortressArena arena, Transform spawn, int playerClass = 0)
    {
        if (spawn == arena.playerSpawn)
            return arena.playerClasses != null && arena.playerClasses.Length > 0
                ? arena.playerClasses[Mathf.Clamp(playerClass, 0, arena.playerClasses.Length - 1)] : null;
        return spawn.GetComponent<FortressSpawnPoint>()?.character;
    }

    public static List<Transform> Spawns(FortressArena arena)
    {
        var result = new List<Transform>();
        if (!arena) return result;
        if (arena.playerSpawn && arena.playerSpawn.gameObject.activeInHierarchy) result.Add(arena.playerSpawn);
        if (arena.enemySpawns)
            foreach (var spawn in arena.enemySpawns.GetComponentsInChildren<FortressSpawnPoint>()) result.Add(spawn.transform);
        return result;
    }

    public static bool TryFloor(FortressArena arena, Vector2 feet, out float top)
    {
        top = feet.y;
        if (!arena || !arena.terrainRoot) return false;
        float nearest = float.PositiveInfinity;
        foreach (var platform in arena.terrainRoot.GetComponentsInChildren<FortressTerrain>())
        {
            Rect r = platform.WorldRect;
            if (feet.x < r.xMin || feet.x > r.xMax) continue;
            float distance = Mathf.Abs(feet.y - r.yMax);
            if (distance < nearest) { nearest = distance; top = r.yMax; }
        }
        return !float.IsPositiveInfinity(nearest);
    }

    public static int Snap(IEnumerable<Transform> spawns)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup(), count = 0;
        Undo.SetCurrentGroupName("Snap fortress spawns to terrain");
        foreach (var spawn in spawns)
        {
            if (!spawn) continue;
            var arena = spawn.GetComponentInParent<FortressArena>();
            if (!TryFloor(arena, spawn.position, out float top) || Mathf.Approximately(spawn.position.y, top)) continue;
            Undo.RecordObject(spawn, "Snap fortress spawn");
            var position = spawn.position; position.y = top; spawn.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawn); count++;
        }
        Undo.FlushUndoRecordObjects();
        Undo.CollapseUndoOperations(group);
        return count;
    }

    public static List<Issue> Diagnose(FortressArena arena, int playerClass = 0)
    {
        var issues = new List<Issue>();
        if (!arena || !arena.terrainRoot) return issues;
        var terrain = arena.terrainRoot.GetComponentsInChildren<FortressTerrain>();
        var occupied = new List<Rect>();
        foreach (var spawn in Spawns(arena))
        {
            var data = Definition(arena, spawn, playerClass);
            if (!data || !data.prefab) { Add(issues, spawn, "캐릭터 데이터 또는 Prefab이 없습니다."); continue; }
            float scale = data.prefab.transform.localScale.x;
            float width = data.halfWidth * scale, height = data.height * scale;
            Rect body = new Rect(spawn.position.x - width, spawn.position.y + .06f, width * 2, Mathf.Max(.01f, height - .06f));
            bool supported = TryFloor(arena, spawn.position, out float top) && Mathf.Abs(top - spawn.position.y) < .05f;
            if (!supported) Add(issues, spawn, "발이 바닥에 닿아 있지 않습니다. 바닥 맞추기를 사용하세요.");
            foreach (var floor in terrain)
                if (body.Overlaps(floor.WorldRect)) { Add(issues, spawn, "몸이 지형 안에 겹칩니다."); break; }
            foreach (Rect other in occupied)
                if (body.Overlaps(other)) { Add(issues, spawn, "다른 캐릭터의 시작 위치와 겹칩니다."); break; }
            if (arena.rules && (body.xMin < arena.rules.horizontalLimits.x || body.xMax > arena.rules.horizontalLimits.y))
                Add(issues, spawn, "이동 가능한 월드 범위 밖에 있습니다.");
            occupied.Add(body);
        }
        return issues;
    }

    static void Add(List<Issue> issues, Transform target, string message)
        => issues.Add(new Issue { target = target, message = target.name + ": " + message });
}
