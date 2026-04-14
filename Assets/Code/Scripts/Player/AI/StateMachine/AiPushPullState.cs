using Interfaces;
using UnityEngine;

namespace Player.AI.StateMachine
{
    internal class AiPushPullState : AiBaseState
    {
        private IPushable _pushable;
        internal AiPushPullState(AiContext ctx, AiMovementStateMachine stateMachine) : base(ctx, stateMachine){}

        public override void EnterState()
        {
            _pushable = ctx.controller.Pushable;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_pushable == null || ctx.controller.Pushable == null)
            {
                stateMachine.ChangeState<AiIdleState>();
                return;
            }

            if (!isGrounded)
            {
                if (isInCoyoteTime && stateMachine.IsInJumpBuffer)
                {
                    stateMachine.ChangeState<AiJumpState>();
                    //TODO add physics ground force
                    return;
                }
                else if(!isInCoyoteTime)
                {
                    stateMachine.ChangeState<AiFallingState>();
                    return;    
                }
                // We are in coyote time but not jumping. Just continue the state.
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState<AiJumpState>();
                //TODO add physics ground force
                return;
            }
            if (horizontalInput == 0f)
            {
                stateMachine.ChangeState<AiIdleState>();
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
