using Interfaces;
using UnityEngine;
using UnityEngine.Audio;

namespace Platforming.Sound
{
    /// <summary>
    /// Ambient water droplet sound. Attach to any object that should emit dripping sounds.
    /// Plays a looping ambient clip on Start with 3D spatial positioning.
    /// All audio clips are centralised in the EnvironmentSoundProfileSo.
    /// </summary>
    public class WaterDropletsSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnvironmentSoundProfileSo soundProfile;

        [Header("Spatial")]
        [Tooltip("0 = 2D, 1 = full 3D positional audio.")]
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
            _source.loop = true;
            _source.spatialBlend = spatialBlend;
            _source.maxDistance = maxDistance;
            _source.rolloffMode = AudioRolloffMode.Linear;
            if (sfxMixerGroup != null) _source.outputAudioMixerGroup = sfxMixerGroup;
        }

        private void Start()
        {
            if (soundProfile == null)
            {
                Debug.LogWarning($"[{gameObject.name}] WaterDropletsSoundController: No EnvironmentSoundProfileSo assigned.", this);
                return;
            }

            if (soundProfile.waterDropletsLoop == null)
            {
                Debug.LogWarning($"[{gameObject.name}] WaterDropletsSoundController: No waterDropletsLoop clip in profile '{soundProfile.name}'.", this);
                return;
            }

            _source.clip = soundProfile.waterDropletsLoop;
            _source.volume = soundProfile.waterDropletsVolume;
            _source.Play();
        }
    }
}
