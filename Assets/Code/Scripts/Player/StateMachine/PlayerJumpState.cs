using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerJumpState : PlayerBaseState
    {
        private float _timer;
        private bool _spaceReleased;

        protected override bool IsGroundedState => false;
        
        public PlayerJumpState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }
        
        public override void EnterState()
        {
            UpdateInput();
            _timer = 0f;
            _spaceReleased = false;
            ctx.rb.linearVelocityY = 0f;
            
            ctx.stateProvider.NotifyJump();

            Vector2 dir = Vector2.up;
            float slopeAngle = Vector2.Angle(ctx.stateProvider.GroundNormal, Vector2.up);
            
            if (slopeAngle > settings.MaxSlopeAngle)
            {
                dir = ctx.stateProvider.GroundNormal;
            }
            else if (settings.UseDiagonalJump && !ctx.stateProvider.IsNearValidWall)
            {
                float lAngle = ( 90f + settings.JumpRunningMaxAngle) * Mathf.Deg2Rad;
                float rAngle = (90f - settings.JumpRunningMaxAngle) * Mathf.Deg2Rad;
                
                Vector2 dirL = new Vector2(Mathf.Cos(lAngle), Mathf.Sin(lAngle));
                Vector2 dirR = new Vector2(Mathf.Cos(rAngle), Mathf.Sin(rAngle));
                
                float t = (horizontalInput + 1f) / 2f;
                dir = Vector3.Slerp(dirL, dirR, t);
            }

            Vector2 initialForce = settings.InitialJumpForce * ctx.rb.mass * dir;
            ctx.rb.AddForce(initialForce, ForceMode2D.Impulse);  
            //TODO add physics ground force
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            TryFlipSprite();

            if (!jumpAction.IsPressed() && !_spaceReleased)
            {
                _spaceReleased = true;
                if (ctx.rb.linearVelocityY > 0f)
                {
                    ctx.rb.linearVelocityY *= settings.JumpCutMultiplier;
                }
            }

            bool autoGrabAllowed = ctx.collisionHandler.SwingBone == null || ctx.collisionHandler.SwingBone.VineHelper != stateMachine.LastVine;
            bool wantsToGrab = (settings.AutoGrabVines && autoGrabAllowed) || jumpAction.triggered;
            if (wantsToGrab && ctx.collisionHandler.CanSwing)
            {
                stateMachine.ChangeState<PlayerSwingingState>();
                return;
            }
            
            if (ctx.rb.linearVelocityY <= 0f)
            {
                stateMachine.ChangeState<PlayerFallingState>();
                return;
            }

            if (ctx.stateProvider.IsNearValidWall)
            {
                float dot = Vector2.Dot(new Vector2(ctx.rb.linearVelocityX, 0f), ctx.stateProvider.WallHitNormal);
                if (dot <= 0.01f)
                {
                    stateMachine.ChangeState<PlayerClimbingState>();
                    return;
                }
            }
            
        }

        public override void FixedTick(float dt)
        {
            _timer += dt;

            ApplyAccel(dt, settings.JumpAcceleration, settings.JumpDeceleration, settings.MaxHorizontalVelocity);
            
            
            float t = Mathf.InverseLerp(settings.MinJumpTime, settings.MaxJumpTime, _timer);
            t = Mathf.Clamp01(t);
            t = settings.JumpGravityCurve.Evaluate(t);
            float g = Mathf.Lerp(settings.JumpInitialGravity, settings.FallingGravity, t);
            
            ApplyGravity(dt, g);
            ApplyCornerCorrection(dt);
        }
        
        
    }
}
