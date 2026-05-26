using Interfaces;
using UnityEngine;

namespace Interactions
{
    /// <summary>
    /// Component that records its initial Transform state on Awake, and provides
    /// a Reset method to restore it to that state. Useful for resetting hazards, 
    /// platforms, or collectibles when the player dies or resets the room.
    /// </summary>
    public class ResetObject : MonoBehaviour, IResettable
    {
        [Tooltip("If assigned, Reset will destroy this object and spawn a fresh copy of this prefab. If left empty, Reset will just move this object back to its start position.")]
        [SerializeField] private GameObject _prefabToRespawn;

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;
        private Transform _initialParent;
        
        private Rigidbody2D _rb;

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;
            _initialParent = transform.parent;
            
            _rb = GetComponent<Rigidbody2D>();
        }

        public void TriggerReset()
        {
            if (_prefabToRespawn != null)
            {
                // Spawn a fresh copy of the prefab
                GameObject newInstance = Instantiate(_prefabToRespawn, _initialParent);
                newInstance.transform.localPosition = _initialLocalPosition;
                newInstance.transform.localRotation = _initialLocalRotation;
                newInstance.transform.localScale = _initialLocalScale;

                // Destroy this old instance
                Destroy(gameObject);
                return;
            }

            // Fallback: just move the transform
            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialLocalScale;
            transform.SetParent(_initialParent);
            
            // If there's a Rigidbody2D attached, kill its momentum so it doesn't instantly snap back out
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
        }
    }
}
