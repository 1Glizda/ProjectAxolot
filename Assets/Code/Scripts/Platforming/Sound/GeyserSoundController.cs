using Interfaces;
using UnityEngine;

namespace Platforming.Sound
{
    /// <summary>
    /// Geyser sound controller. Attach alongside a GeyserBehaviour component.
    /// Plays ON/OFF one-shot clips when the geyser state changes, and optionally
    /// loops an active rumble while the geyser is erupting.
    /// All audio clips are centralised in the EnvironmentSoundProfileSo.
    /// </summary>
    public class GeyserSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnvironmentSoundProfileSo soundProfile;
        [SerializeField] private GeyserBehaviour geyserBehaviour;

        [Header("Spatial")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float maxDistance = 25f;

        private AudioSource _oneShotSource;
        private AudioSource _loopSource;
        private GeyserBehaviour.GeyserState _lastState;

        private void Awake()
        {
            // One-shot source for ON/OFF clips
            _oneShotSource = gameObject.AddComponent<AudioSource>();
            _oneShotSource.playOnAwake = false;
            _oneShotSource.spatialBlend = spatialBlend;
            _oneShotSource.maxDistance = maxDistance;
            _oneShotSource.rolloffMode = AudioRolloffMode.Linear;

            // Loop source for active rumble
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
            _loopSource.spatialBlend = spatialBlend;
            _loopSource.maxDistance = maxDistance;
            _loopSource.rolloffMode = AudioRolloffMode.Linear;
        }

        private void Start()
        {
            if (geyserBehaviour == null)
                geyserBehaviour = GetComponent<GeyserBehaviour>();

            if (soundProfile == null)
            {
                Debug.LogWarning($"[{gameObject.name}] GeyserSoundController: No EnvironmentSoundProfileSo assigned.", this);
                return;
            }

            if (geyserBehaviour == null)
            {
                Debug.LogWarning($"[{gameObject.name}] GeyserSoundController: GeyserBehaviour not found. Please assign it.", this);
                return;
            }

            _lastState = geyserBehaviour.CurrentState;
        }

        private void Update()
        {
            if (geyserBehaviour == null || soundProfile == null) return;

            GeyserBehaviour.GeyserState currentState = geyserBehaviour.CurrentState;

            if (currentState != _lastState)
            {
                HandleStateChange(_lastState, currentState);
                _lastState = currentState;
            }
        }

        private void HandleStateChange(GeyserBehaviour.GeyserState from, GeyserBehaviour.GeyserState to)
        {
            switch (to)
            {
                case GeyserBehaviour.GeyserState.Active:
                    // Geyser turned ON
                    if (soundProfile.geyserOnClip != null)
                        _oneShotSource.PlayOneShot(soundProfile.geyserOnClip, soundProfile.geyserOneShotVolume);

                    if (soundProfile.geyserActiveLoop != null && !_loopSource.isPlaying)
                    {
                        _loopSource.clip = soundProfile.geyserActiveLoop;
                        _loopSource.volume = soundProfile.geyserLoopVolume;
                        _loopSource.Play();
                    }
                    break;

                case GeyserBehaviour.GeyserState.Inactive:
                    // Geyser turned OFF
                    if (soundProfile.geyserOffClip != null)
                        _oneShotSource.PlayOneShot(soundProfile.geyserOffClip, soundProfile.geyserOneShotVolume);

                    if (_loopSource.isPlaying)
                        _loopSource.Stop();
                    break;

                case GeyserBehaviour.GeyserState.Blocked:
                    // Blocked — stop the loop
                    if (_loopSource.isPlaying)
                        _loopSource.Stop();
                    break;
            }
        }
    }
}
