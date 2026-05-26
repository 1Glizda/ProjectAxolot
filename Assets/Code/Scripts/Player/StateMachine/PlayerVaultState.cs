using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerVaultState : PlayerBaseState
    {

        private Vector2 _p0;
        private Vector2 _p1;
        private Vector2 _p2;

        private float _timer;
        private bool _jumpTriggered;

        internal PlayerVaultState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        { }

        public override void EnterState()
        {
            _timer = 0f;
            _jumpTriggered = false;
            
            _p2 = ctx.stateProvider.VaultTarget;
            _p1 = new Vector2(ctx.rb.position.x, _p2.y);
            
            ctx.rb.bodyType = RigidbodyType2D.Kinematic;
            _p0 = ctx.rb.position;
            ctx.rb.linearVelocity = Vector2.zero;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (!_jumpTriggered)
            {
                _jumpTriggered = jumpAction.triggered;
            }
        }

        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);

            if (_jumpTriggered)
            {
                // Transitioning will call ExitState and make the Rigidbody dynamic/re-enable colliders
                stateMachine.WasDetached = true;
                stateMachine.ChangeState<PlayerFallingState>();

                // Determine wall normal from vault direction
                Vector2 wallNormal = (_p2.x > _p0.x) ? Vector2.left : Vector2.right;

                // Flip sprite to face away from the wall
                if (wallNormal.x > 0f)
                {
                    Vector3 euler = ctx.spriteObject.transform.localEulerAngles;
                    euler.y = 0f;
                    ctx.spriteObject.transform.localEulerAngles = euler;
                }
                else
                {
                    Vector3 euler = ctx.spriteObject.transform.localEulerAngles;
                    euler.y = 180f;
                    ctx.spriteObject.transform.localEulerAngles = euler;
                }

                ctx.stateProvider.NotifyJump();
                ctx.rb.linearVelocity = Vector2.zero;
                ctx.rb.AddForce(GetAngledVector(wallNormal) * settings.WallJumpForce, ForceMode2D.Impulse);
                return;
            }

            _timer += dt;
            float t = _timer / settings.VaultDuration;

            Vector2 newPos = CalculatePoint(t, _p0, _p1, _p2);
            ctx.rb.MovePosition(newPos);

            if (t >= 1)
            {
                stateMachine.ChangeState<PlayerFallingState>();
            }
        }

        public override void ExitState()
        {
            ctx.rb.bodyType = RigidbodyType2D.Dynamic;
        }

        private Vector2 GetAngledVector(Vector2 wallNormal)
        {
            float rotationAngle = wallNormal.x > 0 ? settings.WallJumpAngle : -settings.WallJumpAngle;
            Quaternion rotation = Quaternion.Euler(0f, 0f, rotationAngle);
            return rotation * wallNormal;
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
