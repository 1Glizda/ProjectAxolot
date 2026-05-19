using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.StateMachine
{
    internal abstract class PlayerBaseState
    {
    
        protected readonly PlayerContext ctx;
        protected readonly MovementStateMachine stateMachine;
        protected readonly InputAction moveAction;
        protected readonly InputAction jumpAction;
        
        protected bool isGrounded;
        protected bool isInCoyoteTime;
        protected PlayerGroundData groundData;
        protected PlayerSettingsSo settings;
        protected float horizontalInput;
        protected float verticalInput;

        protected virtual bool IsGroundedState => isGrounded;

        
        protected PlayerBaseState(PlayerContext ctx, MovementStateMachine stateMachine)
        {
           this.ctx = ctx;
           this.stateMachine = stateMachine;
           
           settings = ctx.settings;
           moveAction = ctx.manager.MoveAction;
           jumpAction = ctx.manager.JumpAction;
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
                // Traditional horizontal-only movement in the air
                float currentV = ctx.rb.linearVelocityX;
                float targetV = horizontalInput * maxV;

                if (horizontalInput != 0f)
                {
                    bool isTurning = (horizontalInput > 0 && currentV < 0) || (horizontalInput < 0 && currentV > 0);
                    float speed = isTurning ? Mathf.Max(acceleration, deceleration) : acceleration;
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
            
            float newV = ctx.rb.linearVelocityY - (gravity * dt);
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

    }
}
