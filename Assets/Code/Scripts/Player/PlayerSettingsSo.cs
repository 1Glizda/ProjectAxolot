using UnityEngine;

namespace Player
{
 
    [CreateAssetMenu(fileName = "PlayerSettings So", menuName = "PlayerSettings")]
    internal class PlayerSettingsSo : ScriptableObject
    {
        public float GroundedAcceleration => _groundedAcceleration;
        public float GroundedDeceleration => _groundedDeceleration;
        public float JumpAcceleration => _jumpAcceleration;
        public float JumpDeceleration => _jumpDeceleration;
        public float MaxHorizontalVelocity => _maxHorizontalVelocity;
        public float TerminalVerticalVelocity => _terminalVerticalVelocity;
        public float BaseGravity => _baseGravity;
        public AnimationCurve JumpGravityCurve => _jumpGravityCurve;
        public float JumpInitialGravity => _jumpInitialGravity;
        public float FallingGravity => _fallingGravity;
        public float StandstillThreshold => _standstillThreshold;
        public float InitialJumpForce => _initialJumpForce;
        public float JumpRunningMaxAngle => _jumpRunningMaxAngle;
        public float JumpHoldAccel => _jumpHoldAccel;
        public float MaxJumpTime => _maxJumpTime;
        public float MinJumpTime => _minJumpTime;
        public float JumpBufferTime => _jumpBufferTime;
        public LayerMask GroundLayers => _groundLayers;
        public float GroundCheckDistance => _groundCheckDistance;
        public float CoyoteTime => _coyoteTime;
        public float ApplyRotationThreshold => _applyRotationThreshold;
        public float MaxRotation => _maxRotation;
        
        public float WallDetectionRange => _wallDetectionRange;
        public LayerMask WallLayers => _wallLayers;
        public float WallAcceleration => _wallAcceleration;
        public float WallDeceleration => _wallDeceleration;
        public float MaxClimbSpeed => _maxClimbSpeed;
        public float WallSlideSpeed => _wallSlideSpeed;
        public float WallDetachForce => _wallDetachForce;
        public float WallAttachGraceTime => _wallAttachGraceTime;
        
        public float WallJumpForce => _wallJumpForce;
        public float WallJumpAngle => _wallJumpAngle;
        public float VaultDuration => _vaultDuration; 
        
        public float SwingAcceleration  => _swingAcceleration;
        public float SwingAngularDrag => _swingAngularDrag;
        public float SwingEntryMomentumTransfer => _swingEntryMomentumTransfer;
        
        public float VineClimbSpeed => _vineClimbSpeed;
        public float VineJumpForce => _vineJumpForce;
        public float VineReleaseHoldTime => _vineReleaseHoldTime;
            
        public float PushPullForce => _pushPullForce;
        
        [Header("Acceleration Settings")]
        [SerializeField] private float _groundedAcceleration = 30f;
        
        [SerializeField] private float _groundedDeceleration = 50f;
        
        [SerializeField] private float _jumpAcceleration = 15f;
        
        [SerializeField] private float _jumpDeceleration = 25f;
        
        [SerializeField] private float _maxHorizontalVelocity = 10f;


        [Header("Gravity Settings")]
        [SerializeField] private float _terminalVerticalVelocity;
        
        [SerializeField] private float _baseGravity = 10f;
        
        [SerializeField] private AnimationCurve _jumpGravityCurve;

        [SerializeField] private float _jumpInitialGravity;
        
        [SerializeField] private float _fallingGravity;
        
        
        [Header("Movement")]
        [SerializeField] private float _standstillThreshold = 0.02f;
        
        
        [Header("Jump")]
        [SerializeField] private float _initialJumpForce = 4f;

        [SerializeField] private float _jumpRunningMaxAngle = 45f;
        
        [SerializeField] private float _jumpHoldAccel = 5f;
        
        [SerializeField] private float _maxJumpTime = 0.3f;
        
        [SerializeField] private float _minJumpTime = 0.1f;

        [SerializeField] private float _jumpBufferTime = 0.15f;
        
        
        
        [Header("Ground Checks")]
        [SerializeField] private LayerMask _groundLayers;
        
        [SerializeField] private float _groundCheckDistance;
        
        [SerializeField] private float _coyoteTime = 0.1f;

        [Header("Slope Rotation")]
        [SerializeField] private float _applyRotationThreshold;
        
        [SerializeField] private float _maxRotation;

        [Header("Wall Climbing")]
        [SerializeField] private float _wallDetectionRange;
        
        [SerializeField] private LayerMask _wallLayers;
        
        [SerializeField] private float _wallAcceleration;

        [SerializeField] private float _wallDeceleration;
        
        [SerializeField] private float _maxClimbSpeed;
        
        [SerializeField] private float _wallSlideSpeed = 2f;

        [SerializeField] private float _wallDetachForce;
        
        [SerializeField] private float _wallAttachGraceTime = 0.15f;
        
        [SerializeField] private float _wallJumpForce;
        
        [SerializeField] private float _wallJumpAngle;
        

        [Header("Vaulting")]
        [SerializeField] private float _vaultDuration = 0.2f;

        [Header("Swinging")]
        [SerializeField] private float _swingAcceleration;
        
        [SerializeField] private float _swingAngularDrag;

        [SerializeField] private float _swingEntryMomentumTransfer = 0.2f;

        [SerializeField] private float _vineClimbSpeed = 0.15f;
        
        [SerializeField] private float _vineJumpForce = 12f;

        [SerializeField] private float _vineReleaseHoldTime = 0.5f;


        [Header("Pushing Pulling")]
        [SerializeField] private float _pushPullForce;
        
    }
}
