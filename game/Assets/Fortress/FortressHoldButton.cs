using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniFortress
{
    public sealed class FortressHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public FortressGame game;
        public float direction;
        [Tooltip("켜면 이동 대신 누르는 동안 위력을 모으고, 떼면 발사합니다.")]
        public bool charge;
        bool held;
        public void OnPointerDown(PointerEventData data)
        {
            held = true;
            if (charge) game.RequestBeginCharge(); else game.SetMoveInput(direction);
        }
        public void OnPointerUp(PointerEventData data) => Release(true);
        // Leaving the button stops movement, but a charge only ends when the pointer is released.
        public void OnPointerExit(PointerEventData data) { if (!charge) Release(false); }
        void OnDisable() => Release(false);
        void Release(bool pointerUp)
        {
            if (held && game)
            {
                if (!charge) game.SetMoveInput(0);
                else if (pointerUp) game.RequestReleaseCharge();
            }
            held = false;
        }
    }
}
