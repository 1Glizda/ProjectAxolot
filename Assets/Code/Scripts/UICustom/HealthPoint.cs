using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UICustom
{
    public class HealthPoint : MonoBehaviour
    {
        [SerializeField] private RectTransform _bg;
        [SerializeField] private Image _pointImage;

        [Header("Show")]
        [SerializeField] private AnimationCurve _bgScaleShowCurve;
        [SerializeField] private Vector3 _maxBgScale;
        
        [SerializeField] private AnimationCurve _pointAlphaShowCurve;
        
        [Header("Hide")]
        [SerializeField] private AnimationCurve _pointAlphaHideCurve;

        private Vector3 _initialBgScale;
        private Coroutine _animationCoroutine;


        private void Awake()
        {
            _initialBgScale = _bg.localScale;
        }

        
        public void AnimatePoint(float time, bool show, Vector3? startScreenPos = null)
        {
            if(_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(show ? ShowPoint(time, startScreenPos) : HidePoint(time));
        }
        
        private IEnumerator ShowPoint(float time, Vector3? startScreenPos)
        {
            _bg.localScale = _initialBgScale;
            _pointImage.color = Color.clear;
            Vector3 endLocalPos = Vector3.zero;
            
            if (startScreenPos.HasValue)
            {
                Canvas canvas = _bg.GetComponentInParent<Canvas>();
                Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                if (cam == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = Camera.main;

                RectTransformUtility.ScreenPointToWorldPointInRectangle(_bg.parent as RectTransform, startScreenPos.Value, cam, out Vector3 worldPoint);
                _bg.position = worldPoint;
            }
            else
            {
                _bg.localPosition = endLocalPos;
            }

            Vector3 actualStartLocalPos = _bg.localPosition;

            float timer = 0f;
            while (timer < time)
            {
                float t = timer / time;
                
                float u = _bgScaleShowCurve.Evaluate(t);
                _bg.localScale = Vector3.Lerp(_initialBgScale, _maxBgScale, u);
                
                float v = _pointAlphaShowCurve.Evaluate(t);
                _pointImage.color = Color.Lerp(Color.clear, Color.white, v);

                if (startScreenPos.HasValue)
                {
                    // Ease-out position interpolation for a nice flying effect
                    float posT = 1f - Mathf.Pow(1f - t, 3f);
                    _bg.localPosition = Vector3.Lerp(actualStartLocalPos, endLocalPos, posT);
                }
                
                timer += Time.deltaTime;
                yield return null;
            }
            _bg.localScale = _initialBgScale;
            _bg.localPosition = endLocalPos;
            _pointImage.color = Color.white;
            _animationCoroutine = null;
        }

        private IEnumerator HidePoint(float time)
        {
            _bg.localScale = _initialBgScale;
            _pointImage.color = Color.white;
            
            float timer = 0f;
            while (timer < time)
            {
                float t = timer / time;
                
                float u = _pointAlphaHideCurve.Evaluate(t);
                _pointImage.color = Color.Lerp(Color.clear, Color.white, u);
                
                timer += Time.deltaTime;
                yield return null;
            }
            _pointImage.color = Color.clear;
            _animationCoroutine = null;
        }
    }
}