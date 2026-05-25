
using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerRunState : PlayerBaseState
    {
        private float _pushIntentTimer;

        public PlayerRunState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void EnterState()
        {
            base.EnterState();
            _pushIntentTimer = 0f;
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
                if (settings.UseManualGrabForPushables)
                {
                    if (grabAction.IsPressed())
                    {
                        stateMachine.ChangeState<PlayerPushPullState>();
                        return;
                    }
                }
                else
                {
                    if (horizontalInput != 0f)
                    {
                        _pushIntentTimer += dt;
                        if (_pushIntentTimer >= settings.PushIntentDelay)
                        {
                            stateMachine.ChangeState<PlayerPushPullState>();
                            return;
                        }
                    }
                    else
                    {
                        _pushIntentTimer = 0f;
                    }
                }
            }
            else
            {
                _pushIntentTimer = 0f;
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
