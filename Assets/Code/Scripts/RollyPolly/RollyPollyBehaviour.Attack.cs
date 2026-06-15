using UnityEngine;

namespace RollyPolly
{
    public partial class RollyPollyBehaviour
    {
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
            Vector2 eyePos = myPos + Vector2.up * RollyPollyConstants.EyeLevelOffset;
            Vector2 targetPos = playerPos + Vector2.up * RollyPollyConstants.EyeLevelOffset;
            Vector2 losDir = (targetPos - eyePos).normalized;
            float losDist = Vector2.Distance(eyePos, targetPos);

            var hit = Physics2D.Raycast(eyePos, losDir, losDist, _patrolBlockingLayers);
            if (hit.collider != null)
            {
                return; // Obstacle is blocking the view!
            }

            // 6. Gap check (Don't aggro if there is a pit between us)
            int stepCount = Mathf.CeilToInt(losDist / RollyPollyConstants.GapCheckStep);
            for (int i = 1; i < stepCount; i++)
            {
                Vector2 stepOrigin = eyePos + losDir * (i * RollyPollyConstants.GapCheckStep);
                var groundHit = Physics2D.Raycast(stepOrigin, Vector2.down, RollyPollyConstants.GapCheckDepth, _patrolBlockingLayers);
                if (groundHit.collider == null)
                {
                    return; // Gap/Pit detected! Abort attack.
                }
            }

            // Player detected with valid line of sight and continuous ground!
            ChangeState(ERollyState.Transition);
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
                if (_patrolCollider != null) _patrolCollider.enabled = false;
                if (_attackCollider != null)
                {
                    _attackCollider.enabled = true;
                    _activeCollider = _attackCollider;
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

            // 1. Hard safety timeout
            if (_stateTimer > RollyPollyConstants.AttackTimeout)
            {
                ChangeState(ERollyState.Patrol);
                return;
            }

            // 2. Loss-of-track check: revert to patrol if the player is too far away
            //    or line of sight has been broken for too long.
            if (_playerRb != null)
            {
                Vector2 myPos = transform.position;
                Vector2 playerPos = _playerRb.transform.position;
                float distToPlayer = Vector2.Distance(myPos, playerPos);

                bool tooFar = distToPlayer > _chaseMaxRange;
                bool losBlocked = false;

                if (!tooFar)
                {
                    Vector2 eyePos = myPos + Vector2.up * RollyPollyConstants.EyeLevelOffset;
                    Vector2 targetPos = playerPos + Vector2.up * RollyPollyConstants.EyeLevelOffset;
                    Vector2 losDir = (targetPos - eyePos).normalized;
                    float losDist = Vector2.Distance(eyePos, targetPos);
                    var losHit = Physics2D.Raycast(eyePos, losDir, losDist, _rollBlockingLayers);
                    losBlocked = losHit.collider != null;
                }

                if (tooFar || losBlocked)
                    _playerLostTimer += Time.fixedDeltaTime;
                else
                    _playerLostTimer = 0f;

                if (_playerLostTimer >= _loseTrackTime)
                {
                    ChangeState(ERollyState.Patrol);
                    return;
                }
            }

            // 3. Roll movement
            if (IsGrounded())
            {
                MoveAlongSlope(_rollSpeed, _rollAcceleration);
            }
            // Airborne: let momentum and gravity handle the trajectory naturally.
        }

        private void TickStunned()
        {
            if (_stateTimer >= _stunDuration)
            {
                _postStunTimer = _postStunAggroCooldown;
                ChangeState(ERollyState.Patrol);
            }
        }

        private void TryFlipAttack()
        {
            Vector2 direction = _isFlipped ? Vector2.left : Vector2.right;
            Vector2 origin = (Vector2)transform.position + Vector2.up * RollyPollyConstants.EyeLevelOffset;
            var wallHit = Physics2D.Raycast(origin, direction, _wallCheckDistance, _rollBlockingLayers);
            Debug.DrawRay(origin, direction * _wallCheckDistance, Color.magenta);

            if (wallHit.collider)
            {
                // Let physical collision handle player hits and breakable walls
                if (wallHit.collider.CompareTag("Player") || wallHit.collider.GetComponent<Platforming.BreakableWall>())
                {
                    return;
                }

                float hitAngle = Vector2.Angle(wallHit.normal, Vector2.up);
                if (hitAngle > RollyPollyConstants.WallAngleThreshold)
                {
                    _isFlipped = !_isFlipped;
                    UpdateSpriteDirection();
                    if (_rb != null) _rb.linearVelocityX = 0f;
                }
            }
        }

        private void RotateAttackSprite()
        {
            if (_stutterTimer > 0f) return; // Do not rotate visual sprite when stuttered

            if (_attackSprite != null && _rb != null)
            {
                float speed = _rb.linearVelocityX;
                // Rotate visual sprite around Z axis based on horizontal speed
                // Account for localScale.x sign to prevent flipped backwards rotation
                float scaleSign = Mathf.Sign(transform.localScale.x);
                float angleChange = -speed * _rollRotationSpeed * scaleSign * Time.deltaTime;
                _attackSprite.transform.Rotate(0f, 0f, angleChange);
            }
        }
    }
}
