using Interfaces;
using UnityEngine;
using UnityEngine.Audio;

namespace Platforming.Sound
{
    /// <summary>
    /// Sound controller for VerticalMover. Attach alongside a VerticalMover component.
    /// Plays ON/OFF one-shot clips when the mover starts/stops, and optionally
    /// loops an active rumble while the mover is playing.
    /// Reuses the geyser audio clips from EnvironmentSoundProfileSo.
    /// </summary>
    public class VerticalMoverSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnvironmentSoundProfileSo soundProfile;
        [SerializeField] private VerticalMover verticalMover;

        [Header("Spatial")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;

        [Header("Mixer")]
        [Tooltip("Assign the SFX mixer group so volume can be controlled from settings.")]
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private float maxDistance = 25f;

        private AudioSource _onSource;
        private AudioSource _offSource;
        private AudioSource _loopSource;
        private bool _wasPlaying;

        private bool _isFadingOutOnSource;
        private float _fadeStartTime;
        private float _initialFadeVolume;

        private void Awake()
        {
            // On source for ON clip (so it can be faded independently)
            _onSource = gameObject.AddComponent<AudioSource>();
            _onSource.playOnAwake = false;
            _onSource.spatialBlend = spatialBlend;
            _onSource.maxDistance = maxDistance;
            _onSource.rolloffMode = AudioRolloffMode.Linear;
            if (sfxMixerGroup != null) _onSource.outputAudioMixerGroup = sfxMixerGroup;

            // Off source for OFF/Idle clip
            _offSource = gameObject.AddComponent<AudioSource>();
            _offSource.playOnAwake = false;
            _offSource.spatialBlend = spatialBlend;
            _offSource.maxDistance = maxDistance;
            _offSource.rolloffMode = AudioRolloffMode.Linear;
            if (sfxMixerGroup != null) _offSource.outputAudioMixerGroup = sfxMixerGroup;

            // Loop source for active rumble
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
            _loopSource.spatialBlend = spatialBlend;
            _loopSource.maxDistance = maxDistance;
            _loopSource.rolloffMode = AudioRolloffMode.Linear;
            if (sfxMixerGroup != null) _loopSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        private void Start()
        {
            if (verticalMover == null)
                verticalMover = GetComponent<VerticalMover>();

            if (soundProfile == null)
            {
                Debug.LogWarning($"[{gameObject.name}] VerticalMoverSoundController: No EnvironmentSoundProfileSo assigned.", this);
                return;
            }

            if (verticalMover == null)
            {
                Debug.LogWarning($"[{gameObject.name}] VerticalMoverSoundController: VerticalMover not found. Please assign it.", this);
                return;
            }

            _wasPlaying = verticalMover.IsPlaying;
        }

        private void Update()
        {
            if (verticalMover == null || soundProfile == null) return;

            // Handle smooth fade out of the ON source over 1.0 second
            if (_isFadingOutOnSource)
            {
                float elapsed = Time.time - _fadeStartTime;
                if (elapsed >= 1f)
                {
                    _onSource.Stop();
                    _onSource.volume = 0f;
                    _isFadingOutOnSource = false;
                }
                else
                {
                    _onSource.volume = Mathf.Lerp(_initialFadeVolume, 0f, elapsed / 1f);
                }
            }

            bool isPlaying = verticalMover.IsPlaying;

            if (isPlaying != _wasPlaying)
            {
                HandleStateChange(isPlaying);
                _wasPlaying = isPlaying;
            }
        }

        private void HandleStateChange(bool isPlaying)
        {
            if (isPlaying)
            {
                // Mover turned ON - stop any pending fade out
                _isFadingOutOnSource = false;

                if (soundProfile.geyserOnClip != null)
                {
                    _onSource.clip = soundProfile.geyserOnClip;
                    _onSource.volume = soundProfile.geyserOneShotVolume;
                    _onSource.Play();
                }

                if (soundProfile.geyserActiveLoop != null && !_loopSource.isPlaying)
                {
                    _loopSource.clip = soundProfile.geyserActiveLoop;
                    _loopSource.volume = soundProfile.geyserLoopVolume;
                    _loopSource.Play();
                }
            }
            else
            {
                // Mover turned OFF - Smoothly fade out the ON clip over 1 second
                if (_onSource.isPlaying && !_isFadingOutOnSource)
                {
                    _isFadingOutOnSource = true;
                    _fadeStartTime = Time.time;
                    _initialFadeVolume = _onSource.volume;
                }

                if (soundProfile.geyserOffClip != null)
                {
                    _offSource.PlayOneShot(soundProfile.geyserOffClip, soundProfile.geyserOneShotVolume);
                }

                if (_loopSource.isPlaying)
                {
                    _loopSource.Stop();
                }
            }
        }
    }
}
