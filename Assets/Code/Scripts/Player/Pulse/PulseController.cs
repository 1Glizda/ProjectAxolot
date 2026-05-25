using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Player.Pulse
{
    internal class PulseController : MonoBehaviour
    {
        public UnityEvent<float> OnPulse;

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        
        [Header("Pulse")]
        [SerializeField] private GameObject _radialPulse;
        
        [Header("Settings")]
        [SerializeField] private float _cooldownTimer;
        
        
        private PlayerControllerContext _ctx;
        private InputAction _pulseAction;
        
        private float _timer;

        
        private void Start()
        {
            _ctx = PlayerController.PlayerControllerContext;
            _pulseAction = _ctx.handler.PulseAction;
            
            _pulseAction.performed += FirePulse;
        }
        
        private void Update()
        {
            if(_timer > -1f) _timer -= Time.deltaTime;
        }
        
        
        private void FirePulse(InputAction.CallbackContext context)
        {
            if (Time.timeScale == 0f) return;
            if (_timer > 0f) return;
            _timer = _cooldownTimer;
            
            Instantiate(_radialPulse, transform.position, Quaternion.identity);
            OnPulse?.Invoke(_cooldownTimer);
        }
        
    }
}
