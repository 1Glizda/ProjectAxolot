using System;
using Player;
using UnityEngine;

namespace Platforming
{
    public class BouncePad : MonoBehaviour
    {
        [Header("Bounce Settings")]
        [SerializeField] private float _pushForce = 12f; // Changed to match velocity scale (e.g., 10-15 is a good jump)
        [Tooltip("Percentage of incoming falling velocity added to the boost. 0.5 = 50%")]
        [SerializeField] private float _incomingVelocityMultiplier = 0.5f;
        
        [Tooltip("Negative is left, positive is right.")]
        [SerializeField] private float _angleOffset = 0f;
        [SerializeField] private float clampSpeed = 20f; // Optional: Cap the maximum speed from the bounce


        // We track the last bounce time to prevent a single object from triggering it multiple frames in a row
        private float _lastBounceTime;
        private const float BounceCooldown = 0.1f; 

        private Vector2 GetBounceDirection()
        {
            // Multiplying a Quaternion by a Vector3 rotates that vector
            return Quaternion.Euler(0, 0, -_angleOffset) * Vector3.up;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            // Prevent multi-frame double-triggers
            if (Time.time < _lastBounceTime + BounceCooldown) return;

            bool isPlayer = other.collider.CompareTag("Player");
            bool isAI = other.collider.CompareTag("AI");

            if (!isPlayer && !isAI) return;

            Rigidbody2D rb = other.rigidbody;
            if (rb == null) return;

            // 1. Calculate the velocity-based boost
            // We use the absolute value of the raw incoming Y velocity before the collision completely stops it
            float incomingSpeed = Mathf.Abs(other.relativeVelocity.y);
            float boost = incomingSpeed * _incomingVelocityMultiplier;

            // 2. Determine base force depending on who hit it
            float baseForce = _pushForce;
            
            // if (isPlayer)
            // {
            //     // Inform the player state machine they are bouncing (helps reset double-jumps/animations)
            //     if (other.collider.TryGetComponent<IPlayerStateProvider>(out var state))
            //     {
            //         // Optional: state.OnBounce(); 
            //     }
            // }

            if (isAI)
            {
                baseForce *= 0.45f; // Keep your AI scaling
            }

            // 3. Apply the velocity directly
            // Direction * base power + the vertical velocity bonus
            Vector2 launchVelocity = GetBounceDirection() * baseForce;
            launchVelocity.y += boost;
            
            if (launchVelocity.magnitude > clampSpeed)
            {
                launchVelocity = launchVelocity.normalized * clampSpeed;
            }
            rb.linearVelocity = launchVelocity;
            _lastBounceTime = Time.time;
        }

        private void OnDrawGizmos()
        {
            Vector3 origin = transform.position;
            Vector3 direction = GetBounceDirection();
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(origin, direction * 1.5f);
            
            Vector3 rightOffset = Quaternion.Euler(0, 0, 140) * direction;
            Vector3 leftOffset = Quaternion.Euler(0, 0, -140) * direction;
            Gizmos.DrawRay(origin + direction * 1.5f, rightOffset * 0.3f);
            Gizmos.DrawRay(origin + direction * 1.5f, leftOffset * 0.3f);
        }
    }
}