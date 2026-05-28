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
        private RigidbodyType2D _initialBodyType;

        private void Awake()
        {
            Player.GameState.CheckpointsManager.RegisterResettable(this);
            
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _initialLocalScale = transform.localScale;
            _initialParent = transform.parent;
            
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null) _initialBodyType = _rb.bodyType;
        }

        private void OnDestroy()
        {
            Player.GameState.CheckpointsManager.UnregisterResettable(this);
        }

        public void TriggerReset()
        {
            if (_prefabToRespawn != null)
            {
                // Unregister BEFORE creating the new instance so there is never a frame
                // where both old and new are in the registry simultaneously.
                // The new instance's Awake() will register itself.
                Player.GameState.CheckpointsManager.UnregisterResettable(this);

                // Spawn a fresh copy of the prefab
                GameObject newInstance = Instantiate(_prefabToRespawn, _initialParent);
                newInstance.transform.localPosition = _initialLocalPosition;
                newInstance.transform.localRotation = _initialLocalRotation;
                newInstance.transform.localScale = _initialLocalScale;

                // Destroy this old instance (OnDestroy will no-op since we already unregistered)
                Destroy(gameObject);
                return;
            }

            // Restore parent FIRST so the following local-space assignments are in the right coordinate space.
            // worldPositionStays = false: don't try to preserve world position — we want to set local coords directly.
            transform.SetParent(_initialParent, false);

            gameObject.SetActive(true);
            
            // If there's a Rigidbody2D attached, kill its momentum and disable CCD so it doesn't sweep across the map
            if (_rb != null)
            {
                // Temporarily disable interpolation and CCD to prevent visual/physical "sweeping" from the old position
                var interp = _rb.interpolation;
                var ccd = _rb.collisionDetectionMode;
                _rb.interpolation = RigidbodyInterpolation2D.None;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

                transform.localPosition = _initialLocalPosition;
                transform.localRotation = _initialLocalRotation;
                transform.localScale = _initialLocalScale;

                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.bodyType = _initialBodyType;
                
                // Clear any accumulated forces
                _rb.Sleep(); 
                _rb.WakeUp();

                _rb.interpolation = interp;
                _rb.collisionDetectionMode = ccd;
            }
            else
            {
                transform.localPosition = _initialLocalPosition;
                transform.localRotation = _initialLocalRotation;
                transform.localScale = _initialLocalScale;
            }
        }
    }
}
