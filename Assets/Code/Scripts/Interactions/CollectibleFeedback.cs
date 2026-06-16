using System.Threading.Tasks;
using Interfaces;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace Interactions
{
    public class CollectibleFeedback : MonoBehaviour, IPulseInteraction
    {

        public UnityEvent onPulseHit;
        
        [SerializeField] private Light2D _light;

        [SerializeField] private float _lightPulseIn = 0.2f;
        [SerializeField] private float _lightPulseOut = 2f;

        private float _timeSincePing;
        private bool _canPulse;
        
        private void OnParticleCollision(GameObject other)
        {
            if (!_canPulse) return;
            _canPulse = false;
            _timeSincePing = 0f;
            
            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                PulseInteract();
                onPulseHit?.Invoke();
            }
        }

        private void Update()
        {
            if (_timeSincePing > 2f)
            {
                _canPulse = true;
                return;
            }
            
            _timeSincePing += Time.deltaTime;
            
        }
        
        public void PulseInteract()
        {
            _ = PulseLight();
        }


        private async Task PulseLight()
        {
            try
            {
                float timer = 0f;
                while (timer < _lightPulseIn)
                {
                    float t = timer / _lightPulseIn;
                    _light.intensity = Mathf.Lerp(0f, 10f, t);
                    timer += Time.deltaTime;
                    await Awaitable.EndOfFrameAsync();
                }

                _light.intensity = 1f;

                timer = 0f;
                while (timer < _lightPulseOut)
                {
                    float t =  timer / _lightPulseOut;
                    _light.intensity = Mathf.Lerp(10f, 0f, t);
                    timer += Time.deltaTime;
                    await Awaitable.EndOfFrameAsync();
                }
                _light.intensity = 0f;
                
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            
        } 
    }
}
