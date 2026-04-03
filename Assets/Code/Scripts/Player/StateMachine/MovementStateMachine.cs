using System;
using UnityEngine.InputSystem;

namespace Player.StateMachine
{
    //state machine that holds the movement states.
    //currently the states know of each other and the state machine, they can call ChangeState() from here
    internal sealed class MovementStateMachine
    {
        public Action<Type> onChangeState;
        public bool IsInJumpBuffer { get {return _jumpBuffer > 0f && _isJumpBuffered;}}

        public bool HasDetachedFromWall => _hasDetachedFromWall;
        

        private readonly PlayerSettingsSo _settings;
        private readonly PlayerContext _ctx;
        
        private PlayerBaseState _activeState;

        private bool _isJumpBuffered;
        private float _jumpBuffer;
        
        private bool _hasDetachedFromWall;
        
        public MovementStateMachine(PlayerSettingsSo settings, PlayerContext ctx)
        {
            _settings = settings;
            _ctx = ctx;
            _activeState = new PlayerIdleState(ctx, this);
            
            _ctx.manager.JumpAction.started += TryBufferJump;
            _jumpBuffer = _settings.JumpBufferTime;
        }

        ~MovementStateMachine()
        {
            _ctx.manager.JumpAction.started -= TryBufferJump;
        }
        
        public void Tick(float dt)
        {
            if(_isJumpBuffered) _jumpBuffer -= dt;
            if (_ctx.controller.IsGrounded)
            {
                _hasDetachedFromWall = false;
            }
            _activeState?.Tick(dt);
        }

        public void FixedTick(float dt)
        {
            _activeState?.FixedTick(dt);
        }
        
        public void ChangeState(PlayerBaseState newState)
        {
            if (_activeState is PlayerJumpState)
            {
                _isJumpBuffered = false;
                _jumpBuffer = _settings.JumpBufferTime;
            }
            
            _activeState.ExitState();
            _activeState = newState;
            _activeState.EnterState();
            
            onChangeState?.Invoke(_activeState.GetType());
        }


        public void DetachedFromWall()
        {
            _hasDetachedFromWall = true;
        }

        private void TryBufferJump(InputAction.CallbackContext context)
        {
            if (_ctx.controller.DistanceToGround <= _settings.JumpBufferMaxDistance && 
                _activeState is PlayerFallingState || _activeState is PlayerIdleState || _activeState is PlayerRunState)
            {
                _isJumpBuffered = true;
                _jumpBuffer = _settings.JumpBufferTime;
            }
        }
    }
}
