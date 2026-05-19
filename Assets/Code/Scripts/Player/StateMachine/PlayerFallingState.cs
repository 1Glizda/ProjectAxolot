using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.StateMachine
{
    internal sealed class PlayerFallingState : PlayerBaseState
    {

        private float _catchBuffer;
        private bool _isCatchBuffered;
        
        private float _graceTimer;

        protected override bool IsGroundedState => false;
        
        public PlayerFallingState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void EnterState()
        {
            jumpAction.performed += BufferCatch;
            _isCatchBuffered = false;
            _graceTimer = 0.35f; // Prevent immediate regrab and allow clearing the trigger for auto-grab
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

            bool autoGrabAllowed = ctx.collisionHandler.SwingBone == null || ctx.collisionHandler.SwingBone.VineHelper != stateMachine.LastVine;
            bool wantsToGrab = (settings.AutoGrabVines && autoGrabAllowed) || (_isCatchBuffered && _catchBuffer > 0f);
            
            bool isSameVine = ctx.collisionHandler.SwingBone != null && ctx.collisionHandler.SwingBone.VineHelper == stateMachine.LastVine;
            // Cooldown only applies to auto-grabbing the same vine.
            // Explicit player actions (manual catch buffer) bypass the cooldown for maximum responsiveness.
            bool grabCooldownActive = isSameVine && _graceTimer > 0f && !_isCatchBuffered;

            if (wantsToGrab && ctx.collisionHandler.CanSwing && !grabCooldownActive)
            {
                stateMachine.ChangeState<PlayerSwingingState>();
                return;
            }

            if (ctx.stateProvider.IsNearValidWall)
            {
                float dot = Vector2.Dot(new Vector2(ctx.rb.linearVelocityX, 0f), ctx.stateProvider.WallHitNormal);
                bool canGrab = dot <= 0.01f;

                if (canGrab)
                {
                    stateMachine.ConsumeJumpBuffer();
                    _isCatchBuffered = false;
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
            stateMachine.WasDetached = false;
        }
    }
}
