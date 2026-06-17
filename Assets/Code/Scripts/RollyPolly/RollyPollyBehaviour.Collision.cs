using System.Collections;
using UnityEngine;
using Interfaces;
using GameState;

namespace RollyPolly
{
    public partial class RollyPollyBehaviour
    {
        /// <summary>
        /// Shared pulse-hit handler. Deduplicated from OnParticleCollision and OnTriggerEnter2D
        /// which previously contained identical logic.
        /// </summary>
        private void HandlePulseHit()
        {
            if (_currentState != ERollyState.Attack || _pulseCooldownTimer > 0f) return;

            _pulseCooldownTimer = _pulseHitCooldown;
            _stutterTimer = _pulseStutterDuration;
            if (_rb != null)
            {
                _rb.linearVelocityX *= _pulseMomentumRatio;
            }
        }

        private void OnParticleCollision(GameObject other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                HandlePulseHit();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDead) return;

            if (other.gameObject.layer == LayerMask.NameToLayer("Pulse"))
            {
                HandlePulseHit();
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
                    // Only apply knockback if the player was successfully damaged (not in invulnerability)
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
                _rb.linearVelocity = new Vector2(Random.Range(-4f, 4f), 20f);
            }

            // Disable all colliders to allow flying up clean
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = false;
            }

            // Spin and fly up
            yield return new WaitForSeconds(RollyPollyConstants.YeetDuration);

            // Explode in a poof effect!
            if (_poofEffectPrefab != null)
            {
                Instantiate(_poofEffectPrefab, transform.position, Quaternion.identity);
            }

            HideOffscreen();
        }

        private void HideOffscreen()
        {
            // Disable all colliders FIRST so the body at offscreen Y generates zero contacts
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = false;
            }

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
            transform.position = new Vector3(0f, RollyPollyConstants.OffscreenY, 0f);
        }

        public void TriggerReset()
        {
            // Cancel any running coroutines (e.g. YeetAndKillRoutine) so they can't
            // call HideOffscreen() after the reset has already happened.
            StopAllCoroutines();

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

                if (_patrolCollider != null)
                {
                    _patrolCollider.enabled = true;
                    _activeCollider = _patrolCollider;
                }
            }

            if (_attackSprite != null)
            {
                _attackSprite.SetActive(false);
                _attackSprite.transform.localRotation = Quaternion.identity;

                if (_attackCollider != null) _attackCollider.enabled = false;
            }

            // Restore transform
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);

            // Restore Rigidbody state
            if (_rb != null)
            {
                _rb.interpolation = RigidbodyInterpolation2D.None;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                // Explicitly force the physics body position AFTER switching to Dynamic.
                // The bodyType change from Static→Dynamic can inherit the old Static body's
                // internal physics position (-9999) instead of reading transform.position.
                // Setting _rb.position directly writes to the Box2D body — this always works.
                _rb.position = (Vector2)_spawnPosition;
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;

                // Restore preferred settings
                _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            // Ensure all colliders are re-enabled (they get disabled during Yeet)
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = true;
            }

            // Re-enforce attack collider to be disabled initially
            if (_attackCollider != null) _attackCollider.enabled = false;
        }
    }
}
