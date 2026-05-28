using System.Collections;
using UnityEngine;
using Player;
using Player.GameState;
using Interfaces;

namespace RollyPolly
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class RollyPollyBehaviour : MonoBehaviour, IPulseInteraction, IResettable
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
        private float _postStunTimer;
        
        private bool _isDead;
        private Vector2 _smoothedGroundNormal = Vector2.up;
        
        
        
        private void Awake()
        {
            Player.GameState.CheckpointsManager.RegisterResettable(this);

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
                _smoothedGroundNormal = Vector2.Lerp(_smoothedGroundNormal, rawNormal, 8f * Time.deltaTime).normalized;
            }
            else
            {
                _smoothedGroundNormal = Vector2.Lerp(_smoothedGroundNormal, Vector2.up, 8f * Time.deltaTime).normalized;
            }

            RotatePatrolSprite();

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
                case ERollyState.Stunned: TickStunned(); break;
            }
        }

        private bool CheckForWall(Vector2 direction)
        {
            var wallHit = Physics2D.Raycast(transform.position + 0.5f * Vector3.up, direction, _wallCheckDistance, _patrolBlockingLayers);
            Debug.DrawRay(transform.position + 0.5f * Vector3.up, direction * _wallCheckDistance, Color.red);
            
            if (wallHit.collider)
            {
                float hitAngle = Vector2.Angle(wallHit.normal, Vector2.up);
                if (hitAngle > 50f)
                {
                    return true;
                }
            }
            return false;
        }

        private void TryFlip()
        {
            Vector2 direction = _isFlipped ? Vector2.left : Vector2.right;
            bool shouldFlip = CheckForWall(direction);
            
            // 2. Geyser check (avoid geysers ahead)
            if (!shouldFlip && _currentState == ERollyState.Patrol)
            {
                float geyserCheckDistance = 1.5f;
                RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + 0.5f * Vector3.up, direction, geyserCheckDistance);
                foreach (var hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject)
                    {
                        var geyser = hit.collider.GetComponent<Platforming.GeyserBehaviour>() ?? hit.collider.GetComponentInParent<Platforming.GeyserBehaviour>();
                        if (geyser != null)
                        {
                            shouldFlip = true;
                            break;
                        }
                    }
                }
            }

            // 3. Ledge check (only if enabled and currently patrolling on the ground)
            // Always cast straight DOWN — immune to slope-peak normal discontinuities
            if (!shouldFlip && _turnAtLedges && IsGrounded())
            {
                float dirX = _isFlipped ? -1f : 1f;
                Vector2 feetCenter = GetColliderBottom();
                Vector2 ledgeCheckOrigin = feetCenter + new Vector2(dirX * _ledgeCheckDistance, 0.15f);
                var ledgeHit = Physics2D.Raycast(ledgeCheckOrigin, Vector2.down, 0.6f, _patrolBlockingLayers);
                Debug.DrawRay(ledgeCheckOrigin, Vector2.down * 0.6f, Color.green);
                
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

        private void TryFlipAttack()
        {
            Vector2 direction = _isFlipped ? Vector2.left : Vector2.right;
            var wallHit = Physics2D.Raycast(transform.position + 0.5f * Vector3.up, direction, _wallCheckDistance, _rollBlockingLayers);
            Debug.DrawRay(transform.position + 0.5f * Vector3.up, direction * _wallCheckDistance, Color.magenta);
            
            if (wallHit.collider)
            {
                // Let physical collision handle player hits and breakable walls
                if (wallHit.collider.CompareTag("Player") || wallHit.collider.GetComponent<Platforming.BreakableWall>())
                {
                    return;
                }

                float hitAngle = Vector2.Angle(wallHit.normal, Vector2.up);
                if (hitAngle > 50f)
                {
                    _isFlipped = !_isFlipped;
                    UpdateSpriteDirection();
                    if (_rb != null) _rb.linearVelocityX = 0f;
                }
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
            if (!IsGrounded()) return;
            if (_postStunTimer > 0f) return; // Cannot aggro during post-stun cooldown

            Vector2 playerPos = _playerRb.transform.position;
            Vector2 myPos = transform.position;

            // 1. Same level check (height tolerance relative to slope normal)
            // Use cached smoothed normal instead of firing a fresh raycast every frame
            Vector2 normal = _smoothedGroundNormal;
            float relativeHeightDiff = Vector2.Dot(playerPos - myPos, normal);
            // Do not detect if player is too far below, or if player is above the enemy (with slope/pivot tolerance)
            if (relativeHeightDiff < -_detectionHeightTolerance || relativeHeightDiff > _aboveHeightTolerance) return;

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
            if (!IsInPatrolZone(playerPos.x)) return;

            // 5. Line of Sight (LoS) check
            // Ensure no walls or ground bumps are blocking the view. We offset by 0.5f to simulate "eye level"
            // for both the Rolly Polly and the Player, avoiding ground clipping.
            Vector2 eyePos = myPos + Vector2.up * 0.5f;
            Vector2 targetPos = playerPos + Vector2.up * 0.5f;
            Vector2 losDir = (targetPos - eyePos).normalized;
            float losDist = Vector2.Distance(eyePos, targetPos);
            
            var hit = Physics2D.Raycast(eyePos, losDir, losDist, _patrolBlockingLayers);
            if (hit.collider != null)
            {
                return; // Obstacle is blocking the view!
            }

            // 6. Gap check (Don't aggro if there is a pit between us)
            // Step along the line of sight every 0.5 units and cast downwards.
            float stepSize = 0.5f;
            int stepCount = Mathf.CeilToInt(losDist / stepSize);
            for (int i = 1; i < stepCount; i++)
            {
                Vector2 stepOrigin = eyePos + losDir * (i * stepSize);
                // Cast down 2.5 units (enough to tolerate slopes, but short enough to detect actual pits)
                var groundHit = Physics2D.Raycast(stepOrigin, Vector2.down, 2.5f, _patrolBlockingLayers);
                if (groundHit.collider == null)
                {
                    return; // Gap/Pit detected! Abort attack.
                }
            }

            // Player detected with valid line of sight and continuous ground!
            ChangeState(ERollyState.Transition);
        }

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
            // Raycast from slightly above the active collider's bottom edge straight down.
            // This correctly matches the physical contact point regardless of nested child scaling.
            Vector2 bottom = GetColliderBottom();
            Vector2 origin = bottom + Vector2.up * 0.1f;
            float rayLength = 0.25f;
            var hit = Physics2D.Raycast(origin, Vector2.down, rayLength, _patrolBlockingLayers);
            return hit.collider != null;
        }

        private Vector2 GetGroundNormal()
        {
            // Raycast from the active collider's bottom for stable, consistent surface normals.
            Vector2 bottom = GetColliderBottom();
            Vector2 origin = bottom + Vector2.up * 0.1f;
            float rayLength = 0.4f;
            var hit = Physics2D.Raycast(origin, Vector2.down, rayLength, _patrolBlockingLayers);
            if (hit.collider != null)
            {
                return hit.normal;
            }
            return Vector2.up;
        }

        private void RotatePatrolSprite()
        {
            if (_patrolSprite == null) return;

            if (_currentState == ERollyState.Patrol && IsGrounded())
            {
                // Use the pre-smoothed normal for jitter-free visual rotation
                float angle = Vector2.SignedAngle(Vector2.up, _smoothedGroundNormal);
                
                // Adjust for parent local scale mirroring when flipped horizontally
                float scaleSign = Mathf.Sign(transform.localScale.x);
                float localAngle = angle * scaleSign;
                
                // Smoothly tilt visually along the sloped ground
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, localAngle);
                _patrolSprite.transform.localRotation = Quaternion.Lerp(_patrolSprite.transform.localRotation, targetRotation, 10f * Time.deltaTime);
            }
            else
            {
                // Smoothly return visual back to default upright rotation when airborne or transitioned
                _patrolSprite.transform.localRotation = Quaternion.Lerp(_patrolSprite.transform.localRotation, Quaternion.identity, 10f * Time.deltaTime);
            }
        }
        
        private void TickPatrol()
        {
            if (_recoilTimer > 0f) return; // Allow drifting while recoiling from player hit
            
            if (IsGrounded())
            {
                // Use the pre-smoothed normal to avoid tangent direction flickering at slope transitions.
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
                Vector2 targetVelocity = travelDir * _speed;
                _rb.linearVelocity = Vector2.MoveTowards(currentVel, targetVelocity, _patrolAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Airborne: do not actively drive horizontal velocity.
                // Let momentum and gravity handle the trajectory naturally.
            }
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

            Vector2 direction = _isFlipped ? Vector2.left : Vector2.right;

            // 2. Safety timeout check (gives up if it rolls forever without hitting a wall)
            if (_stateTimer > 5f)
            {
                ChangeState(ERollyState.Patrol);
                return;
            }

            if (IsGrounded())
            {
                // Use smoothed normal to avoid jitter from raw normal flicker at slope transitions.
                Vector2 smoothNormal = _smoothedGroundNormal;
                Vector2 slopeTangent = new Vector2(smoothNormal.y, -smoothNormal.x);
                float sign = _isFlipped ? -1f : 1f;
                Vector2 travelDir = sign * slopeTangent;

                // Strip into-slope velocity so gravity accumulation doesn't fight the roll.
                Vector2 currentVel = _rb.linearVelocity;
                float normalComponent = Vector2.Dot(currentVel, smoothNormal);
                if (normalComponent < 0f)
                    currentVel -= normalComponent * smoothNormal;
                _rb.linearVelocity = currentVel;

                // Apply constant physical acceleration capped at target speed
                Vector2 targetVelocity = travelDir * _rollSpeed;
                _rb.linearVelocity = Vector2.MoveTowards(currentVel, targetVelocity, _rollAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Airborne: do not actively drive horizontal velocity.
                // Let momentum and gravity handle the trajectory naturally.
            }
        }

        private void TickStunned()
        {
            if (_stateTimer >= _stunDuration)
            {
                _postStunTimer = _postStunAggroCooldown;
                ChangeState(ERollyState.Patrol);
            }
        }

        public void PulseInteract()
        {
            // Satisfy IPulseInteraction interface
        }

        public void TriggerReset()
        {
            _currentState = ERollyState.Patrol;
            _stateTimer = 0f;
            _pulseCooldownTimer = 0f;
            _stutterTimer = 0f;
            _recoilTimer = 0f;
            _postStunTimer = 0f;
            _playerLostTimer = 0f;
            _isDead = false;
            _poofFired = false;
            
            // Restore visual states to patrol default
            if (_patrolSprite != null)
            {
                _patrolSprite.SetActive(true);
                _patrolSprite.transform.localRotation = Quaternion.identity;
                
                var patrolCollider = _patrolSprite.GetComponent<Collider2D>();
                if (patrolCollider != null)
                {
                    patrolCollider.enabled = true;
                    _activeCollider = patrolCollider;
                }
            }

            if (_attackSprite != null)
            {
                _attackSprite.SetActive(false);
                _attackSprite.transform.localRotation = Quaternion.identity;
                
                var attackCollider = _attackSprite.GetComponent<Collider2D>();
                if (attackCollider != null) attackCollider.enabled = false;
            }

            // Restore Rigidbody state
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            // Ensure all colliders are re-enabled (they get disabled during Yeet)
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = true;
            }

            // Re-enforce attack collider to be disabled initially
            if (_attackSprite != null)
            {
                var attackCollider = _attackSprite.GetComponent<Collider2D>();
                if (attackCollider != null) attackCollider.enabled = false;
            }
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

                        // Apply enemy recoil/knockback and stun
                        _recoilTimer = _enemyRecoilDuration;
                        ChangeState(ERollyState.Stunned);

                        if (_rb != null)
                        {
                            _rb.linearVelocity = new Vector2(-dirX * _enemyRecoilForceX, _enemyRecoilForceY);
                        }
                    }
                }
            }

            // Prevent offensive interactions (breaking walls, pushing movables, getting stunned) 
            // if currently knocked back or stuttering.
            if (_currentState == ERollyState.Attack && (_recoilTimer > 0f || _stutterTimer > 0f))
            {
                return;
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

                    // Poof effect and die
                    if (_poofEffectPrefab != null)
                    {
                        Instantiate(_poofEffectPrefab, transform.position, Quaternion.identity);
                    }
                    HideOffscreen();
                    return;
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

            HideOffscreen();
        }

        private void HideOffscreen()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _rb.bodyType = RigidbodyType2D.Static;
            }
            
            // Hide visuals
            if (_patrolSprite != null) _patrolSprite.SetActive(false);
            if (_attackSprite != null) _attackSprite.SetActive(false);

            // Move way off-screen
            transform.position = new Vector3(0f, -9999f, 0f);
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
                var patrolCollider = _patrolSprite != null ? _patrolSprite.GetComponent<Collider2D>() : null;
                var attackCollider = _attackSprite != null ? _attackSprite.GetComponent<Collider2D>() : null;

                if (attackCollider != null) attackCollider.enabled = false;
                if (patrolCollider != null)
                {
                    patrolCollider.enabled = true;
                    _activeCollider = patrolCollider;
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
        Stunned
    }
}
