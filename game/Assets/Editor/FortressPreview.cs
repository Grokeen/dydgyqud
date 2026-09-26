using System;
using System.Collections.Generic;
using MiniFortress;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// All copies live in disposable preview scenes. Never populate or toggle the authored scene's UI.
public sealed class FortressPreview : IDisposable
{
    public enum Screen { Battlefield, Selection, Battle, Result }
    public struct Actor
    {
        public Transform source;
        public FortressCharacterDefinition data;
        public string name;
        public Vector3 feet;
        public float height;
    }
    readonly PreviewRenderUtility world = new PreviewRenderUtility();
    readonly PreviewRenderUtility ui = new PreviewRenderUtility();
    readonly List<Actor> actors = new List<Actor>();
    GameObject worldRoot, uiRoot;
    FortressHud hud;
    Canvas canvas;
    RenderTexture worldSnapshot;
    Screen screen;
    bool disposed;
    public IReadOnlyList<Actor> Actors => actors;
    public Camera Camera => world.camera;

    public FortressPreview(FortressArena arena, FortressHud sourceHud, int selectedClass, Screen mode)
    {
        screen = mode;
        try { Build(arena, sourceHud, selectedClass); }
        catch { Dispose(); throw; }
    }

    void Build(FortressArena arena, FortressHud sourceHud, int selectedClass)
    {
        worldRoot = new GameObject("Fortress world preview") { hideFlags = HideFlags.HideAndDontSave };
        world.AddSingleGO(worldRoot);
        world.camera.CopyFrom(arena.worldCamera);
        // CopyFrom also copies the source camera's scene; restore preview isolation.
        world.camera.scene = worldRoot.scene;
        world.camera.transform.SetPositionAndRotation(arena.worldCamera.transform.position, arena.worldCamera.transform.rotation);
        world.camera.targetTexture = null; world.camera.enabled = false;
        // Canvas rendering can span preview scenes; keep the UI pass out of the world texture.
        world.camera.cullingMask &= ~(1 << 5);
        world.camera.aspect = 16f / 9; world.camera.cameraType = CameraType.Preview;
        var painting = Object.Instantiate(arena.background, worldRoot.transform);
        painting.transform.SetPositionAndRotation(arena.background.transform.position, arena.background.transform.rotation);
        painting.transform.localScale = arena.background.transform.lossyScale;
        foreach (var spawn in FortressPlacement.Spawns(arena))
        {
            var data = FortressPlacement.Definition(arena, spawn, selectedClass);
            if (!data || !data.prefab || !data.prefab.IsConfigured) continue;
            var actor = Object.Instantiate(data.prefab, worldRoot.transform);
            actor.transform.position = spawn.position;
            actor.animator.enabled = false;
            bool player = spawn == arena.playerSpawn;
            actor.motion.localScale = new Vector3(player || arena.playerSpawn.position.x >= spawn.position.x ? 1 : -1, 1, 1);
            actor.aimPivot.localPosition = Vector3.up * data.shoulderHeight;
            actor.aimPivot.localRotation = Quaternion.Euler(0, 0, arena.rules.defaultAngle);
            string name = spawn.GetComponent<FortressSpawnPoint>()?.displayName;
            actors.Add(new Actor { source = spawn, data = data, feet = spawn.position,
                height = data.height * data.prefab.transform.localScale.x,
                name = player || string.IsNullOrWhiteSpace(name) ? data.displayName : name });
        }
        if (!sourceHud || screen == Screen.Battlefield) return;
        uiRoot = new GameObject("Fortress UI preview") { hideFlags = HideFlags.HideAndDontSave };
        ui.AddSingleGO(uiRoot);
        ui.camera.scene = uiRoot.scene;
        hud = Object.Instantiate(sourceHud, uiRoot.transform); hud.enabled = false;
        worldSnapshot = new RenderTexture(1600, 900, 0, RenderTextureFormat.ARGB32)
        { hideFlags = HideFlags.HideAndDontSave };
        worldSnapshot.Create();
        hud.battlefield.texture = worldSnapshot;
        hud.game = null;
        foreach (var hold in hud.GetComponentsInChildren<FortressHoldButton>(true)) hold.enabled = false;
        foreach (var raycaster in hud.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
        foreach (Transform child in hud.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
        canvas = hud.GetComponent<Canvas>();
        // Use an explicit 1600x900 world-space canvas so the Game view's size cannot crop this editor preview.
        canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = ui.camera;
        var canvasRect = (RectTransform)canvas.transform;
        canvasRect.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        canvasRect.localScale = Vector3.one;
        canvasRect.anchorMin = canvasRect.anchorMax = canvasRect.pivot = Vector2.one * .5f;
        canvasRect.sizeDelta = new Vector2(1600, 900);
        hud.GetComponent<CanvasScaler>().enabled = false;
        ui.camera.transform.position = new Vector3(0, 0, -100);
        ui.camera.nearClipPlane = .1f; ui.camera.farClipPlane = 1000;
        ui.camera.orthographic = true; ui.camera.orthographicSize = 450; ui.camera.aspect = 16f / 9;
        ui.camera.clearFlags = CameraClearFlags.SolidColor; ui.camera.backgroundColor = Color.black;
        ui.camera.cullingMask = 1 << 5;
        hud.selectionPanel.SetActive(screen == Screen.Selection);
        hud.battlePanel.SetActive(screen != Screen.Selection);
        hud.resultPanel.SetActive(screen == Screen.Result);
        hud.classCardTemplate.gameObject.SetActive(false); hud.turnTemplate.gameObject.SetActive(false); hud.healthTemplate.gameObject.SetActive(false);
        for (int i = 0; i < arena.playerClasses.Length; i++)
        {
            if (!arena.playerClasses[i]) continue;
            var card = Object.Instantiate(hud.classCardTemplate, hud.classCardRoot);
            card.gameObject.SetActive(true); card.Show(arena.playerClasses[i], selectedClass == i);
        }
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            var turn = Object.Instantiate(hud.turnTemplate, hud.turnRoot); turn.gameObject.SetActive(true);
            turn.Show(actor.data.portrait, actor.name, actor.data.health, actor.data.health, i == 0);
            var bar = Object.Instantiate(hud.healthTemplate, hud.healthRoot); bar.gameObject.SetActive(true);
            bar.Show(null, $"{actor.data.health}/{actor.data.health}", actor.data.health, actor.data.health, false);
            Vector3 view = world.camera.WorldToViewportPoint(actor.feet + Vector3.up * (actor.height + .45f));
            var rect = (RectTransform)bar.transform; rect.pivot = new Vector2(.5f, 0);
            rect.anchorMin = rect.anchorMax = new Vector2(view.x, view.y); rect.anchoredPosition = Vector2.zero;
        }
        var playerData = arena.playerClasses[Mathf.Clamp(selectedClass, 0, arena.playerClasses.Length - 1)];
        hud.playerPortrait.sprite = playerData.portrait;
        hud.playerStatus.text = $"{playerData.displayName}\nHP {playerData.health} / {playerData.health}\n이동 {playerData.movementPerTurn:0.0} / {playerData.movementPerTurn:0.#} m";
        hud.turnText.text = "턴 1"; hud.enemyStatus.text = $"모든 적 처치\n남은 적 {actors.Count - 1} / {actors.Count - 1}명";
        hud.messageText.text = "편집 미리보기 · 전투는 Play에서 확인하세요.";
        hud.resultText.text = "승리 · 모든 적을 처치했습니다!";
        hud.attackText.text = playerData.AttackName + " [Space]";
        hud.aimText.text = $"각도 {arena.rules.defaultAngle:0}°"; hud.powerText.text = $"위력 {arena.rules.defaultPower:0.0}";
        hud.angleSlider.minValue = arena.rules.angleLimits.x; hud.angleSlider.maxValue = arena.rules.angleLimits.y;
        hud.powerSlider.minValue = arena.rules.powerLimits.x; hud.powerSlider.maxValue = arena.rules.powerLimits.y;
        hud.angleSlider.SetValueWithoutNotify(arena.rules.defaultAngle); hud.powerSlider.SetValueWithoutNotify(arena.rules.defaultPower);
    }

    public Texture Render()
    {
        if (disposed) throw new ObjectDisposedException(nameof(FortressPreview));
        var rect = new Rect(0, 0, 1600, 900);
        world.BeginPreview(rect, GUIStyle.none);
        Texture worldImage;
        try { world.Render(true); } finally { worldImage = world.EndPreview(); }
        if (!hud) return worldImage;
        Graphics.Blit(worldImage, worldSnapshot);
        ui.BeginPreview(rect, GUIStyle.none);
        Texture image;
        try
        {
            canvas.scaleFactor = 1;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
            foreach (var graphic in hud.GetComponentsInChildren<Graphic>())
            {
                graphic.SetAllDirty();
                graphic.Rebuild(CanvasUpdate.PreRender);
            }
            Canvas.ForceUpdateCanvases();
            hud.battlefield.canvasRenderer.SetTexture(worldSnapshot);
            ui.Render(true);
        }
        finally { image = ui.EndPreview(); }
        return image;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ui.Cleanup(); world.Cleanup();
        if (worldSnapshot) { worldSnapshot.Release(); Object.DestroyImmediate(worldSnapshot); }
    }
}
