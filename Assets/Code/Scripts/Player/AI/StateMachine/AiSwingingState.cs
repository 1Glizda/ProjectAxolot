using Player.Helpers;
using UnityEngine;

namespace Player.AI.StateMachine
{
    internal sealed class AiSwingingState : AiBaseState
    {
        private Rigidbody2D _currentBoneRb;
        private VineHelper _vine;
        private int _currentBoneIndex;
        private float _releaseTimer;
        
        private bool _queueJump;
        public AiSwingingState(AiContext ctx, AiMovementStateMachine stateMachine) : base(ctx, stateMachine)
        {}

        public override void EnterState()
        {
            _releaseTimer = 0f;
            SwingBone swingBone = ctx.collisionHandler.SwingBone;
            
            _currentBoneRb = swingBone.Rb;
            _vine = swingBone.VineHelper;
            _currentBoneIndex = _vine.GetBoneIndex(swingBone);
            
            ctx.swingHinge.connectedBody = _currentBoneRb;
            ctx.swingHinge.enabled = true;

            Vector2 entryV = ctx.rb.linearVelocity;
            _currentBoneRb.AddForce(entryV * (settings.SwingEntryMomentumTransfer * _currentBoneRb.mass), ForceMode2D.Impulse);
            
            ctx.rb.angularDamping = settings.SwingAngularDrag;
            
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (jumpAction.triggered)
            {
                ctx.swingHinge.enabled = false;
                ctx.swingHinge.connectedBody = null;

                Vector2 jumpDir = Vector2.up + (Vector2.right * horizontalInput);
                jumpDir.Normalize();
                ctx.rb.AddForce(jumpDir * (settings.VineJumpForce * ctx.rb.mass), ForceMode2D.Impulse);
                
                
                stateMachine.ChangeState<AiFallingState>();
                return;
            }

            if (verticalInput < -0.1f)
            {
                _releaseTimer += dt;
                if (_releaseTimer >= settings.VineReleaseHoldTime)
                {
                    ctx.swingHinge.enabled = false;
                    ctx.swingHinge.connectedBody = null;
                    stateMachine.ChangeState<AiFallingState>();
                    return;
                }
            }
            else
            {
                _releaseTimer = 0f;
            }
        }
        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);

            float boneAngle = _currentBoneRb.rotation + 90f;
            
            float angle = Mathf.LerpAngle(ctx.rb.rotation, boneAngle, dt * 10f);
            ctx.rb.MoveRotation(angle);
            
            _currentBoneRb.AddForce(Vector2.right * (horizontalInput * settings.SwingAcceleration * _currentBoneRb.mass), ForceMode2D.Force);
            
            
            HandleClimbing(dt);
        }
        

        
        private void HandleClimbing(float dt)
        {
            if(_currentBoneIndex < _vine.BoneCount - 1) SwitchBone(_currentBoneIndex + 1);
        }
        

        private void SwitchBone(int newIndex)
        {
            _currentBoneIndex = newIndex;
            _currentBoneRb = _vine.GetBoneByIndex(_currentBoneIndex).Rb;
            ctx.swingHinge.connectedBody = _currentBoneRb;
        }
        
        public override void ExitState()
        {
            ctx.swingHinge.enabled = false;
            ctx.swingHinge.connectedBody = null;
            
            ctx.rb.angularVelocity = 0f;
            ctx.rb.angularDamping = 0f;
            ctx.rb.MoveRotation(Quaternion.identity);
        }
        
    }
}
