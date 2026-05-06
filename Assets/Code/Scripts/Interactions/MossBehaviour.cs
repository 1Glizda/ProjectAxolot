using UnityEngine;

namespace Interactions
{
    public class MossBehaviour : PulseLightUpBehaviour
    {
        [Header("Moss Settings")]
        [SerializeField] private bool _isPermanent;
        [SerializeField] private int _climbableLayer = 15;
        
        private int _initialLayer;
        
        private void Awake()
        {
            _initialLayer = gameObject.layer;

            if (_isPermanent)
            {
                _ = base.FadeIn();
                gameObject.layer = _climbableLayer;
            }
        }
        
        
        private void OnEnable()
        {
            if(!_isPermanent) onStateChanged += OnStateChange;
        }

        private void OnDisable()
        {
            if(!_isPermanent) onStateChanged -= OnStateChange;
        }
        
        private void OnStateChange(bool toggle)
        {
            gameObject.layer = toggle ? _climbableLayer : _initialLayer;
        }
    }
}
