using Interfaces;
using UnityEngine;

namespace Platforming.Sound
{
    /// <summary>
    /// Wind whoosh sound controller. Plays two alternating wind clips at random intervals.
    /// Attach to any object or zone that should emit wind sounds.
    /// All audio clips and timing are centralised in the EnvironmentSoundProfileSo.
    /// </summary>
    public class WindSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnvironmentSoundProfileSo soundProfile;

        [Header("Spatial")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float maxDistance = 25f;

        private AudioSource _source;
        private float _timer;
        private float _nextTime;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = spatialBlend;
            _source.maxDistance = maxDistance;
            _source.rolloffMode = AudioRolloffMode.Linear;
        }

        private void Start()
        {
            if (soundProfile == null)
            {
                Debug.LogWarning($"[{gameObject.name}] WindSoundController: No EnvironmentSoundProfileSo assigned.", this);
                return;
            }

            if (soundProfile.windWhoosh1 == null && soundProfile.windWhoosh2 == null)
            {
                Debug.LogWarning($"[{gameObject.name}] WindSoundController: No wind clips in profile '{soundProfile.name}'.", this);
            }

            ResetTimer();
        }

        private void Update()
        {
            if (soundProfile == null) return;

            _timer += Time.deltaTime;

            if (_timer >= _nextTime)
            {
                PlayRandomWhoosh();
                ResetTimer();
            }
        }

        private void PlayRandomWhoosh()
        {
            AudioClip clip = null;
            if (soundProfile.windWhoosh1 != null && soundProfile.windWhoosh2 != null)
                clip = Random.value > 0.5f ? soundProfile.windWhoosh1 : soundProfile.windWhoosh2;
            else if (soundProfile.windWhoosh1 != null)
                clip = soundProfile.windWhoosh1;
            else if (soundProfile.windWhoosh2 != null)
                clip = soundProfile.windWhoosh2;

            if (clip == null) return;

            if (soundProfile.windPitchVariance > 0f)
                _source.pitch = 1f + Random.Range(-soundProfile.windPitchVariance, soundProfile.windPitchVariance);
            else
                _source.pitch = 1f;

            _source.PlayOneShot(clip, soundProfile.windVolume);
        }

        private void ResetTimer()
        {
            _timer = 0f;
            if (soundProfile != null)
                _nextTime = Random.Range(soundProfile.windMinInterval, soundProfile.windMaxInterval);
            else
                _nextTime = 5f;
        }
    }
}
