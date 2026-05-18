
using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerRunState : PlayerBaseState
    {
        public PlayerRunState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            TryFlipSprite();
            
            if (!isGrounded)
            {
                if (isInCoyoteTime && stateMachine.IsInJumpBuffer)
                {
                    stateMachine.ChangeState<PlayerJumpState>();
                    //TODO add physics ground force
                    return;
                }
                
                stateMachine.ChangeState<PlayerFallingState>();
                return;   
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState<PlayerJumpState>();
                //TODO add physics ground force
                return;
            }


            if (ctx.stateProvider.IsNearValidWall && verticalInput > 0f)
            {
                stateMachine.ChangeState<PlayerClimbingState>();
                return;
            }

            if (ctx.stateProvider.IsFootNearPushable)
            {
                stateMachine.ChangeState<PlayerPushPullState>();
            }


            if (ctx.rb.linearVelocityX == 0f && horizontalInput == 0f)
            {
                stateMachine.ChangeState<PlayerIdleState>();
            }
        }

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);
            ApplyAccel(dt, settings.GroundedAcceleration, settings.GroundedDeceleration, settings.MaxHorizontalVelocity);
            ApplyGravity(dt, settings.BaseGravity);

        }
    }
}
