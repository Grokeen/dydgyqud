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

        public void Bind()
        {
            BindCards();
            classCardTemplate.gameObject.SetActive(false);
            turnTemplate.gameObject.SetActive(false);
            healthTemplate.gameObject.SetActive(false);
            battlefield.texture = game.BattlefieldTexture;
            angleSlider.minValue = game.Rules.angleLimits.x; angleSlider.maxValue = game.Rules.angleLimits.y;
            powerSlider.minValue = game.Rules.powerLimits.x; powerSlider.maxValue = game.Rules.powerLimits.y;
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
                var health = Instantiate(healthTemplate, healthRoot); health.gameObject.SetActive(true); healthBars.Add(health);
            }
        }

        void LateUpdate()
        {
            if (!game || !game.Ready) return;
            bool selecting = game.IsSelecting;
            selectionPanel.SetActive(selecting); battlePanel.SetActive(!selecting);
            resultPanel.SetActive(game.IsFinished);
            if (selecting)
            {
                for (int i = 0; i < cards.Count; i++) cards[i].Show(game.Classes[i], game.SelectedClass == i);
                return;
            }
            var player = game.GetActor(0);
            playerPortrait.sprite = player.portrait;
            playerStatus.text = $"{player.name}\nHP {player.hp} / {player.maxHp}\n이동 {game.MovementRemaining:0.0} / {game.PlayerMovementLimit:0.#} m";
            turnText.text = $"턴 {game.Round}";
            enemyStatus.text = $"모든 적 처치\n남은 적 {game.LivingEnemies} / {game.FighterCount - 1}명";
            messageText.text = resultText.text = game.Message;
            attackText.text = game.HasAttacked ? "공격 완료" : game.CurrentAttackName + " [Space]";
            aimText.text = $"각도 {game.Angle:0}°"; powerText.text = $"위력 {game.Power:0.0}";
            angleSlider.SetValueWithoutNotify(game.Angle); powerSlider.SetValueWithoutNotify(game.Power);
            angleSlider.interactable = powerSlider.interactable = game.CanAim;
            fireButton.interactable = game.CanFire; endTurnButton.interactable = game.CanEndTurn;
            jumpButton.interactable = game.CanJump; dropButton.interactable = game.CanEndTurn;
            ShowCards();
            for (int i = 0; i < turns.Count; i++)
            {
                var actor = game.GetActor(i);
                turns[i].Show(actor.portrait, actor.hp > 0 ? actor.name : "처치", actor.hp, actor.maxHp, game.CurrentActor == i && !game.IsFinished);
                var bar = healthBars[i];
                Vector3 view = game.WorldCamera.WorldToViewportPoint(actor.head);
                bar.gameObject.SetActive(actor.hp > 0 && view.z > 0);
                var rect = (RectTransform)bar.transform;
                rect.pivot = new Vector2(.5f, 0);
                rect.anchorMin = rect.anchorMax = new Vector2(view.x, view.y); rect.anchoredPosition = Vector2.zero;
                bar.Show(null, $"{actor.hp}/{actor.maxHp}", actor.hp, actor.maxHp, false);
            }
        }

        void BindCards()
        {
            if (!cardStatus || cardButtons == null || cardButtons.Length == 0) BuildCardPanel();
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
            cardStatus.text = $"에너지 {game.CardEnergy}/{game.MaxCardEnergy} · 덱 {game.DrawPileCount} · 버림 {game.DiscardPileCount}" +
                $" · 방어도 {game.Block} · 다음 공격: {game.PendingAttackBuffs}";
            for (int i = 0; i < cardButtons.Length; i++)
            {
                bool has = i < game.HandCount;
                cardButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                cardLabels[i].text = $"[{i + 1}] {game.CardTitle(i)} ({game.CardCost(i)})\n{game.CardDescription(i)}";
                cardButtons[i].interactable = game.CanPlayCard(i);
            }
        }
    }
}
