using Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Player.Pulse
{
    internal class PulseController : MonoBehaviour
    {
        public event System.Action OnPulse;
        [FormerlySerializedAs("playerStateProvider")]
        [FormerlySerializedAs("_playerController")]
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GameObject _pulsePrefab;
        
        [Header("Settings")]
        [SerializeField] private float _cooldownTimer;
        
        
        private PlayerContext _ctx;
        private InputAction _pulseAction;
        
        private float _timer;
            
        private void Start()
        {
            _ctx = playerController.PlayerContext;
            _pulseAction = _ctx.manager.PulseAction;

            _pulseAction.performed += Pulse;
        }

        private void Update()
        {
            if(_timer > -1f) _timer -= Time.deltaTime;
        }
        
        
        private void Pulse(InputAction.CallbackContext context)
        {
            if (_timer > 0f) return;
            _timer = _cooldownTimer;
            
            Instantiate(_pulsePrefab, transform.position, Quaternion.identity);
            OnPulse?.Invoke();
        }
        
        
    }
}
