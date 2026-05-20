using Interfaces;
using UnityEngine;

namespace Platforming
{
    public class KnockbackHazard : MonoBehaviour
    {
        [Header("Knockback Settings")]
        [SerializeField] private float _horizontalForce = 10f;
        [SerializeField] private float _verticalForce = 5f;

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.collider.CompareTag("Player")) return;
            if (!other.collider.TryGetComponent<IKnockbackable>(out var knockbackable)) return;

            float dirX = Mathf.Sign(other.transform.position.x - transform.position.x);
            Vector2 knockbackVelocity = new Vector2(dirX * _horizontalForce, _verticalForce);

            knockbackable.ApplyKnockback(knockbackVelocity);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
