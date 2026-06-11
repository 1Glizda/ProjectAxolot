using System.Collections;
using UnityEngine;

namespace UICustom
{
    public class HpBar : MonoBehaviour
    {
        [Header("Points")]
        [SerializeField] private HealthPoint[] _points;
        [SerializeField] private float _giveTimeBetween;
        [SerializeField] private float _takeTimeBetween;
        
        [Header("Animation Timing")]
        [SerializeField] private float _giveTime;
        [SerializeField] private float _takeTime;
        
        [Header("Eyes")]
        [SerializeField] private GameObject _happyEyes;
        [SerializeField] private GameObject _neutralEyes;
        [SerializeField] private GameObject _sadEyes;

        [Header("Damage Shake")]
        [SerializeField] private RectTransform _shakeTarget;
        [SerializeField] private float _shakeDuration = 0.35f;
        [SerializeField] private float _shakeStrength = 12f;
        [SerializeField] private float _shakeFrequency = 30f;

        private Coroutine _updateCoroutine;
        private Coroutine _shakeCoroutine;

        public void UpdatePoints(int previous, int current)
        {
            previous = Mathf.Clamp(previous, 0, _points.Length);
            current = Mathf.Clamp(current, 0, _points.Length);

            if (previous == current) return;

            if (_updateCoroutine != null) StopCoroutine(_updateCoroutine);

            // Snap visual state to match 'previous' before animating to 'current'
            for (int i = 0; i < _points.Length; i++)
            {
                _points[i].AnimatePoint(0f, i < previous);
            }

            if (previous > current)
            {
                _updateCoroutine = StartCoroutine(DecreasePoints(previous, current));
                TriggerShake();
            }
            else
            {
                _updateCoroutine = StartCoroutine(IncreasePoints(previous, current));
            }

            UpdateEyeExpressions(current);
        }

        private void TriggerShake()
        {
            if (_shakeTarget == null) return;
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(Shake());
        }

        private IEnumerator Shake()
        {
            Vector2 originalPos = _shakeTarget.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < _shakeDuration)
            {
                float decay = 1f - (elapsed / _shakeDuration);
                float offsetX = Mathf.Sin(elapsed * _shakeFrequency) * _shakeStrength * decay;
                _shakeTarget.anchoredPosition = originalPos + new Vector2(offsetX, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeTarget.anchoredPosition = originalPos;
            _shakeCoroutine = null;
        }

        private IEnumerator DecreasePoints(int previous, int current)
        {
            for (int i = previous - 1; i >= current; i--)
            {
                _points[i].AnimatePoint(_takeTime, false);
                yield return new WaitForSeconds(_takeTimeBetween);
            }
            _updateCoroutine = null;
        }

        private IEnumerator IncreasePoints(int previous, int current)
        {
            Vector3? playerScreenPos = null;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && Camera.main != null)
            {
                playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);
            }

            for (int i = previous; i < current; i++)
            {
                _points[i].AnimatePoint(_giveTime, true, playerScreenPos);
                yield return new WaitForSeconds(_giveTimeBetween);
            }
            _updateCoroutine = null;
        }

        private void UpdateEyeExpressions(int currentHp)
        {
            if (_happyEyes == null || _neutralEyes == null || _sadEyes == null) return;
            
            _happyEyes.SetActive(currentHp == 3);
            _neutralEyes.SetActive(currentHp == 2);
            _sadEyes.SetActive(currentHp == 1);
        }
    }
}
