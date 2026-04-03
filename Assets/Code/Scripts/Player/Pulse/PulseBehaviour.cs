using UnityEngine;

namespace Player.Pulse
{
    internal class PulseBehaviour : MonoBehaviour
    {
        [Header("Travel Distance")]
        [SerializeField] private float _pulseSpeed;
        [SerializeField] private float _pulseTravelDistance = 15f;
        
        [SerializeField] private float _checkReflectionDistance = 0.1f;
        [SerializeField] private Transform _raycastOrigin;
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private LayerMask _layerMask;
        
        private Vector2 _initialDir;
        private Vector2 _currentDir;
        private bool _isInitialized;
        private float _traveledDistance;
        
        private void FixedUpdate()
        {
            if (!_isInitialized) return;

            Move(Time.fixedDeltaTime);
        }
        
        
        public void Initialize(Vector2 initialDir)
        {
            _initialDir = initialDir;
            _currentDir = _initialDir;
            _isInitialized = true;
            
            Debug.DrawRay(transform.position, _initialDir, Color.red, 15f);
        }

        private void Move(float dt)
        {
            Vector2 move =  _currentDir * (_pulseSpeed * dt);
            _rb.MovePosition(_rb.position + move);
            _traveledDistance += move.magnitude;

            if (_traveledDistance > _pulseTravelDistance)
            {
                DestroyPulse();
            }
            
            float angle = Mathf.Atan2(_currentDir.y, _currentDir.x) * Mathf.Rad2Deg;
            _rb.rotation = angle - 90f;
            
            RaycastHit2D hit = Physics2D.Raycast(_raycastOrigin.position,_currentDir, _checkReflectionDistance, _layerMask);
            if (hit.collider)
            {
                _currentDir = Vector2.Reflect(_currentDir, hit.normal);
                angle = Mathf.Atan2(_currentDir.y, _currentDir.x) * Mathf.Rad2Deg;
                _rb.rotation = angle - 90f;
                
                Debug.DrawRay(transform.position, _currentDir, Color.red, 15f);
            }
            
        }

        
        private void DestroyPulse()
        {
            Destroy(gameObject);
        }
    }
}
