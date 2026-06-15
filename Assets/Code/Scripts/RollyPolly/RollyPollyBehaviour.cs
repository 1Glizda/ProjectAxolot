using UnityEngine;
using Interfaces;

namespace RollyPolly
{
    [RequireComponent(typeof(Rigidbody2D))]
    public partial class RollyPollyBehaviour : MonoBehaviour, IPulseInteraction, IResettable
    {
        [Header("References")]
        [SerializeField] private GameObject _patrolSprite;
        [SerializeField] private GameObject _attackSprite;

        [Header("General Settings")]
        [SerializeField] private float _wallCheckDistance;
        [SerializeField] private bool _isFlipped;

        [Header("Patrol")]
        [SerializeField] private float _speed;
        [SerializeField] private float _patrolAcceleration = 15f;
        [SerializeField] private LayerMask _patrolBlockingLayers;
        [SerializeField] private bool _turnAtLedges = true;
        [SerializeField] private float _ledgeCheckDistance = 0.5f;
        [SerializeField] private Transform _leftPatrolBound;
        [SerializeField] private Transform _rightPatrolBound;

        [Header("Transition")]
        [SerializeField] private float _jumpForce;
        [SerializeField] private float _jumpTime;
        [SerializeField] private float _poofTime;
        [SerializeField] private GameObject _poofEffectPrefab;

        [Header("Attack & Stun")]
        [SerializeField] private LayerMask _rollBlockingLayers;
        [SerializeField] private float _rollSpeed = 8f;
        [SerializeField] private float _rollAcceleration = 10f;
        [SerializeField] private float _rollRotationSpeed = 360f;
        [SerializeField] private float _stunDuration = 0.2f;
        [SerializeField] private float _postStunAggroCooldown = 1.5f;

        [Header("Pulse Interactions")]
        [SerializeField] private float _pulseHitCooldown = 2f;
        [SerializeField] private float _pulseStutterDuration = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _pulseMomentumRatio = 0.9f;

        [Header("Detection")]
        [SerializeField] private float _detectionRange = 10f;
        [SerializeField] private float _detectionHeightTolerance = 1.2f;
        [SerializeField] private float _aboveHeightTolerance = 0.5f;
        [SerializeField] private bool _onlyDetectInFacingDirection = true;

        [Header("Loss of Track")]
        [SerializeField] private float _chaseMaxRange = 15f;
        [SerializeField] private float _loseTrackTime = 2f;

        [Header("Player Interaction")]
        [SerializeField] private int _damageAmount = 1;
        [SerializeField] private float _knockbackForceX = 12f;
        [SerializeField] private float _knockbackForceY = 6f;
        [SerializeField] private float _enemyRecoilForceX = 8f;
        [SerializeField] private float _enemyRecoilForceY = 4f;
        [SerializeField] private float _enemyRecoilDuration = 0.3f;
        [SerializeField] private float _movablePushForce = 2000f;
        [SerializeField] private float _movableImpulse = 5f;

        // Cached component references
        private Rigidbody2D _rb;
        private Rigidbody2D _playerRb;
        private Collider2D _activeCollider;
        private Collider2D _patrolCollider;
        private Collider2D _attackCollider;

        // State
        private ERollyState _currentState;
        private float _stateTimer;
        private bool _poofFired;

        // Timers
        private float _pulseCooldownTimer;
        private float _stutterTimer;
        private float _recoilTimer;
        private float _playerLostTimer;
        private float _postStunTimer;

        // Misc
        private bool _isDead;
        private Vector2 _smoothedGroundNormal = Vector2.up;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        private void Awake()
        {
            Player.GameState.CheckpointsManager.RegisterResettable(this);

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _playerRb = playerObj.GetComponent<Rigidbody2D>();
            }

            _currentState = ERollyState.Patrol;
            _rb = GetComponent<Rigidbody2D>();

            // Cache colliders from child sprites to avoid repeated GetComponent calls
            _patrolCollider = _patrolSprite != null ? _patrolSprite.GetComponent<Collider2D>() : null;
            _attackCollider = _attackSprite != null ? _attackSprite.GetComponent<Collider2D>() : null;

            _activeCollider = _patrolCollider;
            if (_activeCollider == null) Debug.LogError("No active collider found", this);

            // Premium Rigidbody2D configuration for smooth physics, gravity, and no sliding/jitter
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Ensure initial sprite states
            if (_patrolSprite != null) _patrolSprite.SetActive(true);
            if (_attackSprite != null) _attackSprite.SetActive(false);
        }

        private void OnDestroy()
        {
            Player.GameState.CheckpointsManager.UnregisterResettable(this);
        }

        private void Start()
        {
            UpdateSpriteDirection();
        }

        private void Update()
        {
            if (_isDead)
            {
                transform.Rotate(0f, 0f, 1080f * Time.deltaTime);
                return;
            }

            if (_pulseCooldownTimer > 0f) _pulseCooldownTimer -= Time.deltaTime;
            if (_stutterTimer > 0f) _stutterTimer -= Time.deltaTime;
            if (_recoilTimer > 0f) _recoilTimer -= Time.deltaTime;
            if (_postStunTimer > 0f) _postStunTimer -= Time.deltaTime;

            if (_currentState == ERollyState.Patrol)
            {
                TryDetectPlayer();
                TryFlip();
            }
            else if (_currentState == ERollyState.Attack)
            {
                RotateAttackSprite();
                TryFlipAttack();
            }

            // Smooth the ground normal every frame for visual stability
            if (IsGrounded())
            {
                Vector2 rawNormal = GetGroundNormal();
                _smoothedGroundNormal = Vector2.Lerp(
                    _smoothedGroundNormal, rawNormal,
                    RollyPollyConstants.NormalSmoothSpeed * Time.deltaTime).normalized;
            }
            else
            {
                _smoothedGroundNormal = Vector2.Lerp(
                    _smoothedGroundNormal, Vector2.up,
                    RollyPollyConstants.NormalSmoothSpeed * Time.deltaTime).normalized;
            }

            RotatePatrolSprite();
            _stateTimer += Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            switch (_currentState)
            {
                case ERollyState.Patrol:    TickPatrol();     break;
                case ERollyState.Transition: TickTransition(); break;
                case ERollyState.Attack:    TickAttack();     break;
                case ERollyState.Stunned:   TickStunned();    break;
            }
        }

