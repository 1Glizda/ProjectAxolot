using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerJumpState : PlayerBaseState
    {
        private float _timer;
        private bool _spaceReleased;
        
        public PlayerJumpState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
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
            
            if (!ctx.stateProvider.IsNearValidWall)
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

            if(!jumpAction.IsPressed()) _spaceReleased = true;
            _timer += dt;

            if (jumpAction.triggered && ctx.collisionHandler.CanSwing)
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
                stateMachine.ChangeState<PlayerClimbingState>();
                return;
            }
            
        }

        public override void FixedTick(float dt)
        {
            ApplyAccel(dt, settings.JumpAcceleration, settings.JumpDeceleration, settings.MaxHorizontalVelocity);

            if (_timer <= settings.MinJumpTime || 
                (_timer <= settings.MaxJumpTime && jumpAction.IsPressed() && !_spaceReleased))
            {
                float force = settings.JumpHoldAccel * ctx.rb.mass;
                ctx.rb.AddForce(Vector2.up * force, ForceMode2D.Force);
            }
            
            
            float t = Mathf.InverseLerp(settings.MinJumpTime, settings.MaxJumpTime, _timer);
            t = Mathf.Clamp01(t);
            t = settings.JumpGravityCurve.Evaluate(t);
            float g = Mathf.Lerp(settings.JumpInitialGravity, settings.FallingGravity, t);
            
            ApplyGravity(dt, g);
        }
        
        
    }
}
