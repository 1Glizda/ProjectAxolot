using UnityEngine;

namespace RollyPolly
{
    public partial class RollyPollyBehaviour
    {
        private void TickPatrol()
        {
            if (_recoilTimer > 0f) return; // Allow drifting while recoiling from player hit

            if (IsGrounded())
            {
                MoveAlongSlope(_speed, _patrolAcceleration);
            }
            // Airborne: let momentum and gravity handle the trajectory naturally.
        }

        private bool CheckForWall(Vector2 direction)
        {
            Vector2 origin = (Vector2)transform.position + Vector2.up * RollyPollyConstants.EyeLevelOffset;
            var wallHit = Physics2D.Raycast(origin, direction, _wallCheckDistance, _patrolBlockingLayers);
            Debug.DrawRay(origin, direction * _wallCheckDistance, Color.red);

            if (wallHit.collider)
            {
                float hitAngle = Vector2.Angle(wallHit.normal, Vector2.up);
                if (hitAngle > RollyPollyConstants.WallAngleThreshold)
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
                RaycastHit2D[] hits = Physics2D.RaycastAll(
                    (Vector2)transform.position + Vector2.up * RollyPollyConstants.EyeLevelOffset,
                    direction,
                    RollyPollyConstants.GeyserCheckDistance);

                foreach (var hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject)
                    {
                        var geyser = hit.collider.GetComponent<Platforming.GeyserBehaviour>()
                                  ?? hit.collider.GetComponentInParent<Platforming.GeyserBehaviour>();
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
                Vector2 ledgeCheckOrigin = feetCenter + new Vector2(dirX * _ledgeCheckDistance, RollyPollyConstants.LedgeCheckOriginOffset);
                var ledgeHit = Physics2D.Raycast(ledgeCheckOrigin, Vector2.down, RollyPollyConstants.LedgeCheckRayLength, _patrolBlockingLayers);
                Debug.DrawRay(ledgeCheckOrigin, Vector2.down * RollyPollyConstants.LedgeCheckRayLength, Color.green);

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
                _patrolSprite.transform.localRotation = Quaternion.Lerp(
                    _patrolSprite.transform.localRotation, targetRotation,
                    RollyPollyConstants.NormalSmoothSpeed * Time.deltaTime);
            }
            else
            {
                // Smoothly return visual back to default upright rotation when airborne or transitioned
                _patrolSprite.transform.localRotation = Quaternion.Lerp(
                    _patrolSprite.transform.localRotation, Quaternion.identity,
                    RollyPollyConstants.NormalSmoothSpeed * Time.deltaTime);
            }
        }
    }
}
