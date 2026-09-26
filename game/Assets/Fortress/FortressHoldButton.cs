using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniFortress
{
    public sealed class FortressHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public FortressGame game;
        public float direction;
        bool held;
        public void OnPointerDown(PointerEventData data) { held = true; game.SetMoveInput(direction); }
        public void OnPointerUp(PointerEventData data) => Release();
        public void OnPointerExit(PointerEventData data) => Release();
        void OnDisable() => Release();
        void Release() { if (held && game) game.SetMoveInput(0); held = false; }
    }
}
