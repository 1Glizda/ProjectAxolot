using UnityEngine;
using UnityEngine.Splines;

namespace Player.AI
{
    /// <summary>
    /// Animator controller that drives Walk/Idle animations purely based on Spline movement,
    /// and handles 2D sprite flipping and smooth slope-tilting along the spline.
    /// </summary>
    public class SimpleAiAnimatorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SplineAnimate splineAnimate;

        [Header("Animator Parameter Names")]
        [Tooltip("Boolean parameter set to true when moving along the spline, false when stopped.")]
        [SerializeField] private string isWalkingParameter = "IsWalking";

        [Tooltip("Float parameter set to the current spline speed, 0 when stopped.")]
        [SerializeField] private string speedParameter = "Speed";

        [Header("Movement Settings")]
        [SerializeField] private float speedThreshold = 0.05f;
        
        [Header("Tilt Settings")]
        [Tooltip("Smoothness of the rotation. Lower values are smoother; higher values snap faster.")]
        [SerializeField] private float tiltSmoothSpeed = 10f;
        [Tooltip("Maximum allowed tilt angle in degrees.")]
        [SerializeField] private float maxTiltAngle = 60f;

        private Vector3 _lastPosition;
        private float _initialScaleX;
        private float _currentTiltAngle;

        private void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (splineAnimate == null) splineAnimate = GetComponent<SplineAnimate>();

            _lastPosition = transform.position;
            _initialScaleX = Mathf.Abs(transform.localScale.x);
        }

        private void Update()
        {
            if (animator == null) return;

            float horizontalSpeed = 0f;
            float directionX = 0f;
            float targetTiltAngle = 0f;

            // Calculate movement velocity based on position changes (since Rb velocity is 0 during SplineAnimate)
            Vector3 currentPosition = transform.position;
            Vector3 velocity = (currentPosition - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPosition;

            // Update movement stats only if SplineAnimate is currently running
            if (splineAnimate != null && splineAnimate.isPlaying)
            {
                horizontalSpeed = Mathf.Abs(velocity.x);
                directionX = velocity.x;

                // Only calculate a tilt angle if we are actually moving
                if (horizontalSpeed > speedThreshold)
                {
                    // Calculate slope angle relative to facing direction
                    // Using absolute X ensures rotation goes in the correct direction whether facing left or right
                    targetTiltAngle = Mathf.Atan2(velocity.y, Mathf.Abs(velocity.x)) * Mathf.Rad2Deg;
                    targetTiltAngle = Mathf.Clamp(targetTiltAngle, -maxTiltAngle, maxTiltAngle);
                }
            }

            // Determine if the character is active/walking
            bool isWalking = horizontalSpeed > speedThreshold;

            // Update Animator parameters
            if (!string.IsNullOrEmpty(isWalkingParameter))
            {
                animator.SetBool(isWalkingParameter, isWalking);
            }
            if (!string.IsNullOrEmpty(speedParameter))
            {
                animator.SetFloat(speedParameter, horizontalSpeed);
            }

            // Flip the entire rigged character using localScale.x (flips all child sprites and bones together)
            if (horizontalSpeed > speedThreshold)
            {
                Vector3 scale = transform.localScale;
                if (directionX < 0f)
                {
                    scale.x = -_initialScaleX;
                }
                else if (directionX > 0f)
                {
                    scale.x = _initialScaleX;
                }
                transform.localScale = scale;
            }

            // Smoothly interpolate the tilt rotation
            _currentTiltAngle = Mathf.LerpAngle(_currentTiltAngle, targetTiltAngle, Time.deltaTime * tiltSmoothSpeed);
            transform.localRotation = Quaternion.Euler(0, 0, _currentTiltAngle);
        }
    }
}
