using UnityEngine;

namespace Player.Sound
{
    /// <summary>
    /// Player-specific sound controller. Hooks into IPlayerStateProvider events
    /// and polls state each frame for movement/idle sound transitions.
    /// 
    /// Setup:
    /// 1. Add this component to the Player root GameObject
    /// 2. Add a SoundController component to the same GameObject (or child)
    /// 3. Assign the SoundProfile SO with your audio clips
    /// 4. Wire up the PlayerController and PulseController references in the inspector
    /// </summary>
    public class PlayerSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Pulse.PulseController pulseController;
        [SerializeField] private SoundController soundController;
        [SerializeField] private SoundProfileSo soundProfile;

        [Header("Movement Detection")]
        [Tooltip("Minimum horizontal speed to be considered 'moving' for footstep audio.")]
        [SerializeField] private float movementThreshold = 0.5f;

        private IPlayerStateProvider _state;
        private bool _isMoving;
        private bool _isClimbingLoop;
        private int _pulseClipIndex;

        // Idle chirp timer
        private float _idleTimer;
        private float _nextChirpTime;
        private bool _isIdle;

        private void Start()
        {
            // Auto-detect references if null
            if (playerController == null) playerController = GetComponentInParent<PlayerController>();
            if (soundController == null) soundController = GetComponent<SoundController>();
            if (pulseController == null) pulseController = GetComponentInParent<Pulse.PulseController>();

            if (playerController == null)
            {
                Debug.LogError($"[{gameObject.name}] PlayerSoundController: PlayerController reference is missing! Please assign it in the Inspector.", this);
                return;
            }

            if (soundController == null)
            {
                Debug.LogError($"[{gameObject.name}] PlayerSoundController: SoundController component is missing! Please attach it to this GameObject or assign it in the Inspector.", this);
                return;
            }

            if (soundProfile == null)
            {
                Debug.LogError($"[{gameObject.name}] PlayerSoundController: SoundProfileSo ScriptableObject is missing! Please create a Sound Profile and assign it in the Inspector.", this);
                return;
            }

            _state = playerController;

            // Subscribe to existing events
            _state.OnJump += HandleJump;
            _state.OnLand += HandleLand;
            _state.OnStartClimb += HandleStartClimb;

            // Subscribe to pulse event
            if (pulseController != null)
                pulseController.OnPulse.AddListener(HandlePulse);
            else
                Debug.LogWarning($"[{gameObject.name}] PlayerSoundController: PulseController not found. Pulse SFX will not play.", this);

            // Log warnings for unassigned clips to guide the developer
            if ((soundProfile.footstepClips == null || soundProfile.footstepClips.Length == 0) && soundProfile.footstepClip == null)
                Debug.LogWarning($"[{gameObject.name}] PlayerSoundController: Footstep clips are not assigned in the SoundProfile '{soundProfile.name}'.", this);
            if (soundProfile.jumpClip == null)
                Debug.LogWarning($"[{gameObject.name}] PlayerSoundController: Jump clip is not assigned in the SoundProfile '{soundProfile.name}'.", this);
            if (soundProfile.landClip == null)
                Debug.LogWarning($"[{gameObject.name}] PlayerSoundController: Land clip is not assigned in the SoundProfile '{soundProfile.name}'.", this);
            if ((soundProfile.pulseClips == null || soundProfile.pulseClips.Length == 0) && soundProfile.pulseClip == null)
                Debug.LogWarning($"[{gameObject.name}] PlayerSoundController: Pulse clips are not assigned in the SoundProfile '{soundProfile.name}'.", this);
            if (soundProfile.climbLoop == null)
                Debug.LogWarning($"[{gameObject.name}] PlayerSoundController: Climb loop clip is not assigned in the SoundProfile '{soundProfile.name}'.", this);
            if (soundProfile.idleChirps == null || soundProfile.idleChirps.Length == 0)
                Debug.LogWarning($"[{gameObject.name}] PlayerSoundController: Idle chirps are not assigned in the SoundProfile '{soundProfile.name}'.", this);

            ResetIdleChirpTimer(soundProfile.idleChirpInitialDelay);
        }

        private void OnDestroy()
        {
            if (_state != null)
            {
                _state.OnJump -= HandleJump;
                _state.OnLand -= HandleLand;
                _state.OnStartClimb -= HandleStartClimb;
            }

            if (pulseController != null)
                pulseController.OnPulse.RemoveListener(HandlePulse);
        }

