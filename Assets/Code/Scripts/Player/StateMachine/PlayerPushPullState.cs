using Interfaces;
using UnityEngine;

namespace Player.StateMachine
{
    internal class PlayerPushPullState : PlayerBaseState
    {
        private IPushable _pushable;
        private float _currentRampTime;
        private bool _isSnapping;
        private float _snapTimeoutTimer;
        internal PlayerPushPullState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine){}

        public override void EnterState()
        {
            _pushable = ctx.stateProvider.Pushable;
            _currentRampTime = 0f;
            _isSnapping = true;
            _snapTimeoutTimer = 0f;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_pushable == null)
            {
                stateMachine.ChangeState<PlayerIdleState>();
                return;
            }

            // Perform a robust, height-immune 2D bounds distance check to ensure player is still near the boulder.
            // This prevents frame-by-frame state dropouts on slopes when vertical alignment shifts slightly.
            if (_pushable is Component component)
            {
                Collider2D boulderCollider = component.GetComponent<Collider2D>();
                if (boulderCollider != null)
                {
                    Bounds boulderBounds = boulderCollider.bounds;
                    Bounds playerBounds = ctx.bodyCollider.bounds;
                    
                    float horizontalDistance = 0f;
                    if (playerBounds.max.x < boulderBounds.min.x)
                    {
                        horizontalDistance = boulderBounds.min.x - playerBounds.max.x;
                    }
                    else if (playerBounds.min.x > boulderBounds.max.x)
                    {
                        horizontalDistance = playerBounds.min.x - boulderBounds.max.x;
                    }

                    if (horizontalDistance > settings.PushableCheckDistance + 0.15f)
                    {
                        stateMachine.ChangeState<PlayerIdleState>();
                        return;
                    }
                }
            }
            else
            {
                stateMachine.ChangeState<PlayerIdleState>();
                return;
            }

            if (!isGrounded)
            {
                if (isInCoyoteTime && stateMachine.IsInJumpBuffer)
                {
                    stateMachine.ChangeState<PlayerJumpState>();
                    //TODO add physics ground force
                    return;
                }
                else if(!isInCoyoteTime)
                {
                    stateMachine.ChangeState<PlayerFallingState>();
                    return;    
                }
                // We are in coyote time but not jumping. Just continue the state.
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState<PlayerJumpState>();
                //TODO add physics ground force
                return;
            }

            if (settings.UseManualGrabForPushables)
            {
                if (!grabAction.IsPressed())
                {
                    stateMachine.ChangeState<PlayerIdleState>();
                    return;
                }
            }
            else
            {
                if (horizontalInput == 0f)
                {
                    stateMachine.ChangeState<PlayerIdleState>();
                    return;
                }

                float facingDir = Mathf.Abs(ctx.spriteObject.transform.localEulerAngles.y) < 90f ? 1f : -1f;
                if (Mathf.Sign(horizontalInput) != Mathf.Sign(facingDir))
                {
                    stateMachine.ChangeState<PlayerRunState>();
                    return;
                }
            }
            
        }

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);

            float targetX = GetTargetSnapX();

            if (_isSnapping)
            {
                _snapTimeoutTimer += dt;

                // Hard timeout safety switch to guarantee players can always push even on high-friction slopes/corners
                if (_snapTimeoutTimer >= 0.15f)
                {
                    ctx.rb.position = new Vector2(targetX, ctx.rb.position.y);
                    _isSnapping = false;
                }
                else
                {
                    // Smoothly slide/lerp the player's position horizontally to the face of the boulder
                    float currentX = Mathf.Lerp(ctx.rb.position.x, targetX, dt * settings.PushSnapSpeed);
                    ctx.rb.position = new Vector2(currentX, ctx.rb.position.y);
                    ctx.rb.linearVelocity = Vector2.zero;

                    if (Mathf.Abs(ctx.rb.position.x - targetX) < 0.015f)
                    {
                        ctx.rb.position = new Vector2(targetX, ctx.rb.position.y);
                        _isSnapping = false;
                    }
                }
                return;
            }

            if (horizontalInput != 0f)
            {
                _currentRampTime = Mathf.Min(_currentRampTime + dt, settings.PushForceRampTime);
            }
            else
            {
                _currentRampTime = Mathf.Max(_currentRampTime - dt, 0f);
            }

            float t = settings.PushForceRampTime > 0f ? (_currentRampTime / settings.PushForceRampTime) : 1f;
            
            // Start at a 50% force baseline to instantly break static friction, then ramp to 100%
            float startingForce = settings.PushPullForce * 0.5f;
            float forceMag = horizontalInput * Mathf.Lerp(startingForce, settings.PushPullForce, t);
            
            _pushable.ApplyPushForce(groundData.slopeTangent * forceMag);
            
            // Sync entire player velocity (both horizontal and vertical) with the boulder when grounded.
            // On slopes, syncing only the horizontal velocity while leaving vertical movement to independent physics
            // causes the player and boulder to separate vertically, leading to intense collision chatter and jitter.
            if (isGrounded)
            {
                ctx.rb.linearVelocity = _pushable.Velocity;
            }
            else
            {
                ctx.rb.linearVelocityX = _pushable.Velocity.x;
            }
            
            ApplyGravity(dt, settings.BaseGravity);
        }

        private float GetTargetSnapX()
        {
            if (_pushable is Component component)
            {
                Collider2D boulderCollider = component.GetComponent<Collider2D>();
                if (boulderCollider != null)
                {
                    Bounds boulderBounds = boulderCollider.bounds;
                    Bounds playerBounds = ctx.bodyCollider.bounds;
                    
                    if (playerBounds.center.x < boulderBounds.center.x)
                    {
                        // Player is on the left side of the boulder
                        return boulderBounds.min.x - playerBounds.extents.x + 0.01f;
                    }
                    else
                    {
                        // Player is on the right side of the boulder
                        return boulderBounds.max.x + playerBounds.extents.x - 0.01f;
                    }
                }
            }
            return ctx.rb.position.x;
        }
    }
}
