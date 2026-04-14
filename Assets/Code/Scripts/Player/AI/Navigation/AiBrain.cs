using System;
using UnityEngine;

namespace Player.AI.Navigation
{
    public class AiBrain : MonoBehaviour, IAiInputManager
    {
        public Vector2 MovementInput => _movementInput;

        public event Action OnJumpStarted;
        public event Action OnJumpCanceled;
        public event Action OnInteractStarted;
        public event Action OnPulseStarted;

        [Header("AI Settings")]
        public float nodeReachDistance = 0.5f;
        public float jumpLogicCooldown = 1.0f;
        
        private Vector2 _movementInput;
        private Transform _currentTarget;
        private float _lastJumpTime;

        // Optionally, reference to our AiController to check walls
        private AiController _aiController;

        private void Start()
        {
            _aiController = GetComponent<AiController>();
            if (AiManager.Instance != null)
            {
                AiManager.Instance.OnAreaChanged += HandleAreaChanged;
            }
        }

        private void OnDestroy()
        {
            if (AiManager.Instance != null)
            {
                AiManager.Instance.OnAreaChanged -= HandleAreaChanged;
            }
        }

        private void HandleAreaChanged(AiArea newArea)
        {
            if (newArea.anchorPoints != null && newArea.anchorPoints.Length > 0)
            {
                // Pick nearest or random. Here we pick random for active patrol.
                int idx = UnityEngine.Random.Range(0, newArea.anchorPoints.Length);
                _currentTarget = newArea.anchorPoints[idx];
            }
        }

        private void Update()
        {
            if (_currentTarget == null)
            {
                _movementInput = Vector2.zero;
                return;
            }

            Vector2 toTarget = _currentTarget.position - transform.position;

            // Check if reached
            if (toTarget.magnitude < nodeReachDistance)
            {
                _movementInput = Vector2.zero;
                // Wait for next target or maybe pick another node in the area
                var currentAreaNodes = AiManager.Instance.ActiveArea?.anchorPoints;
                if(currentAreaNodes != null && currentAreaNodes.Length > 0)
                {
                    _currentTarget = currentAreaNodes[UnityEngine.Random.Range(0, currentAreaNodes.Length)];
                }
                return;
            }

            // X axis movement
            float moveDirection = Mathf.Sign(toTarget.x);
            _movementInput = new Vector2(moveDirection, 0f);

            // Simple jump obstacle logic: if target is higher and we hit a wall
            if (Time.time > _lastJumpTime + jumpLogicCooldown)
            {
                bool shouldJump = false;
                
                if (_aiController != null)
                {
                    if (_aiController.IsNearValidWall || _aiController.IsFootNearValidWall)
                    {
                        shouldJump = true;
                    }
                }
                
                // Or if target is strictly above us within jump reach
                if (toTarget.y > 1.5f && Mathf.Abs(toTarget.x) < 2f)
                {
                     shouldJump = true;
                }

                if (shouldJump)
                {
                    OnJumpStarted?.Invoke();
                    _lastJumpTime = Time.time;
                }
            }
        }
    }
}
