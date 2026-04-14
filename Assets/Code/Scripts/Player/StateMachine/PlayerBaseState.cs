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
        protected readonly InputAction interactAction;
        
        protected bool isGrounded;
        protected bool isInCoyoteTime;
        protected PlayerGroundData groundData;
        protected PlayerSettingsSo settings;
        protected float horizontalInput;
        protected float verticalInput;

        
        protected PlayerBaseState(PlayerContext ctx, MovementStateMachine stateMachine)
        {
           this.ctx = ctx;
           this.stateMachine = stateMachine;
           
           settings = ctx.settings;
           moveAction = ctx.manager.MoveAction;
           jumpAction = ctx.manager.JumpAction;
           interactAction = ctx.manager.InteractAction;
        }

        public virtual void EnterState(){}
        public virtual void Tick(float dt)
        {
            //ends up called in PlayerController.Update()
            isGrounded = ctx.controller.IsGrounded;
            isInCoyoteTime = ctx.controller.IsInCoyoteTime;
            groundData = ctx.controller.GetGroundData();
            
            Vector2 input = moveAction.ReadValue<Vector2>();
            horizontalInput = input.x;
            verticalInput = input.y;
            
            RotateSlopedSprite();
            
        }
        public virtual void FixedTick(float dt)
        {
            //ends up called in PlayerController.FixedUpdate()
        }
        public virtual void ExitState(){}
        
        
        //rotates the sprite to look better while on slopes
        private void RotateSlopedSprite()
        {
            if (!ctx.controller.IsGrounded)
            {
                ApplySpriteRotation(Vector3.zero);
                return;
            }
            Vector3 rotation = new (0, 0, 0);
            Vector2 slopeTangent = groundData.slopeTangent;
            float angle = Vector2.Angle(slopeTangent, Vector2.right);
            
            if (angle < settings.ApplyRotationThreshold)
            {
                ApplySpriteRotation(Vector3.zero);
                return;
            }
            
            angle = Mathf.Min(angle, settings.MaxRotation);
            
            if (slopeTangent.y < 0f)
            {
                angle *= -1;
            }
            
            rotation.z = angle;
            ApplySpriteRotation(rotation);
        }
        
        
        private void ApplySpriteRotation(Vector3 rotation)
        { 
            Quaternion targetRotation = 
                Quaternion.Lerp(ctx.spriteRenderer.transform.localRotation, 
                Quaternion.Euler(rotation), 
                15f * Time.deltaTime); //TODO replace the magic number
            
            ctx.spriteRenderer.transform.localRotation = targetRotation;
        }

        protected void ApplyAccel(float dt, float acceleration, float deceleration, float maxV)
        {
            float currentV = ctx.rb.linearVelocityX;
            
            if (horizontalInput != 0f)
            {
                float deltaV = horizontalInput * acceleration * dt;

                if (deltaV > 0f)
                {
                    if (currentV < 0f)
                    {
                        ApplyDecel(dt, deceleration);
                    }
                    else
                    {
                        float headroom = Mathf.Max(0f, maxV - ctx.rb.linearVelocityX);
                        deltaV = Mathf.Min(deltaV, headroom);    
                    }
                    
                }
                else if (deltaV < 0f)
                {
                    if (currentV > 0f)
                    {
                        ApplyDecel(dt, deceleration);
                    }
                    else
                    {
                        float headroom = Mathf.Max(0f, ctx.rb.linearVelocityX - (-maxV));
                        deltaV = Mathf.Max(deltaV, -headroom);
                    }
                    
                }
                

                Vector2 force = deltaV * ctx.rb.mass * Vector2.right;
                ctx.rb.AddForce(force, ForceMode2D.Impulse);
                return;
            }
            
            if (Mathf.Abs(ctx.rb.linearVelocityX) < settings.StandstillThreshold)
            {
                ctx.rb.linearVelocityX = 0f;
                return;
            }
        }

        protected void ApplyDecel(float dt, float deceleration)
        {
            float currentV = ctx.rb.linearVelocityX;
            if (currentV == 0f)
            {
                return;
            }
            
            float absDeltaV =  deceleration * dt;
            float deltaV = 0f;
            
            if (currentV > 0f)
            {
                absDeltaV = Mathf.Min(absDeltaV, currentV);
                deltaV = -absDeltaV;
            }
            else if (currentV < 0f)
            {
                absDeltaV = Mathf.Min(absDeltaV, Mathf.Abs(currentV));
                deltaV = absDeltaV;
            }
            Vector2 force = deltaV * ctx.rb.mass * Vector2.right;
            ctx.rb.AddForce(force, ForceMode2D.Impulse);
        }

        protected void ApplyGravity(float dt, float gravity)
        {
            float currentV = ctx.rb.linearVelocityY;
            if (currentV <= settings.TerminalVerticalVelocity) return;
            
            float g = -gravity * dt * ctx.rb.mass;
            ctx.rb.AddForce(Vector2.up * g, ForceMode2D.Impulse);
           
        }

        protected void TryFlipSprite()
        {
            if (horizontalInput > 0f)
            {
                ctx.spriteRenderer.flipX = false;
            }
            else if (horizontalInput < 0f)
            {
                ctx.spriteRenderer.flipX = true;
            }
        }

    }
}
