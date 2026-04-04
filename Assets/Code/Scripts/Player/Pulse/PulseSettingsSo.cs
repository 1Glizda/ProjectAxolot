using UnityEngine;

namespace Player.Pulse
{
    [CreateAssetMenu(fileName = "Pulse Settings", menuName = "Pulse Settings")]
    public class PulseSettingsSo : ScriptableObject
    {

        [Header("Base Settings")]
        [SerializeField] private int _maxHits;
        public int MaxHits => _maxHits;
        
        [SerializeField] private LayerMask _pulseMask;
        public LayerMask PulseMask => _pulseMask;
        
        [Header("Pulse Settings")]
        [SerializeField] private float _pulseStartRange;
        public float PulseStartRange => _pulseStartRange;

        [SerializeField] private float _pulseMaxRange;
        public float PulseMaxRange => _pulseMaxRange;
        
        [SerializeField] private float _pulseSpeed;
        public float PulseSpeed => _pulseSpeed;

    }
}
