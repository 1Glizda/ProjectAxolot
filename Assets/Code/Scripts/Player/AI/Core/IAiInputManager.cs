using System;
using UnityEngine;

namespace Player.AI
{
    public interface IAiInputManager
    {
        public Vector2 MovementInput { get; }
        
        // Emulates InputAction events natively
        public event Action OnJumpStarted;
        public event Action OnJumpCanceled;
        
        public event Action OnInteractStarted;
        public event Action OnPulseStarted;
    }
}
