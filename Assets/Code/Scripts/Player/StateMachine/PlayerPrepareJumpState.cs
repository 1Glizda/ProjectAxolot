using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerPrepareJumpState : PlayerBaseState
    {
        private bool _jumpTriggered;
        public PlayerPrepareJumpState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
             
        }

        public override void EnterState()
        {
            base.EnterState();
            ctx.rb.linearVelocityY = 0f;
           
        }
        public override void Tick(float dt)
        {
            base.Tick(dt);
            

            if (horizontalInput == 0f)
            {
                stateMachine.ChangeState(typeof(PlayerClimbingState));
                return;
            }
            
            
            if(!_jumpTriggered) _jumpTriggered = jumpAction.triggered;

            
        }

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);

            float dot = Vector2.Dot(new Vector2(horizontalInput, 0f), ctx.controller.WallHitNormal);
            if (Mathf.Approximately(dot, 1f) && _jumpTriggered)
            { 
                TryFlipSprite();
                ctx.rb.AddForce(GetAngledVector() * settings.WallJumpForce, ForceMode2D.Impulse);
                stateMachine.ChangeState(typeof(PlayerFallingState));
            }
            
        }


        private Vector2 GetAngledVector()
        {
            Vector2 wallNormal = ctx.controller.WallHitNormal;
            float rotationAngle = wallNormal.x > 0 ? settings.WallJumpAngle : -settings.WallJumpAngle;
            
            Quaternion rotation = Quaternion.Euler(0f, 0f, rotationAngle);

            return rotation * wallNormal;
        }
    }
}
