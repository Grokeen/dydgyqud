using MiniFortress;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using B = FortressSceneBuilder;

// Rearranges the battle HUD into the card-battle mockup: character info + relics bottom-left, the hand in the
// middle, deck / discard / end turn on the right and money / currency / settings in the top bar.
// Existing controls are moved, not recreated, so their references and listeners survive. Run once, then edit
// the result in the scene like any other UI.
public static class FortressHudLayout
{
    static readonly Color CardColor = new Color(.06f, .09f, .12f, .97f);
    static readonly Color ArtColor = new Color(.11f, .16f, .2f);
    static readonly Color BadgeColor = new Color(.85f, .6f, .2f);

    // Brings an open battle scene up to the current layout when scripts reload, so it takes effect without
    // hunting for the menu. Each step only runs when its old layout is still present.
    [InitializeOnLoadMethod]
    static void AutoApply()
    {
        EditorApplication.update -= TryAutoApply;
        EditorApplication.update += TryAutoApply;
        // Scripts may reload while playing (Enter Play Mode Options); try again once Play mode stops.
        EditorApplication.playModeStateChanged -= RetryAfterPlay;
        EditorApplication.playModeStateChanged += RetryAfterPlay;
    }

    static void RetryAfterPlay(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.update -= TryAutoApply;
        EditorApplication.update += TryAutoApply;
    }

