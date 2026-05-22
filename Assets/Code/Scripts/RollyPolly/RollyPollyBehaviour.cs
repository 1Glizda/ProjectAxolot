using System.Collections;
using UnityEngine;
using Player;
using Player.GameState;
using Interfaces;

namespace RollyPolly
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class RollyPollyBehaviour : MonoBehaviour, IPulseInteraction
    {

        [Header("References")] 
        [SerializeField] private GameObject _patrolSprite;
        [SerializeField] private GameObject _attackSprite;


        [Header("General Settings")] 
        [SerializeField] private float _wallCheckDistance;
        [SerializeField] private bool _isFlipped;
        
        [Header("Patrol")] 
        [SerializeField] private float _speed;
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
        
        [Header("Attack")]
        [SerializeField] private LayerMask _rollBlockingLayers;
        [SerializeField] private float _rollSpeed = 8f;
        [SerializeField] private float _rollAcceleration = 10f;
        [SerializeField] private float _rollRotationSpeed = 360f;

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
        
        private Rigidbody2D _rb;
        private Rigidbody2D _playerRb;

        private Collider2D _activeCollider;
        
        private ERollyState _currentState;
        private float _stateTimer;
        private bool _poofFired;
        
        private float _pulseCooldownTimer;
        private float _stutterTimer;
        private float _recoilTimer;
        private float _playerLostTimer;
        
        private bool _isDead;
        
        
        
        private void Awake()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _playerRb = playerObj.GetComponent<Rigidbody2D>();
            }
            
            _currentState = ERollyState.Patrol;
            _activeCollider = _patrolSprite.GetComponent<Collider2D>();
            if(!_activeCollider) Debug.LogError("No active collider found", this);
            
            _rb = GetComponent<Rigidbody2D>();
            
            // Premium Rigidbody2D configuration for smooth physics, gravity, and no sliding/jitter
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Ensure initial sprite states
            if (_patrolSprite != null) _patrolSprite.SetActive(true);
            if (_attackSprite != null) _attackSprite.SetActive(false);
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

            if (_currentState == ERollyState.Patrol)
            {
                TryDetectPlayer();
                TryFlip();
            }
            else if (_currentState == ERollyState.Attack)
            {
                RotateAttackSprite();
            }
            _stateTimer += Time.deltaTime;
        }

        private void RotateAttackSprite()
        {
            if (_stutterTimer > 0f) return; // Do not rotate visual sprite when stuttered

            if (_attackSprite != null && _rb != null)
            {
                float speed = _rb.linearVelocityX;
                // Rotate visual sprite around Z axis based on horizontal speed and account for localScale.x sign to prevent flipped backwards rotation
                float scaleSign = Mathf.Sign(transform.localScale.x);
                float angleChange = -speed * _rollRotationSpeed * scaleSign * Time.deltaTime;
                _attackSprite.transform.Rotate(0f, 0f, angleChange);
            }
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            switch (_currentState)
            {
                case ERollyState.Patrol: TickPatrol(); break;
                case ERollyState.Transition: TickTransition(); break;
                case ERollyState.Attack: TickAttack(); break;
            }
        }

        private void TryFlip()
        {
            Vector2 direction = _isFlipped ? Vector2.left : Vector2.right;
            
            // 1. Wall check
            var wallHit = Physics2D.Raycast(transform.position + 0.5f * Vector3.up, direction, _wallCheckDistance, _patrolBlockingLayers);
            Debug.DrawRay(transform.position + 0.5f * Vector3.up, direction * _wallCheckDistance, Color.red);
            
            bool shouldFlip = false;
            
            if (wallHit.collider)
            {
                shouldFlip = true;
            }
            // 2. Ledge check (only if enabled and currently patrolling on the ground)
            else if (_turnAtLedges && IsGrounded())
            {
                Vector2 ledgeCheckOrigin = (Vector2)transform.position + direction * _ledgeCheckDistance;
                var ledgeHit = Physics2D.Raycast(ledgeCheckOrigin + 0.1f * Vector2.up, Vector2.down, 0.5f, _patrolBlockingLayers);
                Debug.DrawRay(ledgeCheckOrigin + 0.1f * Vector2.up, Vector2.down * 0.5f, Color.green);
                
                if (!ledgeHit.collider)
                {
                    shouldFlip = true;
                }
            }

            if (shouldFlip)
            {
                _isFlipped = !_isFlipped;
                UpdateSpriteDirection();
            }
        }

        private bool IsInPatrolZone(float xPos)
        {
            if (_leftPatrolBound == null || _rightPatrolBound == null) return true;

            float minX = Mathf.Min(_leftPatrolBound.position.x, _rightPatrolBound.position.x);
            float maxX = Mathf.Max(_leftPatrolBound.position.x, _rightPatrolBound.position.x);

            return xPos >= minX && xPos <= maxX;
        }

        private void TryDetectPlayer()
        {
            if (_playerRb == null) return;

            Vector2 playerPos = _playerRb.transform.position;
            Vector2 myPos = transform.position;

            // 1. Same level check (height tolerance)
            float heightDiff = playerPos.y - myPos.y;
            // Do not detect if player is too far below, or if player is above the enemy (with slope/pivot tolerance)
            if (heightDiff < -_detectionHeightTolerance || heightDiff > _aboveHeightTolerance) return;

            // 2. Range check
            float distance = Vector2.Distance(myPos, playerPos);
            if (distance > _detectionRange) return;

            // 3. Facing direction check
            Vector2 direction = _isFlipped ? Vector2.left : Vector2.right;
            Vector2 dirToPlayer = (playerPos - myPos).normalized;
            if (_onlyDetectInFacingDirection)
            {
                float dot = Vector2.Dot(direction, dirToPlayer);
                if (dot <= 0) return; // Player is behind the enemy
            }

            // 4. Patrol zone membership check
            if (IsInPatrolZone(playerPos.x))
            {
                // No obstacle in the way -> Player detected within patrol bounds!
                ChangeState(ERollyState.Transition);
            }
        }

        private void UpdateSpriteDirection()
        {
            Vector3 scale = transform.localScale;
            // Negative scale flips horizontally. By default false (facing right) is positive scale.x.
            scale.x = _isFlipped ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        private bool IsGrounded()
        {
            // Simple downward raycast to check if grounded
            float rayLength = 0.2f;
            Vector2 origin = (Vector2)transform.position + 0.05f * Vector2.up;
            var hit = Physics2D.Raycast(origin, Vector2.down, rayLength, _patrolBlockingLayers);
            Debug.DrawRay(origin, Vector2.down * rayLength, Color.yellow);
            return hit.collider != null;
        }
        
        private void TickPatrol()
        {
            if (_recoilTimer > 0f) return; // Allow drifting while recoiling from player hit
            
            // Apply horizontal velocity based on direction while preserving the vertical velocity from gravity
            _rb.linearVelocityX = _isFlipped ? -_speed : _speed;
        }

        private void TickTransition()
        {
            // 1. Wait for jump duration, then spawn poof effect and swap sprites/colliders
            if (!_poofFired && _stateTimer >= _jumpTime)
            {
                _poofFired = true;

                // Instantiate poof effect at current position
                if (_poofEffectPrefab != null)
                {
                    Instantiate(_poofEffectPrefab, transform.position, Quaternion.identity);
                }

                // Swap visual active game objects
                if (_patrolSprite != null) _patrolSprite.SetActive(false);
                if (_attackSprite != null) _attackSprite.SetActive(true);

                // Swap colliders
                var patrolCollider = _patrolSprite != null ? _patrolSprite.GetComponent<Collider2D>() : null;
                var attackCollider = _attackSprite != null ? _attackSprite.GetComponent<Collider2D>() : null;

                if (patrolCollider != null) patrolCollider.enabled = false;
                if (attackCollider != null)
                {
                    attackCollider.enabled = true;
                    _activeCollider = attackCollider;
                }
            }

            // 2. Wait for poof duration to finish before rolling into Attack state
            if (_poofFired && _stateTimer >= _jumpTime + _poofTime)
            {
                ChangeState(ERollyState.Attack);
            }
        }

        private void TickAttack()
        {
            if (_stutterTimer > 0f || _recoilTimer > 0f)
            {
                return; // Allow drifting while stuttered or recoiling
            }

            if (_playerRb == null) return;

            Vector2 playerPos = _playerRb.transform.position;
            Vector2 myPos = transform.position;

            // 1. Distance check
            float distance = Vector2.Distance(myPos, playerPos);

            // 2. Chase max range, height, and patrol zone membership check
            float heightDiff = playerPos.y - myPos.y;
            bool isLost = (distance > _chaseMaxRange) || 
                          (heightDiff > _aboveHeightTolerance) || 
                          (heightDiff < -_detectionHeightTolerance) || 
                          !IsInPatrolZone(playerPos.x);

            if (isLost)
            {
                _playerLostTimer += Time.fixedDeltaTime;
                if (_playerLostTimer >= _loseTrackTime)
                {
                    ChangeState(ERollyState.Patrol);
                    return;
                }
            }
            else
            {
                _playerLostTimer = 0f;
            }

            // Roll towards player horizontally
            float targetXSpeed = Mathf.Sign(playerPos.x - myPos.x) * _rollSpeed;
            _rb.linearVelocityX = Mathf.MoveTowards(_rb.linearVelocityX, targetXSpeed, _rollAcceleration * Time.fixedDeltaTime);
        }

        public void PulseInteract()
        {
            // Satisfy IPulseInteraction interface
        }

        private void OnParticleCollision(GameObject other)
        {
            if (_currentState != ERollyState.Attack || _pulseCooldownTimer > 0f) return;

            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                _pulseCooldownTimer = _pulseHitCooldown;
                _stutterTimer = _pulseStutterDuration;
                if (_rb != null)
                {
                    _rb.linearVelocityX *= _pulseMomentumRatio;
                }
                PulseInteract();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDead) return;

            Platforming.GeyserBehaviour geyser = other.GetComponent<Platforming.GeyserBehaviour>() ?? other.GetComponentInParent<Platforming.GeyserBehaviour>();
            if (geyser != null && geyser.CurrentState == Platforming.GeyserBehaviour.GeyserState.Active)
            {
                YeetAndKill();
                return;
            }

            if (_currentState != ERollyState.Attack || _pulseCooldownTimer > 0f) return;

            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                _pulseCooldownTimer = _pulseHitCooldown;
                _stutterTimer = _pulseStutterDuration;
                if (_rb != null)
                {
                    _rb.linearVelocityX *= _pulseMomentumRatio;
                }
                PulseInteract();
            }
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (_isDead) return;

            Platforming.GeyserBehaviour geyser = other.collider.GetComponent<Platforming.GeyserBehaviour>() ?? other.collider.GetComponentInParent<Platforming.GeyserBehaviour>();
            if (geyser != null && geyser.CurrentState == Platforming.GeyserBehaviour.GeyserState.Active)
            {
                YeetAndKill();
                return;
            }

            // 1. Player contact (applies to both Patrol and Attack states)
            if (other.collider.CompareTag("Player"))
            {
                if (other.collider.TryGetComponent<IKnockbackable>(out var knockbackable))
                {
                    // Only apply knockback if the player was successfully damaged (not currently in their invulnerability state)
                    if (GameStateManager.Instance.DamagePlayer(_damageAmount, other.otherCollider))
                    {
                        float dirX = Mathf.Sign(other.transform.position.x - transform.position.x);
                        Vector2 knockbackVelocity = new Vector2(dirX * _knockbackForceX, _knockbackForceY);
                        knockbackable.ApplyKnockback(knockbackVelocity);

                        // Apply enemy recoil/knockback
                        _recoilTimer = _enemyRecoilDuration;
                        if (_rb != null)
                        {
                            _rb.linearVelocity = new Vector2(-dirX * _enemyRecoilForceX, _enemyRecoilForceY);
                        }
                    }
                }
            }

            // 2. Breakable Wall contact (only in Attack mode)
            if (_currentState == ERollyState.Attack)
            {
                if (other.gameObject.TryGetComponent<Platforming.BreakableWall>(out var wall))
                {
                    // Determine direction of break using relative velocity or collision contacts
                    Vector2 breakDir = _rb != null ? _rb.linearVelocity.normalized : Vector2.zero;
                    if (breakDir.sqrMagnitude < 0.01f && other.contactCount > 0)
                    {
                        breakDir = -other.contacts[0].normal;
                    }
                    if (breakDir.sqrMagnitude < 0.01f)
                    {
                        breakDir = Vector2.right;
                    }

                    // Break the wall
                    wall.Break(breakDir);

                    // Disable Rolly Polly game object
                    gameObject.SetActive(false);
                }
            }
        }

        private void YeetAndKill()
        {
            if (_isDead) return;
            _isDead = true;

            // Swap visual sprites to the attack sprite (the roll ball shape) for spinning
            if (_patrolSprite != null) _patrolSprite.SetActive(false);
            if (_attackSprite != null) _attackSprite.SetActive(true);

            StartCoroutine(YeetAndKillRoutine());
        }

        private IEnumerator YeetAndKillRoutine()
        {
            // Apply a massive upward yeet force
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.constraints = RigidbodyConstraints2D.None; // allow free rotation
                _rb.linearVelocity = new Vector2(UnityEngine.Random.Range(-4f, 4f), 20f);
            }

            // Disable all colliders to allow flying up clean
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = false;
            }

            // Spin and fly up for 0.7 seconds
            yield return new WaitForSeconds(0.7f);

            // Explode in a poof effect!
            if (_poofEffectPrefab != null)
            {
                Instantiate(_poofEffectPrefab, transform.position, Quaternion.identity);
            }

            gameObject.SetActive(false);
        }

        private void ChangeState(ERollyState newState)
        {
            // Transition back to Patrol from Attack
            if (newState == ERollyState.Patrol && _currentState == ERollyState.Attack)
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
                var patrolCollider = _patrolSprite != null ? _patrolSprite.GetComponent<Collider2D>() : null;
                var attackCollider = _attackSprite != null ? _attackSprite.GetComponent<Collider2D>() : null;

                if (attackCollider != null) attackCollider.enabled = false;
                if (patrolCollider != null)
                {
                    patrolCollider.enabled = true;
                    _activeCollider = patrolCollider;
                }

                // Re-align flipping to travel direction based on current physical velocity
                if (_rb != null)
                {
                    _isFlipped = _rb.linearVelocityX < 0f;
                }
                UpdateSpriteDirection();
            }

            _currentState = newState;
            _stateTimer = 0f;

            // Stop horizontal velocity immediately when leaving Patrol or Attack states (except when starting to patrol)
            if (_currentState != ERollyState.Patrol && _currentState != ERollyState.Attack && _rb != null)
            {
                _rb.linearVelocityX = 0f;
            }

            if (_currentState == ERollyState.Transition)
            {
                _poofFired = false;
                _playerLostTimer = 0f; // Reset loss timer when starting chase transition
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

        private void OnDrawGizmos()
        {
            // 1. Detection Range (translucent Cyan circle)
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // 2. Height Tolerance band (translucent Yellow wire cube, adjusted for asymmetric above/below bounds)
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
            float totalHeight = _detectionHeightTolerance + _aboveHeightTolerance;
            float centerY = transform.position.y + (_aboveHeightTolerance - _detectionHeightTolerance) * 0.5f;
            Vector3 size = new Vector3(_detectionRange * 2f, totalHeight, 0.1f);
            Gizmos.DrawWireCube(new Vector3(transform.position.x, centerY, transform.position.z), size);

            // 3. Patrol Bounds visualizer (Magenta vertical lines and transparent floor block)
            if (_leftPatrolBound != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 leftPos = _leftPatrolBound.position;
                Gizmos.DrawLine(new Vector3(leftPos.x, leftPos.y - 5f, leftPos.z), new Vector3(leftPos.x, leftPos.y + 5f, leftPos.z));
                Gizmos.DrawWireSphere(leftPos, 0.2f);
            }

            if (_rightPatrolBound != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 rightPos = _rightPatrolBound.position;
                Gizmos.DrawLine(new Vector3(rightPos.x, rightPos.y - 5f, rightPos.z), new Vector3(rightPos.x, rightPos.y + 5f, rightPos.z));
                Gizmos.DrawWireSphere(rightPos, 0.2f);
            }

            if (_leftPatrolBound != null && _rightPatrolBound != null)
            {
                float minX = Mathf.Min(_leftPatrolBound.position.x, _rightPatrolBound.position.x);
                float maxX = Mathf.Max(_leftPatrolBound.position.x, _rightPatrolBound.position.x);
                float midY = (_leftPatrolBound.position.y + _rightPatrolBound.position.y) * 0.5f;

                Gizmos.color = new Color(1f, 0f, 1.5f, 0.15f);
                Gizmos.DrawCube(new Vector3((minX + maxX) * 0.5f, midY, 0f), new Vector3(maxX - minX, 0.2f, 0.1f));
            }
        }
    }

    public enum ERollyState
    {
        Patrol,
        Transition,
        Attack,
    }
}
