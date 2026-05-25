using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UICustom
{
    public class PulseMuteIcon : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private float _revealDuration;

        public void OnPulseFired(float cooldown)
        {
            StopAllCoroutines();
            StartCoroutine(ShowIcon(cooldown));
        }
        
        
        private IEnumerator ShowIcon(float cooldown)
        {
            _icon.color = Color.clear;

            float timer = 0f;
            while (timer < _revealDuration)
            {
                _icon.color = Color.Lerp(Color.clear, Color.white, timer / _revealDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            _icon.color = Color.white;
            
            float timeLeft = cooldown - timer;
            timer = 0f;
            while (timer < timeLeft)
            {
                float t =  timer / timeLeft;
                t = Mathf.Pow(t, 5);
                
                _icon.color = Color.Lerp(Color.white, Color.clear, t);
                timer += Time.deltaTime;
                yield return null;
            }
            _icon.color = Color.clear;
        }
    }
}