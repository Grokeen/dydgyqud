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
        public int DirectSelection => (first?.WasPressedThisFrame() ?? false) ? 0 : (second?.WasPressedThisFrame() ?? false) ? 1 : -1;
    }
}
