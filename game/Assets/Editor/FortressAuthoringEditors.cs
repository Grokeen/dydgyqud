using MiniFortress;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FortressTerrain))]
public sealed class FortressTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Move 도구로 위치, Box Collider 2D의 Edit Collider로 크기를 조절하세요. 회전/경사는 지원하지 않습니다. Allow Drop Through는 S 키로 내려갈 수 있는 발판입니다. 변경은 다음 Play에 적용됩니다.", MessageType.Info);
        DrawDefaultInspector();
        var platform = (FortressTerrain)target;
        Rect r = platform.WorldRect;
        EditorGUILayout.LabelField("World bounds", $"X {r.xMin:0.##} ~ {r.xMax:0.##}, 바닥 높이 {r.yMax:0.##}");
        if (Quaternion.Angle(platform.transform.rotation, Quaternion.identity) > .01f)
            EditorGUILayout.HelpBox("Rotation을 (0, 0, 0)으로 설정하세요.", MessageType.Error);
    }
}

[CustomEditor(typeof(FortressSpawnPoint))]
[CanEditMultipleObjects]
public sealed class FortressSpawnPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("이 Transform의 위치가 캐릭터의 발 위치입니다. 적은 Enemies 아래 Hierarchy 순서대로 행동합니다. 복제하면 적을 추가할 수 있습니다.", MessageType.Info);
        DrawDefaultInspector();
        if (GUILayout.Button("가장 가까운 바닥 위로 맞추기"))
        {
            var spawns = new System.Collections.Generic.List<Transform>();
            foreach (FortressSpawnPoint spawn in targets) spawns.Add(spawn.transform);
            FortressPlacement.Snap(spawns);
        }
        if (GUILayout.Button("전장 미리보기 열기")) FortressBattlefieldWindow.Open();
    }
    void OnSceneGUI()
    {
        var spawn = (FortressSpawnPoint)target;
        Handles.Label(spawn.transform.position + Vector3.up * 5, spawn.name + " (발 위치)");
    }
}

[CustomEditor(typeof(FortressArena))]
public sealed class FortressArenaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Terrain: 바닥 편집 / Spawns: 캐릭터 배치 / Player Classes: 선택 직업 / Rules: 전투 공통 수치. Prefab과 Data는 Assets/FortressContent에 있습니다.", MessageType.Info);
        DrawDefaultInspector();
        if (GUILayout.Button("전장 편집 · Play 없이 미리보기")) FortressBattlefieldWindow.Open();
        foreach (string error in ((FortressArena)target).ValidateSetup()) EditorGUILayout.HelpBox(error, MessageType.Error);
    }
}
