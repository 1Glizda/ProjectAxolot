using Interfaces;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Interactions
{
    public class LightShroomBehaviour : MonoBehaviour, IPulseInteraction
    {

        [Header("References")]
        [SerializeField] private Light2D _light2D;
            
        [Header("Settings")]
        [SerializeField] private float _timeLitUp = 5f;
        [SerializeField] private float _fadeIn = 0.2f;
        [SerializeField] private float _fadeOut = 0.8f;
        [SerializeField] private float _maxIntensity = 1f;
        
        private bool _isPulsing = false;

        private void Start()
        {
            if (_light2D == null)
            {
                _light2D = GetComponent<Light2D>();
            }
            
            SetLightIntensity(0f);
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
                
                SetLightIntensity(t/time);
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            
            SetLightIntensity(1f);
        }
        
        private async Task FadeOut()
        {
            float t = 0f;
            float time = Mathf.Max(0.01f, _fadeOut);
            
            while (t < time)
            {
                t += Time.deltaTime;
                
                SetLightIntensity(1 - t/time);
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            SetLightIntensity(0f);
        }
        
        private void SetLightIntensity(float normalizedValue)
        {
            if(_light2D == null) return;
            
            _light2D.intensity = normalizedValue * _maxIntensity;
        }
    }
}
