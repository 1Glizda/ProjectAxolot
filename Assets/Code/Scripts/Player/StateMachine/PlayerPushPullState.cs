using Interfaces;
using UnityEngine;

namespace Player.StateMachine
{
    internal class PlayerPushPullState : PlayerBaseState
    {
        private IPushable _pushable;
        internal PlayerPushPullState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine){}

        public override void EnterState()
        {
            _pushable = ctx.controller.Pushable;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

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
                stateMachine.ChangeState<PlayerPushPullState>();
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState<PlayerJumpState>();
                //TODO add physics ground force
                return;
            }
            if (horizontalInput == 0f)
            {
                stateMachine.ChangeState<PlayerIdleState>();
            }
            
        }

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);
            float forceMag = horizontalInput * settings.PushPullForce;
            
            _pushable.ApplyPushForce(groundData.slopeTangent * forceMag);
            
            ApplyAccel(dt, settings.GroundedAcceleration, settings.GroundedDeceleration, settings.MaxHorizontalVelocity);
            ApplyGravity(dt, settings.BaseGravity);
        }
    }
}
