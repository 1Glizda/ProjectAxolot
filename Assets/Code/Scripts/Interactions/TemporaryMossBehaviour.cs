using UnityEngine;

namespace Interactions
{
    public class TemporaryMossBehaviour : PulseLightUpBehaviour
    {
        [Header("Moss Settings")]
        [SerializeField] private int _climbableLayer = 15;
        
        private int _initialLayer;
        
        private void Awake()
        {
            _initialLayer = gameObject.layer;
        }
        
        private void OnEnable()
        {
            onStateChanged += OnStateChange;
        }

        private void OnDisable()
        {
            onStateChanged -= OnStateChange;
        }
        
        private void OnStateChange(bool toggle)
        {
            gameObject.layer = toggle ? _climbableLayer : _initialLayer;
        }
    }
}
