using Player.Sound;
using UnityEngine;

namespace Player.AI
{
    /// <summary>
    /// AI-specific sound controller for the SimpleAi companion.
    /// Reads exposed state from SimpleAi and drives a SoundController for:
    /// - Looping footsteps when moving on ground
    /// - Periodic idle chirps when arrived/stationary
    /// - Random singing clips when off-camera (3D positioned audio)
    /// 
    /// Setup:
    /// 1. Add this component to the AI companion root GameObject
    /// 2. Add a SoundController component with TWO AudioSources:
    ///    - Loop source: for footstep loops
    ///    - OneShot source: set Spatial Blend to 1.0 for 3D singing
    /// 3. Assign the SoundProfile SO with your AI audio clips
    /// 4. Wire up the SimpleAi and Renderer references in the inspector
    /// </summary>
    public class AiSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SimpleAi simpleAi;
        [SerializeField] private SoundController soundController;
        [SerializeField] private SoundProfileSo soundProfile;
        [SerializeField] private Renderer visibilityRenderer;

        [Header("Movement Detection")]
        [Tooltip("Minimum horizontal speed to be considered 'moving' for footstep audio.")]
        [SerializeField] private float movementThreshold = 0.5f;

        // Movement tracking
        private bool _isMoving;

        // Idle chirp timer
        private float _idleTimer;
        private float _nextChirpTime;
        private bool _isIdle;

        // Singing timer
        private float _singingTimer;
        private float _nextSingTime;

        private void Start()
        {
            // Auto-detect references if null
            if (simpleAi == null) simpleAi = GetComponentInParent<SimpleAi>();
            if (soundController == null) soundController = GetComponent<SoundController>();
            if (visibilityRenderer == null) visibilityRenderer = GetComponentInChildren<Renderer>();

            if (simpleAi == null)
            {
                Debug.LogError($"[{gameObject.name}] AiSoundController: SimpleAi reference is missing! Please assign it in the Inspector.", this);
                return;
            }

            if (soundController == null)
            {
                Debug.LogError($"[{gameObject.name}] AiSoundController: SoundController component is missing! Please attach it to this GameObject or assign it in the Inspector.", this);
                return;
            }

            if (soundProfile == null)
            {
                Debug.LogError($"[{gameObject.name}] AiSoundController: SoundProfileSo ScriptableObject is missing! Please create a Sound Profile and assign it in the Inspector.", this);
                return;
            }

            // Log warnings for unassigned clips to guide the developer
            if (soundProfile.footstepLoop == null)
                Debug.LogWarning($"[{gameObject.name}] AiSoundController: Footstep loop clip is not assigned in the SoundProfile '{soundProfile.name}'.", this);
            if (soundProfile.idleChirps == null || soundProfile.idleChirps.Length == 0)
                Debug.LogWarning($"[{gameObject.name}] AiSoundController: Idle chirps are not assigned in the SoundProfile '{soundProfile.name}'.", this);
            if (soundProfile.singingClips == null || soundProfile.singingClips.Length == 0)
                Debug.LogWarning($"[{gameObject.name}] AiSoundController: Singing clips are not assigned in the SoundProfile '{soundProfile.name}'.", this);

            ResetIdleChirpTimer(soundProfile.idleChirpInitialDelay);
            ResetSingingTimer();
        }

        private void Update()
        {
            if (simpleAi == null || soundProfile == null || soundController == null) return;

            Rigidbody2D rb = simpleAi.Rb;
            bool grounded = simpleAi.IsGrounded;
            string state = simpleAi.CurrentState;
            float absHSpeed = rb != null ? Mathf.Abs(rb.linearVelocityX) : 0f;

            bool moving = grounded && absHSpeed > movementThreshold;
            bool arrived = state == "ARRIVED" || state == "NO TARGET";

            // ─── Footstep loop ─────────────────────────────────────
            if (moving && !_isMoving)
            {
                soundController.PlayLoop(soundProfile.footstepLoop, soundProfile.footstepVolume);
                _isMoving = true;
                ResetIdleState();
            }
            else if (!moving && _isMoving)
            {
                soundController.StopLoop();
                _isMoving = false;
            }

            // ─── Idle chirps ───────────────────────────────────────
            if (arrived && !moving)
            {
                _idleTimer += Time.deltaTime;

                if (_idleTimer >= _nextChirpTime && soundProfile.idleChirps != null && soundProfile.idleChirps.Length > 0)
                {
                    soundController.PlayRandomOneShot(soundProfile.idleChirps, soundProfile.idleChirpVolume, soundProfile.pitchVariance);
                    ResetIdleChirpTimer();
                }

                if (!_isIdle)
                    _isIdle = true;
            }
            else if (_isIdle)
            {
                ResetIdleState();
            }

            // ─── Off-camera singing (3D positioned) ───────────────
            UpdateSinging();
        }

        // ─── Singing ───────────────────────────────────────────────

        private void UpdateSinging()
        {
            if (soundProfile.singingClips == null || soundProfile.singingClips.Length == 0) return;

            // Only sing when off-camera
            bool isVisible = visibilityRenderer != null && visibilityRenderer.isVisible;
            if (isVisible)
            {
                // Reset timer so singing doesn't fire immediately when going off-camera
                _singingTimer = 0f;
                return;
            }

            _singingTimer += Time.deltaTime;

            if (_singingTimer >= _nextSingTime)
            {
                soundController.PlayRandomOneShot(soundProfile.singingClips, soundProfile.singingVolume, soundProfile.pitchVariance);
                ResetSingingTimer();
            }
        }

        private void ResetSingingTimer()
        {
            _singingTimer = 0f;
            _nextSingTime = Random.Range(soundProfile.singingMinDelay, soundProfile.singingMaxDelay);
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
