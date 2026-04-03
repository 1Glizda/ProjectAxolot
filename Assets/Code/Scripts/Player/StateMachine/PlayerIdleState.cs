namespace Player.StateMachine
{
    internal sealed class PlayerIdleState : PlayerBaseState
    {
        public PlayerIdleState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            TryFlipSprite();
            
            if (!isGrounded)
            {
                if (isInCoyoteTime && stateMachine.IsInJumpBuffer)
                {
                    stateMachine.ChangeState(new PlayerJumpState(ctx, stateMachine));
                    //TODO add physics ground force
                    return;
                }
                else if(!isInCoyoteTime)
                {
                    stateMachine.ChangeState(new PlayerFallingState(ctx, stateMachine));
                    return;    
                }
                
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState(new PlayerJumpState(ctx, stateMachine));
                //TODO add physics ground force
                return;
            }

           

            if (horizontalInput != 0f)
            { 
                stateMachine.ChangeState(new PlayerRunState(ctx, stateMachine));
            }
            
        }

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);
            
            if(horizontalInput == 0f) ApplyDecel(dt, settings.GroundedDeceleration);
            ApplyGravity(dt, settings.BaseGravity);

            
        }

    }
}
