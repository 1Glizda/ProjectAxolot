using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Input
{
    [DefaultExecutionOrder(-100)]
    internal class PlayerInputHandler : MonoBehaviour, IPlayerInputHandler
    {
        public InputAction MoveAction => _moveAction;
        public InputAction JumpAction => _jumpAction;
        public InputAction InteractAction => _interactAction;
        public InputAction PulseAction => _pulseAction;
        public InputAction AltPulseAction => _altPulseAction;
        public InputAction PauseAction => _pauseAction;
        
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;
        private InputAction _pulseAction;
        private InputAction _altPulseAction;
        private InputAction _pauseAction;
        
        private InputSystem_Actions _inputActions;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();

            _moveAction = _inputActions.Player.Move;
            _jumpAction = _inputActions.Player.Jump;
            _interactAction = _inputActions.Player.Interact;
            _pulseAction = _inputActions.Player.Pulse;
            _altPulseAction = _inputActions.Player.PulseAlternate;
            _pauseAction = _inputActions.UI.Cancel;

            _inputActions.Player.Enable();
            _inputActions.UI.Enable();
        }

        public void SetInputActive(bool active)
        {
            if (active)
            {
                _inputActions.Player.Enable();
            }
            else
            {
                _inputActions.Player.Disable();
            }
        }
    }
}
