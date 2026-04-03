using Player.Helpers;
using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerSwingingState : PlayerBaseState
    {
        private Rigidbody2D _currentBoneRb;
        private VineHelper _vine;
        private int _currentBoneIndex;

        private bool _queueJump;
        public PlayerSwingingState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {}

        public override void EnterState()
        {
            SwingBone swingBone = ctx.collisionHandler.SwingBone;
            
            _currentBoneRb = swingBone.Rb;
            _vine = swingBone.VineHelper;
            _currentBoneIndex = _vine.GetBoneIndex(swingBone);
            
            Vector2 entryV = ctx.rb.linearVelocity;
            ctx.rb.position = _currentBoneRb.position - (Vector2)ctx.rb.transform.TransformDirection(ctx.swingHinge.anchor);
            
            ctx.swingHinge.connectedBody = _currentBoneRb;
            ctx.swingHinge.enabled = true;
            
            _currentBoneRb.AddForce(entryV * (settings.SwingEntryMomentumTransfer * _currentBoneRb.mass), ForceMode2D.Impulse);
            
            ctx.rb.angularDamping = settings.SwingAngularDrag;
            
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (jumpAction.triggered)
            {
                stateMachine.ChangeState(typeof(PlayerFallingState));
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
