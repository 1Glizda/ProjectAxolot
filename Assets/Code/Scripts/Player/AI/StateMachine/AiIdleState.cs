namespace Player.AI.StateMachine
{
    internal sealed class AiIdleState : AiBaseState
    {
        public AiIdleState(AiContext ctx, AiMovementStateMachine stateMachine) : base(ctx, stateMachine)
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
                    stateMachine.ChangeState<AiJumpState>();
                    //TODO add physics ground force
                    return;
                }
                else if(!isInCoyoteTime)
                {
                    stateMachine.ChangeState<AiFallingState>();
                    return;    
                }
                
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState<AiJumpState>();
                //TODO add physics ground force
                return;
            }
            if (horizontalInput != 0f)
            { 
                stateMachine.ChangeState<AiRunState>();
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
