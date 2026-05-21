using Interfaces;
using Player.GameState;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Player.Pulse
{
    internal class PulseController : MonoBehaviour
    {

       
        
        public event System.Action OnPulse;
        
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private AimHelper _aimHelper;
        
        [FormerlySerializedAs("_normalPulsePrefab")]
        [Header("Radial Pulse")]
        [SerializeField] private GameObject _radialPulse;
        [SerializeField] private GameObject _holdingPulsePrefab;
        
        [Header("Directional Pulse ")]
        [SerializeField] private GameObject _directionalPulsePrefab;
        
        [Header("Force Pulse")]
        [SerializeField] private Material _forcePulseMaterial;
        
        [Header("Settings")]
        [SerializeField] private float _cooldownTimer;
        
        
        private PlayerControllerContext _ctx;
        private InputAction _pulseAction;
        private InputAction _altPulseAction;
        
        private float _timer;

        private PlayerUnlocks _unlocks;
        
        
        private void Start()
        {
            _ctx = playerController.PlayerControllerContext;
            _pulseAction = _ctx.handler.PulseAction;
            _altPulseAction = _ctx.handler.AltPulseAction;
            _unlocks = GameStateManager.Instance.Unlocks;
            
            _pulseAction.performed += FireRadialPulse;
            _altPulseAction.performed += FireDirectionalPulse;
        }
        
        private void Update()
        {
            if(_timer > -1f) _timer -= Time.deltaTime;
        }
        
        
        private void FireRadialPulse(InputAction.CallbackContext context)
        {
            if (Time.timeScale == 0f) return;
            if (_timer > 0f) return;
            _timer = _cooldownTimer;
            
            Instantiate(_radialPulse, transform.position, Quaternion.identity);
            OnPulse?.Invoke();
        }

        private void FireDirectionalPulse(InputAction.CallbackContext context)
        {
            if (_unlocks.IsDirectionalUnlocked) return;
            
            if (Time.timeScale == 0f) return;
            if (_timer > 0f) return;
            _timer = _cooldownTimer;

            Vector3 direction = _aimHelper.MouseWorld - _ctx.rb.transform.position;
            direction.z = 0f;
            direction.Normalize();
            float aboveHorizon = direction.y > 0 ? 1f : -1f;
            
            float angle = Vector3.Angle(direction, Vector3.right);
            angle -= 5f; //half of the pulse arc
            
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle * aboveHorizon);
            
            Instantiate(_directionalPulsePrefab, transform.position, rotation);
            OnPulse?.Invoke();
        }
        
        
    }
}
