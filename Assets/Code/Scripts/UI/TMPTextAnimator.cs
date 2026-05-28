using System.Collections;
using TMPro;
using UnityEngine;

namespace UI
{
    public class TMPTextAnimator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The TextMeshPro component to animate. Will try to get it from this GameObject if left empty.")]
        [SerializeField] private TMP_Text _text;

        [Header("Timing")]
        [Tooltip("How long it takes to fade in and slide up to the original position.")]
        [SerializeField] private float _fadeInDuration = 0.5f;
        [Tooltip("How long the text stays visible and stationary.")]
        [SerializeField] private float _stayDuration = 2.0f;
        [Tooltip("How long it takes to fade out and slide up away from the original position.")]
        [SerializeField] private float _fadeOutDuration = 0.5f;

        [Header("Movement")]
        [Tooltip("How far on the Y axis the text moves during fade in (moves from -distance to 0) and fade out (moves from 0 to +distance)")]
        [SerializeField] private float _yMoveDistance = 50f;
        [Tooltip("How far on the Y axis the text slowly drifts during the Stay phase.")]
        [SerializeField] private float _stayMoveDistance = 15f;

        [Header("Settings")]
        [Tooltip("If true, the animation starts automatically when the object is enabled.")]
        [SerializeField] private bool _playOnAwake = true;

        private Vector3 _originalLocalPos;
        private Color _originalColor;
        private Coroutine _animationRoutine;
        private bool _isInitialized = false;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
            
            if (_playOnAwake)
            {
                Play();
            }
        }

        private void InitializeIfNeeded()
        {
            if (_isInitialized) return;
            
            if (_text == null) _text = GetComponent<TMP_Text>();
            
            if (_text != null)
            {
                _originalLocalPos = _text.rectTransform.localPosition;
                _originalColor = Color.white;
                
                // Hide initially
                _text.color = Color.clear;
                _isInitialized = true;
            }
        }

        public void Play()
        {
            if (_text == null) return;
            
            if (_animationRoutine != null) 
                StopCoroutine(_animationRoutine);
                
            _animationRoutine = StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            float elapsed;

            // FADE IN
            elapsed = 0f;
            Vector3 startPos = _originalLocalPos + Vector3.down * _yMoveDistance;
            
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeInDuration);
                
                // Ease Out Cubic (starts fast, smoothly slows down to a stop at the target position)
                float easeT = 1f - Mathf.Pow(1f - t, 3f); 
                
                Color c = _originalColor;
                c.a = t; // Fade from 0 to 1 (clear to white)
                _text.color = c;
                
                _text.rectTransform.localPosition = Vector3.LerpUnclamped(startPos, _originalLocalPos, easeT);
                
                yield return null;
            }
            
            _text.color = _originalColor;
            _text.rectTransform.localPosition = _originalLocalPos;

            // STAY
            elapsed = 0f;
            Vector3 stayStartPos = _originalLocalPos;
            Vector3 stayEndPos = _originalLocalPos + Vector3.up * _stayMoveDistance;
            
            while (elapsed < _stayDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _stayDuration);
                
                // Linear drift during stay
                _text.rectTransform.localPosition = Vector3.LerpUnclamped(stayStartPos, stayEndPos, t);
                
                yield return null;
            }

            // FADE OUT
            elapsed = 0f;
            Vector3 fadeOutStartPos = stayEndPos;
            Vector3 fadeOutEndPos = fadeOutStartPos + Vector3.up * _yMoveDistance;
            
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeOutDuration);
                
                // Ease In Cubic (starts slow, speeds up smoothly as it drifts away)
                float easeT = t * t * t;
                
                Color c = _originalColor;
                c.a = 1f - t; // Fade from 1 to 0 (white to clear)
                _text.color = c;
                
                _text.rectTransform.localPosition = Vector3.LerpUnclamped(fadeOutStartPos, fadeOutEndPos, easeT);
                
                yield return null;
            }

            // Hide and revert position at the very end
            _text.color = Color.clear;
            _text.rectTransform.localPosition = _originalLocalPos;
            
            _animationRoutine = null;
        }
    }
}
