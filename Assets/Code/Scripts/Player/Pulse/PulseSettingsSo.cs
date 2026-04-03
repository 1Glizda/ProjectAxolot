using UnityEngine;

namespace Player.Pulse
{
    [CreateAssetMenu(fileName = "Pulse Settings", menuName = "Pulse Settings")]
    public class PulseSettingsSo : ScriptableObject
    {
        [SerializeField] private float _pulseRange;
        public float PulseRange => _pulseRange;
        
    }
}
