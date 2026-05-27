using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerClimbingState : PlayerBaseState
    {

        private bool _jumpTriggered;
        private bool _isDetached;
        private float _attachTimer;
        private float _slideDelayTimer;
        private float _groundedTimer;
        private const float GroundedExitDelay = 0.05f;
        public PlayerClimbingState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
            
        }

        public override void EnterState()
        {
            stateMachine.lastVine = null;
            ctx.stateProvider.NotifyStartClimb();
            ctx.rb.linearVelocity = Vector2.zero;
            _jumpTriggered = false;
            _isDetached = false;
            _attachTimer = settings.WallAttachGraceTime;
            _slideDelayTimer = settings.WallSlideDelay;
            _groundedTimer = 0f;
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

            if (ctx.stateProvider.WallHitNormal != Vector2.zero)
            {
                stateMachine.lastWallNormal = ctx.stateProvider.WallHitNormal;
            }

            if (isGrounded)
            {
                _groundedTimer += dt;
                if (_groundedTimer >= GroundedExitDelay)
                {
                    stateMachine.ChangeState<PlayerIdleState>();
                    return;
                }
            }
            else
            {
                _groundedTimer = 0f;
            }

            if (_attachTimer > 0f)
            {
                _attachTimer -= dt;
            }

            if (verticalInput != 0f)
            {
                _slideDelayTimer = settings.WallSlideDelay;
            }
            else if (_slideDelayTimer > 0f)
            {
                _slideDelayTimer -= dt;
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
                stateMachine.wasDetached = true;
                stateMachine.ChangeState<PlayerFallingState>();
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
            float targetV = (_slideDelayTimer > 0f) ? 0f : -settings.WallSlideSpeed;
            ctx.rb.linearVelocityY = Mathf.MoveTowards(currentV, targetV, settings.WallDeceleration * dt);
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

