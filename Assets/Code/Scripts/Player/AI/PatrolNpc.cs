using System.Collections;
using Interfaces;
using UnityEngine;

namespace Player.AI
{
    /// <summary>
    /// Simple patrol NPC: walks left/right between two bounds.
    /// Pulse turns it into a rollable ball with regular physics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PatrolNpc : MonoBehaviour, IPulseInteraction
    {
        [Header("Patrol")]
        [SerializeField] private Transform leftBound;
        [SerializeField] private Transform rightBound;
        [SerializeField] private float patrolSpeed = 3f;

        [Header("Ground")]
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float groundRayDist = 1f;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        [Header("Colliders")]
        [SerializeField] private Collider2D normalCollider;
        [SerializeField] private CircleCollider2D ballCollider;

        [Header("Ball Transition")]
        [SerializeField] private float ballTransitionDuration = 0.35f;
        [SerializeField] private string ballTrigger = "CurlIntoBall";

        [Header("Ball Physics")]
        [SerializeField] private PhysicsMaterial2D ballPhysicsMaterial;
        [SerializeField] private float ballMass = 0.5f;
        [SerializeField] private float ballAngularDrag = 0.5f;
        [SerializeField] private float ballLinearDrag = 0.5f;

        private Rigidbody2D _rb;
        private float _direction = 1f;
        private bool _isBall;
        private bool _isTransitioning;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;

            if (ballCollider != null)
                ballCollider.enabled = false;
        }

        private void Update()
        {
            if (_isBall || _isTransitioning) return;

            // Move
            float step = _direction * patrolSpeed * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.x += step;

            // Snap to ground
            RaycastHit2D hit = Physics2D.Raycast(pos + Vector3.up * 0.5f, Vector2.down, groundRayDist + 0.5f, groundLayers);
            if (hit.collider != null)
                pos.y = hit.point.y;

            transform.position = pos;

            // Flip at bounds
            if (leftBound != null && pos.x <= leftBound.position.x && _direction < 0f)
                Flip();
            else if (rightBound != null && pos.x >= rightBound.position.x && _direction > 0f)
                Flip();
        }

        private void Flip()
        {
            _direction *= -1f;
            if (spriteRenderer != null)
                spriteRenderer.flipX = _direction < 0f;
        }

        // --- Pulse ---

        private void OnParticleCollision(GameObject other)
        {
            Debug.Log($"[PatrolNpc] OnParticleCollision from '{other.name}' layer={LayerMask.LayerToName(other.layer)}", this);
            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
                PulseInteract();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                Debug.Log($"[PatrolNpc] Pulse detected via trigger from '{other.name}'", this);
                PulseInteract();
            }
        }

        public void PulseInteract()
        {
            if (_isBall || _isTransitioning) return;
            Debug.Log("[PatrolNpc] PulseInteract called! Transitioning to ball.", this);
            StartCoroutine(TransitionToBall());
        }

        private IEnumerator TransitionToBall()
        {
            _isTransitioning = true;

            if (animator != null)
                animator.SetTrigger(ballTrigger);

            yield return new WaitForSeconds(ballTransitionDuration);

            // Swap colliders
            if (normalCollider != null) normalCollider.enabled = false;
            if (ballCollider != null)
            {
                ballCollider.enabled = true;
                if (ballPhysicsMaterial != null)
                    ballCollider.sharedMaterial = ballPhysicsMaterial;
            }

            // Switch to Dynamic with smooth interpolation
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.mass = ballMass;
            _rb.angularDamping = ballAngularDrag;
            _rb.linearDamping = ballLinearDrag;
            _rb.freezeRotation = false;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _isBall = true;
            _isTransitioning = false;

            // Switch layer
            gameObject.layer = LayerMask.NameToLayer("RollyBally");
        }

        // --- Gizmos ---

        private void OnDrawGizmos()
        {
            if (leftBound == null || rightBound == null) return;

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
            float y = transform.position.y;
            Vector3 l = new Vector3(leftBound.position.x, y, 0f);
            Vector3 r = new Vector3(rightBound.position.x, y, 0f);
            Gizmos.DrawLine(l, r);
            Gizmos.DrawSphere(l, 0.15f);
            Gizmos.DrawSphere(r, 0.15f);
        }
    }
}
