using Player.Input;
using Player.StateMachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    //controls the StateMachine and talks to the engine
    internal class PlayerController : MonoBehaviour, IPlayerController
    {
        public bool IsGrounded => _isGrounded;
        public float DistanceToGround => _distanceToGround;
        public bool IsInCoyoteTime => _isInCoyoteTime;
        public bool IsNearValidWall => _isNearValidWall;
        public bool IsFootNearValidWall => _isFootNearValidWall;
        public Vector2 WallHitNormal => _wallHitNormal;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode;
        
        [Header("Context References")]
        [SerializeField] private PlayerCollisionHandler _collisionHandler;
        [SerializeField] private PlayerSettingsSo _settings;
        [FormerlySerializedAs("_inputManager")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _bodyCollider;
        [SerializeField] private Collider2D _feetCollider;
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private HingeJoint2D _swingHinge;
        
        private MovementStateMachine _stateMachine;
        private PlayerContext _context;

        private bool _isGrounded;
        private float _distanceToGround;
        private bool _isInCoyoteTime;
        
        private bool _isNearValidWall;
        private bool _isFootNearValidWall;
        private Vector2 _wallHitNormal;

        private Vector2 _leftWallCheckOrigin;
        private Vector2 _rightWallCheckOrigin;
        private Vector2 _baseWallCheckOrigin;
        private RaycastHit2D _wallHit;
        
        
        private float _coyoteTimer;
        private RaycastHit2D[] _groundHits;

        
        
        private void Awake()
        {
            _context = new PlayerContext(
                inputHandler as IPlayerInputManager,
                this as IPlayerController,
                _collisionHandler,
                _settings,
                _animator,
                _spriteRenderer,
                _bodyCollider,
                _feetCollider,
                _rb,
                _swingHinge
            );

            
            _stateMachine = new MovementStateMachine(_settings, _context);
            _stateMachine.onChangeState += type => Debug.Log(type, this);
            _groundHits = new RaycastHit2D[3];   
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

        private void CheckWall()
        {
            _leftWallCheckOrigin = new Vector2(_bodyCollider.bounds.min.x, _bodyCollider.bounds.center.y);
            _rightWallCheckOrigin = new Vector2(_bodyCollider.bounds.max.x, _bodyCollider.bounds.center.y);
            float distance = _settings.WallDetectionRange;
            LayerMask mask = _settings.WallLayers;

            if (!_spriteRenderer.flipX)
            {
                _wallHit = Physics2D.Raycast(_rightWallCheckOrigin, Vector2.right, distance, mask );
                _baseWallCheckOrigin = new Vector2(_bodyCollider.bounds.max.x, _bodyCollider.bounds.min.y);
                _isFootNearValidWall = Physics2D.Raycast(_baseWallCheckOrigin, Vector2.right, distance, mask).collider;
                
                if (_debugMode)
                {
                    Debug.DrawRay(_rightWallCheckOrigin, Vector2.right * distance, _wallHit.collider? Color.green : Color.red);
                    Debug.DrawRay(_baseWallCheckOrigin, Vector3.right * distance, _isFootNearValidWall ? Color.green : Color.red );
                }
            }
            else
            {
                _wallHit = Physics2D.Raycast(_leftWallCheckOrigin, Vector2.left, distance, mask );
                _baseWallCheckOrigin = new Vector2(_bodyCollider.bounds.min.x, _bodyCollider.bounds.min.y);
                _isFootNearValidWall = Physics2D.Raycast(_baseWallCheckOrigin, Vector2.left, distance, mask).collider;

                if (_debugMode)
                {
                    Debug.DrawRay(_leftWallCheckOrigin, Vector2.left * distance, _wallHit.collider? Color.green : Color.red);
                    Debug.DrawRay(_baseWallCheckOrigin, Vector3.left * distance, _isFootNearValidWall ? Color.green : Color.red );
                }
            }
            
            _isNearValidWall = _wallHit.collider;
            _wallHitNormal = _wallHit.normal;
        }
        
        private void CheckGrounded()
        {
            Bounds bounds = _feetCollider.bounds;
            Vector2[] checkOrigins = new Vector2[3]
            {
                new Vector2(bounds.min.x, bounds.min.y),
                new Vector2(bounds.center.x, bounds.min.y),
                new Vector2(bounds.max.x, bounds.min.y)
            };

            Vector2 middleOrigin = checkOrigins[1];
            
            float distance = _settings.GroundCheckDistance;
            LayerMask mask = _settings.GroundLayers;


            bool didHit = MultiRaycast(out _groundHits, checkOrigins, Vector2.down, distance, mask);
            
            
            if (didHit && _groundHits[1].collider)
            {
                _distanceToGround = 0f;
                
                _isGrounded = true;
                _isInCoyoteTime = false;
            }
            else
            {
                RaycastHit2D hit = Physics2D.Raycast(middleOrigin, Vector2.down, Mathf.Infinity, mask);
                _distanceToGround = hit.distance;
                
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
            foreach (RaycastHit2D hit in _groundHits)
            {
                normal +=  hit.normal;
            }
            
            normal /= _groundHits.Length;
            normal.Normalize();
            
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


        private bool MultiRaycast(out RaycastHit2D[] hits, Vector2[] origins, Vector2 dir, float distance, LayerMask mask)
        {
            hits =  new RaycastHit2D[origins.Length];

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
