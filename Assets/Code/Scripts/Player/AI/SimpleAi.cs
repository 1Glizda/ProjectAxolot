using UnityEngine;
using Player.AI.Navigation;
using Interfaces;
using System.Collections.Generic;

namespace Player.AI
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class SimpleAi : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private float arrivalDistance = 0.5f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float climbSpeed = 3f;

        [Header("Detection")]
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float groundCheckDist = 0.15f;
        [SerializeField] private float wallCheckDist = 0.4f;

        [Header("Wall Jump")]
        [SerializeField] private LayerMask climbableLayers;
        [SerializeField] private float wallJumpForce = 10f;
        [SerializeField] private float wallJumpAngle = 30f;

        [Header("Push")]
        [SerializeField] private float pushForce = 15f;
        [SerializeField] private float pushDetectDist = 0.6f;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private AiBurrowController burrowController;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // ─── Public read-only state for AiSoundController ──────────
        public bool IsGrounded => _isGrounded;
        public bool IsClimbing => _isClimbing;
        public string CurrentState => _currentStateStr;
        public Rigidbody2D Rb => _rb;

        // ─── Events for sound/VFX (stubs — call Trigger* from gameplay code) ──
        public event System.Action OnEat;
        public event System.Action OnTongue;

        /// <summary>Call this from animation events or gameplay logic when the AI eats.</summary>
        public void TriggerEat() => OnEat?.Invoke();
        /// <summary>Call this from animation events or gameplay logic when the AI uses its tongue.</summary>
        public void TriggerTongue() => OnTongue?.Invoke();

        private Rigidbody2D _rb;
        private float _defaultGravity;
        

        // Detection
        private bool _isGrounded;
        private bool _wallAhead;
        private bool _wallAboveHead;
        private bool _wallCenterAhead;
        private bool _climbableWallAhead;
        private Vector2 _wallNormal;

        // State
        private bool _isClimbing;
        private float _jumpCooldown;
        private float _climbDir;
        private string _currentStateStr = "IDLE";
        private float _climbOvershootTimer;
        private float _currentVelocityX; // For SmoothDamp

        // Push
        private bool _isPushing;
        private IPushable _pushable;
        private bool _pushableAhead;

        // Stuck detection
        private Vector2 _lastPos;
        private float _stuckTimer;
        private float _logTimer;
        private float _pauseTimer;
        private float _facingDir = 1f;

        private Transform[] _currentPath;
        private int _currentAnchorIndex;
        private Queue<AiArea> _areaQueue = new Queue<AiArea>();
        private bool _isTraversingArea = false;

        private int _movableLayerMask;

        // ═══════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            
            // Enable interpolation to fix visual jitter (especially during jumps) when physics updates don't match monitor refresh rates
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // prevent passing through colliders at high speeds
            
            _defaultGravity = _rb.gravityScale;
            _movableLayerMask = LayerMask.GetMask("Movable");
            if (groundLayers == 0)
            {
                Debug.LogWarning("[SimpleAi - Awake] GroundLayers is NOT SET! Please set it in the inspector.");
            }
        }

        private void Start()
        {
            if (AiManager.Instance != null)
            {
                AiManager.Instance.OnAreaChanged += HandleAreaChanged;
            }
            if (burrowController != null)
            {
                burrowController.OnBurrowComplete += HandleBurrowComplete;
            }
        }

        private void OnDestroy()
        {
            if (AiManager.Instance != null)
            {
                AiManager.Instance.OnAreaChanged -= HandleAreaChanged;
            }
            if (burrowController != null)
            {
                burrowController.OnBurrowComplete -= HandleBurrowComplete;
            }
        }

        private void HandleAreaChanged(AiArea newArea)
        {
            if (newArea.anchorPoints == null || newArea.anchorPoints.Count == 0) return;

            _areaQueue.Enqueue(newArea);
            TryStartNextArea();
        }

        private void TryStartNextArea()
        {
            if (_isTraversingArea || _areaQueue.Count == 0) return;

            AiArea nextArea = _areaQueue.Dequeue();
            _currentPath = nextArea.anchorPoints.ToArray();
            _currentAnchorIndex = 0;
            target = _currentPath[_currentAnchorIndex];
            _pauseTimer = 0f;
            _isTraversingArea = true;

            // Reset push state on area change
            if (_isPushing) ExitPush("Area changed");

            // If the zone is marked for teleport, burrow instead of walking
            if (nextArea.teleportToNextZone && burrowController != null)
            {
                DebugLog($"[SimpleAi] Teleport zone! Burrowing to {target.name}");
                burrowController.StartBurrow(target.position);
                return;
            }

            DebugLog($"[SimpleAi] Area changed! Path loaded with {_currentPath.Length} points. Next target: {target.name}");
        }

        private void HandleBurrowComplete()
        {
            DebugLog($"[SimpleAi] Burrow complete. Resuming movement.");
            _pauseTimer = 0f;
            if (_currentPath != null && _currentAnchorIndex < _currentPath.Length - 1)
            {
                _currentAnchorIndex++;
                target = _currentPath[_currentAnchorIndex];
            }
            _stuckTimer = 0f;
        }

        // ═══════════════════════════════════════════════════════════
        //  MAIN LOOP
        // ═══════════════════════════════════════════════════════════

        private void FixedUpdate()
        {
            // Lock out all movement while the AI is burrowing
            if (burrowController != null && burrowController.IsBurrowing)
            {
                SetState("BURROWING");
                return;
            }

            if (target == null)
            {
                SetState("NO TARGET");
                return;
            }

            // --- Pause handling ---
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                SetState($"PAUSED ({_pauseTimer:F1}s)");
                _rb.linearVelocityX = Mathf.SmoothDamp(_rb.linearVelocityX, 0f, ref _currentVelocityX, 0.15f);

                if (_pauseTimer <= 0f)
                {
                    SequenceToNextNode();
                }
                return;
            }

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;

            // --- Horizontal direction & Facing ---
            float dirX = 0f;
            bool isFallingToTarget = !_isGrounded && toTarget.y < 0f && _rb.linearVelocityY <= 0f;
            float horizontalDeadzone = isFallingToTarget ? 0.5f : 0.05f;

            if (Mathf.Abs(toTarget.x) > horizontalDeadzone)
            {
                dirX = Mathf.Sign(toTarget.x);
                _facingDir = dirX;
            }

            bool wasGrounded = _isGrounded;
            CheckGround();
            CheckWall(_facingDir);
            CheckPushable(_facingDir);

            // --- Arrived ---
            if (toTarget.magnitude < arrivalDistance)
            {
                // Exit push mode if we were pushing
                if (_isPushing) ExitPush("Arrived at target");

                // Check for pause node settings
                AiNode nodeSettings = target.GetComponent<AiNode>();
                if (nodeSettings != null && nodeSettings.pauseDuration > 0f)
                {
                    _pauseTimer = nodeSettings.pauseDuration;
                    DebugLog($"[SimpleAi] Pausing at {target.name} for {nodeSettings.pauseDuration}s");
                    return;
                }

                SequenceToNextNode();
                return;
            }

            // --- Stuck detection ---
            if (Vector2.Distance(transform.position, _lastPos) < 0.05f)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer > 0.5f) SetState("STUCK_WARNING");
            }
            else
            {
                _stuckTimer = 0f;
            }
            _lastPos = transform.position;

            _jumpCooldown -= Time.deltaTime;

            // --- Log falling without jumping ---
            if (wasGrounded && !_isGrounded && _currentStateStr != "JUMPING" && _currentStateStr != "WALL_JUMPING" && !_isClimbing)
            {
                DebugLog($"[SimpleAi] WARNING: Fell off edge without jumping! State: {_currentStateStr}, Target: {target.name}, toTarget: {toTarget}");
            }

            // --- State routing ---
            if (_isPushing)
            {
                UpdatePushing(toTarget, dirX);
            }
            else if (_isClimbing)
            {
                UpdateClimbing(toTarget);
            }
            else
            {
                UpdateMovement(toTarget, dirX);
            }

            // --- Flip sprite ---
            if (spriteRenderer != null && !_isClimbing && dirX != 0f)
            {
                spriteRenderer.flipX = dirX < 0f;
            }

            LogStatePeriodically();
        }

        // ═══════════════════════════════════════════════════════════
        //  MOVEMENT
        // ═══════════════════════════════════════════════════════════

        private void UpdateMovement(Vector2 toTarget, float dirX)
        {
            SetState($"MOVING (dir:{dirX})");

            // --- Premium SmoothDamp Movement ---
            float targetVelX = dirX * moveSpeed;
            float currentVelX = _rb.linearVelocityX;

            float smoothTime;
            if (_isGrounded)
            {
                smoothTime = Mathf.Abs(targetVelX) > 0.01f ? 0.08f : 0.12f;
            }
            else
            {
                smoothTime = 0.35f;
            }

            _rb.linearVelocityX = Mathf.SmoothDamp(_rb.linearVelocityX, targetVelX, ref _currentVelocityX, smoothTime);

            // --- Enter push if pushable detected ahead and target is beyond it ---
            if (_isGrounded && _pushableAhead && _pushable != null && dirX != 0f)
            {
                DebugLog($"[SimpleAi] Pushable detected ahead! Entering push mode.");
                EnterPush();
                return;
            }

            // --- Mid-air wall re-grab on climbable surfaces ---
            if (!_isGrounded && _climbableWallAhead && _jumpCooldown <= 0f)
            {
                // Only grab if we are moving toward the wall, not jumping away from it
                float velDir = Mathf.Sign(_rb.linearVelocityX);
                float faceDir = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
                bool movingTowardWall = Mathf.Abs(_rb.linearVelocityX) > 0.5f && velDir == faceDir;

                if (movingTowardWall || _wallAhead)
                {
                    DebugLog($"[SimpleAi] Mid-air climbable wall detected! Auto-grabbing.");
                    _climbDir = dirX != 0f ? dirX : faceDir;
                    EnterClimb();
                    return;
                }
            }

            // --- Jump logic ---
            if (_isGrounded && _jumpCooldown <= 0f)
            {
                bool shortWall = _wallAhead && !_wallAboveHead;

                // Anticipatory Jump (wall ahead based on velocity)
                bool obstacleAhead = false;
                if (Mathf.Abs(currentVelX) > 1f)
                {
                    float lookAheadDist = wallCheckDist + (Mathf.Abs(currentVelX) * 0.35f);
                    Vector2 forward = Vector2.right * Mathf.Sign(currentVelX);
                    Vector2 center = bodyCollider.bounds.center;
                    // Lift feet check slightly higher to avoid tripping on small floor bumps
                    Vector2 feet = new Vector2(center.x, bodyCollider.bounds.min.y + 0.3f);

                    LayerMask combinedMask = groundLayers | climbableLayers;
                    obstacleAhead = Physics2D.Raycast(center, forward, lookAheadDist, combinedMask) ||
                                    Physics2D.Raycast(feet, forward, lookAheadDist, combinedMask);
                }

                // Check for ceiling blocking the jump
                Vector2 headPos = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y);
                float ceilingCheckDist = Mathf.Max(1.0f, toTarget.y);
                bool ceilingBlocked = toTarget.y > 0f && Physics2D.Raycast(headPos, Vector2.up, ceilingCheckDist, groundLayers | climbableLayers);

                // Gap Ahead detection (to prevent walking off edges into pits)
                bool gapAhead = false;
                if (Mathf.Abs(currentVelX) > 0.5f && toTarget.y >= -1.0f)
                {
                    float lookAheadDist = 0.8f;
                    Vector2 checkPos = new Vector2(
                        bodyCollider.bounds.center.x + Mathf.Sign(currentVelX) * lookAheadDist,
                        bodyCollider.bounds.center.y
                    );
                    float downDist = bodyCollider.bounds.extents.y + 1.5f;
                    LayerMask combinedMask = groundLayers | climbableLayers;

                    RaycastHit2D gapHit = Physics2D.Raycast(checkPos, Vector2.down, downDist, combinedMask);
                    
                    bool isGap = false;
                    if (!gapHit)
                    {
                        isGap = true;
                    }
                    else if (toTarget.y >= -0.5f && gapHit.point.y < bodyCollider.bounds.min.y - 0.5f)
                    {
                        // If target is up/level, but ground drops off, treat it as a gap
                        isGap = true;
                    }

                    if (isGap && Mathf.Sign(toTarget.x) == Mathf.Sign(currentVelX))
                    {
                        gapAhead = true;
                    }
                }

                // Arc Jump: ballistic trajectory prediction
                bool arcJump = !ceilingBlocked && ShouldArcJump(toTarget);

                // Target directly above with very little horizontal offset → jump straight up
                bool targetDirectlyAbove = toTarget.y > 1.5f && Mathf.Abs(toTarget.x) < 0.5f && !ceilingBlocked;

                if (shortWall && !ceilingBlocked)
                {
                    DebugLog($"[SimpleAi] Jumping over short wall!");
                    ExecuteJump(dirX);
                }
                else if (arcJump)
                {
                    DebugLog($"[SimpleAi] Arc jumping toward target! toTarget: {toTarget}");
                    ExecuteJump(dirX);
                }
                else if (gapAhead && !ceilingBlocked)
                {
                    DebugLog($"[SimpleAi] Gap detected ahead, jumping to cross!");
                    ExecuteJump(dirX);
                }
                else if (obstacleAhead && !_wallAhead && toTarget.y >= -1.5f && toTarget.y < 0.5f && !ceilingBlocked)
                {
                    // Disable anticipatory jumping for elevated targets, let ArcJump handle the exact timing
                    DebugLog($"[SimpleAi] Anticipating obstacle, jumping early!");
                    ExecuteJump(dirX);
                }
                else if (targetDirectlyAbove)
                {
                    DebugLog($"[SimpleAi] Target directly above ({toTarget.y:F2}), jumping!");
                    ExecuteJump(0f);
                }
            }

            // --- Enter climb: tall wall + (target above OR stuck) ---
            bool tallWall = _wallAhead && _wallAboveHead;
            bool shouldClimb = tallWall && (toTarget.y > 0.5f || _stuckTimer > 0.5f);

            if (shouldClimb)
            {
                DebugLog($"[SimpleAi] Encountered tall wall. Entering climb!");
                _climbDir = dirX;
                EnterClimb();
            }
            else if (_stuckTimer > 0.8f && _jumpCooldown <= 0f)
            {
                DebugLog("[SimpleAi] Stuck but no tall wall. Forcing jump to unstick!");
                ExecuteJump(dirX);
                _stuckTimer = 0f;
            }
        }

        private void ExecuteJump(float dirX = 0f)
        {
            // Don't override if already flying upwards fast (e.g. bouncy shroom)
            if (_rb.linearVelocityY < jumpForce * 1.5f)
            {
                _rb.linearVelocityY = Mathf.Max(_rb.linearVelocityY, jumpForce);
            }

            if (dirX != 0f)
            {
                _rb.linearVelocityX = dirX * moveSpeed;
            }

            _jumpCooldown = 0.1f;
            SetState("JUMPING");
        }

        /// <summary>
        /// Ballistic arc prediction. Solves the projectile motion equation to determine
        /// if jumping NOW would land the AI on or near the target in a natural parabolic arc.
        /// </summary>
        private bool ShouldArcJump(Vector2 toTarget)
        {
            // Need meaningful horizontal distance to justify an arc
            if (Mathf.Abs(toTarget.x) < 1.0f) return false;

            // Don't arc jump to targets far below (just walk off the edge and fall)
            if (toTarget.y < -1.0f) return false;

            float gravity = Mathf.Abs(Physics2D.gravity.y * _rb.gravityScale);
            if (gravity < 0.01f) return false;

            float v0y = jumpForce;
            float vx = moveSpeed;

            // Max reachable height with a single jump
            float maxHeight = (v0y * v0y) / (2f * gravity);
            if (toTarget.y > maxHeight * 0.9f) return false; // Can't reach this height

            // For targets at roughly the same height, only arc jump if there's a gap ahead
            // (prevents unnecessary jumping on flat ground)
            if (toTarget.y < 0.5f)
            {
                bool gapFound = false;
                float targetDist = Mathf.Abs(toTarget.x);
                float checkLimit = Mathf.Min(targetDist * 0.8f, 3.0f); // Don't check all the way to target to avoid hitting its base
                
                // Check multiple points ahead to verify there's an actual drop-off/gap
                LayerMask combinedMask = groundLayers | climbableLayers;
                for (float d = 0.5f; d <= checkLimit; d += 0.5f)
                {
                    Vector2 gapCheckOrigin = new Vector2(
                        bodyCollider.bounds.center.x + Mathf.Sign(toTarget.x) * d,
                        bodyCollider.bounds.center.y
                    );
                    
                    // Raycast down from center.y to below feet
                    float downDist = bodyCollider.bounds.extents.y + 1.5f;
                    if (!Physics2D.Raycast(gapCheckOrigin, Vector2.down, downDist, combinedMask))
                    {
                        gapFound = true;
                        break;
                    }
                }
                
                if (!gapFound) return false; // Solid ground all the way, just walk
            }

            // Solve quadratic: dy = v0y*t - 0.5*g*t²
            // Rearranged: 0.5*g*t² - v0y*t + dy = 0
            float discriminant = v0y * v0y - 2f * gravity * toTarget.y;
            if (discriminant < 0f) return false;

            float sqrtDisc = Mathf.Sqrt(discriminant);
            float t1 = (v0y - sqrtDisc) / gravity; // ascending pass
            float t2 = (v0y + sqrtDisc) / gravity; // descending pass

            float targetDx = Mathf.Abs(toTarget.x);

            // We want to land EXACTLY on the target or slightly overshoot it.
            // diff is how much we will overshoot the target if we jump right now.
            float diff1 = (t1 > 0.05f) ? (vx * t1 - targetDx) : -999f;
            float diff2 = (t2 > 0.05f) ? (vx * t2 - targetDx) : -999f;
            
            // 1. Perfect jump windows
            if (diff1 >= -0.08f && diff1 < 1.5f) return true;
            if (diff2 >= -0.08f && diff2 < 1.5f) return true;

            // 2. Emergency jump (Too Close)
            // If we are already closer than the shortest possible perfect jump arc, we missed the window 
            // (e.g. we just landed on a small platform and the next point is close).
            // Jump immediately so we don't walk off the edge into a pit!
            float shortestJump = float.MaxValue;
            if (t1 > 0.05f) shortestJump = Mathf.Min(shortestJump, vx * t1);
            if (t2 > 0.05f) shortestJump = Mathf.Min(shortestJump, vx * t2);

            if (shortestJump != float.MaxValue && targetDx < shortestJump - 0.5f)
            {
                return true;
            }

            return false;
        }

        // ═══════════════════════════════════════════════════════════
        //  CLIMBING & WALL JUMP
        // ═══════════════════════════════════════════════════════════

        private void UpdateClimbing(Vector2 toTarget)
        {
            SetState($"CLIMBING (dir:{_climbDir})");

            if (_wallAboveHead && _stuckTimer >= 10f)
            {
                DebugLog("[SimpleAi] Stuck climbing for 10s. Forcing drop.");
                ExitClimb("Stuck timeout failsafe");
                _stuckTimer = 0f;
                return;
            }

            if (TryWallJump(toTarget)) return;

            if (!_climbableWallAhead)
            {
                float checkDist = 4.0f; 
                Vector2 backDir = Vector2.right * -_climbDir;
                Vector2 origin = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.center.y);
                
                if (toTarget.y > 0.5f || (Mathf.Sign(toTarget.x) == Mathf.Sign(backDir.x)))
                {
                    RaycastHit2D backHit = Physics2D.Raycast(origin, backDir, checkDist, climbableLayers);
                    if (backHit.collider != null)
                    {
                        DebugLog($"[SimpleAi] Moss ended, but found moss behind! Wall jumping!");
                        ExitClimb("Wall jump to opposite moss");
                        
                        Vector2 jumpDir = new Vector2(-_climbDir, 1.2f).normalized;
                        _rb.linearVelocity = jumpDir * wallJumpForce;
                        _currentVelocityX = _rb.linearVelocityX;
                        _jumpCooldown = 0.3f;
                        _facingDir = -_climbDir;
                        if (spriteRenderer != null) spriteRenderer.flipX = (_facingDir < 0);
                        return;
                    }
                }
            }

            // Press gently into the wall so we stay attached
            _rb.linearVelocityX = _climbDir * 1f;

            // =================================================================
            // NEW: Ledge Vaulting
            // If the head and center have cleared the wall, we are at the top lip!
            // =================================================================
            if (!_wallAboveHead && !_wallCenterAhead)
            {
                DebugLog("[SimpleAi] Reached top of ledge. Vaulting onto platform!");
                ExitClimb("Vaulting ledge");
                
                // Push up and forward to land safely on top
                _rb.linearVelocityY = jumpForce * 0.7f;
                _rb.linearVelocityX = _climbDir * moveSpeed * 1.2f;
                _currentVelocityX = _rb.linearVelocityX;
                _jumpCooldown = 0.3f;
                return;
            }

            // =================================================================
            // Ledge Vaulting
            // =================================================================
            if (!_wallAboveHead && !_wallCenterAhead)
            {
                DebugLog("[SimpleAi] Reached top of ledge. Vaulting onto platform!");
                ExitClimb("Vaulting ledge");
                
                // Push up and forward to land safely on top
                _rb.linearVelocityY = jumpForce * 0.7f;
                _rb.linearVelocityX = _climbDir * moveSpeed * 1.2f;
                _currentVelocityX = _rb.linearVelocityX;
                _jumpCooldown = 0.3f;
                return;
            }

            // =================================================================
            // Omnidirectional Climbing & Forgiving Arrival
            // =================================================================
            
            // Check if the target is actually attached to this wall (close horizontally)
            bool isTargetOnWall = Mathf.Abs(toTarget.x) < 1.5f;

            // 1. Arrive at node if it's placed ON the wall
            if (Mathf.Abs(toTarget.y) < arrivalDistance && isTargetOnWall)
            {
                _rb.linearVelocityY = 0f; 
                DebugLog("[SimpleAi] Aligned with wall node vertically. Forcing arrival.");
                SequenceToNextNode();
                return; 
            }
            // 2. Keep climbing UP if the target is higher... 
            // OR if we are at the top ledge and need to reach a platform forward!
            else if (toTarget.y > 0f || (!_wallAboveHead && !isTargetOnWall && Mathf.Sign(toTarget.x) == Mathf.Sign(_climbDir)))
            {
                _rb.linearVelocityY = climbSpeed;
            }
            // 3. Otherwise, climb down
            else
            {
                _rb.linearVelocityY = -climbSpeed; 
                if (_isGrounded)
                {
                    ExitClimb("Hit ground");
                    return;
                }
            }

            // Failsafe: Wall is completely gone or moss ended
            if (!_wallAhead)
            {
                ExitClimb("Cleared wall failsafe");
            }
            else if (!_climbableWallAhead)
            {
                ExitClimb("Moss ended");
            }

            // Exit: target is directly below us vertically
            if (toTarget.y < -2f && Mathf.Abs(toTarget.x) < 1.5f)
            {
                ExitClimb("Target is below");
            }
        }

        /// <summary>
        /// Attempts a wall jump toward the next anchor if it's on the opposite side.
        /// Returns true if a wall jump was executed.
        /// </summary>
        private bool TryWallJump(Vector2 toTarget)
        {
            // Only wall jump off climbable surfaces
            if (!_climbableWallAhead) return false;

            // Target must be on the opposite side horizontally
            bool targetOnOppositeSide = (toTarget.x > 0.3f && _climbDir < 0f) ||
                                         (toTarget.x < -0.3f && _climbDir > 0f);
            if (!targetOnOppositeSide) return false;

            // We should be at roughly the right height or above the target
            // Jump when level with or above the target (within 1.5 units below target is OK)
            bool atRightHeight = toTarget.y < 1.5f;
            if (!atRightHeight) return false;

            DebugLog($"[SimpleAi] Wall jumping! Target on opposite side. toTarget: {toTarget}");
            ExitClimb("Wall jump");

            // Calculate angled jump direction from wall normal
            Vector2 jumpDir = GetWallJumpDirection();

            _rb.linearVelocity = jumpDir * wallJumpForce;
            _currentVelocityX = _rb.linearVelocityX;
            _jumpCooldown = 0.3f;

            // Flip sprite to face jump direction
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = jumpDir.x < 0f;
            }

            SetState("WALL_JUMPING");
            return true;
        }

        private Vector2 GetWallJumpDirection()
        {
            // Wall normal points away from the wall
            Vector2 wallNormal = _wallNormal;
            if (wallNormal == Vector2.zero)
            {
                // Fallback: opposite of climb direction
                wallNormal = Vector2.right * -_climbDir;
            }

            // Rotate the normal upward by wallJumpAngle degrees
            // (same math as PlayerPrepareJumpState.GetAngledVector)
            float angle = wallNormal.x > 0 ? wallJumpAngle : -wallJumpAngle;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            return (rotation * wallNormal).normalized;
        }

        private void EnterClimb()
        {
            _isClimbing = true;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
            _stuckTimer = 0f;
            _climbOvershootTimer = 0f;
            DebugLog("[SimpleAi] --- ENTERED CLIMB ---");
        }

        private void ExitClimb(string reason)
        {
            _isClimbing = false;
            _rb.gravityScale = _defaultGravity;
            DebugLog($"[SimpleAi] --- EXITED CLIMB ({reason}) ---");
        }

        // ═══════════════════════════════════════════════════════════
        //  PUSHING
        // ═══════════════════════════════════════════════════════════

        private void UpdatePushing(Vector2 toTarget, float dirX)
        {
            SetState("PUSHING");

            // Exit: lost contact with pushable
            if (_pushable == null || !_pushableAhead)
            {
                ExitPush("Lost contact with pushable");
                return;
            }

            // Exit: left the ground
            if (!_isGrounded)
            {
                ExitPush("Left ground while pushing");
                return;
            }

            // Push in the direction of the target
            float pushDir = dirX != 0f ? dirX : Mathf.Sign(toTarget.x);
            Vector2 force = Vector2.right * pushDir * pushForce;
            _pushable.ApplyPushForce(force);

            // Sync AI velocity with pushable to prevent physics jitter
            _rb.linearVelocityX = _pushable.Velocity.x;
        }

        private void EnterPush()
        {
            _isPushing = true;
            DebugLog("[SimpleAi] --- ENTERED PUSH ---");
        }

        private void ExitPush(string reason)
        {
            _isPushing = false;
            _pushable = null;
            DebugLog($"[SimpleAi] --- EXITED PUSH ({reason}) ---");
        }

        private void SequenceToNextNode()
        {
            if (_currentPath != null && _currentAnchorIndex < _currentPath.Length - 1)
            {
                _currentAnchorIndex++;
                target = _currentPath[_currentAnchorIndex];
                DebugLog($"[SimpleAi] Reached anchor {_currentAnchorIndex - 1}. Sequencing to next: {target.name}");
            }
            else
            {
                SetState("ARRIVED");
                _rb.linearVelocityX = Mathf.SmoothDamp(_rb.linearVelocityX, 0f, ref _currentVelocityX, 0.15f);
                if (_isClimbing) ExitClimb("Arrived at target");
                
                // Finished the current area, check queue
                _isTraversingArea = false;
                TryStartNextArea();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  DETECTION
        // ═══════════════════════════════════════════════════════════

        private void CheckGround()
        {
            if (bodyCollider == null) return;
            
            bool wasGrounded = _isGrounded;
            LayerMask combinedMask = groundLayers | climbableLayers;
            
            // Perform 3 raycasts across the bottom width of the collider
            float inset = 0.15f; // small inset to avoid wall friction issues
            float extentsX = bodyCollider.bounds.extents.x - inset;
            float centerY = bodyCollider.bounds.min.y;
            float centerX = bodyCollider.bounds.center.x;

            Vector2 leftOrigin = new Vector2(centerX - extentsX, centerY);
            Vector2 centerOrigin = new Vector2(centerX, centerY);
            Vector2 rightOrigin = new Vector2(centerX + extentsX, centerY);

            _isGrounded = Physics2D.Raycast(leftOrigin, Vector2.down, groundCheckDist, combinedMask) ||
                          Physics2D.Raycast(centerOrigin, Vector2.down, groundCheckDist, combinedMask) ||
                          Physics2D.Raycast(rightOrigin, Vector2.down, groundCheckDist, combinedMask);

            // Diagnostic check: is there a collider underneath that we are ignoring due to layer masks?
            if (!_isGrounded && debugMode)
            {
                RaycastHit2D diagHit = Physics2D.Raycast(centerOrigin, Vector2.down, groundCheckDist + 0.2f, ~0);
                if (diagHit.collider != null && !diagHit.collider.isTrigger)
                {
                    DebugLog($"[SimpleAi Diagnostic] Physically touching '{diagHit.collider.name}' (Layer: {LayerMask.LayerToName(diagHit.collider.gameObject.layer)}), but logically NOT grounded because this layer is not in Ground/Climbable layers!");
                }
            }

            if (!wasGrounded && _isGrounded) DebugLog("[SimpleAi] Landed on ground");
        }

        private void CheckWall(float dir)
        {
            if (bodyCollider == null) return;

            // Combined mask: detect both ground walls and climbable walls
            LayerMask combinedMask = groundLayers | climbableLayers;

            // Cast in the direction the AI is trying to go/climb
            float checkDir = _isClimbing ? _climbDir : dir;
            if (checkDir == 0f) checkDir = 1f;

            Vector2 forward = Vector2.right * checkDir;

            // Start raycasts slightly inset from the bounds so they don't start inside a wall if flush against it
            float inset = 0.05f;
            float extentsX = bodyCollider.bounds.extents.x - inset;
            Vector2 edgeCenter = new Vector2(bodyCollider.bounds.center.x + checkDir * extentsX, bodyCollider.bounds.center.y);
            Vector2 edgeHead = new Vector2(bodyCollider.bounds.center.x + checkDir * extentsX, bodyCollider.bounds.max.y - 0.1f);
            Vector2 edgeFeet = new Vector2(bodyCollider.bounds.center.x + checkDir * extentsX, bodyCollider.bounds.min.y + 0.1f);

            // Multiple raycasts with combined mask
            RaycastHit2D hitCenter = Physics2D.Raycast(edgeCenter, forward, wallCheckDist, combinedMask);
            RaycastHit2D hitFeet = Physics2D.Raycast(edgeFeet, forward, wallCheckDist, combinedMask);

            _wallAhead = hitCenter.collider || hitFeet.collider;
            _wallAboveHead = Physics2D.Raycast(edgeHead, forward, wallCheckDist, combinedMask);

            _wallCenterAhead = hitCenter.collider != null;

            // Store wall normal from the best hit
            if (hitCenter.collider)
                _wallNormal = hitCenter.normal;
            else if (hitFeet.collider)
                _wallNormal = hitFeet.normal;
            else
                _wallNormal = Vector2.zero;

            // Check if the wall is specifically on the climbable layer
            _climbableWallAhead = (hitCenter.collider && IsOnClimbableLayer(hitCenter.collider.gameObject)) ||
                                   (hitFeet.collider && IsOnClimbableLayer(hitFeet.collider.gameObject));
        }

        private void CheckPushable(float dir)
        {
            if (bodyCollider == null) return;

            Vector2 forward = Vector2.right * dir;
            Vector2 origin = new Vector2(
                dir > 0 ? bodyCollider.bounds.max.x : bodyCollider.bounds.min.x,
                bodyCollider.bounds.min.y + 0.15f
            );

            // Don't overwrite the pushable reference while actively pushing
            if (_isPushing) return;

            _pushable = null;
            _pushableAhead = false;

            RaycastHit2D hit = Physics2D.Raycast(origin, forward, pushDetectDist, _movableLayerMask);
            if (hit.collider)
            {
                IPushable pushable = hit.transform.GetComponent<IPushable>();
                if (pushable != null)
                {
                    _pushable = pushable;
                    _pushableAhead = true;
                }
            }
        }

        private bool IsOnClimbableLayer(GameObject obj)
        {
            return (climbableLayers & (1 << obj.layer)) != 0;
        }

        // ═══════════════════════════════════════════════════════════
        //  UTILITY
        // ═══════════════════════════════════════════════════════════

        private void SetState(string state)
        {
            if (_currentStateStr != state)
            {
                _currentStateStr = state;
            }
        }

        private void DebugLog(string message)
        {
            if (debugMode) Debug.Log(message, this);
        }

        private void LogStatePeriodically()
        {
            if (!debugMode) return;
            _logTimer += Time.deltaTime;
            if (_logTimer > 1f)
            {
                DebugLog($"[SimpleAi Status 1s] State: {_currentStateStr} | Grounded: {_isGrounded} | Wall: {_wallAhead} | WallAbove: {_wallAboveHead} | Climbable: {_climbableWallAhead} | Pushable: {_pushableAhead} | Stuck: {_stuckTimer:F2}s | Vel: {_rb.linearVelocity}");
                _logTimer = 0f;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  DEBUG GIZMOS
        // ═══════════════════════════════════════════════════════════

        private void OnDrawGizmos()
        {
            if (!debugMode || bodyCollider == null) return;

            // Ground check
            Vector2 groundOrigin = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDist);

            // Wall checks
            float dir = Application.isPlaying ? _facingDir : (spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f);
            Vector2 forward = Vector2.right * dir;

            float extentsX = bodyCollider.bounds.extents.x;
            Vector2 center = new Vector2(bodyCollider.bounds.center.x + dir * extentsX, bodyCollider.bounds.center.y);
            Vector2 head = new Vector2(bodyCollider.bounds.center.x + dir * extentsX, bodyCollider.bounds.max.y - 0.1f);
            Vector2 feet = new Vector2(bodyCollider.bounds.center.x + dir * extentsX, bodyCollider.bounds.min.y + 0.1f);

            Gizmos.color = _wallAhead ? Color.yellow : Color.gray;
            Gizmos.DrawLine(center, center + forward * wallCheckDist);
            Gizmos.DrawLine(feet, feet + forward * wallCheckDist);

            Gizmos.color = _wallAboveHead ? Color.red : Color.gray;
            Gizmos.DrawLine(head, head + forward * wallCheckDist);

            // Climbable wall indicator
            if (_climbableWallAhead)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(center + forward * wallCheckDist, 0.15f);
            }

            // Push detection ray
            Vector2 pushOrigin = new Vector2(
                dir > 0 ? bodyCollider.bounds.max.x : bodyCollider.bounds.min.x,
                bodyCollider.bounds.min.y + 0.15f
            );
            Gizmos.color = _pushableAhead ? Color.blue : new Color(0.3f, 0.3f, 0.8f, 0.3f);
            Gizmos.DrawLine(pushOrigin, pushOrigin + forward * pushDetectDist);

            // Gap ahead detection visualization
            if (_rb != null)
            {
                float velX = _rb.linearVelocityX;
                if (Mathf.Abs(velX) > 0.1f)
                {
                    float lookAheadDist = 0.8f;
                    Vector2 checkPos = new Vector2(
                        bodyCollider.bounds.center.x + Mathf.Sign(velX) * lookAheadDist,
                        bodyCollider.bounds.center.y
                    );
                    float downDist = bodyCollider.bounds.extents.y + 1.5f;
                    
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(checkPos, checkPos + Vector2.down * downDist);
                    Gizmos.DrawWireSphere(checkPos, 0.1f);
                }
            }

            // Target
            if (target != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(target.position, arrivalDistance);
                Gizmos.DrawLine(transform.position, target.position);
            }

            // Wall normal + projected wall jump direction (while climbing)
            if (_wallNormal != Vector2.zero && _isClimbing)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawRay(center, _wallNormal * 0.5f);

                Vector2 jumpDir = GetWallJumpDirection();
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                Gizmos.DrawRay(center, jumpDir * 1.5f);
            }
        }
    }
}
