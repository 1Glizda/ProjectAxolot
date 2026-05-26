using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.StateMachine
{
    internal sealed class PlayerFallingState : PlayerBaseState
    {

        private float _catchBuffer;
        private bool _isCatchBuffered;
        
        private float _graceTimer;
        private float _wallJumpLockoutTimer;
        private float _wallCoyoteTimer;

        protected override bool IsGroundedState => false;
        
        public PlayerFallingState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void EnterState()
        {
            jumpAction.performed += BufferCatch;
            _isCatchBuffered = false;
            _graceTimer = 0.35f; // Prevent immediate regrab and allow clearing the trigger for auto-grab
            _wallJumpLockoutTimer = stateMachine.wasDetached ? 0.15f : 0f;

            if (stateMachine.PreviousStateType == typeof(PlayerClimbingState))
            {
                _wallCoyoteTimer = settings.WallCoyoteTime;
            }
            else
            {
                _wallCoyoteTimer = 0f;
            }
        }
        
        public override void Tick(float dt)
        {
            base.Tick(dt);
            TryFlipSprite();

            // Allow jumping if we have coyote time (e.g., just walked off a ledge) 
            // OR if we are actively sliding down a steep slope
            if ((isInCoyoteTime || ctx.stateProvider.IsOnSteepSlope) && stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState<PlayerJumpState>();
                return;
            }

            if (_graceTimer > 0)
            {
                _graceTimer -= dt;
            }

            if (_wallJumpLockoutTimer > 0f)
            {
                _wallJumpLockoutTimer -= dt;
            }

            if (_wallCoyoteTimer > 0f)
            {
                _wallCoyoteTimer -= dt;
                
                if (jumpAction.triggered)
                {
                    Vector2 wallNormal = stateMachine.lastWallNormal;
                    if (wallNormal.x > 0f)
                    {
                        Vector3 euler = ctx.spriteObject.transform.localEulerAngles;
                        euler.y = 0f;
                        ctx.spriteObject.transform.localEulerAngles = euler;
                    }
                    else if (wallNormal.x < 0f)
                    {
                        Vector3 euler = ctx.spriteObject.transform.localEulerAngles;
                        euler.y = 180f;
                        ctx.spriteObject.transform.localEulerAngles = euler;
                    }

                    ctx.stateProvider.NotifyJump();
                    ctx.rb.linearVelocity = Vector2.zero;

                    float rotationAngle = wallNormal.x > 0 ? settings.WallJumpAngle : -settings.WallJumpAngle;
                    Quaternion rotation = Quaternion.Euler(0f, 0f, rotationAngle);
                    Vector2 forceVec = (rotation * wallNormal) * settings.WallJumpForce;

                    ctx.rb.AddForce(forceVec, ForceMode2D.Impulse);
                    stateMachine.wasDetached = true;
                    _wallJumpLockoutTimer = 0.15f;
                    _wallCoyoteTimer = 0f;
                    return;
                }
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

            bool autoGrabAllowed = ctx.collisionHandler.SwingBone == null || ctx.collisionHandler.SwingBone.VineHelper != stateMachine.lastVine;
            bool wantsToGrab = (settings.AutoGrabVines && autoGrabAllowed) || (_isCatchBuffered && _catchBuffer > 0f);
            
            bool isSameVine = ctx.collisionHandler.SwingBone != null && ctx.collisionHandler.SwingBone.VineHelper == stateMachine.lastVine;
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
                if (!stateMachine.wasDetached)
                {
                    float dot = Vector2.Dot(new Vector2(ctx.rb.linearVelocityX, 0f), ctx.stateProvider.WallHitNormal);
                    bool canGrab = dot <= 0.01f;

                    if (canGrab)
                    {
                        if (stateMachine.IsInJumpBuffer)
                        {
                            stateMachine.ConsumeJumpBuffer();
                            
                            Vector2 wallNormal = ctx.stateProvider.WallHitNormal;
                            if (wallNormal.x > 0f)
                            {
                                Vector3 euler = ctx.spriteObject.transform.localEulerAngles;
                                euler.y = 0f;
                                ctx.spriteObject.transform.localEulerAngles = euler;
                            }
                            else if (wallNormal.x < 0f)
                            {
                                Vector3 euler = ctx.spriteObject.transform.localEulerAngles;
                                euler.y = 180f;
                                ctx.spriteObject.transform.localEulerAngles = euler;
                            }

                            ctx.stateProvider.NotifyJump();
                            ctx.rb.linearVelocity = Vector2.zero;

                            float rotationAngle = wallNormal.x > 0 ? settings.WallJumpAngle : -settings.WallJumpAngle;
                            Quaternion rotation = Quaternion.Euler(0f, 0f, rotationAngle);
                            Vector2 forceVec = (rotation * wallNormal) * settings.WallJumpForce;

                            ctx.rb.AddForce(forceVec, ForceMode2D.Impulse);
                            stateMachine.wasDetached = true;
                            _wallJumpLockoutTimer = 0.15f;
                            return;
                        }

                        stateMachine.ConsumeJumpBuffer();
                        _isCatchBuffered = false;
                        stateMachine.ChangeState<PlayerClimbingState>();
                        return;
                    }
                }
            }
            else
            {
                // Player cleared the wall — allow grabbing a new wall
                stateMachine.wasDetached = false;
            }

        }

        public override void FixedTick(float dt)
        {
            if (_wallJumpLockoutTimer <= 0f)
            {
                ApplyAccel(dt, settings.JumpAcceleration, settings.JumpDeceleration, settings.MaxHorizontalVelocity);
            }
            if (!ctx.stateProvider.IsOnSteepSlope)
            {
                ApplyGravity(dt, settings.FallingGravity);
            }
            ApplyCornerCorrection(dt);
        }

        private void BufferCatch(InputAction.CallbackContext context)
        {
            if (Time.timeScale == 0f) return;
            _isCatchBuffered = true;
            _catchBuffer = 0.5f;
        }
        
        public override void ExitState()
        {
            jumpAction.performed -= BufferCatch;
            stateMachine.wasDetached = false;
        }
    }
}
