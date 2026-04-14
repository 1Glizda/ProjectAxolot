using Player.Helpers;
using System;
using System.Collections.Generic;

namespace Player.AI.StateMachine
{
    //state machine that holds the movement states.
    //currently the states know of each other and the state machine, they can call ChangeState() from here
    internal sealed class AiMovementStateMachine
    {
        public Action<Type> onChangeState;
        public bool IsInJumpBuffer { get {return _jumpBuffer > 0f && _isJumpBuffered;}}
        public bool WasDetached;

        public void ConsumeJumpBuffer()
        {
            _isJumpBuffered = false;
            _jumpBuffer = 0f;
        }
        
        

        private readonly PlayerSettingsSo _settings;
        private readonly AiContext _ctx;
        
        private AiBaseState _activeState;

        private bool _isJumpBuffered;
        private float _jumpBuffer;
        
        private readonly Dictionary<Type, AiBaseState> _states = new Dictionary<Type, AiBaseState>();
        
        
        public AiMovementStateMachine(PlayerSettingsSo settings, AiContext ctx)
        {
            _settings = settings;
            _ctx = ctx;
            _activeState = new AiIdleState(ctx, this);
            
            _ctx.manager.OnJumpStarted += TryBufferJump;
            _jumpBuffer = _settings.JumpBufferTime;
            
            #region POPULATE STATES DICTIONARY
            _states.Add(typeof(AiIdleState), _activeState);
            _states.Add(typeof(AiRunState), new AiRunState(ctx, this));
            _states.Add(typeof(AiJumpState), new AiJumpState(ctx, this));
            _states.Add(typeof(AiClimbingState), new AiClimbingState(ctx, this));
            _states.Add(typeof(AiFallingState), new AiFallingState(ctx, this));
            _states.Add(typeof(AiPrepareJumpState), new AiPrepareJumpState(ctx, this));
            _states.Add(typeof(AiSwingingState), new AiSwingingState(ctx, this));
            _states.Add(typeof(AiVaultState), new AiVaultState(ctx, this));
            _states.Add(typeof(AiPushPullState), new AiPushPullState(ctx, this));
            #endregion
        }

        ~AiMovementStateMachine()
        {
            _ctx.manager.OnJumpStarted -= TryBufferJump;
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


        public void ChangeState<T>() where T : AiBaseState
        {
            ChangeState(typeof(T));    
        }
        
        private void ChangeState(Type newStateType)
        {
            if (_activeState is AiJumpState)
            {
                _isJumpBuffered = false;
                _jumpBuffer = _settings.JumpBufferTime;
            }
            
            _activeState.ExitState();
            _activeState = _states[newStateType];
            _activeState.EnterState();
            
            onChangeState?.Invoke(_activeState.GetType());
        }


        private void TryBufferJump()
        {
            if (_activeState is AiFallingState || _activeState is AiIdleState || _activeState is AiRunState)
            {
                _isJumpBuffered = true;
                _jumpBuffer = _settings.JumpBufferTime;
            }
        }
    }
}
