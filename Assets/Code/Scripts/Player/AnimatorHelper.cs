using UnityEngine;

namespace Player
{
    public class AnimatorHelper : MonoBehaviour
    {
        //animator properties
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int IsClimbing = Animator.StringToHash("IsClimbing");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int HorizontalVelocity = Animator.StringToHash("HorizontalVelocity");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int StartClimb = Animator.StringToHash("StartClimb");
        private static readonly int GrabVine = Animator.StringToHash("GrabVine");
        private static readonly int IsWallIdle = Animator.StringToHash("IsWallIdle");
        private static readonly int IsWallResting = Animator.StringToHash("IsWallResting");


        [SerializeField] private Animator _animator;

        private IPlayerStateProvider _playerState;
        private bool _isInitialized;
        
        

        private void Update()
        {
            if (!_isInitialized || _animator == null || _animator.runtimeAnimatorController == null || !_animator.isActiveAndEnabled) return;
            _animator.SetBool(IsClimbing,  _playerState.IsClimbing);
            _animator.SetFloat(VerticalVelocity, _playerState.VerticalVelocity);
            _animator.SetFloat(HorizontalVelocity, Mathf.Abs(_playerState.HorizontalVelocity));
            _animator.SetBool(IsGrounded, _playerState.IsGrounded);
            _animator.SetBool(IsWallIdle, _playerState.IsWallIdle);
            _animator.SetBool(IsWallResting, _playerState.IsWallResting);
        }
        
        
        public void Initialize(IPlayerStateProvider stateProvider)
        {
            _playerState = stateProvider;
            _isInitialized = true;
            _playerState.OnJump+=OnJump;
            _playerState.OnStartClimb+=OnStartClimb;
            _playerState.OnGrabVine+=OnGrabVine;
        }

        private void OnEnable()
        {
            if (!_isInitialized) return;
            _playerState.OnJump += OnJump;
            _playerState.OnStartClimb += OnStartClimb;
            _playerState.OnGrabVine += OnGrabVine;
        }
        private void OnDisable()
        {
            if (!_isInitialized) return;
            _playerState.OnJump -= OnJump;
            _playerState.OnStartClimb -= OnStartClimb;
            _playerState.OnGrabVine -= OnGrabVine;
        }
        
        private void OnJump()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null || !_animator.isActiveAndEnabled) return;
            _animator.SetTrigger(Jump);
        }

        private void OnStartClimb()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null || !_animator.isActiveAndEnabled) return;
            _animator.SetTrigger(StartClimb);
        }

        private void OnGrabVine()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null || !_animator.isActiveAndEnabled) return;
            _animator.SetTrigger(GrabVine);
        }

    }
}
