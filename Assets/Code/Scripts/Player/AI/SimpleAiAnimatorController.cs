using UnityEngine;
using UnityEngine.Splines;

namespace Player.AI
{
    /// <summary>
    /// Temporary/Permanent controller to bridge the AI character's state to the Player's Animator Controller.
    /// Works automatically for both Spline-based movement (using SplineAnimate) and normal physics-based movement (using SimpleAi).
    /// </summary>
    public class SimpleAiAnimatorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SimpleAi simpleAi;
        [SerializeField] private SplineAnimate splineAnimate;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Grounded Detection (For Splines)")]
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float groundCheckDistance = 0.5f;

        // Player Animator Controller Parameter Hashes
        private static readonly int HorizontalVelocity = Animator.StringToHash("HorizontalVelocity");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int IsClimbing = Animator.StringToHash("IsClimbing");

        private Vector3 _lastPosition;
        private bool _wasGrounded = true;

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (simpleAi == null) simpleAi = GetComponent<SimpleAi>();
            if (splineAnimate == null) splineAnimate = GetComponent<SplineAnimate>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (animator == null) return;

            float horizontalSpeed = 0f;
            float verticalSpeed = 0f;
            bool grounded = true;
            bool climbing = false;

            // Detect if we are currently moving along a spline
            bool isUsingSpline = splineAnimate != null && splineAnimate.isPlaying;

            if (isUsingSpline)
            {
                // Calculate manual velocity based on position changes (since Rb velocity is 0 during SplineAnimate)
                Vector3 currentPosition = transform.position;
                Vector3 velocity = (currentPosition - _lastPosition) / Time.deltaTime;
                _lastPosition = currentPosition;

                horizontalSpeed = Mathf.Abs(velocity.x);
                verticalSpeed = velocity.y;
                
                // Spline Ground Check
                grounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayers);

                // Handle sprite flipping along the spline path
                if (horizontalSpeed > 0.05f && spriteRenderer != null)
                {
                    spriteRenderer.flipX = velocity.x < 0f;
                }
            }
            else if (simpleAi != null)
            {
                // Read directly from the physics-based companion AI component
                Rigidbody2D rb = simpleAi.Rb;
                if (rb != null)
                {
                    horizontalSpeed = Mathf.Abs(rb.linearVelocityX);
                    verticalSpeed = rb.linearVelocityY;
                }
                grounded = simpleAi.IsGrounded;
                climbing = simpleAi.CurrentState.Contains("CLIMB") || simpleAi.CurrentState.Contains("WALL_JUMP");
            }

            // Trigger Jump animation trigger on ground-to-air transitions
            if (!grounded && _wasGrounded && verticalSpeed > 0.5f)
            {
                animator.SetTrigger(Jump);
            }
            _wasGrounded = grounded;

            // Update parameters matching the Player's controller variables
            animator.SetFloat(HorizontalVelocity, horizontalSpeed);
            animator.SetFloat(VerticalVelocity, verticalSpeed);
            animator.SetBool(IsGrounded, grounded);
            animator.SetBool(IsClimbing, climbing);
        }
    }
}
