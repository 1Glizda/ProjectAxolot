using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ScreenFader : MonoBehaviour
    {
        [Tooltip("The RawImage to fade. Make sure it spans the whole screen in a Canvas.")]
        [SerializeField] private RawImage _fadeImage;
        
        [Tooltip("The color to fade to.")]
        [SerializeField] private Color _fadeColor = Color.black;
        
        [Tooltip("How long it takes to fade TO the color (matches Pre Reset Delay in CheckpointsManager)")]
        [SerializeField] private float _fadeOutDuration = 0.5f;
        
        [Tooltip("How long it takes to fade FROM the color back to clear (matches Post Reset Delay in CheckpointsManager)")]
        [SerializeField] private float _fadeInDuration = 0.3f;

        private Coroutine _currentFade;

        private void Awake()
        {
            if (_fadeImage != null)
            {
                // Ensure we start completely clear
                Color c = _fadeColor;
                c.a = 0f;
                _fadeImage.color = c;
                _fadeImage.raycastTarget = false;
            }
        }

        /// <summary>
        /// Hook this up to CheckpointsManager.OnDeathStart
        /// </summary>
        public void FadeToColor()
        {
            if (_fadeImage == null) return;
            if (_currentFade != null) StopCoroutine(_currentFade);
            _currentFade = StartCoroutine(FadeRoutine(1f, _fadeOutDuration));
        }

        /// <summary>
        /// Hook this up to CheckpointsManager.OnReviveStart
        /// </summary>
        public void FadeToClear()
        {
            if (_fadeImage == null) return;
            if (_currentFade != null) StopCoroutine(_currentFade);
            _currentFade = StartCoroutine(FadeRoutine(0f, _fadeInDuration));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            // Enable raycast block while fading
            _fadeImage.raycastTarget = true;
            
            Color startColor = _fadeImage.color;
            Color targetColor = _fadeColor;
            targetColor.a = targetAlpha;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration > 0f ? elapsed / duration : 1f;
                _fadeImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            _fadeImage.color = targetColor;
            
            // Disable raycasts if fully transparent so it doesn't block gameplay input/UI
            if (targetAlpha <= 0f)
            {
                _fadeImage.raycastTarget = false;
            }
        }
    }
}
