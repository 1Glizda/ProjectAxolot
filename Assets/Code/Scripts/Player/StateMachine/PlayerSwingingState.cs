using Player.Helpers;
using UnityEngine;

namespace Player.StateMachine
{
    internal sealed class PlayerSwingingState : PlayerBaseState
    {
        private Rigidbody2D _currentBoneRb;
        private VineHelper _vine;
        private int _currentBoneIndex;
        
        private Vector2 _entryVelocity;
        private bool _isTranslating;
        private float _translationTimer;
        private Vector2 _translationStartPos;
        private float _translationStartAngle;
        
        public PlayerSwingingState(PlayerControllerContext ctx, MovementStateMachine stateMachine) : base(ctx, stateMachine)
        {}

        public override void EnterState()
        {
            ctx.stateProvider.NotifyGrabVine();
            SwingBone swingBone = ctx.collisionHandler.SwingBone;
            
            _currentBoneRb = swingBone.Rb;
            _vine = swingBone.VineHelper;
            _currentBoneIndex = _vine.GetBoneIndex(swingBone);
            
            // Cache the player's initial velocity at the moment of entry
            _entryVelocity = ctx.rb.linearVelocity;
            
            Vector2 entryV = ctx.rb.linearVelocity;
            _currentBoneRb.AddForce(entryV * (settings.SwingEntryMomentumTransfer * ctx.rb.mass), ForceMode2D.Impulse);

            // Force the connection anchor to be exactly on the bone's centerline (Vector2.zero)
            // so the player undergoes a beautiful horizontal slide from the trigger edge to the center of the vine
            ctx.swingHinge.autoConfigureConnectedAnchor = false;
            ctx.swingHinge.connectedAnchor = Vector2.zero;
            ctx.swingHinge.connectedBody = _currentBoneRb;
            ctx.swingHinge.enabled = false;

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

            if (jumpAction.triggered)
            {
                ctx.swingHinge.enabled = false;
                ctx.swingHinge.connectedBody = null;

                ctx.stateProvider.NotifyJump();

                // Preserve and amplify the swing momentum from the rope bone
                Vector2 swingVelocity = _currentBoneRb.linearVelocity;
                ctx.rb.linearVelocity = swingVelocity * settings.SwingMomentumMultiplier;

                // Apply the diagonal vine jump boost force on top!
                Vector2 jumpDir = Vector2.up + (Vector2.right * horizontalInput);
                jumpDir.Normalize();
                ctx.rb.AddForce(jumpDir * (settings.VineJumpForce * ctx.rb.mass), ForceMode2D.Impulse);
                
                stateMachine.ChangeState<PlayerFallingState>();
                return;
            }

            if (_isTranslating) return;
        }
        public override void FixedTick(float dt)
        {
            base.FixedTick(dt);

            if (_isTranslating)
            {
                _translationTimer += dt;
                float duration = settings.VineCatchTranslationDuration;
                float t = duration > 0f ? Mathf.Clamp01(_translationTimer / duration) : 1f;
                
                // Hermite spline basis functions for position (starts exactly at v0 speed, eases out to v1 speed)
                float t2 = t * t;
                float t3 = t2 * t;
                
                float h00 = 2f * t3 - 3f * t2 + 1f;
                float h10 = t3 - 2f * t2 + t;
                float h01 = -2f * t3 + 3f * t2;
                float h11 = t3 - t2;
                
                // Compute where the player's body needs to be so their anchor lines up with the bone's anchor
                Vector2 boneAnchorWorld = (Vector2)_currentBoneRb.transform.TransformPoint(ctx.swingHinge.connectedAnchor);
                float targetAngle = _currentBoneRb.rotation + 90f;
                
                // Use a quadratic ease-out for rotation (starts fast, slows down at the end)
                float easedRotationT = 2f * t - t2;
                float currentAngle = Mathf.LerpAngle(_translationStartAngle, targetAngle, easedRotationT);
                Vector2 playerAnchorLocal = Quaternion.Euler(0, 0, currentAngle) * ctx.swingHinge.anchor;
                Vector2 targetPos = boneAnchorWorld - playerAnchorLocal;
                
                // Hermite spline interpolation for smooth velocity-based momentum blend
                Vector2 p0 = _translationStartPos;
                Vector2 v0 = _entryVelocity;
                Vector2 p1 = targetPos;
                Vector2 v1 = _currentBoneRb.linearVelocity; // Match the bone's current velocity at the end
                
                Vector2 lerpedPos = h00 * p0 + h10 * (v0 * duration) + h01 * p1 + h11 * (v1 * duration);
                
                // Use MovePosition and MoveRotation for smooth dynamic interpolation
                ctx.rb.MovePosition(lerpedPos);
                ctx.rb.MoveRotation(currentAngle);
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
            stateMachine.lastVine = _vine;
            ctx.swingHinge.enabled = false;
            ctx.swingHinge.connectedBody = null;
            ctx.swingHinge.autoConfigureConnectedAnchor = true;
            
            ctx.rb.angularVelocity = 0f;
            ctx.rb.angularDamping = 0f;
            ctx.rb.MoveRotation(Quaternion.identity);
        }
        
    }
}
