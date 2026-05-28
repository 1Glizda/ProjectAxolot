using Interfaces;
using Platforming;
using Player.GameState;
using System.Collections;
using UnityEngine;

namespace Interactions
{
    public class ExplodingMushroomBehaviour : MonoBehaviour, IPulseInteraction
    {
        [Header("References")]
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Animator _animator;

        [Header("Explosion Settings")]
        [SerializeField] private float _explosionRadius = 5f;
        [SerializeField] private float _innerRadius = 2f;
        [SerializeField] private float _explosionForce = 15f;
        [SerializeField] private LayerMask _affectedLayers;

        [Header("Damage Settings")]
        [SerializeField] private int _innerZoneDamage = 2;
        [SerializeField] private int _outerZoneDamage = 1;

        [Header("Timing")]
        [SerializeField] private float _recoveryTime = 3f;
        [SerializeField] private float _contactDelay = 0.5f;
        [SerializeField] private Vector3 _preExplosionScale = new Vector3(0.8f, 0.8f, 1f);

        private bool _isExploded = false;
        private Vector3 _initialScale;

        /// <summary>Fired when the mushroom explodes.</summary>
        public event System.Action OnExplode;
        /// <summary>Fired when the mushroom recovers after explosion.</summary>
        public event System.Action OnRecover;

        private void Awake()
        {
            _initialScale = transform.localScale;
        }

        private void OnParticleCollision(GameObject other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                PulseInteract();
            }
        }

        public void PulseInteract()
        {
            if (_isExploded) return;
            StartCoroutine(ExplodeRoutine(_contactDelay));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") || (collision.rigidbody != null && collision.rigidbody.CompareTag("Player")))
            {
                if (!_isExploded)
                    StartCoroutine(ExplodeRoutine(_contactDelay));
            }
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.CompareTag("Player") || (collider.attachedRigidbody != null && collider.attachedRigidbody.CompareTag("Player")))
            {
                if (!_isExploded)
                    StartCoroutine(ExplodeRoutine(_contactDelay));
            }
        }

        private IEnumerator ExplodeRoutine(float delay)
        {
            _isExploded = true;
            Vector3 originalScale = _initialScale;

            if (delay > 0f)
            {
                float time = 0f;
                while (time < delay)
                {
                    transform.localScale = Vector3.Lerp(originalScale, _preExplosionScale, time / delay);
                    time += Time.deltaTime;
                    yield return null;
                }
                transform.localScale = _preExplosionScale;
            }

            // Active sprite stays visible — fire the explosion animation on it.
            // Damage is applied via the Animation Event (TriggerExplosionDamage) so it
            // syncs with the visual impact frame. The Animator handles all visual state.
            if (_collider != null) _collider.enabled = false;
            if (_animator != null) _animator.SetTrigger("Explode");

            OnExplode?.Invoke();

            // Wait for the full recovery period before reverting.
            // _recoveryTime should be set to at least the length of the Explode animation.
            yield return new WaitForSeconds(_recoveryTime);

            // Recover
            _isExploded = false;
            if (_collider != null) _collider.enabled = true;
            if (_animator != null) _animator.SetTrigger("Revert");
            transform.localScale = originalScale;
            OnRecover?.Invoke();
        }

        /// <summary>
        /// Called by an Animation Event on the Explode clip at the impact frame.
        /// Applies physics force and damage to nearby objects.
        /// </summary>
        public void TriggerExplosionDamage()
        {
            if (!_isExploded) return; // safety guard if called outside of explosion
            Explode();
        }

        private void Explode()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _affectedLayers);

            foreach (Collider2D hit in hits)
            {
                Rigidbody2D rb = hit.attachedRigidbody;
                if (rb == null) continue;

                Vector2 dir = (rb.position - (Vector2)transform.position);
                float distance = dir.magnitude;
                dir = distance > 0.01f ? dir.normalized : Vector2.up;

                float falloff;
                if (distance <= _innerRadius)
                {
                    falloff = 1f;
                }
                else
                {
                    float falloffRange = Mathf.Max(0.001f, _explosionRadius - _innerRadius);
                    falloff = 1f - Mathf.Clamp01((distance - _innerRadius) / falloffRange);
                }

                rb.linearVelocity = dir * (_explosionForce * falloff);

                // Apply damage if the hit target is the player
                if (hit.CompareTag("Player") || rb.CompareTag("Player"))
                {
                    int damage = distance <= _innerRadius ? _innerZoneDamage : _outerZoneDamage;
                    GameStateManager.Instance.DamagePlayer(damage, _collider);
                }

                if (rb.TryGetComponent<Platforming.BreakableWall>(out var wall))
                {
                    wall.Break(dir);
                }
            }
        }


        private void OnDrawGizmosSelected()
        {
            // Outer radius (falloff zone)
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, _explosionRadius);
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);

            // Inner radius (full force zone)
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, _innerRadius);
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, _innerRadius);
        }
    }
}
