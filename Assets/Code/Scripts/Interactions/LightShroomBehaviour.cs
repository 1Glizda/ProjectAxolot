using Interfaces;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Interactions
{
    public class LightShroomBehaviour : MonoBehaviour, IPulseInteraction
    {

        [Header("References")]
        [SerializeField] private SpriteRenderer _renderer;
            
        [Header("Settings")]
        [SerializeField] private float _timeLitUp = 5f;
        [SerializeField] private float _fadeIn = 0.2f;
        [SerializeField] private float _fadeOut = 0.8f;
        
        
        private static readonly int EmissionFactorID = Shader.PropertyToID("_EmissionFactor");
        private MaterialPropertyBlock _propertyBlock;

        private bool _isPulsing = false;

        private void Start()
        {
            _renderer = GetComponent<SpriteRenderer>();
            
            _propertyBlock = new MaterialPropertyBlock();
            
            SetEmissionOnPropertyBlock(0f);
        }


        private void OnParticleCollision(GameObject other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                PulseInteract();
            }
            
        }

        public void PulseInteract()
        {
            if (!_isPulsing)
            {
                _ = LightUp();
            }
        }


        private async Task LightUp()
        {
            _isPulsing = true;

            try
            {
                await FadeIn();

                await Awaitable.WaitForSecondsAsync(_timeLitUp);

                await FadeOut();
            }
            finally
            {
                _isPulsing = false;
            }
        }


        private async Task FadeIn()
        {
            float t = 0f;
            float time = Mathf.Max(0.01f, _fadeIn);
            
            while (t < time)
            {
                t += Time.deltaTime;
                
                SetEmissionOnPropertyBlock(t/time);
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            
            SetEmissionOnPropertyBlock(1f);
        }
        
        private async Task FadeOut()
        {
            float t = 0f;
            float time = Mathf.Max(0.01f, _fadeOut);
            
            while (t < time)
            {
                t += Time.deltaTime;
                
                SetEmissionOnPropertyBlock(1 - t/time);
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            SetEmissionOnPropertyBlock(0f);
        }

        
        private void SetEmissionOnPropertyBlock(float value)
        {
            if(_renderer == null) return;
            
            
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(EmissionFactorID, value);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
