namespace Player.StateMachine
{
    internal sealed class PlayerIdleState : PlayerBaseState
    {
        public PlayerIdleState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void EnterState()
        {
            stateMachine.LastVine = null;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            TryFlipSprite();
            
            if (!isGrounded)
            {
                if (isInCoyoteTime && stateMachine.IsInJumpBuffer)
                {
                    stateMachine.ChangeState<PlayerJumpState>();
                    //TODO add physics ground force
                    return;
                }
                else if(!isInCoyoteTime)
                {
                    stateMachine.ChangeState<PlayerFallingState>();
                    return;    
                }
                
            }
            else if (stateMachine.IsInJumpBuffer)
            {
                stateMachine.ChangeState<PlayerJumpState>();
                //TODO add physics ground force
                return;
            }
            if (ctx.stateProvider.IsNearValidWall && verticalInput > 0f)
            {
                stateMachine.ChangeState<PlayerClimbingState>();
                return;
            }

            if (ctx.stateProvider.IsFootNearPushable && settings.UseManualGrabForPushables && grabAction.IsPressed())
            {
                stateMachine.ChangeState<PlayerPushPullState>();
                return;
            }

            if (horizontalInput != 0f)
            { 
                stateMachine.ChangeState<PlayerRunState>();
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
