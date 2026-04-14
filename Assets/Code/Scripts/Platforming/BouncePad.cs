using System;
using UnityEngine;

namespace Platforming
{
    public class BouncePad : MonoBehaviour
    {
        [SerializeField] private float _pushForce;
        [Tooltip("Negative is left, positive is right.")]
        [SerializeField] private float _angleOffset = 0f;

        private Vector2 GetBounceDirection()
        {
            return Quaternion.Euler(0, 0, -_angleOffset) * Vector3.up;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.collider.CompareTag("Player"))
            {
                other.rigidbody.AddForce(GetBounceDirection() * _pushForce, ForceMode2D.Impulse);
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
