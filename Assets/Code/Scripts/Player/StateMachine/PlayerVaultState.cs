using UnityEngine;

namespace Player.StateMachine
{
    public class PlayerVaultState : PlayerBaseState
    {

        private Vector2 _p0;
        private Vector2 _p1;
        private Vector2 _p2;

        private float _timer;

        public PlayerVaultState(PlayerContext ctx, MovementStateMachine stateMachine, VaultHelper vaultHelper) : base(
        ctx, stateMachine)
        {
            _p1 = vaultHelper.VaultApex.position;
            _p2 = vaultHelper.VaultTarget.position;
        }

        public override void EnterState()
        {
            ctx.rb.bodyType = RigidbodyType2D.Kinematic;
            _p0 = ctx.rb.position;
            ctx.rb.linearVelocityY = 0f;

        }

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);
            _timer += dt;
            float t = _timer / settings.VaultDuration;

            Vector2 newPos = CalculatePoint(t, _p0, _p1, _p2);
            ctx.rb.MovePosition(newPos);

            if (t >= 1)
            {
                stateMachine.ChangeState(new PlayerFallingState(ctx, stateMachine));
            }
        }


        public override void ExitState()
        {
            ctx.rb.bodyType = RigidbodyType2D.Dynamic;
        }

        private Vector2 CalculatePoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            t = Mathf.Clamp01(t);
            Vector2 a = Vector2.Lerp(p0, p1, t);
            Vector2 b = Vector2.Lerp(p1, p2, t);
            return Vector2.Lerp(a, b, t);
        }
    }
}
