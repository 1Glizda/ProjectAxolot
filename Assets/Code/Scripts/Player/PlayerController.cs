using Interfaces;
using Player.Input;
using Player.StateMachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    //controls the StateMachine and talks to the engine
    internal class PlayerController : MonoBehaviour, IPlayerStateProvider
    {
        public event System.Action OnJump;
        public event System.Action OnStartClimb;
        public bool IsClimbing => _isInClimbingState && VerticalVelocity > 0.1f;
        public float VerticalVelocity => _rb.linearVelocityY;
        public float HorizontalVelocity => _rb.linearVelocityX;

        public bool IsGrounded => _isGrounded;
        public bool IsInCoyoteTime => _isInCoyoteTime;
        public bool IsNearValidWall => _isNearValidWall;
        public bool IsFootNearValidWall => _isFootNearValidWall;
        public Vector2 WallHitNormal => _wallHitNormal;
        public bool IsFootNearPushable => _isFootNearPushable;
        public IPushable Pushable => _pushable;
        public bool CanVault => _canVault;
        public Vector2 VaultTarget => _vaultTarget;
        
        public PlayerContext PlayerContext => _context;


        [Header("Debug")]
        [SerializeField] private bool _debugMode;
        
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
        private PlayerContext _context;

        private bool _isGrounded;
        private bool _isInClimbingState;
        private bool _isInCoyoteTime;
        
        private bool _isNearValidWall;
        private bool _isFootNearValidWall;
        private Vector2 _wallHitNormal;
        private bool _isFootNearPushable;
        
        
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

        
        
        private void Awake()
        {
            _pushableLayerIndex = LayerMask.NameToLayer("Movable");

            _context = new PlayerContext(
                inputHandler as IPlayerInputManager,
                this as IPlayerStateProvider,
                _collisionHandler,
                _settings,
                _spriteObject,
                _bodyCollider,
                _feetCollider,
                _rb,
                _swingHinge
            );

            
            _stateMachine = new MovementStateMachine(_settings, _context);
            _groundHits = new RaycastHit2D[3];   
            
            _stateMachine.onChangeState += type => {
                if (_debugMode) Debug.Log(type, this);
                _isInClimbingState = type == typeof(PlayerClimbingState) || type == typeof(PlayerPrepareJumpState);
            };
            
            _animatorHelper.Initialize(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            CheckGrounded();
            CheckWall();
            _stateMachine.Tick(dt);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            _stateMachine.FixedTick(dt);
        }

        public void NotifyJump()
        {
            OnJump?.Invoke();
        }

        public void NotifyStartClimb()
        {
            OnStartClimb?.Invoke();
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
            _isFootNearValidWall = Physics2D.Raycast(_baseWallCheckOrigin, dir, distance, mask).collider;
            
        
            RaycastHit2D headHit = Physics2D.Raycast(headOrigin, dir, distance, mask);
            _canVault = false;
            
            if (!headHit.collider && (_wallHit.collider || _isFootNearValidWall))
            {
                // cast down from end point of head check
                Vector2 downRayOrigin = headOrigin + (dir * distance);
                float downRayDistance = _bodyCollider.bounds.size.y + 0.5f;
                RaycastHit2D downHit = Physics2D.Raycast(downRayOrigin, Vector2.down, downRayDistance, mask);
                
                if (downHit.collider)
                {
                    // check head room
                    Vector2 boxSize = new Vector2(_bodyCollider.bounds.size.x * 0.8f, 0.1f);
                    float requiredHeight = _bodyCollider.bounds.size.y;
                    RaycastHit2D clearanceCheck = Physics2D.BoxCast(downHit.point + Vector2.up * 0.1f, boxSize, 0f, Vector2.up, requiredHeight, mask);

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
            
            _isNearValidWall = _wallHit.collider;
            _wallHitNormal = _wallHit.normal;
            
            // TODO check for pushable
            /*
            _pushable = null;
            if (_isFootNearValidWall && _wallHit.collider && _wallHit.collider.gameObject.layer == _pushableLayerIndex)
            {
                _pushable = _wallHit.collider.GetComponent<IPushable>();
            }
            _isFootNearPushable = _pushable != null; 
            */

            _baseWallCheckOrigin.y += 0.1f;
            _pushable = null;
            Debug.DrawRay(_baseWallCheckOrigin, dir * distance, Color.blue);
            RaycastHit2D pushableHit = Physics2D.Raycast(_baseWallCheckOrigin, dir, distance, LayerMask.GetMask("Movable"));
            if(pushableHit.collider) _pushable = pushableHit.transform.GetComponent<IPushable>();
            _isFootNearPushable = _pushable != null;

        }
        
        private void CheckGrounded()
        {
            Bounds bounds = _feetCollider.bounds;
            float yOffset = 0.05f;
            _checkOrigins[0] = new Vector2(bounds.min.x, bounds.min.y + yOffset);
            _checkOrigins[1] = new Vector2(bounds.center.x, bounds.min.y + yOffset);
            _checkOrigins[2] = new Vector2(bounds.max.x, bounds.min.y + yOffset);

            Vector2 middleOrigin = _checkOrigins[1];
            
            float distance = _settings.GroundCheckDistance + yOffset;
            LayerMask mask = _settings.GroundLayers;


            bool didHit = MultiRaycast(_groundHits, _checkOrigins, Vector2.down, distance, mask);
            
            
            if (didHit)
            {
               
                _isGrounded = true;
                _isInCoyoteTime = false;
            }
            else
            {
                RaycastHit2D hit = Physics2D.Raycast(middleOrigin, Vector2.down, Mathf.Infinity, mask);
                _isGrounded = false;
                
                if (!_isInCoyoteTime)
                {
                    _isInCoyoteTime = true;
                    _coyoteTimer = 0f;
                }
                else if(_coyoteTimer > _settings.CoyoteTime)
                {
                    _isInCoyoteTime = false;
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


        
    }
}
