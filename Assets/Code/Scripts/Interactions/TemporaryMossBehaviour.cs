using UnityEngine;

namespace Interactions
{
    public class TemporaryMossBehaviour : PulseLightUpBehaviour
    {
        [Header("Moss Settings")]
        [SerializeField] private int _climbableLayer = 15;
        
        private int _initialLayer;
        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _mpb;
        
        private static readonly int RevealProp = Shader.PropertyToID("_Reveal");
        
        private void Awake()
        {
            _initialLayer = gameObject.layer;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
            
            // Start fully hidden
            SetReveal(0f);
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
        
        protected override void OnNormalizedValueChanged(float normalizedValue)
        {
            SetReveal(normalizedValue);
        }
        
        private void SetReveal(float value)
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(RevealProp, value);
            _spriteRenderer.SetPropertyBlock(_mpb);
        }
    }
}
