using UnityEngine;
using UnityEngine.InputSystem;
using Interfaces;
using Player.Input;

namespace Platforming
{
    public enum VerticalMoverMode
    {
        PlayerChaser,
        Keyframes,
        PlayerOffsetCatchup
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

        [Header("Speed (Hard Mode - Default)")]
        [Tooltip("Minimum upward speed (units/sec) always applied.")]
        [SerializeField] private float _minVelocity = 1f;

        [Tooltip("Extra speed added per unit of vertical distance the player is above this object. " +
                 "e.g. 0.5 means +0.5 units/sec for every 1 unit the player is higher.")]
        [SerializeField] private float _speedPerUnit = 0.5f;

        [Tooltip("Clamps the maximum speed so it never goes infinite.")]
        [SerializeField] private float _maxVelocity = 20f;

        [Tooltip("Smoothing applied to velocity changes (lower = snappier, higher = smoother).")]
        [SerializeField] private float _smoothing = 5f;

        [Header("Offset Catchup Settings (Hard Mode)")]
        [Tooltip("The desired Y distance below the player. E.g. 5 means it tries to stay 5 units below the player, speeding up if it falls further behind.")]
        [SerializeField] private float _targetPlayerDistance = 5f;

        [Header("Speed (Normal Mode)")]
        [SerializeField] private float _normalMinVelocity = 0.5f;
        [SerializeField] private float _normalSpeedPerUnit = 0.25f;
        [SerializeField] private float _normalMaxVelocity = 10f;
        [SerializeField] private float _normalSmoothing = 5f;
        [SerializeField] private float _normalTargetPlayerDistance = 8f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _modeSprite;
        [SerializeField] private Color _hardModeColor = Color.red;
        [SerializeField] private Color _normalModeColor = Color.green;

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
        public bool IsPlaying => _playing;
        private bool _isHardMode = true;
        private InputSystem_Actions _inputActions;

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

            _inputActions = new InputSystem_Actions();
            _inputActions.Player.Enable();
            _inputActions.Player.HardMode.performed += ToggleHardMode;

            UpdateVisuals();

            _currentVelocity = CurrentMinVelocity;
            _initialPos = transform.position;
            _originalY = transform.position.y;

            if (_playOnAwake) Play();
        }

        private void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Player.HardMode.performed -= ToggleHardMode;
                _inputActions.Disable();
                _inputActions.Dispose();
            }
        }

        private void ToggleHardMode(InputAction.CallbackContext context)
        {
            _isHardMode = !_isHardMode;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_modeSprite != null)
            {
                _modeSprite.color = _isHardMode ? _hardModeColor : _normalModeColor;
            }
        }

        private float CurrentMinVelocity => _isHardMode ? _minVelocity : _normalMinVelocity;
        private float CurrentSpeedPerUnit => _isHardMode ? _speedPerUnit : _normalSpeedPerUnit;
        private float CurrentMaxVelocity => _isHardMode ? _maxVelocity : _normalMaxVelocity;
        private float CurrentSmoothing => _isHardMode ? _smoothing : _normalSmoothing;
        private float CurrentTargetPlayerDistance => _isHardMode ? _targetPlayerDistance : _normalTargetPlayerDistance;

        public void TriggerReset()
        {
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.position = _initialPos;
                rb.linearVelocity = Vector2.zero;
            }
            transform.position = _initialPos;
            _originalY = _initialPos.y;
            
            _currentVelocity = CurrentMinVelocity;
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
            else if (_mode == VerticalMoverMode.PlayerOffsetCatchup)
            {
                UpdateOffsetCatchup();
            }
        }

        private void UpdateOffsetCatchup()
        {
            float targetVelocity = CurrentMinVelocity;

            if (_player != null)
            {
                // Target Y is the specified distance below the player
                float targetY = _player.position.y - CurrentTargetPlayerDistance;
                float distanceBelowTarget = targetY - transform.position.y;

                if (distanceBelowTarget > 0f)
                {
                    // Too far below, speed up to catch up to the desired offset
                    targetVelocity = Mathf.Min(CurrentMinVelocity + distanceBelowTarget * CurrentSpeedPerUnit, CurrentMaxVelocity);
                }
            }

            _currentVelocity = Mathf.Lerp(_currentVelocity, targetVelocity, CurrentSmoothing * Time.deltaTime);
            transform.position += Vector3.up * (_currentVelocity * Time.deltaTime);
        }

        private void UpdateChaser()
        {
            // Positive distance = player is above this object
            float distanceAbove = _player != null
                ? Mathf.Max(0f, _player.position.y - transform.position.y)
                : 0f;

            float targetVelocity = Mathf.Min(CurrentMinVelocity + distanceAbove * CurrentSpeedPerUnit, CurrentMaxVelocity);

            _currentVelocity = Mathf.Lerp(_currentVelocity, targetVelocity, CurrentSmoothing * Time.deltaTime);

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
