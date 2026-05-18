using Player.Helpers;
using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerSwingingState : PlayerBaseState
    {
        private Rigidbody2D _currentBoneRb;
        private VineHelper _vine;
        private int _currentBoneIndex;
        private float _releaseTimer;
        
        private bool _isTranslating;
        private float _translationTimer;
        private Vector2 _translationStartPos;
        private float _translationStartAngle;
        
        private bool _queueJump;
        public PlayerSwingingState(PlayerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {}

        public override void EnterState()
        {
            _releaseTimer = 0f;
            SwingBone swingBone = ctx.collisionHandler.SwingBone;
            
            _currentBoneRb = swingBone.Rb;
            _vine = swingBone.VineHelper;
            _currentBoneIndex = _vine.GetBoneIndex(swingBone);
            
            Vector2 entryV = ctx.rb.linearVelocity;
            _currentBoneRb.AddForce(entryV * (settings.SwingEntryMomentumTransfer * _currentBoneRb.mass), ForceMode2D.Impulse);

            _isTranslating = true;
            _translationTimer = 0f;
            _translationStartPos = ctx.rb.position;
            _translationStartAngle = ctx.rb.rotation;
            
            ctx.rb.linearVelocity = Vector2.zero;
            ctx.rb.angularVelocity = 0f;
            ctx.rb.angularDamping = settings.SwingAngularDrag;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_isTranslating) return;

            if (jumpAction.triggered)
            {
                ctx.swingHinge.enabled = false;
                ctx.swingHinge.connectedBody = null;

                ctx.stateProvider.NotifyJump();

                Vector2 jumpDir = Vector2.up + (Vector2.right * horizontalInput);
                jumpDir.Normalize();
                ctx.rb.AddForce(jumpDir * (settings.VineJumpForce * ctx.rb.mass), ForceMode2D.Impulse);
                
                
                stateMachine.ChangeState<PlayerFallingState>();
                return;
            }

            if (verticalInput < -0.1f)
            {
                _releaseTimer += dt;
                if (_releaseTimer >= settings.VineReleaseHoldTime)
                {
                    ctx.swingHinge.enabled = false;
                    ctx.swingHinge.connectedBody = null;
                    stateMachine.ChangeState<PlayerFallingState>();
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

            if (_isTranslating)
            {
                _translationTimer += dt;
                float duration = settings.VineCatchTranslationDuration;
                float t = duration > 0f ? Mathf.Clamp01(_translationTimer / duration) : 1f;
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                
                // Compute where the player's body needs to be so their anchor lines up with the bone's anchor
                Vector2 boneAnchorWorld = (Vector2)_currentBoneRb.transform.TransformPoint(ctx.swingHinge.connectedAnchor);
                float targetAngle = _currentBoneRb.rotation + 90f;
                float currentAngle = Mathf.LerpAngle(_translationStartAngle, targetAngle, easedT);
                Vector2 playerAnchorLocal = Quaternion.Euler(0, 0, currentAngle) * ctx.swingHinge.anchor;
                Vector2 targetPos = boneAnchorWorld - playerAnchorLocal;
                
                // Lerp from cached start to moving target
                ctx.rb.position = Vector2.Lerp(_translationStartPos, targetPos, easedT);
                ctx.rb.rotation = currentAngle;
                ctx.rb.linearVelocity = Vector2.zero;
                ctx.rb.angularVelocity = 0f;

                if (t >= 1f)
                {
                    _isTranslating = false;
                    // Snap precisely before enabling the joint
                    ctx.rb.position = targetPos;
                    ctx.rb.rotation = targetAngle;
                    ctx.rb.linearVelocity = _currentBoneRb.linearVelocity;
                    ctx.rb.angularVelocity = _currentBoneRb.angularVelocity;
                    ctx.swingHinge.connectedBody = _currentBoneRb;
                    ctx.swingHinge.enabled = true;
                }
                return;
            }

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
            stateMachine.LastVine = _vine;
            ctx.swingHinge.enabled = false;
            ctx.swingHinge.connectedBody = null;
            
            ctx.rb.angularVelocity = 0f;
            ctx.rb.angularDamping = 0f;
            ctx.rb.MoveRotation(Quaternion.identity);
        }
        
    }
}
