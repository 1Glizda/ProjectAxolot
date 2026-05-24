using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerClimbingState : PlayerBaseState
    {

        private bool _jumpTriggered;
        private bool _isDetached;
        private float _attachTimer;
        public PlayerClimbingState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
            
        }

        public override void EnterState()
        {
            stateMachine.LastVine = null;
            ctx.stateProvider.NotifyStartClimb();
            ctx.rb.linearVelocity = Vector2.zero;
            _jumpTriggered = false;
            _isDetached = false;
            _attachTimer = settings.WallAttachGraceTime;
        }
        
        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_isDetached)
            {
                stateMachine.ChangeState<PlayerFallingState>();
                return;
            }
            
            if (ctx.stateProvider.CanVault && ctx.stateProvider.IsFootNearValidWall)
            {
                stateMachine.ChangeState<PlayerVaultState>();
                return;
            }

            if (!ctx.stateProvider.IsNearValidWall)
            {
                stateMachine.ChangeState<PlayerFallingState>();
                return;
            }

            if (_attachTimer > 0f)
            {
                _attachTimer -= dt;
            }

            float dot = Vector2.Dot(new Vector2(horizontalInput, 0f), ctx.stateProvider.WallHitNormal);
            
            // Hold A/D away from wall → detach
            if (dot > 0f && _attachTimer <= 0f)
            {
                if (isGrounded)
                {
                    stateMachine.ChangeState<PlayerRunState>();
                    return;
                }
                else
                {
                    // Detach from wall with a push
                    Vector2 dir = ctx.stateProvider.WallHitNormal;
                    ctx.rb.AddForce(settings.WallDetachForce * ctx.rb.mass * dir, ForceMode2D.Impulse);
                    stateMachine.WasDetached = true;
                    _isDetached = true;
                    return;
                }
            }
            
            if(!_jumpTriggered) _jumpTriggered = jumpAction.triggered;
            
        }
        
        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);
            if (verticalInput != 0f && !_jumpTriggered)
            {
                Climb(dt);
            }
            else if (!_jumpTriggered)
            {
                Stop(dt);
            }
            
            // Wall jump: press Jump while on wall
            if (_jumpTriggered)
            {
                TryFlipSprite();
                ctx.stateProvider.NotifyJump();
                ctx.rb.linearVelocity = Vector2.zero;
                ctx.rb.AddForce(GetAngledVector() * settings.WallJumpForce, ForceMode2D.Impulse);
                stateMachine.WasDetached = true;
                _isDetached = true;
                return;
            }
            
        }

        private void Climb(float dt)
        {
            float currentV = ctx.rb.linearVelocityY;
            float targetV = verticalInput * settings.MaxClimbSpeed;
            
            if (verticalInput > 0f && ctx.stateProvider.IsHeadBlocked)
            {
                targetV = 0f;
            }
            
            ctx.rb.linearVelocityY = Mathf.MoveTowards(currentV, targetV, settings.WallAcceleration * dt);
        }
        
        private void Stop(float dt)
        {
            float currentV = ctx.rb.linearVelocityY;
            ctx.rb.linearVelocityY = Mathf.MoveTowards(currentV, -settings.WallSlideSpeed, settings.WallDeceleration * dt);
        }

        private Vector2 GetAngledVector()
        {
            Vector2 wallNormal = ctx.stateProvider.WallHitNormal;
            float rotationAngle = wallNormal.x > 0 ? settings.WallJumpAngle : -settings.WallJumpAngle;
            
            Quaternion rotation = Quaternion.Euler(0f, 0f, rotationAngle);

            return rotation * wallNormal;
        }
        
    }
}

