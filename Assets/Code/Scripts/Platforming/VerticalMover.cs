using UnityEngine;

namespace Platforming
{
    /// <summary>
    /// Moves this object upward at a minimum velocity.
    /// When the player moves up, the object's speed increases proportionally
    /// so it can "chase" the player's ascent.
    /// </summary>
    public class VerticalMover : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _player;

        [Header("Speed")]
        [Tooltip("Minimum upward speed (units/sec) always applied.")]
        [SerializeField] private float _minVelocity = 1f;

        [Tooltip("Extra speed added per unit of vertical distance the player is above this object. " +
                 "e.g. 0.5 means +0.5 units/sec for every 1 unit the player is higher.")]
        [SerializeField] private float _speedPerUnit = 0.5f;

        [Tooltip("Clamps the maximum speed so it never goes infinite.")]
        [SerializeField] private float _maxVelocity = 20f;

        [Tooltip("Smoothing applied to velocity changes (lower = snappier, higher = smoother).")]
        [SerializeField] private float _smoothing = 5f;

        [Header("Playback")]
        [SerializeField] private bool _playOnAwake = false;

        // ── runtime ──────────────────────────────────────────────────────────
        private float _currentVelocity;
        private bool  _playing;

        // ── public API ───────────────────────────────────────────────────────
        public void Play()
        {
            _playing = true;
        }

        public void Stop()  => _playing = false;
        public void Pause() => _playing = false;
        public void Resume() => _playing = true;

        // ── lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (_player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null) _player = go.transform;
            }

            _currentVelocity = _minVelocity;

            if (_playOnAwake) Play();
        }

        private void Update()
        {
            if (!_playing) return;

            // Positive distance = player is above this object
            float distanceAbove = _player != null
                ? Mathf.Max(0f, _player.position.y - transform.position.y)
                : 0f;

            float targetVelocity = Mathf.Min(_minVelocity + distanceAbove * _speedPerUnit, _maxVelocity);

            _currentVelocity = Mathf.Lerp(_currentVelocity, targetVelocity, _smoothing * Time.deltaTime);

            transform.position += Vector3.up * (_currentVelocity * Time.deltaTime);
        }
    }
}
