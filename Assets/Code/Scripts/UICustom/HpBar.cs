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

        private Coroutine _updateCoroutine;

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
            }
            else
            {
                _updateCoroutine = StartCoroutine(IncreasePoints(previous, current));
            }

            UpdateEyeExpressions(current);
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
            for (int i = previous; i < current; i++)
            {
                _points[i].AnimatePoint(_giveTime, true);
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