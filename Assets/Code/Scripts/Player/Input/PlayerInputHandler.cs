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
        public InputAction PauseAction => _pauseAction;
        public InputAction GrabWallAction => _grabWallAction;
        public InputAction ResetAction => _resetAction;
        public bool IsInputActive => _isInputActive;
        
        private bool _isInputActive = true;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;
        private InputAction _pulseAction;
        private InputAction _pauseAction;
        private InputAction _grabWallAction;
        private InputAction _resetAction;
        
        private InputSystem_Actions _inputActions;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();

            _moveAction = _inputActions.Player.Move;
            _jumpAction = _inputActions.Player.Jump;
            _interactAction = _inputActions.Player.Interact;
            _pulseAction = _inputActions.Player.Pulse;
            _pauseAction = _inputActions.UI.Cancel;
            _grabWallAction = _inputActions.Player.GrabWall;
            _resetAction = _inputActions.Player.Reset;

            _inputActions.Player.Enable();
            _inputActions.UI.Enable();
        }

        public void SetInputActive(bool active)
        {
            _isInputActive = active;
            if (active)
            {
                _inputActions.Player.Enable();
            }
            else
            {
                _inputActions.Player.Disable();
            }
        }

        private void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Disable();
                _inputActions.Dispose();
            }
        }
    }
}
