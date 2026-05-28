using Interfaces;
using UnityEngine;
using UnityEngine.Audio;

namespace Platforming.Sound
{
    /// <summary>
    /// Spike / hazard damage sound controller. Attach alongside a KnockbackHazard component.
    /// Plays a one-shot damage SFX when the player is hit.
    /// All audio clips are centralised in the EnvironmentSoundProfileSo.
    /// </summary>
    public class SpikeSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnvironmentSoundProfileSo soundProfile;
        [SerializeField] private KnockbackHazard knockbackHazard;

        [Header("Spatial")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;

        [Header("Mixer")]
        [Tooltip("Assign the SFX mixer group so volume can be controlled from settings.")]
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private float maxDistance = 20f;

        private AudioSource _source;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = spatialBlend;
            _source.maxDistance = maxDistance;
            _source.rolloffMode = AudioRolloffMode.Linear;
            if (sfxMixerGroup != null) _source.outputAudioMixerGroup = sfxMixerGroup;
        }

        private void Start()
        {
            if (knockbackHazard == null)
                knockbackHazard = GetComponent<KnockbackHazard>();

            if (soundProfile == null)
            {
                Debug.LogWarning($"[{gameObject.name}] SpikeSoundController: No EnvironmentSoundProfileSo assigned.", this);
                return;
            }

            if (knockbackHazard == null)
            {
                Debug.LogWarning($"[{gameObject.name}] SpikeSoundController: KnockbackHazard not found. Please assign it.", this);
                return;
            }

            if (soundProfile.spikeDamageClip == null)
                Debug.LogWarning($"[{gameObject.name}] SpikeSoundController: No spikeDamageClip in profile '{soundProfile.name}'.", this);

            knockbackHazard.OnPlayerHit += HandlePlayerHit;
        }

        private void OnDestroy()
        {
            if (knockbackHazard != null)
                knockbackHazard.OnPlayerHit -= HandlePlayerHit;
        }

        private void HandlePlayerHit()
        {
            if (soundProfile == null || soundProfile.spikeDamageClip == null || _source == null) return;

            if (soundProfile.spikePitchVariance > 0f)
                _source.pitch = 1f + Random.Range(-soundProfile.spikePitchVariance, soundProfile.spikePitchVariance);
            else
                _source.pitch = 1f;

            _source.PlayOneShot(soundProfile.spikeDamageClip, soundProfile.spikeDamageVolume);
        }
    }
}
