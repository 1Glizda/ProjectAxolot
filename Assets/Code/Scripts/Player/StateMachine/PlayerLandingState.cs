namespace Player.StateMachine
{
    public class PlayerLandingState : PlayerBaseState
    {

        public PlayerLandingState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            
            
            if (horizontalInput != 0f)
            {
                //TODO accelerate
            }
            else
            {
                //TODO decelerate
            }
            
        }
    }
}
