using MiniFortress;
using UnityEditor;
using UnityEngine;

public sealed class FortressBattlefieldWindow : EditorWindow
{
    [SerializeField] FortressArena arena;
    [SerializeField] FortressPreview.Screen screen = FortressPreview.Screen.Battle;
    [SerializeField] int playerClass;
    [SerializeField] bool live = true, showTerrain;
    FortressPreview preview;
    Texture frame;
    bool refresh = true;
    double nextRefresh;
    Vector2 scroll;
    string previewError;
    int pendingRender;

    [MenuItem("Mini Fortress/Battlefield Editor")]
    public static void Open()
    {
        var window = GetWindow<FortressBattlefieldWindow>("전장 편집");
        window.minSize = new Vector2(780, 650);
        window.FindArena(); window.refresh = true; window.Show();
    }

    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update += RenderPendingPreview;
    }
    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.update -= RenderPendingPreview;
        ReleasePreview();
    }
    void OnPlayMode(PlayModeStateChange state) { ReleasePreview(); refresh = true; Repaint(); }
    void ReleasePreview() { pendingRender = 0; preview?.Dispose(); preview = null; frame = null; }
    void RenderPendingPreview()
    {
        if (pendingRender == 0 || preview == null) return;
        // Let Unity rebuild the cloned Canvas's native batches before capturing it.
        if (--pendingRender > 0) { EditorApplication.QueuePlayerLoopUpdate(); return; }
        try { frame = preview.Render(); }
        catch (System.Exception exception) { previewError = exception.Message; ReleasePreview(); }
        Repaint();
    }
    void FindArena()
    {
        if (arena) return;
        foreach (var item in FindObjectsByType<FortressArena>(FindObjectsSortMode.None))
            if (!UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(item.gameObject.scene)) { arena = item; break; }
    }
    void OnInspectorUpdate()
    {
        if (live && EditorApplication.timeSinceStartup >= nextRefresh && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            nextRefresh = EditorApplication.timeSinceStartup + .75;
            refresh = true; Repaint();
        }
    }

    void OnGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorGUILayout.HelpBox("Play를 종료하면 씬 편집 미리보기를 사용할 수 있습니다.", MessageType.Info); return; }
        FindArena();
        EditorGUI.BeginChangeCheck();
        arena = (FortressArena)EditorGUILayout.ObjectField("편집할 전장", arena, typeof(FortressArena), true);
        using (new EditorGUILayout.HorizontalScope())
        {
            screen = (FortressPreview.Screen)EditorGUILayout.EnumPopup("화면", screen);
            live = GUILayout.Toggle(live, "자동 갱신", GUILayout.Width(85));
            showTerrain = GUILayout.Toggle(showTerrain, "바닥 영역", GUILayout.Width(85));
            if (GUILayout.Button("새로 고침", GUILayout.Width(90))) refresh = true;
        }
        if (EditorGUI.EndChangeCheck()) refresh = true;
        if (!arena)
        {
            EditorGUILayout.HelpBox("FortressBattle 씬을 열거나 FortressArena가 있는 오브젝트를 지정하세요.", MessageType.Info);
            if (GUILayout.Button("편집용 전장 열기")) { FortressSceneBuilder.Open(); FindArena(); refresh = true; }
            return;
        }
        var errors = arena.ValidateSetup();
        if (errors.Count > 0)
        {
            ReleasePreview();
            foreach (string error in errors) EditorGUILayout.HelpBox(error, MessageType.Error);
            return;
        }
        var labels = new string[arena.playerClasses.Length];
        for (int i = 0; i < labels.Length; i++) labels[i] = arena.playerClasses[i].displayName;
        EditorGUI.BeginChangeCheck();
        playerClass = EditorGUILayout.Popup("미리 볼 직업", Mathf.Clamp(playerClass, 0, labels.Length - 1), labels);
        if (EditorGUI.EndChangeCheck()) refresh = true;
        EditorGUILayout.LabelField("그림 속 캐릭터를 클릭하면 실제 Spawn을 선택합니다. 위치·크기는 Scene/Inspector에서 편집하세요.", EditorStyles.wordWrappedMiniLabel);

        float height = Mathf.Clamp((position.width - 20) * 9 / 16, 180, Mathf.Max(180, position.height - 330));
        Rect area = GUILayoutUtility.GetRect(10, height, GUILayout.ExpandWidth(true));
        Rect image = Fit(area);
        if (refresh && Event.current.type == EventType.Repaint && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            ReleasePreview(); previewError = null;
            try
            {
                FortressHud hud = null;
                foreach (var candidate in FindObjectsByType<FortressHud>(FindObjectsSortMode.None))
                    if (candidate.gameObject.scene == arena.gameObject.scene) { hud = candidate; break; }
                preview = new FortressPreview(arena, hud, playerClass, screen);
                pendingRender = 2;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (System.Exception exception) { previewError = exception.Message; ReleasePreview(); }
            refresh = false;
        }
        EditorGUI.DrawRect(area, Color.black);
        if (frame) GUI.DrawTexture(image, frame, ScaleMode.StretchToFill, false);
        if (previewError != null) EditorGUILayout.HelpBox(previewError, MessageType.Error);
        if (preview != null && screen != FortressPreview.Screen.Selection)
        {
            if (showTerrain)
                foreach (var platform in arena.terrainRoot.GetComponentsInChildren<FortressTerrain>())
                    Outline(ToGUI(platform.WorldRect, image), platform.allowDropThrough ? Color.yellow : Color.green);
            foreach (var actor in preview.Actors)
            {
                float halfWidth = Mathf.Max(.8f, actor.data.halfWidth * actor.data.prefab.transform.localScale.x);
                Rect hit = ToGUI(new Rect(actor.feet.x - halfWidth, actor.feet.y, halfWidth * 2, actor.height), image);
                if (Selection.activeTransform == actor.source) Outline(hit, Color.cyan);
                EditorGUIUtility.AddCursorRect(hit, MouseCursor.Link);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hit.Contains(Event.current.mousePosition))
                { Select(actor.source); Event.current.Use(); }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("전장 선택")) Selection.activeGameObject = arena.gameObject;
            if (GUILayout.Button("바닥 선택")) Selection.activeGameObject = arena.terrainRoot.gameObject;
            if (GUILayout.Button("모든 출전 위치 바닥 맞추기"))
            { int count = FortressPlacement.Snap(FortressPlacement.Spawns(arena)); ShowNotification(new GUIContent(count + "개 위치 수정 · Ctrl+Z로 취소")); refresh = true; }
        }
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(170));
        foreach (var spawn in FortressPlacement.Spawns(arena))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(spawn.name, GUILayout.MinWidth(180))) Select(spawn);
                var data = FortressPlacement.Definition(arena, spawn, playerClass);
                if (GUILayout.Button("능력치", GUILayout.Width(70))) Selection.activeObject = data;
                if (GUILayout.Button("Prefab", GUILayout.Width(70))) AssetDatabase.OpenAsset(data.prefab.gameObject);
                if (GUILayout.Button("바닥 맞춤", GUILayout.Width(80))) { FortressPlacement.Snap(new[] { spawn }); refresh = true; }
            }
        }
        var issues = FortressPlacement.Diagnose(arena, playerClass);
        if (issues.Count == 0) EditorGUILayout.HelpBox("배치 진단: 바닥 지지·지형 겹침·시작 위치 겹침 문제가 없습니다.", MessageType.Info);
        foreach (var issue in issues)
        {
            EditorGUILayout.HelpBox(issue.message, MessageType.Warning);
            if (GUILayout.Button("해당 위치 선택", GUILayout.Width(130))) Select(issue.target);
        }
        EditorGUILayout.EndScrollView();
    }

    static void Select(Transform target)
    {
        Selection.activeGameObject = target.gameObject; EditorGUIUtility.PingObject(target.gameObject);
        if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Frame(new Bounds(target.position + Vector3.up * 2, new Vector3(16, 12, 1)), false);
    }
    static Rect Fit(Rect area)
    {
        float width = Mathf.Min(area.width, area.height * 16 / 9);
        return new Rect(area.center.x - width / 2, area.center.y - width * 9 / 32, width, width * 9 / 16);
    }
    Rect ToGUI(Rect world, Rect image)
    {
        Vector3 min = preview.Camera.WorldToViewportPoint(new Vector3(world.xMin, world.yMin, 0));
        Vector3 max = preview.Camera.WorldToViewportPoint(new Vector3(world.xMax, world.yMax, 0));
        return Rect.MinMaxRect(image.x + min.x * image.width, image.y + (1 - max.y) * image.height,
            image.x + max.x * image.width, image.y + (1 - min.y) * image.height);
    }
    static void Outline(Rect rect, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
    }
}
