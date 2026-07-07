using UnityEngine;
using UnityEngine.InputSystem;

namespace Member.KYM.Scripts.CoreSystems
{
    [CreateAssetMenu(fileName = "Player input", menuName = "KimSO/Player input", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public Vector2 InputDirection { get; private set; }
        public bool IsDrifting { get; private set; }
        public uint BoostPressVersion { get; private set; }
        
        private Controls _controls;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
            InputDirection = Vector2.zero;
            IsDrifting = false;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            InputDirection = context.ReadValue<Vector2>();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                BoostPressVersion++;
            }
        }

        public void OnDrift(InputAction.CallbackContext context)
        {
            IsDrifting = context.ReadValueAsButton();
        }
    }
}
