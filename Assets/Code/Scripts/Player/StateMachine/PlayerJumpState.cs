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
            _timer = 0f;
            ctx.rb.linearVelocityY = 0f;

            float lAngle = ( 90f + settings.JumpRunningMaxAngle) * Mathf.Deg2Rad;
            float rAngle = (90f - settings.JumpRunningMaxAngle) * Mathf.Deg2Rad;
            
            Vector2 dirL = new Vector2(Mathf.Cos(lAngle), Mathf.Sin(lAngle));
            Vector2 dirR = new Vector2(Mathf.Cos(rAngle), Mathf.Sin(rAngle));
            Vector2 dir = Vector3.Slerp(dirL, dirR, horizontalInput);
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

            if (ctx.controller.IsNearValidWall)
            {
                stateMachine.ChangeState<PlayerClimbingState>();
                return;
            }
            
        }

        public override void FixedTick(float dt)
        {
            ApplyAccel(dt, settings.JumpAcceleration, settings.JumpDeceleration, settings.MaxHorizontalVelocity);
            
            if(horizontalInput == 0f) ApplyDecel(dt, settings.JumpDeceleration);

            if (_timer <= settings.MinJumpTime || 
                _timer <= settings.MaxJumpTime && !jumpAction.IsPressed() && !_spaceReleased)
            {
                float accel = settings.JumpHoldAccel * dt * ctx.rb.mass;
                ctx.rb.AddForce(Vector2.up * accel, ForceMode2D.Impulse);
            }
            
            
            float t = Mathf.InverseLerp(settings.MinJumpTime, settings.MaxJumpTime, _timer);
            t = Mathf.Clamp01(t);
            t = settings.JumpGravityCurve.Evaluate(t);
            float g = Mathf.Lerp(settings.JumpInitialGravity, settings.FallingGravity, t);
            
            ApplyGravity(dt, g);
        }
        
        
    }
}
