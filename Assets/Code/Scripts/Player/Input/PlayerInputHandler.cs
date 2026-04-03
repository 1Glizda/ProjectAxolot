using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Input
{
    [DefaultExecutionOrder(-100)]
    internal class PlayerInputHandler : MonoBehaviour, IPlayerInputManager
    {
        public InputAction MoveAction => _moveAction;
        public InputAction JumpAction => _jumpAction;
        public InputAction PulseAction => _pulseAction;
        public InputAction PointAction => _pointAction;
        
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _pulseAction;
        private InputAction _pointAction;
        
        private InputSystem_Actions _inputActions;

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();

            _moveAction = _inputActions.Player.Move;
            _jumpAction = _inputActions.Player.Jump;
            _pulseAction = _inputActions.Player.Pulse;
            _pointAction = _inputActions.Player.Point;
            
            _inputActions.Player.Enable();
        }
    }
}
