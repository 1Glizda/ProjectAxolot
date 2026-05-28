using UnityEngine;
using UnityEngine.Events;

namespace Platforming
{
    public class TriggerEvent2D : MonoBehaviour
    {
        [SerializeField] private string _requiredTag;
        [SerializeField] private bool _fireOnce = true;
        [SerializeField] private UnityEvent _onTriggerEnter;

        private bool _fired;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fireOnce && _fired) return;
            if (!string.IsNullOrEmpty(_requiredTag) && !other.CompareTag(_requiredTag)) return;

            _fired = true;
            _onTriggerEnter.Invoke();
        }

        public void Reset() => _fired = false;
    }
}
