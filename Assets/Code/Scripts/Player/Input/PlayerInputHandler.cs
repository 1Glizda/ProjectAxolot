using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Input
{
    [DefaultExecutionOrder(-100)]
    internal class PlayerInputHandler : MonoBehaviour, IPlayerInputManager
    {
        public InputAction MoveAction => _moveAction;
        public InputAction JumpAction => _jumpAction;
        public InputAction InteractAction => _interactAction;
        public InputAction PulseAction => _pulseAction;
        
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _interactAction;
        
        private InputAction _pulseAction;
        
        
        private InputSystem_Actions _inputActions;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();

            _moveAction = _inputActions.Player.Move;
            _jumpAction = _inputActions.Player.Jump;
            _interactAction = _inputActions.Player.Interact;
            
            _pulseAction = _inputActions.Player.Pulse;
            
            _inputActions.Player.Enable();
        }
    }
}
