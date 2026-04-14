
using UnityEngine;

namespace Player.AI.StateMachine
{
    internal sealed class AiRunState : AiBaseState
    {
        public AiRunState(AiContext ctx, AiMovementStateMachine stateMachine) : base(ctx, stateMachine)
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
                    stateMachine.ChangeState<AiJumpState>();
                    //TODO add physics ground force
                    return;
                }
                
                stateMachine.ChangeState<AiFallingState>();
                return;   
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState<AiJumpState>();
                //TODO add physics ground force
                return;
            }


            if (ctx.controller.IsFootNearPushable)
            {
                stateMachine.ChangeState<AiPushPullState>();
            }


            if (ctx.rb.linearVelocityX == 0f && horizontalInput == 0f)
            {
                stateMachine.ChangeState<AiIdleState>();
            }
        }

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);
            ApplyAccel(dt, settings.GroundedAcceleration, settings.GroundedDeceleration, settings.MaxHorizontalVelocity);
            if(horizontalInput == 0f) ApplyDecel(dt, settings.GroundedDeceleration);
            ApplyGravity(dt, settings.BaseGravity);

        }
    }
}
