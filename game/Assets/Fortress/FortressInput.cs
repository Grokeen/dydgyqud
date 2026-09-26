using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniFortress
{
    public sealed class FortressInput : MonoBehaviour
    {
        public InputActionAsset actions;
        InputActionAsset runtimeActions;
        InputAction move, angle, power, jump, drop, fire, restart, back, confirm, select, first, second, endTurn;
        void OnEnable()
        {
            if (!actions) return;
            runtimeActions = Instantiate(actions);
            move = Get("Move"); angle = Get("Angle"); power = Get("Power");
            jump = Get("Jump"); drop = Get("Drop"); fire = Get("Fire"); restart = Get("Restart");
            back = Get("Back"); confirm = Get("Confirm"); select = Get("Select");
            first = Get("SelectFirst"); second = Get("SelectSecond"); endTurn = Get("EndTurn");
            runtimeActions.Enable();
        }
        InputAction Get(string name) => runtimeActions.FindAction("Battle/" + name, true);
        void OnDisable()
        {
            if (!runtimeActions) return;
            runtimeActions.Disable(); Destroy(runtimeActions);
        }
        public float Move => move?.ReadValue<float>() ?? 0;
        public float Angle => angle?.ReadValue<float>() ?? 0;
        public float Power => power?.ReadValue<float>() ?? 0;
        public bool Jump => jump?.WasPressedThisFrame() ?? false;
        public bool Drop => drop?.WasPressedThisFrame() ?? false;
        public bool Fire => fire?.WasPressedThisFrame() ?? false;
        public bool Restart => restart?.WasPressedThisFrame() ?? false;
        public bool Back => back?.WasPressedThisFrame() ?? false;
        public bool Confirm => confirm?.WasPressedThisFrame() ?? false;
        public bool EndTurn => endTurn?.WasPressedThisFrame() ?? false;
        public int SelectionDelta => select != null && select.WasPressedThisFrame() ? (int)Mathf.Sign(select.ReadValue<float>()) : 0;
        static readonly Key[] top = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
        static readonly Key[] pad = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5 };
        // Hand slots 1-5; read only during battle, so they do not clash with class selection keys.
        public int CardHotkey
        {
            get
            {
                var keyboard = Keyboard.current; if (keyboard == null) return -1;
                for (int i = 0; i < top.Length; i++)
                    if (keyboard[top[i]].wasPressedThisFrame || keyboard[pad[i]].wasPressedThisFrame) return i;
                return -1;
            }
        }
        public int DirectSelection => (first?.WasPressedThisFrame() ?? false) ? 0 : (second?.WasPressedThisFrame() ?? false) ? 1 : -1;
    }
}
