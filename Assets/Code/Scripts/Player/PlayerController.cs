using System.Collections;
using Interfaces;
using Player.Input;
using Player.StateMachine;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    //controls the StateMachine and talks to the engine
    internal class PlayerController : MonoBehaviour, IPlayerStateProvider, IKnockbackable
    {
        
        public event System.Action OnJump;
        public event System.Action OnStartClimb;
        public event System.Action OnLand;
        public event System.Action OnGrabVine;
        
        public bool IsClimbing => _isClimbingAnim && _isInClimbingState;
        public bool IsJumping => _isJumping;
        public float VerticalVelocity => _rb.linearVelocityY;
        public float HorizontalVelocity => _rb.linearVelocityX;

        public bool IsGrounded => _isGrounded;
        public bool IsInCoyoteTime => _isInCoyoteTime;
        public bool IsOnSteepSlope => _isOnSteepSlope;
        public Vector2 GroundNormal => _groundNormal;
        public bool IsNearValidWall => (!_isWallHitMovable || !((IPlayerInputHandler)inputHandler).GrabWallAction.IsPressed()) && _isNearValidWall;
        public bool IsFootNearValidWall => (!_isWallHitMovable || !((IPlayerInputHandler)inputHandler).GrabWallAction.IsPressed()) && _isFootNearValidWall;
        public bool IsHeadBlocked => _isHeadBlocked;
        public Vector2 WallHitNormal => _wallHitNormal;
        public bool IsFootNearPushable => _isFootNearPushable;
        public IPushable Pushable => _pushable;
        public bool CanVault => _canVault;
        public Vector2 VaultTarget => _vaultTarget;
        
        public static PlayerControllerContext PlayerControllerContext => _controllerContext;


        [Header("Debug")]
        [SerializeField] private bool _debugMode;
        [SerializeField] private TMP_Text _stateText;
        
        [Header("ToInitialize")]
        [SerializeField] private AnimatorHelper _animatorHelper;
        
        [Header("Context References")]
        [SerializeField] private PlayerCollisionHandler _collisionHandler;
        [SerializeField] private PlayerSettingsSo _settings;
        [FormerlySerializedAs("_inputManager")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private GameObject _spriteObject;
        [SerializeField] private Collider2D _bodyCollider;
        [SerializeField] private Collider2D _feetCollider;
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private HingeJoint2D _swingHinge;
        
        private MovementStateMachine _stateMachine;
        private static PlayerControllerContext _controllerContext;

        private bool _isGrounded;
        private bool _isInClimbingState;
        private bool _isJumping;
        private bool _isInCoyoteTime;
        private bool _isOnSteepSlope;
        private Vector2 _groundNormal = Vector2.up;
        
        private bool _isNearValidWall;
        private bool _isFootNearValidWall;
        private bool _isWallHitMovable;
        private bool _isHeadBlocked;
        private Vector2 _wallHitNormal;
        private bool _isFootNearPushable;
        
        private float _climbingAnimStopTimer;
        private bool _isClimbingAnim;
        private bool _isDroppingThrough;
        
        
        private Vector2 _leftWallCheckOrigin;
        private Vector2 _rightWallCheckOrigin;
        private Vector2 _baseWallCheckOrigin;
        private RaycastHit2D _wallHit;
        
        private IPushable _pushable;
        private bool _canVault;
        private Vector2 _vaultTarget;
        
        
        private float _coyoteTimer;
        private RaycastHit2D[] _groundHits;
        private Vector2[] _checkOrigins = new Vector2[3];
        private int _pushableLayerIndex;
        private float _resetHoldTimer;

        
        
        private void Awake()
        {
            _pushableLayerIndex = LayerMask.NameToLayer("Movable");
            
            _controllerContext = new PlayerControllerContext(
                inputHandler as IPlayerInputHandler,
                this as IPlayerStateProvider,
                _collisionHandler,
                _settings,
                _spriteObject,
                _bodyCollider,
                _feetCollider,
                _rb,
                _swingHinge
            );

            
            _stateMachine = new MovementStateMachine(_settings, _controllerContext);
            _groundHits = new RaycastHit2D[3];   
            
            _stateMachine.onChangeState += type => {
                if (_debugMode) Debug.Log(type, this);
                if (_stateText != null) _stateText.text = type.Name;
                _isInClimbingState = type == typeof(PlayerClimbingState);
                _isJumping = type == typeof(PlayerJumpState);
            };
            
            _animatorHelper.Initialize(this);
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;

            float dt = Time.deltaTime;
            CheckGrounded();
            CheckWall();
            _stateMachine.Tick(dt);

            bool isTryingToClimb = Mathf.Abs(VerticalVelocity) > 0.1f || Mathf.Abs(((IPlayerInputHandler)inputHandler).MoveAction.ReadValue<Vector2>().y) > 0.1f;
            if (_isInClimbingState && isTryingToClimb)
            {
                _climbingAnimStopTimer = 0.15f;
                _isClimbingAnim = true;
            }
            else
            {
                if (_climbingAnimStopTimer > 0f) _climbingAnimStopTimer -= dt;
                else _isClimbingAnim = false;
            }

            if (((IPlayerInputHandler)inputHandler).MoveAction.ReadValue<Vector2>().y < -0.5f)
            {
                TryDropThroughPlatform();
            }

            if (((IPlayerInputHandler)inputHandler).ResetAction != null)
            {
                if (((IPlayerInputHandler)inputHandler).ResetAction.IsPressed())
                {
                    _resetHoldTimer += dt;
                    if (_resetHoldTimer >= 1.0f)
                    {
                        _resetHoldTimer = 0f;
                        if (Player.GameState.GameStateManager.Instance != null)
                        {
                            Player.GameState.GameStateManager.Instance.KillPlayer();
                        }
                    }
                }
                else
                {
                    _resetHoldTimer = 0f;
                }
            }
        }

        private void TryDropThroughPlatform()
        {
            if (!_isGrounded || _isDroppingThrough) return;
            
            bool dropped = false;
            foreach (var hit in _groundHits)
            {
                if (hit.collider != null && hit.collider.GetComponent<PlatformEffector2D>() != null)
                {
                    StartCoroutine(DropThroughRoutine(hit.collider));
                    dropped = true;
                }
            }
            
            if (dropped)
            {
                StartCoroutine(DropThroughCooldown());
            }
        }

        private IEnumerator DropThroughCooldown()
        {
            _isDroppingThrough = true;
            yield return new WaitForSeconds(0.4f);
            _isDroppingThrough = false;
        }

        private IEnumerator DropThroughRoutine(Collider2D platformCollider)
        {
            if (platformCollider == null) yield break;
            
            if (_bodyCollider != null) Physics2D.IgnoreCollision(_bodyCollider, platformCollider, true);
            if (_feetCollider != null) Physics2D.IgnoreCollision(_feetCollider, platformCollider, true);
            
            yield return new WaitForSeconds(0.4f);
            
            if (platformCollider != null)
            {
                if (_bodyCollider != null) Physics2D.IgnoreCollision(_bodyCollider, platformCollider, false);
                if (_feetCollider != null) Physics2D.IgnoreCollision(_feetCollider, platformCollider, false);
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            _stateMachine.FixedTick(dt);
        }

        public void NotifyJump()
        {
            _isInCoyoteTime = false;
            OnJump?.Invoke();
        }

        public void NotifyStartClimb()
        {
            OnStartClimb?.Invoke();
        }

        public void NotifyLand()
        {
            OnLand?.Invoke();
        }
        
        public void NotifyGrabVine()
        {
            OnGrabVine?.Invoke();
        }

        public void ApplyKnockback(Vector2 velocity)
        {
            _controllerContext.PendingKnockbackVelocity = velocity;
            _stateMachine.ChangeState<PlayerKnockbackState>();
        }

        private void CheckForPushable()
        {
            
        }
        
        private void CheckWall()
        {
            _leftWallCheckOrigin = new Vector2(_bodyCollider.bounds.min.x, _bodyCollider.bounds.center.y);
            _rightWallCheckOrigin = new Vector2(_bodyCollider.bounds.max.x, _bodyCollider.bounds.center.y);
            float distance = _settings.WallDetectionRange;
            LayerMask mask = _settings.WallLayers;
            
            bool isFacingRight = Mathf.Abs(_spriteObject.transform.localEulerAngles.y) < 90f;
            Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
            Vector2 centerOrigin = isFacingRight ? _rightWallCheckOrigin : _leftWallCheckOrigin;
            
            _baseWallCheckOrigin = new Vector2(isFacingRight ? _bodyCollider.bounds.max.x : _bodyCollider.bounds.min.x, _bodyCollider.bounds.min.y);
            Vector2 headOrigin = new Vector2(isFacingRight ? _bodyCollider.bounds.max.x : _bodyCollider.bounds.min.x, _bodyCollider.bounds.max.y);

            _wallHit = Physics2D.Raycast(centerOrigin, dir, distance, mask);
            RaycastHit2D footHit = Physics2D.Raycast(_baseWallCheckOrigin, dir, distance, mask);
            _isFootNearValidWall = footHit.collider;
            
            RaycastHit2D headHit = Physics2D.Raycast(headOrigin, dir, distance, mask);
            
            _isWallHitMovable = (_wallHit.collider != null && (_wallHit.collider.gameObject.layer == _pushableLayerIndex || _wallHit.collider.GetComponentInParent<IPushable>() != null)) ||
                                 (footHit.collider != null && (footHit.collider.gameObject.layer == _pushableLayerIndex || footHit.collider.GetComponentInParent<IPushable>() != null)) ||
                                 (headHit.collider != null && (headHit.collider.gameObject.layer == _pushableLayerIndex || headHit.collider.GetComponentInParent<IPushable>() != null));
            
            _isHeadBlocked = false;
            if (!headHit.collider)
            {
                RaycastHit2D headGroundHit = Physics2D.Raycast(headOrigin, dir, distance, _settings.GroundLayers);
                if (headGroundHit.collider)
                {
                    _isHeadBlocked = true;
                }
            }

            _canVault = false;
            
            if (!headHit.collider && (_wallHit.collider || _isFootNearValidWall))
            {
                LayerMask vaultMask = mask | _settings.GroundLayers;
                // cast down from end point of head check
                Vector2 downRayOrigin = headOrigin + (dir * 0.5f);
                float downRayDistance = _bodyCollider.bounds.size.y + 0.5f;
                RaycastHit2D downHit = Physics2D.Raycast(downRayOrigin, Vector2.down, downRayDistance, vaultMask);
                
                if (downHit.collider)
                {
                    // check head room
                    Vector2 boxSize = new Vector2(_bodyCollider.bounds.size.x * 0.8f, 0.1f);
                    float requiredHeight = _bodyCollider.bounds.size.y;
                    RaycastHit2D clearanceCheck = Physics2D.BoxCast(downHit.point + Vector2.up * 0.1f, boxSize, 0f, Vector2.up, requiredHeight, vaultMask);

                    if (!clearanceCheck.collider)
                    {
                        _canVault = true;
                        _vaultTarget = downHit.point;
                        if (_debugMode) Debug.DrawRay(downRayOrigin, Vector2.down * downRayDistance, Color.cyan);
                    }
                    else
                    {
                        _canVault = false;
                        if (_debugMode) Debug.DrawRay(downHit.point + Vector2.up * 0.1f, Vector2.up * requiredHeight, Color.red);
                    }
                }
                else if (_debugMode) Debug.DrawRay(downRayOrigin, Vector2.down * downRayDistance, Color.yellow);
            }
            if (_debugMode) Debug.DrawRay(headOrigin, dir * distance, headHit.collider ? Color.cyan : Color.yellow);

            
            if (_debugMode)
            {
                Debug.DrawRay(centerOrigin, dir * distance, _wallHit.collider ? Color.green : Color.red);
                Debug.DrawRay(_baseWallCheckOrigin, dir * distance, _isFootNearValidWall ? Color.green : Color.red);
            }
            
            _isNearValidWall = _wallHit.collider || _isFootNearValidWall;
            _wallHitNormal = _wallHit.collider ? _wallHit.normal : footHit.normal;
            
            // TODO check for pushable
            /*
            _pushable = null;
            if (_isFootNearValidWall && _wallHit.collider && _wallHit.collider.gameObject.layer == _pushableLayerIndex)
            {
                _pushable = _wallHit.collider.GetComponent<IPushable>();
            }
            _isFootNearPushable = _pushable != null; 
            */

            _baseWallCheckOrigin.y += _settings.PushableCheckYOffset;
            _pushable = null;
            Debug.DrawRay(_baseWallCheckOrigin, dir * _settings.PushableCheckDistance, Color.blue);
            RaycastHit2D pushableHit = Physics2D.Raycast(_baseWallCheckOrigin, dir, _settings.PushableCheckDistance, LayerMask.GetMask("Movable"));
            if(pushableHit.collider) _pushable = pushableHit.transform.GetComponent<IPushable>();
            _isFootNearPushable = _pushable != null;

        }
        
        private void CheckGrounded()
        {
            if (_isDroppingThrough)
            {
                _isGrounded = false;
                _isInCoyoteTime = false;
                _isOnSteepSlope = false;
                return;
            }

            Bounds bounds = _feetCollider.bounds;
            float yOffset = 0.05f;
            _checkOrigins[0] = new Vector2(bounds.min.x, bounds.min.y + yOffset);
            _checkOrigins[1] = new Vector2(bounds.center.x, bounds.min.y + yOffset);
            _checkOrigins[2] = new Vector2(bounds.max.x, bounds.min.y + yOffset);

            Vector2 middleOrigin = _checkOrigins[1];
            
            float distance = _settings.GroundCheckDistance + yOffset;
            LayerMask mask = _settings.GroundLayers;

            bool wasGrounded = _isGrounded;
            bool wasOnSteepSlope = _isOnSteepSlope;
            bool didHit = MultiRaycast(_groundHits, _checkOrigins, Vector2.down, distance, mask);
            
            _isOnSteepSlope = false;
            
            if (didHit)
            {
                Vector2 normal = Vector2.zero;
                int validHits = 0;
                foreach (RaycastHit2D hit in _groundHits)
                {
                    if (hit.collider)
                    {
                        normal += hit.normal;
                        validHits++;
                    }
                }
                
                if (validHits > 0)
                {
                    normal /= validHits;
                    normal.Normalize();
                }
                else
                {
                    normal = Vector2.up;
                }

                _groundNormal = normal;
                float slopeAngle = Vector2.Angle(normal, Vector2.up);

                if (slopeAngle > _settings.MaxSlopeAngle)
                {
                    _isGrounded = false;
                    _isInCoyoteTime = false;
                    _isOnSteepSlope = true;
                }
                else
                {
                    _isGrounded = true;
                    _isInCoyoteTime = false;
                    
                    // Fire landing event on the frame we touch the ground
                    if (!wasGrounded) NotifyLand();
                }
            }
            else
            {
                // Run a slightly deeper raycast to detect steep slopes that might be physically colliding 
                // but suspended slightly below the feet check distance due to body collider contact.
                RaycastHit2D[] steepHits = new RaycastHit2D[3];
                float steepDistance = 0.25f + yOffset;
                bool hitSteep = MultiRaycast(steepHits, _checkOrigins, Vector2.down, steepDistance, mask);
                
                if (hitSteep)
                {
                    Vector2 normal = Vector2.zero;
                    int validHits = 0;
                    foreach (RaycastHit2D hit in steepHits)
                    {
                        if (hit.collider)
                        {
                            normal += hit.normal;
                            validHits++;
                        }
                    }
                    
                    if (validHits > 0)
                    {
                        normal /= validHits;
                        normal.Normalize();
                    }
                    else
                    {
                        normal = Vector2.up;
                    }

                    float slopeAngle = Vector2.Angle(normal, Vector2.up);
                    if (slopeAngle > _settings.MaxSlopeAngle)
                    {
                        _isGrounded = false;
                        _isInCoyoteTime = false;
                        _isOnSteepSlope = true;
                        _groundNormal = normal;
                    }
                }

                if (!_isOnSteepSlope)
                {
                    RaycastHit2D hit = Physics2D.Raycast(middleOrigin, Vector2.down, Mathf.Infinity, mask);
                    _isGrounded = false;
                    
                    if (wasGrounded || wasOnSteepSlope)
                    {
                        _isInCoyoteTime = true;
                        _coyoteTimer = 0f;
                    }
                    else if(_coyoteTimer > _settings.CoyoteTime)
                    {
                        _isInCoyoteTime = false;
                    }
                }
            }
            _coyoteTimer += Time.deltaTime;
            
            
        }

        private Vector2 ComputeSlopeTangent()
        {
            Vector2 normal = Vector2.zero;
            int validHits = 0;
            foreach (RaycastHit2D hit in _groundHits)
            {
                if (hit.collider)
                {
                    normal += hit.normal;
                    validHits++;
                }
            }
            
            if (validHits > 0)
            {
                normal /= validHits;
                normal.Normalize();
            }
            else
            {
                normal = Vector2.up;
            }
            
            _groundNormal = normal;
            Vector2 slope = new (normal.y, -normal.x);
            
            if (_debugMode && _isGrounded)
            {
                Debug.DrawRay(_groundHits[1].point, slope, Color.chocolate);
                Debug.DrawRay(_groundHits[1].point, normal, Color.purple);
            }
            
            return slope;
            
        }   
        
        public PlayerGroundData GetGroundData()
        {
            return new PlayerGroundData(ComputeSlopeTangent());
            
        }


        private bool MultiRaycast(RaycastHit2D[] hits, Vector2[] origins, Vector2 dir, float distance, LayerMask mask)
        {
            for (int i = 0; i < origins.Length; i++)
            {
                hits[i] = Physics2D.Raycast(origins[i], dir, distance, mask);
            }
            
            if (_debugMode)
            {
                for (int i = 0; i < origins.Length; i++)
                {
                    Vector2 checkOrigin = origins[i];
                    Debug.DrawRay(checkOrigin, Vector2.down * distance, hits[i].collider ? Color.green : Color.red);
                }
            }

            return hits[0].collider || hits[1].collider || hits[2].collider;
        }

        public void Teleport(Vector2 position)
        {
            StartCoroutine(TeleportRoutine(position));
        }
        
        private IEnumerator TeleportRoutine(Vector2 targetPosition)
        {
            yield return new WaitForEndOfFrame();
            ((IPlayerInputHandler)inputHandler).SetInputActive(false);

            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
    
            var originalInterpolation = _rb.interpolation;
            _rb.interpolation = RigidbodyInterpolation2D.None;
    
            _rb.position = targetPosition;
            transform.position = targetPosition;
    
            Physics2D.SyncTransforms();
            _rb.interpolation = originalInterpolation;

            _isGrounded = false;
            _isInCoyoteTime = false;
            _coyoteTimer = 0f;

            _stateMachine.ChangeState<PlayerIdleState>();
            yield return new WaitForFixedUpdate();
    
            ((IPlayerInputHandler)inputHandler).SetInputActive(true);
        }
        
    }
}
