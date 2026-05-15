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
            _pushable = ctx.stateProvider.Pushable;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_pushable == null || ctx.stateProvider.Pushable == null)
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

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);
            float forceMag = horizontalInput * settings.PushPullForce;
            
            _pushable.ApplyPushForce(groundData.slopeTangent * forceMag);
            
            // Sync player velocity with the boulder to prevent physics jitter/bouncing
            ctx.rb.linearVelocityX = _pushable.Velocity.x;
            
            ApplyGravity(dt, settings.BaseGravity);
        }
    }
}