        public void PulseInteract()
        {
            // Satisfies IPulseInteraction interface.
            // Actual pulse handling is triggered via physics callbacks (OnParticleCollision /
            // OnTriggerEnter2D) which call HandlePulseHit() — the interface method exists
            // solely for other systems that interact with IPulseInteraction by reference.
            HandlePulseHit();
        }

        private void ChangeState(ERollyState newState)
        {
            bool wasAttackOrStunned = _currentState == ERollyState.Attack || _currentState == ERollyState.Stunned;
            // Only revert to patrol visuals when actually returning to Patrol state
            bool isReverting = newState == ERollyState.Patrol && wasAttackOrStunned;

            if (isReverting)
            {
                // Play transformation poof effect
                if (_poofEffectPrefab != null)
                {
                    Instantiate(_poofEffectPrefab, transform.position, Quaternion.identity);
                }

                // Revert active sprites
                if (_patrolSprite != null) _patrolSprite.SetActive(true);
                if (_attackSprite != null)
                {
                    _attackSprite.SetActive(false);
                    _attackSprite.transform.localRotation = Quaternion.identity;
                }

                // Revert active colliders
                if (_attackCollider != null) _attackCollider.enabled = false;
                if (_patrolCollider != null)
                {
                    _patrolCollider.enabled = true;
                    _activeCollider = _patrolCollider;
                }

                // Re-align flipping to travel direction based on current physical velocity when resuming Patrol
                if (_rb != null && newState == ERollyState.Patrol)
                {
                    _isFlipped = _rb.linearVelocityX < 0f;
                }
                UpdateSpriteDirection();
            }

            _currentState = newState;
            _stateTimer = 0f;

            // Stop horizontal velocity immediately when entering Stunned, Transition, etc.
            if (_currentState != ERollyState.Patrol && _currentState != ERollyState.Attack && _rb != null)
            {
                _rb.linearVelocityX = 0f;
            }

            if (_currentState == ERollyState.Transition)
            {
                _poofFired = false;
                _playerLostTimer = 0f; // Reset loss timer when starting chase transition

                // Snap to face player immediately upon detecting them
                if (_playerRb != null)
                {
                    _isFlipped = _playerRb.transform.position.x < transform.position.x;
                    UpdateSpriteDirection();
                }

                if (_attackSprite != null)
                {
                    _attackSprite.transform.localRotation = Quaternion.identity;
                }
                if (_rb != null)
                {
                    _rb.linearVelocityY = _jumpForce;
                }
            }
        }

        // ─── Shared Helpers ──────────────────────────────────────────────

        private void UpdateSpriteDirection()
        {
            Vector3 scale = transform.localScale;
            // Negative scale flips horizontally. By default false (facing right) is positive scale.x.
            scale.x = _isFlipped ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        /// <summary>
        /// Returns the world-space position at the bottom-center of the active collider.
        /// This accounts for all nested child offsets and scaling automatically via Bounds.
        /// </summary>
        private Vector2 GetColliderBottom()
        {
            if (_activeCollider != null)
            {
                Bounds b = _activeCollider.bounds;
                return new Vector2(b.center.x, b.min.y);
            }
            return (Vector2)transform.position;
        }

        private bool IsGrounded()
        {
            Vector2 bottom = GetColliderBottom();
            Vector2 origin = bottom + Vector2.up * RollyPollyConstants.GroundCheckOriginOffset;
            var hit = Physics2D.Raycast(origin, Vector2.down, RollyPollyConstants.GroundedRayLength, _patrolBlockingLayers);
            return hit.collider != null;
        }

        private Vector2 GetGroundNormal()
        {
            Vector2 bottom = GetColliderBottom();
            Vector2 origin = bottom + Vector2.up * RollyPollyConstants.GroundCheckOriginOffset;
            var hit = Physics2D.Raycast(origin, Vector2.down, RollyPollyConstants.GroundNormalRayLength, _patrolBlockingLayers);
            if (hit.collider != null)
            {
                return hit.normal;
            }
            return Vector2.up;
        }

        /// <summary>
        /// Shared slope-aligned movement used by both TickPatrol and TickAttack.
        /// Strips into-slope velocity to prevent gravity accumulation, then accelerates
        /// along the slope tangent up to the target speed.
        /// </summary>
        private void MoveAlongSlope(float targetSpeed, float acceleration)
        {
            Vector2 normal = _smoothedGroundNormal;
            Vector2 slopeTangent = new Vector2(normal.y, -normal.x);
            Vector2 travelDir = _isFlipped ? -slopeTangent : slopeTangent;

            // Strip the velocity component sinking into the slope surface so gravity
            // accumulation doesn't push the body through the ground between ticks.
            Vector2 currentVel = _rb.linearVelocity;
            float normalComponent = Vector2.Dot(currentVel, normal);
            if (normalComponent < 0f)
                currentVel -= normalComponent * normal;
            _rb.linearVelocity = currentVel;

            // Apply constant physical acceleration capped at target speed
            Vector2 targetVelocity = travelDir * targetSpeed;
            _rb.linearVelocity = Vector2.MoveTowards(currentVel, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
    }
}
