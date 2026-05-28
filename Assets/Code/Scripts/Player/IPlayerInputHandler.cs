using UnityEngine.InputSystem;

namespace Player
{
    public interface IPlayerInputHandler
    {
        public InputAction MoveAction { get; }
        public InputAction JumpAction { get; }
        public InputAction InteractAction { get; }
        public InputAction PulseAction { get; }
        public InputAction PauseAction { get; }
        public InputAction GrabWallAction { get; }
        public InputAction ResetAction { get; }
        public bool IsInputActive { get; }
        public void SetInputActive(bool active);
    }
}
