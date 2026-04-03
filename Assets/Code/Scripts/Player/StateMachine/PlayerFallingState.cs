using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.StateMachine
{
    internal sealed class PlayerFallingState : PlayerBaseState
    {

        private float _catchBuffer;
        private bool _isCatchBuffered;
        
        public PlayerFallingState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void EnterState()
        {
            jumpAction.performed += BufferCatch;
        }
        
        public override void Tick(float dt)
        {
            base.Tick(dt);
            TryFlipSprite();

            if (_isCatchBuffered)
            {
                _catchBuffer -= dt;
                if(_catchBuffer <= 0) _isCatchBuffered = false;
            }
            else
            {
                _catchBuffer = 0.5f;
            }
            
            if (isGrounded)
            {
                stateMachine.ChangeState(typeof(PlayerIdleState));
                return;
            }

            if (_isCatchBuffered && _catchBuffer > 0f && ctx.collisionHandler.CanSwing)
            {
                stateMachine.ChangeState(typeof(PlayerSwingingState));
                return;
            }

            if (ctx.controller.IsNearValidWall)
            {
                if (!stateMachine.HasDetachedFromWall)
                {
                    stateMachine.ChangeState(typeof(PlayerClimbingState));
                    return;
                }
                
                float dot = Vector2.Dot(new Vector2(horizontalInput, 0f), ctx.controller.WallHitNormal);
                if (dot < 0f)
                {
                    //moving towards wall
                    stateMachine.ChangeState(typeof(PlayerClimbingState));
                } 
            }

        }

        public override void FixedTick(float dt)
        {
            ApplyAccel(dt, settings.JumpAcceleration, settings.JumpDeceleration, settings.MaxHorizontalVelocity);
            ApplyGravity(dt, settings.FallingGravity);

        }

        private void BufferCatch(InputAction.CallbackContext context)
        {
            _isCatchBuffered = true;
            _catchBuffer = 0.5f;
        }
        
        public override void ExitState()
        {
            jumpAction.performed -= BufferCatch;
        }
    }
}
