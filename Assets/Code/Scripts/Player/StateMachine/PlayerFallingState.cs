using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.StateMachine
{
    internal sealed class PlayerFallingState : PlayerBaseState
    {

        private float _catchBuffer;
        private bool _isCatchBuffered;
        
        private float _graceTimer;
        
        public PlayerFallingState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void EnterState()
        {
            jumpAction.performed += BufferCatch;
            _isCatchBuffered = false;
            _graceTimer = 0.15f; // prevent immediate regrab
        }
        
        public override void Tick(float dt)
        {
            base.Tick(dt);
            TryFlipSprite();

            if (_graceTimer > 0)
            {
                _graceTimer -= dt;
            }

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
                stateMachine.ChangeState<PlayerIdleState>();
                return;
            }

            if (_isCatchBuffered && _catchBuffer > 0f && ctx.collisionHandler.CanSwing && _graceTimer <= 0)
            {
                stateMachine.ChangeState<PlayerSwingingState>();
                return;
            }

            if (ctx.controller.IsNearValidWall)
            {
                if (jumpAction.IsPressed() || stateMachine.IsInJumpBuffer)
                {
                    stateMachine.ConsumeJumpBuffer();
                    stateMachine.ChangeState<PlayerClimbingState>();
                    return;
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
