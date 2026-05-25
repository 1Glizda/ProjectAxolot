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

        
        public void AnimatePoint(float time, bool show)
        {
            if(_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(show ? ShowPoint(time) : HidePoint(time));
        }
        
        private IEnumerator ShowPoint(float time)
        {
            _bg.localScale = _initialBgScale;
            _pointImage.color = Color.clear;
            
            float timer = 0f;
            while (timer < time)
            {
                float t = timer / time;
                
                float u = _bgScaleShowCurve.Evaluate(t);
                _bg.localScale = Vector3.Lerp(_initialBgScale, _maxBgScale, u);
                
                float v = _pointAlphaShowCurve.Evaluate(t);
                _pointImage.color = Color.Lerp(Color.clear, Color.white, v);
                
                timer += Time.deltaTime;
                yield return null;
            }
            _bg.localScale = _initialBgScale;
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