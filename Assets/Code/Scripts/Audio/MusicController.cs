using UnityEngine;

namespace Audio
{
    /// <summary>
    /// A simple music controller to manage and switch between three background tracks.
    /// Supports automatic crossfading and runtime volume changes.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicController : MonoBehaviour
    {
        [Header("Audio Tracks")]
        [SerializeField] private AudioClip _track1;
        [SerializeField] private AudioClip _track2;
        [SerializeField] private AudioClip _track3;

        [Header("Settings")]
        [Tooltip("If true, the music will automatically start playing Track 1 on start.")]
        [SerializeField] private bool _playTrack1OnStart = true;

        [Header("Volume & Fading")]
        [Range(0f, 1f)]
        [SerializeField] private float _targetVolume = 1f;
        [SerializeField] private float _fadeDuration = 5f;

        private AudioSource _audioSource1;
        private AudioSource _audioSource2;
        
        private bool _isSource1Active = true;
        private float _source1TargetVol = 0f;
        private float _source2TargetVol = 0f;

        private void Awake()
        {
            _audioSource1 = GetComponent<AudioSource>();
            _audioSource2 = gameObject.AddComponent<AudioSource>();
            
            // Copy basic properties so they match (e.g. audio mixer)
            _audioSource2.outputAudioMixerGroup = _audioSource1.outputAudioMixerGroup;
            _audioSource2.spatialBlend = _audioSource1.spatialBlend;
            _audioSource2.priority = _audioSource1.priority;

            _audioSource1.loop = true;
            _audioSource2.loop = true;

            _audioSource1.volume = 0f;
            _audioSource2.volume = 0f;
        }

        private void Update()
        {
            float speed = _fadeDuration > 0f ? (1f / _fadeDuration) * Time.deltaTime : 1f;

            _audioSource1.volume = Mathf.MoveTowards(_audioSource1.volume, _source1TargetVol * _targetVolume, speed);
            _audioSource2.volume = Mathf.MoveTowards(_audioSource2.volume, _source2TargetVol * _targetVolume, speed);

            // Stop the inactive one if it reached 0 volume to save processing power
            if (_audioSource1.volume == 0f && _source1TargetVol == 0f && _audioSource1.isPlaying) _audioSource1.Stop();
            if (_audioSource2.volume == 0f && _source2TargetVol == 0f && _audioSource2.isPlaying) _audioSource2.Stop();
        }

        private void Start()
        {
            if (_playTrack1OnStart && _track1 != null)
            {
                PlayTrack1();
            }
        }

        /// <summary>
        /// Switches playback to Track 1.
        /// </summary>
        public void PlayTrack1()
        {
            PlayTrack(_track1);
        }

        /// <summary>
        /// Switches playback to Track 2.
        /// </summary>
        public void PlayTrack2()
        {
            PlayTrack(_track2);
        }

        /// <summary>
        /// Switches playback to Track 3.
        /// </summary>
        public void PlayTrack3()
        {
            PlayTrack(_track3);
        }

        /// <summary>
        /// Restarts the currently active track from the beginning.
        /// </summary>
        public void RestartPlayback()
        {
            AudioSource activeSource = _isSource1Active ? _audioSource1 : _audioSource2;
            if (activeSource.clip != null)
            {
                activeSource.Stop();
                activeSource.time = 0f;
                activeSource.Play();
            }
        }

        /// <summary>
        /// Internal helper to switch tracks. 
        /// Avoids restarting the track if it's already the one playing.
        /// </summary>
        private void PlayTrack(AudioClip newTrack)
        {
            if (newTrack == null)
            {
                Debug.LogWarning($"[{gameObject.name}] MusicController: Attempted to play a track, but the AudioClip is missing!");
                return;
            }

            AudioSource activeSource = _isSource1Active ? _audioSource1 : _audioSource2;

            // Don't restart if the same track is already playing and is the active target
            if (activeSource.clip == newTrack && ((_isSource1Active && _source1TargetVol > 0f) || (!_isSource1Active && _source2TargetVol > 0f)))
                return;

            // Switch active source
            _isSource1Active = !_isSource1Active;
            AudioSource newActiveSource = _isSource1Active ? _audioSource1 : _audioSource2;
            
            newActiveSource.clip = newTrack;
            newActiveSource.Play();

            if (_isSource1Active)
            {
                _source1TargetVol = 1f;
                _source2TargetVol = 0f;
            }
            else
            {
                _source1TargetVol = 0f;
                _source2TargetVol = 1f;
            }
            
            // Instantly apply volume if fade duration is 0
            if (_fadeDuration <= 0f)
            {
                _audioSource1.volume = _source1TargetVol * _targetVolume;
                _audioSource2.volume = _source2TargetVol * _targetVolume;
            }
        }
    }
}
