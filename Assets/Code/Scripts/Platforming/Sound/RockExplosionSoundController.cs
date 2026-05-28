using Interfaces;
using UnityEngine;
using UnityEngine.Audio;

namespace Platforming.Sound
{
    /// <summary>
    /// Rock explosion sound controller. Attach alongside a BreakableWall component.
    /// Plays a one-shot explosion clip when the wall breaks.
    /// All audio clips are centralised in the EnvironmentSoundProfileSo.
    /// </summary>
    public class RockExplosionSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnvironmentSoundProfileSo soundProfile;
        [SerializeField] private BreakableWall breakableWall;

        [Header("Spatial")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;

        [Header("Mixer")]
        [Tooltip("Assign the SFX mixer group so volume can be controlled from settings.")]
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private float maxDistance = 30f;

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
            if (breakableWall == null)
                breakableWall = GetComponent<BreakableWall>();

            if (soundProfile == null)
            {
                Debug.LogWarning($"[{gameObject.name}] RockExplosionSoundController: No EnvironmentSoundProfileSo assigned.", this);
                return;
            }

            if (breakableWall == null)
            {
                Debug.LogWarning($"[{gameObject.name}] RockExplosionSoundController: BreakableWall not found. Please assign it.", this);
                return;
            }

            if (soundProfile.rockExplosionClip == null)
                Debug.LogWarning($"[{gameObject.name}] RockExplosionSoundController: No rockExplosionClip in profile '{soundProfile.name}'.", this);

            breakableWall.OnBreak += HandleBreak;
        }

        private void OnDestroy()
        {
            if (breakableWall != null)
                breakableWall.OnBreak -= HandleBreak;
        }

        private void HandleBreak()
        {
            if (soundProfile == null || soundProfile.rockExplosionClip == null || _source == null) return;

            if (soundProfile.rockExplosionPitchVariance > 0f)
                _source.pitch = 1f + Random.Range(-soundProfile.rockExplosionPitchVariance, soundProfile.rockExplosionPitchVariance);
            else
                _source.pitch = 1f;

            _source.PlayOneShot(soundProfile.rockExplosionClip, soundProfile.rockExplosionVolume);
        }
    }
}