    // Only edits the saved scene: never while playing, where changes are thrown away on stop.
    static void TryAutoApply()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= TryAutoApply;
        var hud = Object.FindAnyObjectByType<FortressHud>(FindObjectsInactive.Include);
        if (!hud || !NeedsAnyStep(hud)) return;
        ApplyMenu();
        EditorSceneManager.SaveScene(hud.gameObject.scene);
    }

    static bool NeedsMockup(FortressHud hud) => hud.cardSlots == null || hud.cardSlots.Length == 0;
    static bool NeedsChargeLayout(FortressHud hud) => hud.angleSlider || hud.powerSlider;
    static bool NeedsHealthGauge(FortressHud hud) => !hud.healthFill;
    static bool NeedsRewardPanel(FortressHud hud) => !hud.rewardPanel;
    static bool NeedsStageBanner(FortressHud hud) => !hud.stageBanner;
    static bool NeedsLastPowerMarker(FortressHud hud) => !hud.lastPowerMarker && hud.powerFill;
    static bool NeedsAnyStep(FortressHud hud) => NeedsMockup(hud) || NeedsChargeLayout(hud) || NeedsHealthGauge(hud)
        || NeedsRewardPanel(hud) || NeedsStageBanner(hud) || NeedsLastPowerMarker(hud);

    [MenuItem("Mini Fortress/HUD 목업 레이아웃 적용")]
    static void ApplyMenu()
    {
        var hud = Object.FindAnyObjectByType<FortressHud>(FindObjectsInactive.Include);
        var arena = Object.FindAnyObjectByType<FortressArena>();
        if (Application.isPlaying) { Debug.LogWarning("플레이를 멈춘 뒤 적용하세요. 플레이 중 변경은 정지할 때 사라집니다."); return; }
        if (!hud) { Debug.LogError("FortressHud가 있는 전장 씬을 여세요."); return; }
        if (!NeedsAnyStep(hud)) { Debug.LogWarning("이미 최신 HUD 레이아웃입니다.", hud); return; }
        Undo.RegisterFullObjectHierarchyUndo(hud.gameObject, "Apply HUD mockup layout");
        if (NeedsMockup(hud)) Apply(hud, arena && arena.rules ? arena.rules.handSize : 5);
        if (NeedsChargeLayout(hud)) ApplyChargeLayout(hud);
        if (NeedsHealthGauge(hud)) ApplyHealthGauge(hud);
        if (NeedsRewardPanel(hud)) ApplyRewardPanel(hud);
        if (NeedsStageBanner(hud)) ApplyStageBanner(hud);
        if (NeedsLastPowerMarker(hud)) ApplyLastPowerMarker(hud);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Debug.Log("HUD 레이아웃을 적용했습니다. Ctrl+Z로 되돌리거나 씬을 저장하세요.", hud);
    }

    // Space-charge controls: the angle / power sliders go away, the top-left character panel moves into the
    // bottom-left box, and movement and power are shown as gauges.
    public static void ApplyChargeLayout(FortressHud hud)
    {
        var box = (RectTransform)(hud.angleSlider ? hud.angleSlider.transform.parent : hud.aimText.transform.parent);
        var oldPanel = hud.playerStatus.transform.parent;
        MoveTo(hud.playerPortrait.transform, box, 10, 10, 80, 100);
        MoveTo(hud.playerStatus.transform, box, 100, 8, 334, 54); hud.playerStatus.fontSize = 19;
        if (oldPanel != box && oldPanel.childCount == 0) Undo.DestroyObjectImmediate(oldPanel.gameObject);
        if (hud.characterPortrait) { Undo.DestroyObjectImmediate(hud.characterPortrait.gameObject); hud.characterPortrait = null; }
        if (hud.characterDetail) { Place((RectTransform)hud.characterDetail.transform, 100, 64, 334, 44); hud.characterDetail.fontSize = 15; }
        if (hud.angleSlider) { Undo.DestroyObjectImmediate(hud.angleSlider.gameObject); hud.angleSlider = null; }
        if (hud.powerSlider) { Undo.DestroyObjectImmediate(hud.powerSlider.gameObject); hud.powerSlider = null; }

        (hud.moveFill, hud.moveText) = Gauge(box, "Movement Gauge", "이동", 116, new Color(.22f, .8f, .48f), null);
        (hud.powerFill, _) = Gauge(box, "Power Gauge", "위력", 144, new Color(1, .72f, .2f), hud.powerText);
        Place((RectTransform)hud.aimText.transform, 10, 172, 200, 26); hud.aimText.fontSize = 17;

        MoveButton(box.Find("Move Left"), box, 10, 204, 48, 40);
        MoveButton(box.Find("Move Right"), box, 62, 204, 48, 40);
        MoveButton(hud.jumpButton.transform, box, 114, 204, 70, 40);
        MoveButton(hud.dropButton.transform, box, 188, 204, 96, 40);
        MoveButton(hud.fireButton.transform, box, 288, 204, 146, 40);
        var charge = hud.fireButton.GetComponent<FortressHoldButton>();
        if (!charge) charge = Undo.AddComponent<FortressHoldButton>(hud.fireButton.gameObject);
        charge.game = hud.game; charge.charge = true;
        var help = box.Find("Movement Help")?.GetComponent<Text>();
        if (help)
        {
            Place((RectTransform)help.transform, 10, 248, 424, 20);
            help.text = "A/D 이동 · W 점프 · S 내려가기 · ↑↓ 각도 · Space 길게 눌러 발사 · Tab 턴 종료"; help.fontSize = 12;
        }
        EditorUtility.SetDirty(hud);
    }

    // White tick on the power gauge track at the previous shot's power; FortressHud slides it along.
    public static void ApplyLastPowerMarker(FortressHud hud)
    {
        var marker = B.Picture(hud.powerFill.transform.parent, "Last Power", 0, 0, 4, 22);
        marker.color = Color.white; marker.preserveAspect = false;
        var rect = (RectTransform)marker.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
        Created(marker.gameObject);
        marker.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .8f);
        hud.lastPowerMarker = rect;
        marker.gameObject.SetActive(false);
        EditorUtility.SetDirty(hud);
    }

    // A dark band across the middle of the map announcing the stage; it never blocks clicks.
    public static void ApplyStageBanner(FortressHud hud)
    {
        var band = B.Picture(hud.battlePanel.transform, "Stage Banner", 0, 300, 1600, 150);
        band.color = new Color(0, 0, 0, .6f); band.preserveAspect = false;
        Created(band.gameObject);
        var group = band.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = group.interactable = false;
        hud.stageBanner = group;
        hud.stageBannerTitle = B.Label(band.transform, "Title", "스테이지 1", 0, 18, 1600, 72, 52);
        hud.stageBannerTitle.alignment = TextAnchor.MiddleCenter; hud.stageBannerTitle.color = new Color(1, .85f, .5f);
        hud.stageBannerSubtitle = B.Label(band.transform, "Subtitle", "검은 달의 성채 · 적 2명", 0, 94, 1600, 36, 22);
        hud.stageBannerSubtitle.alignment = TextAnchor.MiddleCenter;
        band.gameObject.SetActive(false);
        band.transform.SetAsLastSibling();
        EditorUtility.SetDirty(hud);
    }

    // Stage clear screen: three reward cards (outlined by rarity) and a skip button, above everything else.
    public static void ApplyRewardPanel(FortressHud hud)
    {
        var panel = B.Panel(hud.battlePanel.transform, "Stage Reward", 400, 170, 800, 480);
        Created(panel.gameObject);
        hud.rewardPanel = panel.gameObject;
        hud.rewardTitle = B.Label(panel.transform, "Title", "스테이지 1 클리어!", 0, 20, 800, 50, 32);
        hud.rewardTitle.alignment = TextAnchor.MiddleCenter;
        var subtitle = B.Label(panel.transform, "Subtitle", "덱에 추가할 카드를 고르세요 · 1 / 2 / 3 · 4 건너뛰기", 0, 74, 800, 30, 18);
        subtitle.alignment = TextAnchor.MiddleCenter;
        var cards = B.Rect(panel.transform, "Reward Cards", 55, 116, 690, 270);
        hud.rewardSlots = new FortressCardSlot[3];
        for (int i = 0; i < hud.rewardSlots.Length; i++)
        {
            var slot = hud.rewardSlots[i] = Card(cards, i, i * 245, 200, 270);
            slot.name = "Reward " + (i + 1);
            slot.GetComponent<Outline>().effectDistance = new Vector2(3, -3);
        }
        hud.rewardSkipButton = B.Button(panel.transform, "Skip", "건너뛰기 [4]", 290, 408, 220, 50, null);
        panel.gameObject.SetActive(false);
        panel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(hud);
    }

    // HP gauge above the movement gauge. Name, then defense / energy / buffs, sit beside a smaller portrait.
    public static void ApplyHealthGauge(FortressHud hud)
    {
        var box = (RectTransform)hud.playerStatus.transform.parent;
        Place((RectTransform)hud.playerPortrait.transform, 10, 8, 70, 74);
        Place((RectTransform)hud.playerStatus.transform, 90, 6, 344, 28); hud.playerStatus.fontSize = 19;
        if (hud.characterDetail) { Place((RectTransform)hud.characterDetail.transform, 90, 36, 344, 44); hud.characterDetail.fontSize = 15; }
        (hud.healthFill, hud.healthText) = Gauge(box, "Health Gauge", "체력", 88, new Color(.86f, .25f, .25f), null);
        hud.healthText.text = "120 / 120";
        hud.healthFill.transform.parent.parent.SetSiblingIndex(hud.moveFill ? hud.moveFill.transform.parent.parent.GetSiblingIndex() : box.childCount - 1);
        EditorUtility.SetDirty(hud);
    }

    // Label, dark track with a horizontal fill, and a value text on the right. Reuses valueText when given.
    static (Image fill, Text value) Gauge(RectTransform parent, string name, string title, float y, Color color, Text valueText)
    {
        var row = B.Rect(parent, name, 10, y, 424, 24);
        Created(row.gameObject);
        B.Label(row, "Title", title, 0, 0, 56, 24, 16);
        var track = B.Panel(row, "Track", 60, 5, 270, 14); track.color = new Color(.02f, .04f, .06f);
        var fill = B.Picture(track.transform, "Fill", 0, 0, 270, 14);
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.preserveAspect = false; fill.color = color;
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 1;
        if (valueText) MoveTo(valueText.transform, row, 336, 0, 88, 24);
        else valueText = B.Label(row, "Value", "10.0 / 10 m", 336, 0, 88, 24, 15);
        valueText.fontSize = 15; valueText.alignment = TextAnchor.MiddleRight;
        return (fill, valueText);
    }

    static void MoveTo(Transform item, Transform parent, float x, float y, float w, float h)
    {
        if (item.parent != parent) Undo.SetTransformParent(item, parent, "Move HUD item");
        Place((RectTransform)item, x, y, w, h);
    }

    public static void Apply(FortressHud hud, int handSize)
    {
        var battle = hud.battlePanel.transform;
        var top = hud.turnRoot.parent;
        var controls = (RectTransform)hud.angleSlider.transform.parent;

        // Top bar: money, currency and a settings button; the old top buttons move into the settings menu.
        Counter(top, "Money", "돈 0", 1206, 104);
        Counter(top, "Currency", "재화 0", 1318, 112);
        var settings = B.Button(top, "Settings", "설정", 1438, 8, 116, 52, hud.ToggleSettings);
        Created(settings.gameObject);
        var menu = B.Panel(battle, "Settings Menu", 1384, 86, 200, 124);
        Created(menu.gameObject);
        MoveButton(top.Find("Selection"), menu.transform, 10, 10, 180, 46);
        MoveButton(top.Find("Restart"), menu.transform, 10, 66, 180, 46);
        menu.gameObject.SetActive(false);
        hud.settingsPanel = menu.gameObject;

        // Bottom left: the old control strip becomes the character info box above the relic row.
        controls.name = "Character Info";
        Place(controls, 16, 548, 444, 272);
        hud.characterPortrait = B.Picture(controls, "Portrait", 10, 10, 72, 92);
        hud.characterDetail = B.Label(controls, "Detail", "궁수\nHP 120 / 120 · 방어도 0\n에너지 3 / 3\n다음 공격: 없음", 92, 8, 342, 96, 16);
        Created(hud.characterPortrait.gameObject); Created(hud.characterDetail.gameObject);
        Place((RectTransform)hud.aimText.transform, 10, 108, 100, 30); hud.aimText.fontSize = 18;
        Place((RectTransform)hud.angleSlider.transform, 112, 108, 240, 30);
        Place((RectTransform)hud.powerText.transform, 10, 144, 100, 30); hud.powerText.fontSize = 18;
        Place((RectTransform)hud.powerSlider.transform, 112, 144, 240, 30);
        MoveButton(controls.Find("Move Left"), controls, 10, 184, 48, 44);
        MoveButton(controls.Find("Move Right"), controls, 62, 184, 48, 44);
        MoveButton(hud.jumpButton.transform, controls, 114, 184, 70, 44);
        MoveButton(hud.dropButton.transform, controls, 188, 184, 96, 44);
        MoveButton(hud.fireButton.transform, controls, 288, 184, 146, 44); hud.attackText.fontSize = 16;
        var help = controls.Find("Movement Help")?.GetComponent<Text>();
        if (help)
        {
            Place((RectTransform)help.transform, 10, 236, 424, 28);
            help.text = "A/D 이동 · W 점프 · S 내려가기 · Space 발사 · Tab 턴 종료"; help.fontSize = 13;
        }

        var relics = B.Panel(battle, "Relics", 16, 828, 444, 58);
        Created(relics.gameObject);
        B.Label(relics.transform, "Title", "유물", 12, 12, 64, 34, 20);
        for (int i = 0; i < 5; i++)
        {
            var slot = B.Panel(relics.transform, "Relic Slot " + (i + 1), 84 + i * 70, 9, 58, 40);
            slot.color = ArtColor;
        }

        // Middle: message and card status above the hand.
        var message = (RectTransform)hud.messageText.transform.parent;
        Place(message, 476, 520, 827, 36);
        Place((RectTransform)hud.messageText.transform, 12, 3, 803, 30); hud.messageText.fontSize = 17;
        hud.cardStatus = B.Label(battle, "Card Status", "에너지 3/3 · 방어도 0 · 다음 공격: 없음", 476, 560, 827, 30, 16);
        hud.cardStatus.alignment = TextAnchor.MiddleCenter;
        hud.cardStatus.gameObject.AddComponent<Shadow>();
        Created(hud.cardStatus.gameObject);

        var hand = B.Rect(battle, "Card Hand", 476, 596, 827, 260);
        Created(hand.gameObject);
        int slots = Mathf.Max(1, handSize);
        float gap = 13, width = (827 - gap * (slots - 1)) / slots;
        hud.cardSlots = new FortressCardSlot[slots];
        for (int i = 0; i < slots; i++) hud.cardSlots[i] = Card(hand, i, i * (width + gap), width, 260);
        hud.cardButtons = null;

        // Right: deck, discard and the end-turn button.
        hud.deckText = Pile(battle, "Deck", "내 덱", "남은 장수", 1319);
        hud.discardText = Pile(battle, "Discard", "버린 카드", "장", 1459);
        MoveButton(hud.endTurnButton.transform, battle, 1319, 762, 265, 94);
        var endLabel = hud.endTurnButton.GetComponentInChildren<Text>();
        endLabel.text = "턴 종료 [Tab]"; endLabel.fontSize = 26;

        // Popups stay above the new widgets.
        menu.transform.SetAsLastSibling();
        hud.resultPanel.transform.SetAsLastSibling();
        EditorUtility.SetDirty(hud);
    }

    static FortressCardSlot Card(Transform hand, int index, float x, float w, float h)
    {
        var frame = B.Panel(hand, "Card " + (index + 1), x, 0, w, h);
        frame.color = CardColor;
        var view = frame.gameObject.AddComponent<FortressCardSlot>();
        view.button = frame.gameObject.AddComponent<Button>(); view.button.targetGraphic = frame;
        var colors = view.button.colors; colors.highlightedColor = new Color(.8f, .9f, 1); colors.disabledColor = new Color(.45f, .45f, .45f, .75f);
        view.button.colors = colors;
        view.button.navigation = new Navigation { mode = Navigation.Mode.None };
        var badge = B.Picture(frame.transform, "Cost Badge", 8, 8, 34, 34); badge.color = BadgeColor; badge.preserveAspect = false;
        view.cost = B.Label(badge.transform, "Cost", "1", 0, 0, 34, 34, 22);
        view.cost.alignment = TextAnchor.MiddleCenter; view.cost.color = new Color(.08f, .06f, .03f); view.cost.fontStyle = FontStyle.Bold;
        view.title = B.Label(frame.transform, "Title", "카드 이름", 46, 10, w - 54, 28, 15);
        view.title.alignment = TextAnchor.MiddleCenter; view.title.fontStyle = FontStyle.Bold;
        var art = B.Picture(frame.transform, "Art (drop a sprite here)", 8, 48, w - 16, 112); art.color = ArtColor; art.preserveAspect = false;
        view.description = B.Label(frame.transform, "Description", "카드 설명", 10, 166, w - 20, h - 176, 13);
        view.description.alignment = TextAnchor.UpperCenter;
        view.description.horizontalOverflow = HorizontalWrapMode.Wrap;
        return view;
    }

    static Text Pile(Transform parent, string name, string title, string caption, float x)
    {
        var panel = B.Panel(parent, name, x, 596, 125, 150);
        Created(panel.gameObject);
        B.Label(panel.transform, "Title", title, 0, 14, 125, 28, 17).alignment = TextAnchor.MiddleCenter;
        var count = B.Label(panel.transform, "Count", "0", 0, 50, 125, 60, 40);
        count.alignment = TextAnchor.MiddleCenter;
        B.Label(panel.transform, "Caption", caption, 0, 112, 125, 26, 13).alignment = TextAnchor.MiddleCenter;
        return count;
    }

    static void Counter(Transform parent, string name, string text, float x, float w)
    {
        var panel = B.Panel(parent, name, x, 8, w, 52);
        Created(panel.gameObject);
        B.Label(panel.transform, "Value", text, 0, 0, w, 52, 18).alignment = TextAnchor.MiddleCenter;
    }

    static void MoveButton(Transform button, Transform parent, float x, float y, float w, float h)
    {
        if (!button) return;
        if (button.parent != parent) Undo.SetTransformParent(button, parent, "Move HUD button");
        Place((RectTransform)button, x, y, w, h);
        var label = button.Find("Label") as RectTransform;
        if (label) Place(label, 8, 4, w - 16, h - 8);
    }

    static void Place(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
    }

    static void Created(GameObject created) => Undo.RegisterCreatedObjectUndo(created, "Apply HUD mockup layout");
}