        private void Update()
        {
            if (_state == null || soundProfile == null || soundController == null) return;

            bool grounded = _state.IsGrounded;
            bool climbing = _state.IsClimbing;
            float absHSpeed = Mathf.Abs(_state.HorizontalVelocity);
            bool moving = grounded && absHSpeed > movementThreshold && !climbing;

            // ─── Climbing loop ─────────────────────────────────────
            if (climbing && !_isClimbingLoop)
            {
                Debug.Log($"[{gameObject.name}] PlayerSoundController: Start climbing loop. Clip: {(soundProfile.climbLoop != null ? soundProfile.climbLoop.name : "null")}, Volume: {soundProfile.climbVolume}", this);
                soundController.PlayLoop(soundProfile.climbLoop, soundProfile.climbVolume);
                _isClimbingLoop = true;
                _isMoving = false;
                ResetIdleState();
            }
            else if (!climbing && _isClimbingLoop)
            {
                Debug.Log($"[{gameObject.name}] PlayerSoundController: Stop climbing loop.", this);
                soundController.StopLoop();
                _isClimbingLoop = false;
            }

            // ─── Movement tracking (for idle state) ────────────
            if (!climbing)
            {
                if (moving && !_isMoving)
                {
                    _isMoving = true;
                    ResetIdleState();
                }
                else if (!moving && _isMoving)
                {
                    _isMoving = false;
                }
            }

            // ─── Idle chirps ───────────────────────────────────────
            if (grounded && !moving && !climbing)
            {
                _idleTimer += Time.deltaTime;

                if (_idleTimer >= _nextChirpTime && soundProfile.idleChirps != null && soundProfile.idleChirps.Length > 0)
                {
                    soundController.PlayRandomOneShot(soundProfile.idleChirps, soundProfile.idleChirpVolume, soundProfile.pitchVariance);
                    ResetIdleChirpTimer();
                }
            }
            else if (_isIdle)
            {
                ResetIdleState();
            }
        }

        // ─── Event Handlers ────────────────────────────────────────

        private void HandleJump()
        {
            Debug.Log($"[{gameObject.name}] PlayerSoundController: HandleJump called!");
            if (_isMoving)
            {
                _isMoving = false;
            }

            if (soundProfile.jumpClip != null)
                soundController.PlayOneShotDebounced(soundProfile.jumpClip, soundProfile.jumpVolume, 0.15f, soundProfile.pitchVariance);
        }

        private void HandleLand()
        {
            Debug.Log($"[{gameObject.name}] PlayerSoundController: HandleLand called!");
            if (soundProfile.landClip != null)
                soundController.PlayOneShotDebounced(soundProfile.landClip, soundProfile.landVolume, 0.15f, soundProfile.pitchVariance);
        }

        private void HandleStartClimb()
        {
            Debug.Log($"[{gameObject.name}] PlayerSoundController: HandleStartClimb called!");
            if (_isMoving)
            {
                _isMoving = false;
            }
        }

        private void HandlePulse(float _)
        {
            if (soundProfile.pulseClips != null && soundProfile.pulseClips.Length > 0)
            {
                AudioClip clip = soundProfile.pulseClips[_pulseClipIndex];
                if (clip != null)
                {
                    soundController.PlayOneShot(clip, soundProfile.pulseVolume);
                }
                _pulseClipIndex = (_pulseClipIndex + 1) % soundProfile.pulseClips.Length;
            }
            else if (soundProfile.pulseClip != null)
            {
                soundController.PlayOneShot(soundProfile.pulseClip, soundProfile.pulseVolume);
            }
        }

        // ─── Animation Events ──────────────────────────────────────

        /// <summary>
        /// Called by an Animation Event to trigger a single footstep sound.
        /// </summary>
        public void PlayFootstep()
        {
            if (soundProfile == null || soundController == null) return;

            if (soundProfile.footstepClips != null && soundProfile.footstepClips.Length > 0)
            {
                soundController.PlayRandomOneShot(soundProfile.footstepClips, soundProfile.footstepVolume, soundProfile.pitchVariance);
            }
            else if (soundProfile.footstepClip != null)
            {
                // We reuse pitchVariance here if you have PlayOneShotDebounced, but PlayOneShot works too.
                // Depending on soundController's available methods, we'll use PlayOneShot but maybe add pitch variance later if it's supported.
                // For now, let's just use what's available or PlayRandomOneShot with 1 clip.
                var arr = new AudioClip[] { soundProfile.footstepClip };
                soundController.PlayRandomOneShot(arr, soundProfile.footstepVolume, soundProfile.pitchVariance);
            }
        }

        // ─── Idle Timer ────────────────────────────────────────────

        private void ResetIdleChirpTimer(float overrideDelay = -1f)
        {
            _idleTimer = 0f;
            _isIdle = true;
            _nextChirpTime = overrideDelay >= 0f
                ? overrideDelay
                : Random.Range(soundProfile.idleChirpMinInterval, soundProfile.idleChirpMaxInterval);
        }

        private void ResetIdleState()
        {
            _idleTimer = 0f;
            _isIdle = false;
            ResetIdleChirpTimer(soundProfile.idleChirpInitialDelay);
        }
    }
}
