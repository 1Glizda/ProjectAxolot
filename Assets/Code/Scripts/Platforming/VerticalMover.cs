using UnityEngine;
using Interfaces;

namespace Platforming
{
    public enum VerticalMoverMode
    {
        PlayerChaser,
        Keyframes
    }

    [System.Serializable]
    public struct VerticalKeyframe
    {
        [Tooltip("Target Y offset from the initial starting position.")]
        public float TargetYDelta;
        
        [Tooltip("How long (in seconds) it takes to travel to this keyframe from the previous one.")]
        public float Duration;
    }

    /// <summary>
    /// Moves this object vertically. Can either chase the player or follow a sequence of keyframes.
    /// </summary>
    public class VerticalMover : MonoBehaviour, IResettable
    {
        [Header("Mode")]
        [SerializeField] private VerticalMoverMode _mode = VerticalMoverMode.PlayerChaser;

        [Header("Chaser Settings")]
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

        [Header("Keyframe Settings")]
        [Tooltip("List of target Y deltas and how many seconds it takes to reach each one.")]
        [SerializeField] private VerticalKeyframe[] _keyframes;
        [Tooltip("If true, the animation will snap back to the start and loop forever once finished.")]
        [SerializeField] private bool _loopKeyframes = false;

        [Header("Playback")]
        [SerializeField] private bool _playOnAwake = false;

        // ── runtime ──────────────────────────────────────────────────────────
        private float _currentVelocity;
        private bool  _playing;

        private float _originalY;
        private Vector3 _initialPos;
        private int _currentKeyframeIndex;
        private float _keyframeTimer;
        private float _startYDelta;

        // ── public API ───────────────────────────────────────────────────────
        public void Play()
        {
            if (!_playing && _mode == VerticalMoverMode.Keyframes && _currentKeyframeIndex == 0 && _keyframeTimer == 0f)
            {
                _originalY = transform.position.y;
                _startYDelta = 0f;
            }
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
            _initialPos = transform.position;
            _originalY = transform.position.y;

            if (_playOnAwake) Play();
        }

        public void TriggerReset()
        {
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.position = _initialPos;
                rb.linearVelocity = Vector2.zero;
            }
            transform.position = _initialPos;
            _originalY = _initialPos.y;
            
            _currentVelocity = _minVelocity;
            _currentKeyframeIndex = 0;
            _keyframeTimer = 0f;
            _startYDelta = 0f;
            
            _playing = false;
        }

        private void Update()
        {
            if (!_playing) return;

            if (_mode == VerticalMoverMode.PlayerChaser)
            {
                UpdateChaser();
            }
            else if (_mode == VerticalMoverMode.Keyframes)
            {
                UpdateKeyframes();
            }
        }

        private void UpdateChaser()
        {
            // Positive distance = player is above this object
            float distanceAbove = _player != null
                ? Mathf.Max(0f, _player.position.y - transform.position.y)
                : 0f;

            float targetVelocity = Mathf.Min(_minVelocity + distanceAbove * _speedPerUnit, _maxVelocity);

            _currentVelocity = Mathf.Lerp(_currentVelocity, targetVelocity, _smoothing * Time.deltaTime);

            transform.position += Vector3.up * (_currentVelocity * Time.deltaTime);
        }

        private void UpdateKeyframes()
        {
            if (_keyframes == null || _keyframes.Length == 0) return;
            if (_currentKeyframeIndex >= _keyframes.Length) return;

            var currentFrame = _keyframes[_currentKeyframeIndex];
            _keyframeTimer += Time.deltaTime;

            float t = currentFrame.Duration > 0f ? Mathf.Clamp01(_keyframeTimer / currentFrame.Duration) : 1f;

            // Smoothstep for nicer movement
            float easeT = t * t * (3f - 2f * t);
            
            float currentYDelta = Mathf.Lerp(_startYDelta, currentFrame.TargetYDelta, easeT);
            transform.position = new Vector3(transform.position.x, _originalY + currentYDelta, transform.position.z);

            if (t >= 1f)
            {
                _startYDelta = currentFrame.TargetYDelta;
                _keyframeTimer = 0f;
                _currentKeyframeIndex++;

                if (_currentKeyframeIndex >= _keyframes.Length)
                {
                    if (_loopKeyframes)
                    {
                        _currentKeyframeIndex = 0;
                        _startYDelta = 0f; 
                        // Note: Will snap back to original position on the next frame.
                    }
                    else
                    {
                        _playing = false;
                    }
                }
            }
        }
    }
}
