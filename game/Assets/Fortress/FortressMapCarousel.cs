using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MiniFortress
{
    // 코덱스orig: 기존 HUD 파일을 수정하지 않고 메인 메뉴 맵 카드를 3장 캐러셀로 연출합니다.
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class FortressMapCarousel : MonoBehaviour
    {
        const float MapGap = 12f;
        const float CardY = -508f;
        const float AnimationTime = .2f;

        sealed class CardState
        {
            public RectTransform rect;
            public CanvasGroup canvasGroup;
            public bool visible;
            public Vector2 positionVelocity;
            public Vector3 scaleVelocity;
            public float alphaVelocity;
        }

        static void AttachToHudAfterSceneLoad()
        {
            if (!Application.isPlaying) return;
            var hud = Object.FindFirstObjectByType<FortressHud>();
            if (hud && !hud.GetComponent<FortressMapCarousel>())
                hud.gameObject.AddComponent<FortressMapCarousel>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install() => AttachToHudAfterSceneLoad();

        FortressHud hud;
        Transform cachedFrame;
        CardState[] cards;

        void Awake() => hud = GetComponent<FortressHud>();

        void Update()
        {
            if (!IsMapMenuOpen()) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            int direction = 0;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) direction = -1;
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) direction = 1;
            if (direction == 0) return;

            int count = hud.game.MapCount;
            if (count < 2) return;
            int next = (hud.game.SelectedMap + direction + count) % count;
            hud.game.SelectMap(next);
        }

        void LateUpdate()
        {
            if (!IsMapMenuOpen()) return;
            Transform menu = hud.selectionPanel.transform.parent.Find("Main Menu");
            Transform frame = menu ? menu.Find("Menu Frame") : null;
            if (!frame) return;

            EnsureCards(frame);
            int selected = hud.game.SelectedMap;
            for (int i = 0; i < cards.Length; i++) AnimateCard(i, selected);
        }

        bool IsMapMenuOpen()
        {
            if (!hud || !hud.game || !hud.game.Ready || !hud.selectionPanel || !hud.game.IsSelecting) return false;
            Transform menuRoot = hud.selectionPanel.transform.parent;
            if (!menuRoot) return false;
            Transform menu = menuRoot.Find("Main Menu");
            return menu && menu.gameObject.activeSelf;
        }

        void EnsureCards(Transform frame)
        {
            int count = hud.game.MapCount;
            if (cachedFrame == frame && cards != null && cards.Length == count) return;

            cachedFrame = frame;
            cards = new CardState[count];
            for (int i = 0; i < count; i++)
            {
                Transform card = frame.Find("Map Choice " + i);
                if (!card) continue;
                var group = card.GetComponent<CanvasGroup>();
                if (!group) group = card.gameObject.AddComponent<CanvasGroup>();
                cards[i] = new CardState { rect = card.GetComponent<RectTransform>(), canvasGroup = group };
            }
        }

        void AnimateCard(int index, int selected)
        {
            CardState state = cards[index];
            if (state == null || !state.rect || !state.canvasGroup) return;

            int count = cards.Length;
            int distance = (index - selected + count) % count;
            int slot = distance == 0 ? 0 : distance == 1 ? 1 : distance == count - 1 ? -1 : int.MinValue;
            if (slot == int.MinValue)
            {
                if (state.visible) state.rect.gameObject.SetActive(false);
                state.visible = false;
                return;
            }

            var frameRect = cachedFrame as RectTransform;
            float centerX = frameRect
                ? (frameRect.rect.width - state.rect.rect.width) * .5f
                : state.rect.anchoredPosition.x;
            float targetX = centerX + slot * (state.rect.rect.width + MapGap);
            float targetAlpha = slot == 0 ? 1f : .48f;
            float targetScale = slot == 0 ? 1.04f : .86f;
            if (!state.visible)
            {
                state.rect.gameObject.SetActive(true);
                state.rect.anchoredPosition = new Vector2(targetX + (slot == 0 ? 0 : slot * 42f), CardY);
                state.rect.localScale = Vector3.one * (slot == 0 ? .94f : .8f);
                state.canvasGroup.alpha = 0;
                state.positionVelocity = Vector2.zero;
                state.scaleVelocity = Vector3.zero;
                state.alphaVelocity = 0;
                state.visible = true;
            }

            Vector2 targetPosition = new Vector2(targetX, CardY);
            Vector3 targetScaleVector = Vector3.one * targetScale;
            state.rect.anchoredPosition = Vector2.SmoothDamp(state.rect.anchoredPosition, targetPosition,
                ref state.positionVelocity, AnimationTime);
            state.rect.localScale = Vector3.SmoothDamp(state.rect.localScale, targetScaleVector,
                ref state.scaleVelocity, AnimationTime);
            state.canvasGroup.alpha = Mathf.SmoothDamp(state.canvasGroup.alpha, targetAlpha,
                ref state.alphaVelocity, AnimationTime);
            state.canvasGroup.interactable = true;
            state.canvasGroup.blocksRaycasts = true;
        }
    }
}
