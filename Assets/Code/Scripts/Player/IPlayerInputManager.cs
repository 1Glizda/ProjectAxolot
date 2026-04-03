using UnityEngine.InputSystem;

namespace Player
{
    public interface IPlayerInputManager
    {
        public InputAction MoveAction { get; }
        public InputAction JumpAction { get; }
        public InputAction InteractAction { get; }
    }
}
