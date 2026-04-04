using Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Pulse
{
    internal class PulseController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        
        [Header("Pulse Settings")]
        [SerializeField] private PulseSettingsSo _settings;
        
        private PlayerContext _ctx;
        private InputAction _pulseAction;

        private RaycastHit2D[] _hits;
         
            
            
        private void Awake()
        {
            _ctx = _playerController.PlayerContext;
            _pulseAction = _ctx.manager.PulseAction;
            
            _hits = new RaycastHit2D[_settings.MaxHits];
        }

        private void Pulse()
        {
            
        }
        
        
    }
}
