using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MiniFortress
{
    [DefaultExecutionOrder(100)]
    public sealed class FortressHud : MonoBehaviour
    {
        const string GameTitle = "월하요새: 최후의 궤적";
        // Main menu tagline/description when the selected map has none of its own (FortressMapDefinition.menuTagline ...).
        const string DefaultMenuTagline = "한 발의 궤적이 성벽의 운명을 가른다";
        const string DefaultMenuDescription = "전략 카드로 전황을 바꾸고, 각도를 겨눠 월하요새를 지켜라.";
        const float AimWheelUnitsPerNotch = 120f;
        const float AimWheelDegreesPerNotch = 3f;
        const float CameraZoomUnitsPerNotch = 120f;
        const float CameraZoomFactorPerNotch = .9f;
        const float MinCameraZoomSize = 8f;
        const float MaxCameraZoomSize = 48f;
        public FortressGame game;
        public RawImage battlefield;
        public GameObject selectionPanel, battlePanel, resultPanel;
        public Text playerStatus, turnText, enemyStatus, messageText, resultText, attackText, aimText, powerText;
        public Image playerPortrait;
        public Slider angleSlider, powerSlider;
        public Button fireButton, endTurnButton, jumpButton, dropButton;
        public Transform classCardRoot, turnRoot;
        public RectTransform healthRoot;
        public FortressClassCard classCardTemplate;
        public FortressActorHud turnTemplate, healthTemplate;
        readonly List<FortressClassCard> cards = new List<FortressClassCard>();
        readonly List<FortressActorHud> turns = new List<FortressActorHud>();
        readonly List<FortressActorHud> healthBars = new List<FortressActorHud>();
        [Header("Cards (비워 두면 실행 시 텍스트 패널을 만듭니다)")]
        public Text cardStatus;
        public Button[] cardButtons;
        Text[] cardLabels;
        [Header("Mockup layout (비워 두면 위의 텍스트 카드 방식)")]
        public FortressCardSlot[] cardSlots;
        public Text characterDetail, deckText, discardText;
        public Image characterPortrait;
        public GameObject settingsPanel;
        [Header("Stage banner (새 스테이지 시작 시 잠깐 표시)")]
        public CanvasGroup stageBanner;
        public Text stageBannerTitle, stageBannerSubtitle;
        [Min(.1f)] public float stageBannerSeconds = 2.5f;
        int bannerStage;
        bool wasSelecting = true;
        float bannerTime;
        [Header("Stage reward (비워 두면 결과 패널에 글자로 표시)")]
        public GameObject rewardPanel;
        public Text rewardTitle;
        public FortressCardSlot[] rewardSlots;
        public Button rewardSkipButton;
        public Color[] rarityColors = { new Color(.62f, .66f, .7f), new Color(.3f, .6f, 1), new Color(.72f, .4f, 1), new Color(1, .78f, .25f) };
        [Header("Gauges")]
        public Image healthFill, moveFill, powerFill;
        [Tooltip("위력 게이지 위에 이전 발사 위력을 표시하는 눈금")]
        public RectTransform lastPowerMarker;
        public Text healthText, moveText;
        float displayedPlayerHealth;
        bool playerHealthInitialized;
        GameObject mainMenuPanel;
        Text menuTagline, menuDescription;
        readonly List<Image> mapChoiceImages = new List<Image>();
        readonly List<Outline> mapChoiceOutlines = new List<Outline>();
        Text arrowCountLabel;
        Button arrowCountDown, arrowCountUp;
        Sprite[] mapPreviewSprites;
        float aimWheelRemainder, cameraZoomWheelRemainder;

        public void ToggleSettings() { if (settingsPanel) settingsPanel.SetActive(!settingsPanel.activeSelf); }

        public void Bind()
        {
            LoadMapPreviewSprites();
            BindCards(); BindRewards();
            classCardTemplate.gameObject.SetActive(false);
            turnTemplate.gameObject.SetActive(false);
            healthTemplate.gameObject.SetActive(false);
            battlefield.texture = game.BattlefieldTexture;
            if (angleSlider) { angleSlider.minValue = game.Rules.angleLimits.x; angleSlider.maxValue = game.Rules.angleLimits.y; }
            if (powerSlider) { powerSlider.minValue = game.Rules.powerLimits.x; powerSlider.maxValue = game.Rules.powerLimits.y; }
            for (int i = 0; i < game.Classes.Length; i++)
            {
                var card = Instantiate(classCardTemplate, classCardRoot);
                card.gameObject.SetActive(true); int choice = i;
                card.button.onClick.AddListener(() => game.SelectClass(choice));
                cards.Add(card);
            }
            for (int i = 0; i < game.FighterCount; i++)
            {
                var turn = Instantiate(turnTemplate, turnRoot); turn.gameObject.SetActive(true); turns.Add(turn);
                // Health bars float over enemies only; the player's health is in the character box.
                var bar = i == 0 ? null : Instantiate(healthTemplate, healthRoot);
                if (bar && bar.label) bar.label.horizontalOverflow = HorizontalWrapMode.Overflow; // room for bleed
                healthBars.Add(bar);
            }
            var gameTitle = battlePanel.transform.Find("Top Bar/Game Title")?.GetComponent<Text>();
            if (gameTitle) gameTitle.text = GameTitle;
            BuildArrowCountControls();
            BuildMainMenu();
        }

        // 코덱스code(CodexCode): Unity 직렬화 중 Resources.Load를 호출하면 에디터가 컴포넌트를 복원하는 동안 예외가 납니다.
        // 씬 런타임 초기화(Bind)에서 지연 로드하고, 누락된 리소스는 카드의 기본 배경으로 표시합니다.
        void LoadMapPreviewSprites()
        {
            // CodexCode: take each thumbnail from its map preset so the restored original battlefield is included too.
            mapPreviewSprites = new Sprite[game.MapCount];
            for (int i = 0; i < mapPreviewSprites.Length; i++) mapPreviewSprites[i] = game.MapPreviewSprite(i);
        }

        // 코덱스code(CodexCode): 궁수 본사격의 발사량을 1발 단위로 정하는 UI입니다. 소모량과 실제 화살 발사는 FortressGame이 처리합니다.
        void BuildArrowCountControls()
        {
            if (!fireButton) return;
            var fireRect = (RectTransform)fireButton.transform;
            float x = fireRect.anchoredPosition.x;
            float y = -fireRect.anchoredPosition.y - 30;
            float width = fireRect.rect.width;
            var parent = fireButton.transform.parent;
            var labelRect = Box(parent, "Arrow Count", x, y, width, 24);
            arrowCountLabel = labelRect.gameObject.AddComponent<Text>();
            arrowCountLabel.font = messageText.font; arrowCountLabel.fontSize = 13;
            arrowCountLabel.color = new Color(.9f, .84f, .65f); arrowCountLabel.alignment = TextAnchor.MiddleCenter;
            arrowCountLabel.raycastTarget = false;
        }

        Button MenuControlButton(Transform parent, string name, string label, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var rect = Box(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.12f, .18f, .21f, .98f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action);
            MenuLabel(rect, "Label", label, 0, 0, width, height, 16, new Color(.96f, .91f, .79f), FontStyle.Bold);
            return button;
        }

        void BuildMainMenu()
        {
            var canvasRoot = selectionPanel.transform.parent;
            var overlayRect = Box(canvasRoot, "Main Menu", 0, 0, 1600, 900);
            overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(.5f, .5f); overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            mainMenuPanel = overlayRect.gameObject;
            var shade = mainMenuPanel.AddComponent<Image>();
            shade.color = new Color(.012f, .02f, .035f, .78f);

            var frame = Box(mainMenuPanel.transform, "Menu Frame", 0, 0, 1000, 840);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = Vector2.zero;
            var plate = frame.gameObject.AddComponent<Image>();
            plate.color = new Color(.025f, .045f, .06f, .97f);
            Image[] frameEdges = null;
            FortressUiFrame.Ensure(ref frameEdges, frame, "Menu Border");
            FortressUiFrame.Set(frameEdges, new Color(.75f, .52f, .21f, .95f), 2.5f);

            MenuLabel(frame, "Edition", "FORTRESS STRATEGY  ·  BALLISTIC COMBAT", 120, 54, 760, 28, 15,
                new Color(.79f, .64f, .38f), FontStyle.Bold);
            MenuLabel(frame, "Game Title", GameTitle, 80, 104, 840, 82, 50,
                new Color(.94f, .92f, .84f), FontStyle.Bold);
            menuTagline = MenuLabel(frame, "Tagline", DefaultMenuTagline, 120, 190, 760, 42, 24,
                new Color(.78f, .83f, .82f), FontStyle.Normal);
            MenuShape(frame, "Title Rule", 180, 255, 640, 2, new Color(.63f, .43f, .18f, .8f));
            menuDescription = MenuLabel(frame, "Description", DefaultMenuDescription,
                110, 282, 780, 54, 19, new Color(.73f, .78f, .79f), FontStyle.Normal);

            MenuFeature(frame, "01", "전략 카드", "전술을 고르고 에너지를 관리", 116);
            MenuFeature(frame, "02", "포물선 조준", "각도와 충전으로 한 발을 설계", 386);
            MenuFeature(frame, "03", "요새 수호", "움직이고 버티며 적을 격파", 656);
            MenuLabel(frame, "Map Heading", "전장을 선택하세요", 120, 474, 760, 30, 18,
                new Color(.82f, .72f, .5f), FontStyle.Bold);
            // FortressMapCarousel shows at most three cards and places them; size cards for three, whatever the map count.
            const float mapGap = 12, mapWidth = 260;
            int mapColumns = Mathf.Min(game.MapCount, 3);
            float mapStart = (1000 - (mapWidth * mapColumns + mapGap * (mapColumns - 1))) * .5f;
            for (int i = 0; i < game.MapCount; i++)
                CreateMapChoice(frame, i, mapStart + Mathf.Min(i, 2) * (mapWidth + mapGap), 508, mapWidth, 192);
            CreateMenuButton(frame, "출전 준비", 340, 710, 320, 58);
            MenuLabel(frame, "Footer", "맵과 캐릭터를 선택하고 전투를 시작하세요", 180, 778, 640, 26, 15,
                new Color(.56f, .64f, .66f), FontStyle.Normal);
            mainMenuPanel.transform.SetAsLastSibling();
        }

        void CreateMapChoice(Transform parent, int index, float x, float y, float width, float height)
        {
            var rect = Box(parent, "Map Choice " + index, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = game.SelectedMap == index ? new Color(.28f, .23f, .14f) : new Color(.07f, .1f, .115f, .96f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, .88f, .62f);
            colors.pressedColor = new Color(.78f, .68f, .48f);
            button.colors = colors;
            // 코덱스code(CodexCode) 작업 메모: Resources의 맵 미리보기 이미지를 카드 상단에 배치하고, 선택된 맵은 금색 테두리로 표시합니다.
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.92f, .69f, .3f, .95f);
            outline.effectDistance = new Vector2(2, -2);
            outline.enabled = game.SelectedMap == index;
            int choice = index;
            button.onClick.AddListener(() => game.SelectMap(choice));
            var previewRect = Box(rect, "Map Preview", 0, 0, width, 138);
            var preview = previewRect.gameObject.AddComponent<Image>();
            preview.sprite = mapPreviewSprites != null && index >= 0 && index < mapPreviewSprites.Length
                ? mapPreviewSprites[index] : null;
            preview.preserveAspect = true;
            preview.color = Color.white;
            preview.raycastTarget = false;
            MenuLabel(rect, "Name", game.MapName(index), 8, 140, width - 16, 24, 19,
                new Color(.94f, .92f, .84f), FontStyle.Bold);
            MenuLabel(rect, "Description", game.MapDescription(index), 8, 164, width - 16, 22, 13,
                new Color(.68f, .76f, .77f), FontStyle.Normal);
            mapChoiceImages.Add(image);
            mapChoiceOutlines.Add(outline);
        }

        void UpdateMapChoiceHighlight()
        {
            for (int i = 0; i < mapChoiceImages.Count; i++)
            {
                mapChoiceImages[i].color = game.SelectedMap == i
                    ? new Color(.28f, .23f, .14f) : new Color(.07f, .1f, .115f, .96f);
                mapChoiceOutlines[i].enabled = game.SelectedMap == i;
            }
            // The tagline and description above the map cards follow the selected map.
            int selected = game.SelectedMap;
            if (selected < 0 || selected >= game.MapCount) return;
            string tagline = game.MapMenuTagline(selected), description = game.MapMenuDescription(selected);
            if (menuTagline) menuTagline.text = string.IsNullOrWhiteSpace(tagline) ? DefaultMenuTagline : tagline;
            if (menuDescription) menuDescription.text = string.IsNullOrWhiteSpace(description) ? DefaultMenuDescription : description;
        }

        void CreateMenuButton(Transform parent, string caption, float x, float y, float width, float height)
        {
            var rect = Box(parent, "Start Button", x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.38f, .25f, .1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(.38f, .25f, .1f);
            colors.highlightedColor = new Color(.68f, .48f, .19f);
            colors.pressedColor = new Color(.27f, .19f, .09f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(CloseMainMenu);
            MenuLabel(rect, "Label", caption, 0, 0, width, height, 24, new Color(.98f, .94f, .84f), FontStyle.Bold);
        }

        void CloseMainMenu()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (selectionPanel) selectionPanel.SetActive(game && game.IsSelecting);
        }

        Text MenuLabel(Transform parent, string name, string value, float x, float y, float width, float height,
            int size, Color color, FontStyle style)
        {
            var rect = Box(parent, name, x, y, width, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = messageText.font;
            text.fontSize = size; text.fontStyle = style; text.color = color; text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Image MenuShape(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var rect = Box(parent, name, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }

        void MenuFeature(Transform parent, string number, string title, string detail, float x)
        {
            var card = Box(parent, "Feature " + number, x, 376, 228, 88);
            var background = card.gameObject.AddComponent<Image>();
            background.color = new Color(.07f, .1f, .115f, .92f); background.raycastTarget = false;
            MenuLabel(card, "Number", number, 12, 10, 34, 22, 13, new Color(.82f, .61f, .29f), FontStyle.Bold);
            MenuLabel(card, "Title", title, 10, 32, 208, 28, 18, new Color(.9f, .91f, .86f), FontStyle.Bold);
            MenuLabel(card, "Detail", detail, 8, 61, 212, 22, 11, new Color(.62f, .7f, .71f), FontStyle.Normal);
        }

        void LateUpdate()
        {
            if (!game || !game.Ready || game.FighterCount == 0) return;
            bool selecting = game.IsSelecting;
            bool onMainMenu = mainMenuPanel && mainMenuPanel.activeSelf;
            bool rewardScreen = rewardPanel && game.IsChoosingReward;
            UpdateMapChoiceHighlight();
            if (arrowCountLabel)
                arrowCountLabel.text = game.UsesArrows ? $"화살 {game.ArrowCount}/{game.MaxArrowCount} · 일제 발사" : "";
            selectionPanel.SetActive(selecting && !onMainMenu);
            battlePanel.SetActive(!selecting && !onMainMenu && !rewardScreen);
            if (rewardPanel) rewardPanel.SetActive(rewardScreen && !onMainMenu);
            resultPanel.SetActive(game.IsFinished && !rewardScreen && !onMainMenu);
            UpdateStageBanner(selecting);
            if (onMainMenu) return;
            if (rewardScreen) { ShowRewards(); return; }
            if (selecting)
            {
                for (int i = 0; i < cards.Count; i++) cards[i].Show(game.Classes[i], game.SelectedClass == i);
                return;
            }
            var player = game.GetActor(0);
            playerPortrait.sprite = player.portrait;
            // Gauges take over health and movement; the status text keeps whatever has no gauge.
            string quiver = game.UsesArrows ? $" · 화살 {game.ArrowCount}/{game.MaxArrowCount}"
                + (game.RecoveredArrowsNextTurn > 0 ? $" (+{game.RecoveredArrowsNextTurn})" : "") : "";
            playerStatus.text = healthFill ? player.name + quiver : moveFill ? $"{player.name}\nHP {player.hp} / {player.maxHp}"
                : $"{player.name}\nHP {player.hp} / {player.maxHp}\n이동 {game.MovementRemaining:0.0} / {game.PlayerMovementLimit:0.#} m";
            if (healthFill)
            {
                float targetHealth = player.maxHp > 0 ? Mathf.Clamp01(player.hp / (float)player.maxHp) : 0;
                if (!playerHealthInitialized)
                {
                    displayedPlayerHealth = targetHealth;
                    playerHealthInitialized = true;
                }
                else
                    displayedPlayerHealth = Mathf.MoveTowards(displayedPlayerHealth, targetHealth, Time.unscaledDeltaTime * 1.6f);
                healthFill.fillAmount = displayedPlayerHealth;
            }
            if (healthText) healthText.text = $"{player.hp} / {player.maxHp}";
            if (moveFill) moveFill.fillAmount = game.PlayerMovementLimit > 0 ? game.MovementRemaining / game.PlayerMovementLimit : 0;
            if (moveText) moveText.text = $"{game.MovementRemaining:0.0} / {game.PlayerMovementLimit:0.#} m";
            if (powerFill) powerFill.fillAmount = game.ChargeFraction;
            if (lastPowerMarker)
            {
                bool shown = game.LastPowerFraction >= 0;
                lastPowerMarker.gameObject.SetActive(shown);
                if (shown) lastPowerMarker.anchorMin = lastPowerMarker.anchorMax = new Vector2(game.LastPowerFraction, .5f);
            }
            if (characterPortrait) characterPortrait.sprite = player.portrait;
            if (characterDetail)
                characterDetail.text = healthFill
                    ? $"방어도 {game.Block} · 에너지 {game.CardEnergy} / {game.MaxCardEnergy}\n다음 공격: {game.PendingAttackBuffs}"
                    : $"{player.name}\nHP {player.hp} / {player.maxHp} · 방어도 {game.Block}\n" +
                      $"에너지 {game.CardEnergy} / {game.MaxCardEnergy}\n다음 공격: {game.PendingAttackBuffs}";
            turnText.text = $"턴 {game.Round}";
            var actingEnemy = game.CurrentActor > 0 ? game.GetActor(game.CurrentActor) : default;
            enemyStatus.text = $"Stage {game.Stage} · Enemies {game.LivingEnemies}/{game.EnemyCount}" + (game.CurrentActor > 0 ? $"\n{actingEnemy.name} · Bleed {actingEnemy.bleed}" : "");
            messageText.text = resultText.text = game.Message;
            attackText.text = game.HasAttacked ? "공격 완료" : game.IsCharging ? "위력 모으는 중…" : game.CurrentAttackName + " [Space]";
            aimText.text = $"각도 {game.Angle:0}°";
            // While charging show the live value; otherwise remind the player of the previous shot.
            powerText.text = !powerFill ? $"위력 {game.Power:0.0}"
                : game.IsCharging || game.LastPowerFraction < 0 ? $"{game.ChargeFraction * 100:0}%"
                : $"이전 {game.LastPowerFraction * 100:0}%";
            if (angleSlider) { angleSlider.SetValueWithoutNotify(game.Angle); angleSlider.interactable = game.CanAim; }
            if (powerSlider) { powerSlider.SetValueWithoutNotify(game.Power); powerSlider.interactable = game.CanAim; }
            fireButton.interactable = game.CanFire; endTurnButton.interactable = game.CanEndTurn;
            jumpButton.interactable = game.CanJump; dropButton.interactable = game.CanEndTurn;
            ShowCards();
            for (int i = 0; i < turns.Count; i++)
            {
                var actor = game.GetActor(i);
                turns[i].gameObject.SetActive(game.IsPresent(i));
                var bar = healthBars[i];
                if (bar)
                {
                    Vector3 view = game.WorldCamera.WorldToViewportPoint(actor.head);
                    bar.gameObject.SetActive(actor.hp > 0 && view.z > 0);
                    var rect = (RectTransform)bar.transform;
                    rect.pivot = new Vector2(.5f, 0);
                    rect.anchorMin = rect.anchorMax = new Vector2(view.x, view.y); rect.anchoredPosition = Vector2.zero;
                    string bleed = actor.bleed > 0 ? $"  출혈 {actor.bleed}/{game.BleedThresholdNow}" : "";
                    bar.Show(null, $"{actor.hp}/{actor.maxHp}{bleed}", actor.hp, actor.maxHp, false);
                }
                turns[i].Show(actor.portrait, actor.hp > 0 ? actor.name : "처치", actor.hp, actor.maxHp, game.CurrentActor == i && !game.IsFinished);
            }
        }

        void Update()
        {
            if (!game || !game.Ready || (settingsPanel && settingsPanel.activeSelf))
            {
                aimWheelRemainder = 0;
                cameraZoomWheelRemainder = 0;
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;
            var keyboard = Keyboard.current;
            bool controlHeld = keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
            if (controlHeld)
            {
                aimWheelRemainder = 0;
                cameraZoomWheelRemainder += mouse.scroll.ReadValue().y / CameraZoomUnitsPerNotch;
                int zoomNotches = cameraZoomWheelRemainder > 0
                    ? Mathf.FloorToInt(cameraZoomWheelRemainder) : Mathf.CeilToInt(cameraZoomWheelRemainder);
                if (zoomNotches == 0) return;
                cameraZoomWheelRemainder -= zoomNotches;
                game.AdjustCameraZoom(Mathf.Pow(CameraZoomFactorPerNotch, zoomNotches), MinCameraZoomSize, MaxCameraZoomSize);
                return;
            }

            cameraZoomWheelRemainder = 0;
            if (!game.CanAim)
            {
                aimWheelRemainder = 0;
                return;
            }
            aimWheelRemainder += mouse.scroll.ReadValue().y / AimWheelUnitsPerNotch;
            int notches = aimWheelRemainder > 0 ? Mathf.FloorToInt(aimWheelRemainder) : Mathf.CeilToInt(aimWheelRemainder);
            if (notches == 0) return;

            aimWheelRemainder -= notches;
            game.SetAngle(game.Angle + notches * AimWheelDegreesPerNotch);
        }
        // Shown when a battle starts and whenever the stage changes, then fades out.
        void UpdateStageBanner(bool selecting)
        {
            if (!stageBanner) return;
            if (!selecting && (wasSelecting || game.Stage != bannerStage))
            {
                bannerStage = game.Stage; bannerTime = stageBannerSeconds;
                if (stageBannerTitle) stageBannerTitle.text = $"스테이지 {game.Stage}";
                if (stageBannerSubtitle)
                    stageBannerSubtitle.text = (game.Stage == 1 ? "검은 달의 성채" : "다음 지역으로 진입") + $" · 적 {game.EnemyCount}명";
            }
            wasSelecting = selecting;
            if (selecting) bannerTime = 0;
            bannerTime = Mathf.Max(0, bannerTime - Time.unscaledDeltaTime);
            stageBanner.gameObject.SetActive(bannerTime > 0);
            stageBanner.alpha = Mathf.Clamp01(bannerTime / .6f);
        }
        void BindRewards()
        {
            if (!rewardPanel || rewardSlots == null) return;
            // The layout tool builds the panel inside Battle HUD, which LateUpdate hides on the reward screen;
            // lift it out (same 1600x900 frame, same position) so it stays visible, above the battle HUD.
            if (rewardPanel.transform.IsChildOf(battlePanel.transform))
            {
                rewardPanel.transform.SetParent(battlePanel.transform.parent, false);
                rewardPanel.transform.SetAsLastSibling();
                if (mainMenuPanel) mainMenuPanel.transform.SetAsLastSibling();
            }
            for (int i = 0; i < rewardSlots.Length; i++)
            {
                int choice = i;
                if (rewardSlots[i] && rewardSlots[i].button) rewardSlots[i].button.onClick.AddListener(() => game.RequestChooseReward(choice));
            }
            if (rewardSkipButton) rewardSkipButton.onClick.AddListener(game.RequestSkipReward);
        }

        void ShowRewards()
        {
            if (rewardTitle) rewardTitle.text = $"스테이지 {game.Stage} 클리어!";
            if (rewardSlots == null) return;
            for (int i = 0; i < rewardSlots.Length; i++)
            {
                var slot = rewardSlots[i];
                if (!slot) continue;
                bool has = i < game.RewardCount;
                slot.gameObject.SetActive(has);
                if (!has) continue;
                slot.Show(game.RewardTitle(i), game.RewardCost(i), game.RewardDescription(i), true, game.RewardArtwork(i));
                var outline = slot.GetComponent<Outline>();
                int rarity = (int)game.RewardRarity(i);
                if (outline && rarity >= 0 && rarity < rarityColors.Length) outline.effectColor = rarityColors[rarity];
            }
        }

        void BindCards()
        {
            if (cardSlots != null && cardSlots.Length > 0) cardButtons = System.Array.ConvertAll(cardSlots, slot => slot.button);
            else if (!cardStatus || cardButtons == null || cardButtons.Length == 0) BuildCardPanel();
            cardLabels = new Text[cardButtons.Length];
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int slot = i;
                cardButtons[i].onClick.AddListener(() => game.RequestPlayCard(slot));
                cardLabels[i] = cardButtons[i].GetComponentInChildren<Text>();
            }
        }

        // Text-only fallback, styled by cloning the existing end-turn button.
        void BuildCardPanel()
        {
            var panel = Box(battlePanel.transform, "Card Hand", 360, 590, 830, 100);
            var image = panel.gameObject.AddComponent<Image>(); image.color = new Color(.025f, .055f, .08f, .82f);
            image.raycastTarget = false;
            cardStatus = Instantiate(messageText, panel); cardStatus.name = "Card Status";
            Place((RectTransform)cardStatus.transform, 12, 4, 806, 26); cardStatus.fontSize = 16;
            int slots = Mathf.Max(1, game.Rules.handSize);
            float width = (806 - 6 * (slots - 1)) / (float)slots;
            cardButtons = new Button[slots];
            for (int i = 0; i < slots; i++)
            {
                var button = Instantiate(endTurnButton, panel); button.name = "Card " + (i + 1);
                button.onClick = new Button.ButtonClickedEvent(); // drop the cloned end-turn listener
                Place((RectTransform)button.transform, 12 + i * (width + 6), 32, width, 62);
                var label = (RectTransform)button.GetComponentInChildren<Text>().transform;
                label.anchorMin = Vector2.zero; label.anchorMax = Vector2.one; label.pivot = new Vector2(.5f, .5f);
                label.offsetMin = new Vector2(4, 2); label.offsetMax = new Vector2(-4, -2);
                var text = label.GetComponent<Text>(); text.fontSize = 14; text.verticalOverflow = VerticalWrapMode.Overflow;
                cardButtons[i] = button;
            }
        }
        static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            Place(rect, x, y, w, h); return rect;
        }
        static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }

        void ShowCards()
        {
            bool piles = deckText && discardText;
            if (piles) { deckText.text = game.DrawPileCount.ToString(); discardText.text = game.DiscardPileCount.ToString(); }
            if (cardStatus)
                cardStatus.text = $"에너지 {game.CardEnergy}/{game.MaxCardEnergy}" +
                    (piles ? "" : $" · 덱 {game.DrawPileCount} · 버림 {game.DiscardPileCount}") +
                    $" · 방어도 {game.Block} · 다음 공격: {game.PendingAttackBuffs}";
            for (int i = 0; i < cardButtons.Length; i++)
            {
                bool has = i < game.HandCount;
                cardButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                if (cardSlots != null && i < cardSlots.Length)
                { cardSlots[i].Show(game.CardTitle(i), game.CardCost(i), game.CardDescription(i), game.CanPlayCard(i), game.CardArtwork(i)); continue; }
                cardLabels[i].text = $"[{i + 1}] {game.CardTitle(i)} ({game.CardCost(i)})\n{game.CardDescription(i)}";
                cardButtons[i].interactable = game.CanPlayCard(i);
            }
        }
    }
}
