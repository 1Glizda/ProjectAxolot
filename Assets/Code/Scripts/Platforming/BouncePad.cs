using System;
using Player;
using UnityEngine;

namespace Platforming
{
    public class BouncePad : MonoBehaviour
    {
        [SerializeField] private float _pushForce;
        [Tooltip("Negative is left, positive is right.")]
        [SerializeField] private float _angleOffset = 0f;

        private float _cooldown;

        private Vector2 GetBounceDirection()
        {
            return Quaternion.Euler(0, 0, -_angleOffset) * Vector3.up;
        }

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (_cooldown > 0f) return;

            if (other.collider.CompareTag("Player")
                && other.collider.TryGetComponent<IPlayerStateProvider>(out var state)
                && !state.IsJumping)
            {
                other.rigidbody.AddForce(GetBounceDirection() * _pushForce, ForceMode2D.Impulse);
                _cooldown = 0.2f;
            } else if (other.collider.CompareTag("AI"))
            {
                other.rigidbody.AddForce(GetBounceDirection() * _pushForce * 0.45f, ForceMode2D.Impulse);
                _cooldown = 0.2f;
            }
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
