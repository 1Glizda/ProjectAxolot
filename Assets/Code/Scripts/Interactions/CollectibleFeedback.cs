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
        
        
        
        
        private void OnParticleCollision(GameObject other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                PulseInteract();
                onPulseHit?.Invoke();
            }
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
                    _light.intensity = Mathf.Lerp(0f, 1f, t);
                    timer += Time.deltaTime;
                }

                _light.intensity = 1f;

                timer = 0f;
                while (timer < _lightPulseOut)
                {
                    float t =  timer / _lightPulseOut;
                    _light.intensity = Mathf.Lerp(1f, 0f, t);
                    timer += Time.deltaTime;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            
        } 
    }
}
