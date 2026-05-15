using UnityEngine;

namespace Player
{
    public class AnimatorHelper : MonoBehaviour
    {
        //animator properties
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int IsClimbing = Animator.StringToHash("IsClimbing");
        private static readonly int IsPreparingWallJump = Animator.StringToHash("IsPreparingWallJump");
        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int HorizontalVelocity = Animator.StringToHash("HorizontalVelocity");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int StartClimb = Animator.StringToHash("StartClimb");


        [SerializeField] private Animator _animator;

        private IPlayerStateProvider _playerState;
        private bool _isInitialized;
        
        

        private void Update()
        {
            if (!_isInitialized) return;
            _animator.SetBool(IsClimbing,  _playerState.IsClimbing);
            _animator.SetBool(IsPreparingWallJump, _playerState.IsPreparingWallJump);
            _animator.SetFloat(VerticalVelocity, _playerState.VerticalVelocity);
            _animator.SetFloat(HorizontalVelocity, Mathf.Abs(_playerState.HorizontalVelocity));
            _animator.SetBool(IsGrounded, _playerState.IsGrounded);
        }
        
        
        public void Initialize(IPlayerStateProvider stateProvider)
        {
            _playerState = stateProvider;
            _isInitialized = true;
            _playerState.OnJump+=OnJump;
            _playerState.OnStartClimb+=OnStartClimb;
        }

        private void OnEnable()
        {
            if (!_isInitialized) return;
            _playerState.OnJump += OnJump;
            _playerState.OnStartClimb += OnStartClimb;
        }
        private void OnDisable()
        {
            if (!_isInitialized) return;
            _playerState.OnJump -= OnJump;
            _playerState.OnStartClimb -= OnStartClimb;
        }
        
        private void OnJump()
        {
            _animator.SetTrigger(Jump);
        }

        private void OnStartClimb()
        {
            _animator.SetTrigger(StartClimb);
        }

    }
}
