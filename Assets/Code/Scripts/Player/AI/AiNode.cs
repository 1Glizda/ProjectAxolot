using UnityEngine;

namespace Player.AI.Navigation
{
    [DisallowMultipleComponent]
    public class AiNode : MonoBehaviour
    {
        [Tooltip("How long the AI should pause at this node before moving to the next one.")]
        public float pauseDuration = 0f;
    }
}
