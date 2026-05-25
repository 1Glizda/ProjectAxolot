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
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Sprite _activeSprite;
        [SerializeField] private Sprite _explodedSprite;

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

        private bool _isExploded = false;

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            SetSprite(false);
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
            StartCoroutine(ExplodeRoutine());
        }

        private IEnumerator ExplodeRoutine()
        {
            _isExploded = true;
            SetSprite(true);
            if (_collider != null) _collider.enabled = false;

            Explode();

            yield return new WaitForSeconds(_recoveryTime);

            _isExploded = false;
            SetSprite(false);
            if (_collider != null) _collider.enabled = true;
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

        private void SetSprite(bool exploded)
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.sprite = exploded ? _explodedSprite : _activeSprite;
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
