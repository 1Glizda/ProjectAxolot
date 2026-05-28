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

        private AudioSource _onSource;
        private AudioSource _offSource;
        private AudioSource _loopSource;
        private GeyserBehaviour.GeyserState _lastState;

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

            // Off source for OFF/Idle clip
            _offSource = gameObject.AddComponent<AudioSource>();
            _offSource.playOnAwake = false;
            _offSource.spatialBlend = spatialBlend;
            _offSource.maxDistance = maxDistance;
            _offSource.rolloffMode = AudioRolloffMode.Linear;

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

            GeyserBehaviour.GeyserState currentState = geyserBehaviour.CurrentState;

            if (currentState != _lastState)
            {
                HandleStateChange(_lastState, currentState);
                _lastState = currentState;
            }
        }

        private void HandleStateChange(GeyserBehaviour.GeyserState from, GeyserBehaviour.GeyserState to)
        {
            Debug.Log($"[{gameObject.name}] GeyserState changed from {from} to {to}");
            
            float distanceToListener = -1f;
            AudioListener listener = FindFirstObjectByType<AudioListener>();
            if (listener != null)
            {
                distanceToListener = Vector3.Distance(transform.position, listener.transform.position);
            }

            switch (to)
            {
                case GeyserBehaviour.GeyserState.Active:
                    // Geyser turned ON - stop any pending fade out
                    _isFadingOutOnSource = false;

                    if (soundProfile.geyserOnClip != null)
                    {
                        Debug.Log($"[{gameObject.name}] Playing geyser ON clip: {soundProfile.geyserOnClip.name}\n" +
                                  $"Source Details - Vol: {soundProfile.geyserOneShotVolume}, SpatialBlend: {_onSource.spatialBlend}, " +
                                  $"Mute: {_onSource.mute}, Dist to Listener: {distanceToListener:F2}, MaxDist: {_onSource.maxDistance}");
                        
                        _onSource.clip = soundProfile.geyserOnClip;
                        _onSource.volume = soundProfile.geyserOneShotVolume;
                        _onSource.Play();
                    }
                    else
                    {
                        Debug.LogWarning($"[{gameObject.name}] Cannot play geyser ON clip: geyserOnClip is null in profile.");
                    }

                    if (soundProfile.geyserActiveLoop != null && !_loopSource.isPlaying)
                    {
                        Debug.Log($"[{gameObject.name}] Starting geyser loop: {soundProfile.geyserActiveLoop.name}\n" +
                                  $"Source Details - Vol: {soundProfile.geyserLoopVolume}, SpatialBlend: {_loopSource.spatialBlend}, " +
                                  $"Mute: {_loopSource.mute}, Dist to Listener: {distanceToListener:F2}");
                        _loopSource.clip = soundProfile.geyserActiveLoop;
                        _loopSource.volume = soundProfile.geyserLoopVolume;
                        _loopSource.Play();
                    }
                    else if (soundProfile.geyserActiveLoop == null)
                    {
                        Debug.LogWarning($"[{gameObject.name}] Cannot play geyser loop clip: geyserActiveLoop is null in profile.");
                    }
                    break;

                case GeyserBehaviour.GeyserState.Inactive:
                    // Geyser turned OFF - Smoothly fade out the ON clip over 1 second
                    if (_onSource.isPlaying && !_isFadingOutOnSource)
                    {
                        _isFadingOutOnSource = true;
                        _fadeStartTime = Time.time;
                        _initialFadeVolume = _onSource.volume;
                    }

                    if (soundProfile.geyserOffClip != null)
                    {
                        Debug.Log($"[{gameObject.name}] Playing geyser OFF one-shot: {soundProfile.geyserOffClip.name}\n" +
                                  $"Source Details - Vol: {soundProfile.geyserOneShotVolume}, SpatialBlend: {_offSource.spatialBlend}, " +
                                  $"Mute: {_offSource.mute}, Dist to Listener: {distanceToListener:F2}");
                        _offSource.PlayOneShot(soundProfile.geyserOffClip, soundProfile.geyserOneShotVolume);
                    }
                    else
                    {
                        Debug.LogWarning($"[{gameObject.name}] Cannot play geyser OFF clip: geyserOffClip is null in profile.");
                    }

                    if (_loopSource.isPlaying)
                    {
                        Debug.Log($"[{gameObject.name}] Stopping geyser loop.");
                        _loopSource.Stop();
                    }
                    break;

                case GeyserBehaviour.GeyserState.Blocked:
                    // Blocked — Smoothly fade out the ON clip over 1 second and stop the loop
                    if (_onSource.isPlaying && !_isFadingOutOnSource)
                    {
                        _isFadingOutOnSource = true;
                        _fadeStartTime = Time.time;
                        _initialFadeVolume = _onSource.volume;
                    }
                    
                    if (_loopSource.isPlaying)
                    {
                        Debug.Log($"[{gameObject.name}] Geyser blocked. Stopping geyser loop.");
                        _loopSource.Stop();
                    }
                    break;
            }
        }
    }
}
