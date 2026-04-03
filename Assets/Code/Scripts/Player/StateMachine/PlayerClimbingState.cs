using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerClimbingState : PlayerBaseState
    {

        private bool _jumpTriggered;
        private bool _isDetached;
        public PlayerClimbingState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
            
        }

        public override void EnterState()
        {
            ctx.rb.linearVelocity = Vector2.zero;
        }
        
        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_isDetached)
            {
                stateMachine.ChangeState<PlayerFallingState>();
            }
            
            if (!ctx.controller.IsNearValidWall)
            {
                if (ctx.controller.IsFootNearValidWall && ctx.collisionHandler.CanVault && ctx.collisionHandler
                    .VaultHelper != null)
                {
                    stateMachine.ChangeState<PlayerVaultState>();
                    return;
                }
                stateMachine.ChangeState<PlayerFallingState>();
                return;
            }

            float dot = Vector2.Dot(new Vector2(horizontalInput, 0f), ctx.controller.WallHitNormal);
            
            if (dot > 0f)
            {
                if (isGrounded)
                {
                    stateMachine.ChangeState<PlayerRunState>();
                    return;
                }
                else
                {
                    stateMachine.ChangeState<PlayerPrepareJumpState>();
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
            else if (ctx.rb.linearVelocityY != 0f && !_jumpTriggered)
            {
                Stop(dt);
            }
            
            if (_jumpTriggered)
            {
                Vector2 dir = ctx.controller.WallHitNormal;
                ctx.rb.AddForce(settings.WallDetachForce * ctx.rb.mass * dir, ForceMode2D.Impulse);
                return;
            }
            
        }

        private void Climb(float dt)
        {
            float currentV = ctx.rb.linearVelocityY;
           
            float deltaV = Mathf.Min(settings.WallAcceleration * dt, settings.MaxClimbSpeed - Mathf.Abs(currentV));
            deltaV *= verticalInput;

            float force = deltaV * ctx.rb.mass;
            
            ctx.rb.AddForce(force * Vector3.up, ForceMode2D.Impulse);
        }
        
        private void Stop(float dt)
        {
            float currentV = ctx.rb.linearVelocityY;
            float deltaV = settings.WallDeceleration * dt; 
            deltaV = Mathf.Min(deltaV, Mathf.Abs(currentV));
            deltaV *= -Mathf.Sign(currentV);

            float force = deltaV * ctx.rb.mass;
            
            ctx.rb.AddForce(force * Vector3.up, ForceMode2D.Impulse);
        }
        
    }
}
