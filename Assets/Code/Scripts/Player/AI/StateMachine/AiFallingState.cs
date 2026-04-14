using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.AI.StateMachine
{
    internal sealed class AiFallingState : AiBaseState
    {

        private float _catchBuffer;
        private bool _isCatchBuffered;
        
        private float _graceTimer;
        
        public AiFallingState(AiContext ctx, AiMovementStateMachine stateMachine) : base(ctx, stateMachine)
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
                stateMachine.ChangeState<AiIdleState>();
                return;
            }

            if (_isCatchBuffered && _catchBuffer > 0f && ctx.collisionHandler.CanSwing && _graceTimer <= 0)
            {
                stateMachine.ChangeState<AiSwingingState>();
                return;
            }

            if (ctx.controller.IsNearValidWall)
            {
                bool canGrab = false;
                if (stateMachine.WasDetached)
                {
                    canGrab = (_isCatchBuffered && _catchBuffer > 0f) || stateMachine.IsInJumpBuffer;
                }
                else
                {
                    canGrab = jumpAction.IsPressed() || stateMachine.IsInJumpBuffer;
                }

                if (canGrab)
                {
                    stateMachine.ConsumeJumpBuffer();
                    _isCatchBuffered = false;
                    stateMachine.ChangeState<AiClimbingState>();
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
            stateMachine.WasDetached = false;
        }
    }
}
