using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerKnockbackState : PlayerBaseState
    {
        public PlayerKnockbackState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {
        }

        public override void EnterState()
        {
            ctx.rb.linearVelocity = ctx.PendingKnockbackVelocity;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (isGrounded)
            {
                stateMachine.ChangeState<PlayerIdleState>();
                return;
            }
        }

        public override void FixedTick(float dt)
        {
            // Heavily reduced air control — uses knockback-specific acceleration
            ApplyAccel(dt, settings.KnockbackAirAcceleration, settings.JumpDeceleration, settings.MaxHorizontalVelocity);
            ApplyGravity(dt, settings.FallingGravity);
        }
    }
}
