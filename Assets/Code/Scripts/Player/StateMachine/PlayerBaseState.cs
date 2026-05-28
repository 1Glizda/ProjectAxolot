using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.StateMachine
{
    internal abstract class PlayerBaseState
    {
    
        protected readonly PlayerControllerContext ctx;
        protected readonly MovementStateMachine stateMachine;
        protected readonly InputAction moveAction;
        protected readonly InputAction jumpAction;
        protected readonly InputAction grabAction;
        
        protected bool isGrounded;
        protected bool isInCoyoteTime;
        protected PlayerGroundData groundData;
        protected PlayerSettingsSo settings;
        protected float horizontalInput;
        protected float verticalInput;

        protected virtual bool IsGroundedState => isGrounded;

        
        protected PlayerBaseState(PlayerControllerContext ctx, MovementStateMachine stateMachine)
        {
           this.ctx = ctx;
           this.stateMachine = stateMachine;
           
           settings = ctx.settings;
           moveAction = ctx.handler.MoveAction;
           jumpAction = ctx.handler.JumpAction;
           grabAction = ctx.handler.GrabWallAction;
        }

        public virtual void EnterState(){}
        public virtual void Tick(float dt)
        {
            //ends up called in PlayerController.Update()
            isGrounded = ctx.stateProvider.IsGrounded;
            isInCoyoteTime = ctx.stateProvider.IsInCoyoteTime;
            groundData = ctx.stateProvider.GetGroundData();
            
            UpdateInput();
            
            RotateSlopedSprite();
            
        }
        public virtual void FixedTick(float dt)
        {
            //ends up called in PlayerController.FixedUpdate()
        }
        public virtual void ExitState(){}

        protected void UpdateInput()
        {
            if (!ctx.handler.IsInputActive)
            {
                horizontalInput = 0f;
                verticalInput = 0f;
                return;
            }

            Vector2 input = moveAction.ReadValue<Vector2>();
            horizontalInput = input.x;
            verticalInput = input.y;
        }
        
        
        //rotates the sprite to look better while on slopes
        private void RotateSlopedSprite()
        {
            float currentY = ctx.spriteObject.transform.localEulerAngles.y;
            if (!ctx.stateProvider.IsGrounded)
            {
                ApplySpriteRotation(new Vector3(0, currentY, 0));
                return;
            }
            Vector3 rotation = new (0, currentY, 0);
            Vector2 slopeTangent = groundData.slopeTangent;
            float angle = Vector2.Angle(slopeTangent, Vector2.right);
            
            if (angle < settings.ApplyRotationThreshold)
            {
                ApplySpriteRotation(new Vector3(0, currentY, 0));
                return;
            }
            
            angle = Mathf.Min(angle, settings.MaxRotation);
            
            if (slopeTangent.y < 0f)
            {
                angle *= -1;
            }
            
            if (Mathf.Abs(currentY) > 90f)
            {
                angle *= -1f;
            }
            
            rotation.z = angle;
            ApplySpriteRotation(rotation);
        }
        
        
        private void ApplySpriteRotation(Vector3 rotation)
        { 
            Quaternion targetRotation = 
                Quaternion.Lerp(ctx.spriteObject.transform.localRotation, 
                Quaternion.Euler(rotation), 
                15f * Time.deltaTime); //TODO replace the magic number
            
            ctx.spriteObject.transform.localRotation = targetRotation;
        }

        protected void ApplyAccel(float dt, float acceleration, float deceleration, float maxV)
        {
            if (IsGroundedState)
            {
                // Move perfectly along the slope's tangent
                Vector2 slopeTangent = groundData.slopeTangent;
                float targetSpeed = horizontalInput * maxV;
                Vector2 targetVelocity = targetSpeed * slopeTangent;

                if (horizontalInput != 0f)
                {
                    ctx.rb.linearVelocity = Vector2.MoveTowards(ctx.rb.linearVelocity, targetVelocity, acceleration * dt);
                }
                else
                {
                    ApplyDecel(dt, deceleration);
                }
            }
            else
            {
                if (ctx.stateProvider.IsOnSteepSlope)
                {
                    Vector2 normal = ctx.stateProvider.GroundNormal;
                    Vector2 slideDir = normal.x > 0 ? new Vector2(normal.y, -normal.x) : new Vector2(-normal.y, normal.x);
                    Vector2 targetVelocity = slideDir * settings.SlopeSlideSpeed;
                    
                    ctx.rb.linearVelocity = Vector2.MoveTowards(ctx.rb.linearVelocity, targetVelocity, settings.SlopeSlideAccel * dt);
                    return;
                }

                // Traditional horizontal-only movement in the air
                float currentV = ctx.rb.linearVelocityX;
                float targetV = horizontalInput * maxV;

                if (horizontalInput != 0f)
                {
                    bool isTurning = (horizontalInput > 0 && currentV < 0) || (horizontalInput < 0 && currentV > 0);
                    float speed = isTurning ? Mathf.Max(acceleration, deceleration) : acceleration;
                    
                    // Boost horizontal control at the jump apex
                    if (Mathf.Abs(ctx.rb.linearVelocityY) < settings.JumpApexThreshold)
                    {
                        speed *= settings.JumpApexHorizontalAccelMultiplier;
                    }
                    
                    ctx.rb.linearVelocityX = Mathf.MoveTowards(currentV, targetV, speed * dt);
                }
                else
                {
                    ApplyDecel(dt, deceleration);
                }
            }
        }

        protected void ApplyDecel(float dt, float deceleration)
        {
            if (IsGroundedState)
            {
                // Decelerate entire velocity vector along the slope to prevent any sliding
                ctx.rb.linearVelocity = Vector2.MoveTowards(ctx.rb.linearVelocity, Vector2.zero, deceleration * dt);
                return;
            }

            float currentV = ctx.rb.linearVelocityX;
            if (currentV == 0f) return;
            
            if (Mathf.Abs(currentV) < settings.StandstillThreshold)
            {
                ctx.rb.linearVelocityX = 0f;
                return;
            }

            ctx.rb.linearVelocityX = Mathf.MoveTowards(currentV, 0f, deceleration * dt);
        }

        protected void ApplyGravity(float dt, float gravity)
        {
            if (IsGroundedState)
            {
                // Return early without applying gravity forces when grounded to prevent sliding
                return;
            }

            if (ctx.rb.linearVelocityY <= settings.TerminalVerticalVelocity) return;
            
            float gravityMultiplier = 1f;
            if (Mathf.Abs(ctx.rb.linearVelocityY) < settings.JumpApexThreshold)
            {
                gravityMultiplier = settings.JumpApexGravityMultiplier;
            }
            
            float newV = ctx.rb.linearVelocityY - (gravity * gravityMultiplier * dt);
            ctx.rb.linearVelocityY = Mathf.Max(newV, settings.TerminalVerticalVelocity);
        }

        protected void TryFlipSprite(GameObject objToFlip = null)
        {
            if (objToFlip == null) objToFlip = ctx.spriteObject;

            if (horizontalInput > 0f)
            {
                Vector3 euler = objToFlip.transform.localEulerAngles;
                euler.y = 0f;
                objToFlip.transform.localEulerAngles = euler;
            }
             else if (horizontalInput < 0f)
            {
                Vector3 euler = objToFlip.transform.localEulerAngles;
                euler.y = 180f;
                objToFlip.transform.localEulerAngles = euler;
            }
        }

        protected void ApplyCornerCorrection(float dt)
        {
            // Only apply when rising
            if (ctx.rb.linearVelocityY <= 0.05f) return;

            Bounds bounds = ctx.bodyCollider.bounds;
            Vector2 rayOriginY = new Vector2(0f, bounds.max.y - 0.05f);
            
            // Left and Right ray origins
            Vector2 leftOrigin = new Vector2(bounds.min.x + 0.02f, rayOriginY.y);
            Vector2 rightOrigin = new Vector2(bounds.max.x - 0.02f, rayOriginY.y);

            float rayDist = settings.CornerCorrectionDistance;
            LayerMask mask = settings.GroundLayers;

            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.up, rayDist, mask);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.up, rayDist, mask);

            // If only one side hits, perform the horizontal nudge!
            if (leftHit.collider != null && rightHit.collider == null)
            {
                // Left is blocked, nudge to the right
                ctx.rb.position += new Vector2(settings.CornerCorrectionAmount * dt, 0f);
            }
            else if (rightHit.collider != null && leftHit.collider == null)
            {
                // Right is blocked, nudge to the left
                ctx.rb.position -= new Vector2(settings.CornerCorrectionAmount * dt, 0f);
            }
        }

    }
}
