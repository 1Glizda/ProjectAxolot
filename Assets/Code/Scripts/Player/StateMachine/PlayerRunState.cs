
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
                    stateMachine.ChangeState(typeof(PlayerJumpState));
                    //TODO add physics ground force
                    return;
                }
                else if(!isInCoyoteTime)
                {
                    stateMachine.ChangeState(typeof(PlayerFallingState));
                    return;    
                }
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState(typeof(PlayerJumpState));
                //TODO add physics ground force
                return;
            }
            
            
            if (ctx.controller.IsNearValidWall)
            {
                
                float dot = Vector2.Dot(new Vector2(horizontalInput, 0f), ctx.controller.WallHitNormal);
                Debug.Log(dot);
                if (dot < 0f)
                {
                    //moving towards wall
                    stateMachine.ChangeState(typeof(PlayerClimbingState));
                } 

            }

            if (ctx.rb.linearVelocityX == 0f && horizontalInput == 0f)
            {
                stateMachine.ChangeState(typeof(PlayerIdleState));
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
