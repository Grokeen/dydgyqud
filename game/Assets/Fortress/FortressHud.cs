using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFortress
{
    [DefaultExecutionOrder(100)]
    public sealed class FortressHud : MonoBehaviour
    {
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

        public void ToggleSettings() { if (settingsPanel) settingsPanel.SetActive(!settingsPanel.activeSelf); }

        public void Bind()
        {
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
        }

        void LateUpdate()
        {
            if (!game || !game.Ready || game.FighterCount == 0) return;
            bool selecting = game.IsSelecting;
            selectionPanel.SetActive(selecting); battlePanel.SetActive(!selecting);
            bool rewardScreen = rewardPanel && game.IsChoosingReward;
            if (rewardPanel) rewardPanel.SetActive(rewardScreen);
            resultPanel.SetActive(game.IsFinished && !rewardScreen);
            if (rewardScreen) ShowRewards();
            UpdateStageBanner(selecting);
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
            if (healthFill) healthFill.fillAmount = player.maxHp > 0 ? player.hp / (float)player.maxHp : 0;
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
            enemyStatus.text = $"스테이지 {game.Stage} · 모든 적 처치\n남은 적 {game.LivingEnemies} / {game.EnemyCount}명";
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
            if (!rewardPanel) return;
            for (int i = 0; i < rewardSlots.Length; i++)
            {
                int choice = i;
                rewardSlots[i].button.onClick.AddListener(() => game.RequestChooseReward(choice));
            }
            if (rewardSkipButton) rewardSkipButton.onClick.AddListener(game.RequestSkipReward);
        }

        void ShowRewards()
        {
            if (rewardTitle) rewardTitle.text = $"스테이지 {game.Stage} 클리어!";
            for (int i = 0; i < rewardSlots.Length; i++)
            {
                var slot = rewardSlots[i];
                bool has = i < game.RewardCount;
                slot.gameObject.SetActive(has);
                if (!has) continue;
                slot.Show(game.RewardTitle(i), game.RewardCost(i), game.RewardDescription(i), true);
                var outline = slot.GetComponent<Outline>();
                int rarity = (int)game.RewardRarity(i);
                if (outline && rarity < rarityColors.Length) outline.effectColor = rarityColors[rarity];
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
                { cardSlots[i].Show(game.CardTitle(i), game.CardCost(i), game.CardDescription(i), game.CanPlayCard(i)); continue; }
                cardLabels[i].text = $"[{i + 1}] {game.CardTitle(i)} ({game.CardCost(i)})\n{game.CardDescription(i)}";
                cardButtons[i].interactable = game.CanPlayCard(i);
            }
        }
    }
}
