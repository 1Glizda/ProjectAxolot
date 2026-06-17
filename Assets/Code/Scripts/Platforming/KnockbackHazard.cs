using Interfaces;
using GameState;
using System.Collections;
using UnityEngine;

namespace Platforming
{
    public class KnockbackHazard : MonoBehaviour
    {
        [Header("Knockback Settings")]
        [SerializeField] private float _horizontalForce = 10f;
        [SerializeField] private float _verticalForce = 5f;
        
        [Header("Damage")]
        [SerializeField] private bool _applyDamage;
        [SerializeField] private int _damageAmount = 1;
        [SerializeField] private float _repeatInterval = 0.1f;

        /// <summary>Fired when the player is hit and damaged by this hazard.</summary>
        public event System.Action OnPlayerHit;

        private Coroutine _damageCoroutine;
        private Collider2D _playerCollider;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (!other.TryGetComponent<IKnockbackable>(out _)) return;

            _playerCollider = other;
            if (_damageCoroutine != null) StopCoroutine(_damageCoroutine);
            _damageCoroutine = StartCoroutine(DamageLoop());
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (_damageCoroutine != null)
            {
                StopCoroutine(_damageCoroutine);
                _damageCoroutine = null;
            }
            _playerCollider = null;
        }

        private IEnumerator DamageLoop()
        {
            while (_playerCollider != null)
            {
                if (_playerCollider.TryGetComponent<IKnockbackable>(out var knockbackable))
                {
                    if (GameStateManager.Instance.DamagePlayer(_damageAmount, GetComponent<Collider2D>()))
                    {
                        float dirX = Mathf.Sign(_playerCollider.transform.position.x - transform.position.x);
                        Vector2 knockbackVelocity = new Vector2(dirX * _horizontalForce, _verticalForce);
                        knockbackable.ApplyKnockback(knockbackVelocity);
                        OnPlayerHit?.Invoke();
                    }
                }
                yield return new WaitForSeconds(_repeatInterval);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
