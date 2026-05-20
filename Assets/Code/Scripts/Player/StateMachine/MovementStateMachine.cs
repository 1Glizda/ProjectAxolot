using Player.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.StateMachine
{
    //state machine that holds the movement states.
    //currently the states know of each other and the state machine, they can call ChangeState() from here
    internal sealed class MovementStateMachine
    {
        public Action<Type> onChangeState;
        public bool IsInJumpBuffer { get {return _jumpBuffer > 0f && _isJumpBuffered;}}
        public bool WasDetached;
        public Type PreviousStateType { get; private set; }
        public VineHelper LastVine;

        public void ConsumeJumpBuffer()
        {
            _isJumpBuffered = false;
            _jumpBuffer = 0f;
        }
        
        

        private readonly PlayerSettingsSo _settings;
        private readonly PlayerContext _ctx;
        
        private PlayerBaseState _activeState;

        private bool _isJumpBuffered;
        private float _jumpBuffer;
        
        private readonly Dictionary<Type, PlayerBaseState> _states = new Dictionary<Type, PlayerBaseState>();
        
        
        public MovementStateMachine(PlayerSettingsSo settings, PlayerContext ctx)
        {
            _settings = settings;
            _ctx = ctx;
            _activeState = new PlayerIdleState(ctx, this);
            
            _ctx.manager.JumpAction.started += TryBufferJump;
            _jumpBuffer = _settings.JumpBufferTime;
            
            #region POPULATE STATES DICTIONARY
            _states.Add(typeof(PlayerIdleState), _activeState);
            _states.Add(typeof(PlayerRunState), new PlayerRunState(ctx, this));
            _states.Add(typeof(PlayerJumpState), new PlayerJumpState(ctx, this));
            _states.Add(typeof(PlayerClimbingState), new PlayerClimbingState(ctx, this));
            _states.Add(typeof(PlayerFallingState), new PlayerFallingState(ctx, this));
            _states.Add(typeof(PlayerPrepareJumpState), new PlayerPrepareJumpState(ctx, this));
            _states.Add(typeof(PlayerSwingingState), new PlayerSwingingState(ctx, this));
            _states.Add(typeof(PlayerVaultState), new PlayerVaultState(ctx, this));
            _states.Add(typeof(PlayerPushPullState), new PlayerPushPullState(ctx, this));
            _states.Add(typeof(PlayerKnockbackState), new PlayerKnockbackState(ctx, this));
            #endregion
        }

        ~MovementStateMachine()
        {
            _ctx.manager.JumpAction.started -= TryBufferJump;
        }
        
        public void Tick(float dt)
        {
            if(_isJumpBuffered) _jumpBuffer -= dt;
            _activeState?.Tick(dt);
        }

        public void FixedTick(float dt)
        {
            _activeState?.FixedTick(dt);
        }


        public void ChangeState<T>() where T : PlayerBaseState
        {
            ChangeState(typeof(T));    
        }
        
        private void ChangeState(Type newStateType)
        {
            if (_activeState is PlayerJumpState)
            {
                _isJumpBuffered = false;
                _jumpBuffer = _settings.JumpBufferTime;
            }
            
            PreviousStateType = _activeState.GetType();
            _activeState.ExitState();
            _activeState = _states[newStateType];
            _activeState.EnterState();
            
            onChangeState?.Invoke(_activeState.GetType());
        }


        private void TryBufferJump(InputAction.CallbackContext context)
        {
            if (Time.timeScale == 0f) return;
            if (_activeState is PlayerFallingState || _activeState is PlayerIdleState || _activeState is PlayerRunState)
            {
                _isJumpBuffered = true;
                _jumpBuffer = _settings.JumpBufferTime;
            }
        }
    }
}
