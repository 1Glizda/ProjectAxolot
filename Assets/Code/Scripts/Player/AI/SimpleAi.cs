using UnityEngine;
using Player.AI.Navigation;

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

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D bodyCollider;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        private Rigidbody2D _rb;
        private float _defaultGravity;

        // Detection
        private bool _isGrounded;
        private bool _wallAhead;
        private bool _wallAboveHead;

        // State
        private bool _isClimbing;
        private float _jumpCooldown;
        private float _climbDir; 
        private string _currentStateStr = "IDLE";
        private float _climbOvershootTimer;
        private float _currentVelocityX; // For SmoothDamp

        // Stuck detection
        private Vector2 _lastPos;
        private float _stuckTimer;
        private float _logTimer;
        
        // Pathing
        private Transform[] _currentPath;
        private int _currentAnchorIndex;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _defaultGravity = _rb.gravityScale;
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
        }

        private void OnDestroy()
        {
            if (AiManager.Instance != null)
            {
                AiManager.Instance.OnAreaChanged -= HandleAreaChanged;
            }
        }

        private void HandleAreaChanged(AiArea newArea)
        {
            if (newArea.anchorPoints != null && newArea.anchorPoints.Length > 0)
            {
                _currentPath = newArea.anchorPoints;
                _currentAnchorIndex = 0;
                target = _currentPath[_currentAnchorIndex];
                DebugLog($"[SimpleAi] Area changed! Path loaded with {_currentPath.Length} points. Next target: {target.name}");
            }
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                SetState("NO TARGET");
                return;
            }

            CheckGround();
            CheckWall();

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;

            // --- Arrived ---
            if (toTarget.magnitude < arrivalDistance)
            {
                // Sequence to the next anchor point if available
                if (_currentPath != null && _currentAnchorIndex < _currentPath.Length - 1)
                {
                    _currentAnchorIndex++;
                    target = _currentPath[_currentAnchorIndex];
                    DebugLog($"[SimpleAi] Reached anchor {_currentAnchorIndex - 1}. Sequencing to next: {target.name}");
                    return; // Skip halting this frame so it maintains momentum to the next point
                }

                SetState("ARRIVED");
                // Premium SmoothDamp stop
                _rb.linearVelocityX = Mathf.SmoothDamp(_rb.linearVelocityX, 0f, ref _currentVelocityX, 0.15f);
                if (_isClimbing) ExitClimb("Arrived at target");
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
            
            // To prevent flipping rapidly mid-air if the target is directly below:
            float dirX = 0f;
            
            // If we are falling and the target is beneath us, use a much wider deadzone so we drop straight down
            bool isFallingToTarget = !_isGrounded && toTarget.y < 0f && _rb.linearVelocityY <= 0f;
            float horizontalDeadzone = isFallingToTarget ? 0.5f : 0.05f;

            if (Mathf.Abs(toTarget.x) > horizontalDeadzone)
            {
                dirX = Mathf.Sign(toTarget.x);
            }
            else
            {
                dirX = 0f;
            }

            // --- Climbing or normal movement ---
            if (_isClimbing)
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

            LogStatePeriodically(toTarget);
        }

        private void UpdateMovement(Vector2 toTarget, float dirX)
        {
            SetState($"MOVING (dir:{dirX})");

            // --- Premium SmoothDamp Movement ---
            float targetVelX = dirX * moveSpeed;
            float currentVelX = _rb.linearVelocityX; // Re-added for anticipatory jump math
            
            // Choose the spring smoothTime based on the environment
            float smoothTime = 0f;
            if (_isGrounded)
            {
                // Snappy but beautifully smoothed on ground (slower smoothTime when stopping)
                smoothTime = Mathf.Abs(targetVelX) > 0.01f ? 0.08f : 0.12f;
            }
            else
            {
                // Floaty, realistic mid-air momentum
                smoothTime = 0.35f; 
            }
                
            _rb.linearVelocityX = Mathf.SmoothDamp(_rb.linearVelocityX, targetVelX, ref _currentVelocityX, smoothTime);


            // Jump logic
            if (_isGrounded && _jumpCooldown <= 0f)
            {
                bool shortWall = _wallAhead && !_wallAboveHead;
                bool targetAbove = toTarget.y > 1.5f;

                // --- Anticipatory Jump ---
                // Calculate how far ahead to look based on current speed (e.g., look ahead 0.35 seconds)
                bool obstacleAhead = false;
                if (Mathf.Abs(currentVelX) > 1f)
                {
                    float lookAheadDist = wallCheckDist + (Mathf.Abs(currentVelX) * 0.35f);
                    Vector2 forward = Vector2.right * Mathf.Sign(currentVelX);
                    Vector2 center = bodyCollider.bounds.center;
                    Vector2 feet = new Vector2(center.x, bodyCollider.bounds.min.y + 0.1f);
                    
                    // Does our velocity path intersect a wall?
                    obstacleAhead = Physics2D.Raycast(center, forward, lookAheadDist, groundLayers) ||
                                    Physics2D.Raycast(feet, forward, lookAheadDist, groundLayers);
                }

                if (shortWall)
                {
                    DebugLog($"[SimpleAi] Jumping over short wall! ahead={_wallAhead}, above={_wallAboveHead}");
                    ExecuteJump();
                }
                else if (obstacleAhead && !_wallAhead && toTarget.y >= -1.5f)
                {
                    // If we see a wall coming, we are NOT currently touching it, and target isn't deep below us: Jump early!
                    DebugLog($"[SimpleAi] Anticipating obstacle calculated by speed, jumping early attempt!");
                    ExecuteJump();
                }
                else if (targetAbove)
                {
                    DebugLog($"[SimpleAi] Target is above ({toTarget.y:F2}), jumping!");
                    ExecuteJump();
                }
            }

            // Enter climb: tall wall + (target above OR stuck for too long)
            bool tallWall = _wallAhead && _wallAboveHead;
            bool shouldClimb = tallWall && (toTarget.y > 0.5f || _stuckTimer > 0.5f);

            if (shouldClimb)
            {
                DebugLog($"[SimpleAi] Encountered tall wall. Target > 0.5? ({toTarget.y > 0.5f}). Stuck? ({_stuckTimer > 0.5f}). Entering climb!");
                _climbDir = dirX;
                EnterClimb();
            }
            else if (_stuckTimer > 0.8f && _jumpCooldown <= 0f)
            {
                // If stuck but NOT facing a tall wall, maybe we are stuck on a corner. Try a random jump to free ourselves.
                DebugLog("[SimpleAi] Stuck for a while but no tall wall detected. Forcing a jump to unstick!");
                ExecuteJump();
                _stuckTimer = 0f;
            }
        }

        private void ExecuteJump()
        {
            // If we are already flying upwards extremely fast (e.g. from a bouncy shroom), 
            // do NOT cap our velocity back down to a standard jump!
            if (_rb.linearVelocityY < jumpForce * 1.5f)
            {
                // Only override if we're not currently rocket-jumping from a bounce pad
                _rb.linearVelocityY = Mathf.Max(_rb.linearVelocityY, jumpForce);
            }
            
            _jumpCooldown = 0.5f; 
            SetState("JUMPING");
        }

        private void UpdateClimbing(Vector2 toTarget)
        {
            SetState($"CLIMBING (dir:{_climbDir})");

            // Failsafe: if we are stuck in place on a wall with wall above us for 10 seconds, drop
            if (_wallAboveHead && _stuckTimer >= 10f)
            {
                DebugLog("[SimpleAi] Stuck climbing for 10s. Forcing drop to prevent softlock.");
                ExitClimb("Stuck timeout failsafe");
                _stuckTimer = 0f;
                return;
            }

            // Press gently into the wall so we stay attached
            _rb.linearVelocityX = _climbDir * 1f;

            // Climb up
            _rb.linearVelocityY = climbSpeed;

            // Exit: No forward resistance remaining (entire body has cleared the wall) -> Overshoot slightly
            if (!_wallAhead)
            {
                _climbOvershootTimer += Time.deltaTime;
                if (_climbOvershootTimer >= 0.1f) // Adjust this to climb higher/lower past the edge (e.g. 0.1s = tiny bit more)
                {
                    DebugLog("[SimpleAi] No forward resistance + overshoot. Vaulting!");
                    ExitClimb("Cleared wall");
                    // Vault: jump up and over
                    _rb.linearVelocityY = jumpForce * 0.6f;
                    _rb.linearVelocityX = _climbDir * moveSpeed;
                    _currentVelocityX = _rb.linearVelocityX; // Pre-warm the dampener so it doesn't fight the vault
                    _jumpCooldown = 0.2f;
                    _climbOvershootTimer = 0f;
                    return;
                }
            }
            else
            {
                _climbOvershootTimer = 0f;
            }

            // Exit: target is directly below us vertically
            if (toTarget.y < -2f && Mathf.Abs(toTarget.x) < 1.5f)
            {
                DebugLog("[SimpleAi] Target is directly below us now. Dropping from climb.");
                ExitClimb("Target is below");
            }
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

        // --- Detection ---

        private void CheckGround()
        {
            if (bodyCollider == null) return;
            Vector2 origin = new Vector2(
                bodyCollider.bounds.center.x,
                bodyCollider.bounds.min.y
            );
            bool wasGrounded = _isGrounded;
            _isGrounded = Physics2D.Raycast(origin, Vector2.down, groundCheckDist, groundLayers);
            
            if (!wasGrounded && _isGrounded) DebugLog("[SimpleAi] Landed on ground");
        }

        private void CheckWall()
        {
            if (bodyCollider == null) return;
            
            // Cast in the direction the AI is actually trying to go/climb
            float dir = _isClimbing ? _climbDir : (spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f);
            if (dir == 0) dir = 1f; // safety fallback

            Vector2 forward = Vector2.right * dir;
            Vector2 center = (Vector2)bodyCollider.bounds.center;
            
            Vector2 head = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y - 0.1f);
            Vector2 feet = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y + 0.1f);

            // Use multiple raycasts to ensure we don't miss a wall between tiles
            bool hitCenter = Physics2D.Raycast(center, forward, wallCheckDist, groundLayers);
            bool hitFeet = Physics2D.Raycast(feet, forward, wallCheckDist, groundLayers);
            
            _wallAhead = hitCenter || hitFeet; // True as long as there is any forward resistance
            _wallAboveHead = Physics2D.Raycast(head, forward, wallCheckDist, groundLayers);
        }

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

        private void LogStatePeriodically(Vector2 toTarget)
        {
            if (!debugMode) return;
            _logTimer += Time.deltaTime;
            if (_logTimer > 1f)
            {
                DebugLog($"[SimpleAi Status 1s] State: {_currentStateStr} | Grounded: {_isGrounded} | Wall Ahead: {_wallAhead} | Wall Above: {_wallAboveHead} | StuckTimer: {_stuckTimer:F2}s | Vel: {_rb.linearVelocity}");
                _logTimer = 0f;
            }
        }

        // --- Debug ---

        private void OnDrawGizmos()
        {
            if (!debugMode || bodyCollider == null) return;

            // Ground check
            Vector2 groundOrigin = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.min.y);
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDist);

            // Wall checks
            float dir = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
            Vector2 forward = Vector2.right * dir;

            Vector2 center = (Vector2)bodyCollider.bounds.center;
            Vector2 head = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y - 0.1f);

            Gizmos.color = _wallAhead ? Color.yellow : Color.gray;
            Gizmos.DrawLine(center, center + forward * wallCheckDist);

            Gizmos.color = _wallAboveHead ? Color.red : Color.gray;
            Gizmos.DrawLine(head, head + forward * wallCheckDist);

            // Target
            if (target != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(target.position, arrivalDistance);
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}
