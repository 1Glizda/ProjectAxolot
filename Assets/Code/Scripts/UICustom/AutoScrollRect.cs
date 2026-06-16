using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace UICustom
{
    /// <summary>
    /// Automatically scrolls a ScrollRect vertically from top to bottom.
    /// Resets back to the top whenever the object is enabled or disabled.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class AutoScrollRect : MonoBehaviour
    {
        [Header("Scroll Settings")]
        [Tooltip("How fast the scroll view moves downwards. Value is in normalized coordinates per second (0 to 1).")]
        [SerializeField] private float _scrollSpeed = 0.05f;
        
        [Tooltip("Wait this many seconds before starting to scroll.")]
        [SerializeField] private float _startDelay = 1.0f;

        [Tooltip("How long it takes to smoothly ramp up to full speed.")]
        [SerializeField] private float _rampUpDuration = 2.0f;

        [Header("Events")]
        [Tooltip("Fired exactly once when the scroll view reaches the very bottom.")]
        public UnityEvent OnScrollEnded;

        private ScrollRect _scrollRect;
        private float _timeSinceEnabled;
        private bool _hasFiredEndEvent;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        private void OnEnable()
        {
            ResetScroll();
        }

        private void OnDisable()
        {
            ResetScroll();
        }

        private void Update()
        {
            if (_scrollRect == null) return;

            _timeSinceEnabled += Time.deltaTime;

            if (_timeSinceEnabled >= _startDelay)
            {
                // Calculate current speed with smooth ramp up
                float timeScrolling = _timeSinceEnabled - _startDelay;
                float currentSpeed = _scrollSpeed;
                
                if (_rampUpDuration > 0f)
                {
                    float rampProgress = Mathf.Clamp01(timeScrolling / _rampUpDuration);
                    // Use SmoothStep for a nice easing curve
                    float easedProgress = Mathf.SmoothStep(0f, 1f, rampProgress);
                    currentSpeed = Mathf.Lerp(0f, _scrollSpeed, easedProgress);
                }

                // verticalNormalizedPosition goes from 1.0 (top) to 0.0 (bottom)
                if (_scrollRect.verticalNormalizedPosition > 0f)
                {
                    _scrollRect.verticalNormalizedPosition -= currentSpeed * Time.deltaTime;
                }
                
                // Safety clamp to ensure it stops cleanly at the bottom
                if (_scrollRect.verticalNormalizedPosition <= 0f)
                {
                    _scrollRect.verticalNormalizedPosition = 0f;

                    if (!_hasFiredEndEvent)
                    {
                        _hasFiredEndEvent = true;
                        OnScrollEnded?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Resets the scroll view perfectly back to the top.
        /// </summary>
        public void ResetScroll()
        {
            if (_scrollRect != null)
            {
                // 1.0 is the very top of the vertical scroll view
                _scrollRect.verticalNormalizedPosition = 1f;
                _scrollRect.velocity = Vector2.zero; // Stops any manual kinetic scrolling
            }
            
            _timeSinceEnabled = 0f;
            _hasFiredEndEvent = false;
        }
    }
}
