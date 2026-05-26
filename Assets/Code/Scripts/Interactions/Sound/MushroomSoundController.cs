using Interfaces;
using UnityEngine;

namespace Interactions.Sound
{
    /// <summary>
    /// Mushroom explosion/cloud sound controller. Attach alongside ExplodingMushroomBehaviour.
    /// Plays explosion one-shot on explode and optionally loops a spore cloud sound
    /// while the mushroom is in its exploded state.
    /// All audio clips are centralised in the EnvironmentSoundProfileSo.
    /// </summary>
    public class MushroomSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnvironmentSoundProfileSo soundProfile;
        [SerializeField] private ExplodingMushroomBehaviour mushroom;

        [Header("Spatial")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float maxDistance = 25f;

        private AudioSource _oneShotSource;
        private AudioSource _loopSource;

        private void Awake()
        {
            _oneShotSource = gameObject.AddComponent<AudioSource>();
            _oneShotSource.playOnAwake = false;
            _oneShotSource.spatialBlend = spatialBlend;
            _oneShotSource.maxDistance = maxDistance;
            _oneShotSource.rolloffMode = AudioRolloffMode.Linear;

            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
            _loopSource.spatialBlend = spatialBlend;
            _loopSource.maxDistance = maxDistance;
            _loopSource.rolloffMode = AudioRolloffMode.Linear;
        }

        private void Start()
        {
            if (mushroom == null)
                mushroom = GetComponent<ExplodingMushroomBehaviour>();

            if (soundProfile == null)
            {
                Debug.LogWarning($"[{gameObject.name}] MushroomSoundController: No EnvironmentSoundProfileSo assigned.", this);
                return;
            }

            if (mushroom == null)
            {
                Debug.LogWarning($"[{gameObject.name}] MushroomSoundController: ExplodingMushroomBehaviour not found. Please assign it.", this);
                return;
            }

            if (soundProfile.mushroomExplosionClip == null)
                Debug.LogWarning($"[{gameObject.name}] MushroomSoundController: No mushroomExplosionClip in profile '{soundProfile.name}'.", this);
            if (soundProfile.mushroomCloudLoop == null)
                Debug.LogWarning($"[{gameObject.name}] MushroomSoundController: No mushroomCloudLoop in profile '{soundProfile.name}'.", this);

            mushroom.OnExplode += HandleExplode;
            mushroom.OnRecover += HandleRecover;
        }

        private void OnDestroy()
        {
            if (mushroom != null)
            {
                mushroom.OnExplode -= HandleExplode;
                mushroom.OnRecover -= HandleRecover;
            }
        }

        private void HandleExplode()
        {
            if (soundProfile == null) return;

            if (soundProfile.mushroomExplosionClip != null)
                _oneShotSource.PlayOneShot(soundProfile.mushroomExplosionClip, soundProfile.mushroomExplosionVolume);

            if (soundProfile.mushroomCloudLoop != null)
            {
                _loopSource.clip = soundProfile.mushroomCloudLoop;
                _loopSource.volume = soundProfile.mushroomCloudVolume;
                _loopSource.Play();
            }
        }

        private void HandleRecover()
        {
            if (_loopSource.isPlaying)
                _loopSource.Stop();
        }
    }
}
