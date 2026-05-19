using UnityEngine;

namespace Player.Sound
{
    /// <summary>
    /// Reusable audio component — attach to any character that needs sound.
    /// Manages a looping AudioSource (footsteps) and a one-shot AudioSource (SFX).
    /// </summary>
    public class SoundController : MonoBehaviour
    {
        [Header("Audio Sources")]
        [Tooltip("Used for looping clips (footsteps, ambient).")]
        [SerializeField] private AudioSource loopSource;

        [Tooltip("Used for one-shot SFX (jump, land, pulse, chirps, singing).")]
        [SerializeField] private AudioSource oneShotSource;

        [Header("Loop Crossfade")]
        [SerializeField] private float loopFadeSpeed = 8f;

        private AudioClip _pendingLoopClip;
        private float _targetLoopVolume;
        private bool _isFadingOut;

        // Debounce tracking
        private AudioClip _lastOneShotClip;
        private float _lastOneShotTime;

        private void Update()
        {
            HandleLoopFade();
        }

        // ─── Looping ───────────────────────────────────────────────

        /// <summary>
        /// Start playing a looping clip. If already playing the same clip, does nothing.
        /// Crossfades from the current loop to the new one.
        /// </summary>
        public void PlayLoop(AudioClip clip, float volume = 1f)
        {
            if (clip == null || loopSource == null) return;

            // Already playing this clip at target volume
            if (loopSource.clip == clip && loopSource.isPlaying && !_isFadingOut)
            {
                _targetLoopVolume = volume;
                return;
            }

            _pendingLoopClip = clip;
            _targetLoopVolume = volume;

            // If something is playing, fade it out first
            if (loopSource.isPlaying)
            {
                _isFadingOut = true;
            }
            else
            {
                StartNewLoop();
            }
        }

        /// <summary>
        /// Fade out and stop the current loop.
        /// </summary>
        public void StopLoop()
        {
            _pendingLoopClip = null;
            _isFadingOut = true;
        }

        /// <summary>
        /// True if the loop source is currently playing audio.
        /// </summary>
        public bool IsLooping => loopSource != null && loopSource.isPlaying;

        private void HandleLoopFade()
        {
            if (loopSource == null) return;

            if (_isFadingOut)
            {
                loopSource.volume = Mathf.MoveTowards(loopSource.volume, 0f, loopFadeSpeed * Time.deltaTime);
                if (loopSource.volume <= 0.01f)
                {
                    loopSource.Stop();
                    loopSource.volume = 0f;
                    _isFadingOut = false;

                    // If there is a pending clip, start it now
                    if (_pendingLoopClip != null)
                    {
                        StartNewLoop();
                    }
                }
            }
            else if (loopSource.isPlaying)
            {
                // Fade in towards target volume
                loopSource.volume = Mathf.MoveTowards(loopSource.volume, _targetLoopVolume, loopFadeSpeed * Time.deltaTime);
            }
        }

        private void StartNewLoop()
        {
            loopSource.clip = _pendingLoopClip;
            loopSource.loop = true;
            loopSource.volume = 0f;
            loopSource.Play();
            _pendingLoopClip = null;
            _isFadingOut = false;
        }

        // ─── One-Shot ──────────────────────────────────────────────

        /// <summary>
        /// Play a one-shot clip with optional pitch randomization.
        /// </summary>
        public void PlayOneShot(AudioClip clip, float volume = 1f, float pitchVariance = 0f)
        {
            if (clip == null || oneShotSource == null) return;

            if (pitchVariance > 0f)
                oneShotSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            else
                oneShotSource.pitch = 1f;

            oneShotSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Play a one-shot clip, but only if the same clip hasn't been played within minInterval seconds.
        /// Useful for preventing rapid-fire SFX spam (landing, pulse, etc.).
        /// </summary>
        public void PlayOneShotDebounced(AudioClip clip, float volume = 1f, float minInterval = 0.1f, float pitchVariance = 0f)
        {
            if (clip == null || oneShotSource == null) return;

            float now = Time.time;
            if (_lastOneShotClip == clip && now - _lastOneShotTime < minInterval) return;

            _lastOneShotClip = clip;
            _lastOneShotTime = now;

            PlayOneShot(clip, volume, pitchVariance);
        }

        /// <summary>
        /// Pick a random clip from an array and play it as a one-shot.
        /// </summary>
        public void PlayRandomOneShot(AudioClip[] clips, float volume = 1f, float pitchVariance = 0f)
        {
            if (clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlayOneShot(clip, volume, pitchVariance);
        }
    }
}
