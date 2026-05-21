using UnityEngine.InputSystem;

namespace Player
{
    public interface IPlayerInputHandler
    {
        public InputAction MoveAction { get; }
        public InputAction JumpAction { get; }
        public InputAction InteractAction { get; }
        public InputAction PulseAction { get; }
        public InputAction AltPulseAction { get; }
        public InputAction PauseAction { get; }
        public void SetInputActive(bool active);
    }
}
