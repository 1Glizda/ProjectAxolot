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
        public bool UseDiagonalJump => _useDiagonalJump;
        public float JumpRunningMaxAngle => _jumpRunningMaxAngle;
        public float JumpHoldAccel => _jumpHoldAccel;
        public float MaxJumpTime => _maxJumpTime;
        public float MinJumpTime => _minJumpTime;
        public float JumpCutMultiplier => _jumpCutMultiplier;
        public float JumpBufferTime => _jumpBufferTime;
        public float JumpApexThreshold => _jumpApexThreshold;
        public float JumpApexGravityMultiplier => _jumpApexGravityMultiplier;
        public float JumpApexHorizontalAccelMultiplier => _jumpApexHorizontalAccelMultiplier;
        public float CornerCorrectionAmount => _cornerCorrectionAmount;
        public float CornerCorrectionDistance => _cornerCorrectionDistance;
        public LayerMask GroundLayers => _groundLayers;
        public float GroundCheckDistance => _groundCheckDistance;
        public float CoyoteTime => _coyoteTime;
        public float ApplyRotationThreshold => _applyRotationThreshold;
        public float MaxRotation => _maxRotation;
        
        public float MaxSlopeAngle => _maxSlopeAngle;
        public float SlopeSlideSpeed => _slopeSlideSpeed;
        public float SlopeSlideAccel => _slopeSlideAccel;
        
        public float WallDetectionRange => _wallDetectionRange;
        public LayerMask WallLayers => _wallLayers;
        public float WallAcceleration => _wallAcceleration;
        public float WallDeceleration => _wallDeceleration;
        public float MaxClimbSpeed => _maxClimbSpeed;
        public float WallSlideSpeed => _wallSlideSpeed;
        public float WallDetachForce => _wallDetachForce;
        public float WallAttachGraceTime => _wallAttachGraceTime;
        public float WallCoyoteTime => _wallCoyoteTime;
        
        public float WallJumpForce => _wallJumpForce;
        public float WallJumpAngle => _wallJumpAngle;
        public float VaultDuration => _vaultDuration; 
        public float WallSlideDelay => _wallSlideDelay;
        
        public float SwingAcceleration  => _swingAcceleration;
        public float SwingAngularDrag => _swingAngularDrag;
        public float SwingEntryMomentumTransfer => _swingEntryMomentumTransfer;
        public float SwingMomentumMultiplier => _swingMomentumMultiplier;
        

        public float VineJumpForce => _vineJumpForce;
        public bool AutoGrabVines => _autoGrabVines;
        public float VineReleaseHoldTime => _vineReleaseHoldTime;
        public float VineCatchTranslationDuration => _vineCatchTranslationDuration;
            
        public float PushPullForce => _pushPullForce;
        public float PushableCheckYOffset => _pushableCheckYOffset;
        public float PushableCheckDistance => _pushableCheckDistance;
        public bool UseManualGrabForPushables => _useManualGrabForPushables;
        public float PushIntentDelay => _pushIntentDelay;
        public float PushForceRampTime => _pushForceRampTime;
        public float PushSnapSpeed => _pushSnapSpeed;
        
        public float KnockbackAirAcceleration => _knockbackAirAcceleration;
        
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

        [SerializeField] private bool _useDiagonalJump = false;

        [SerializeField] private float _jumpRunningMaxAngle = 45f;
        
        [SerializeField] private float _jumpHoldAccel = 5f;
        
        [SerializeField] private float _maxJumpTime = 0.3f;
        
        [SerializeField] private float _minJumpTime = 0.1f;

        [SerializeField] private float _jumpCutMultiplier = 0.5f;

        [SerializeField] private float _jumpBufferTime = 0.15f;
        
        [SerializeField] private float _jumpApexThreshold = 2f;
        
        [SerializeField] private float _jumpApexGravityMultiplier = 0.5f;
        
        [SerializeField] private float _jumpApexHorizontalAccelMultiplier = 1.5f;
        
        [SerializeField] private float _cornerCorrectionAmount = 5f;
        
        [SerializeField] private float _cornerCorrectionDistance = 0.3f;
        
        
        
        [Header("Ground Checks")]
        [SerializeField] private LayerMask _groundLayers;
        
        [SerializeField] private float _groundCheckDistance;
        
        [SerializeField] private float _coyoteTime = 0.1f;

        [Header("Slope Rotation")]
        [SerializeField] private float _applyRotationThreshold;
        
        [SerializeField] private float _maxRotation;

        [Header("Slope Sliding")]
        [SerializeField] private float _maxSlopeAngle = 45f;
        [SerializeField] private float _slopeSlideSpeed = 12f;
        [SerializeField] private float _slopeSlideAccel = 40f;

        [Header("Wall Climbing")]
        [SerializeField] private float _wallDetectionRange;
        
        [SerializeField] private LayerMask _wallLayers;
        
        [SerializeField] private float _wallAcceleration;

        [SerializeField] private float _wallDeceleration;
        
        [SerializeField] private float _maxClimbSpeed;
        
        [SerializeField] private float _wallSlideSpeed = 2f;

        [SerializeField] private float _wallDetachForce;
        
        [SerializeField] private float _wallAttachGraceTime = 0.15f;
        
        [SerializeField] private float _wallCoyoteTime = 0.15f;
        
        [SerializeField] private float _wallJumpForce;
        
        [SerializeField] private float _wallJumpAngle;
        
        [SerializeField] private float _wallSlideDelay = 2f;
        

        [Header("Vaulting")]
        [SerializeField] private float _vaultDuration = 0.2f;

        [Header("Swinging")]
        [SerializeField] private float _swingAcceleration;
        
        [SerializeField] private float _swingAngularDrag;

        [SerializeField] private float _swingEntryMomentumTransfer = 0.2f;
        
        [SerializeField] private float _swingMomentumMultiplier = 1.3f;


        [SerializeField] private float _vineJumpForce = 12f;
        [SerializeField] private bool _autoGrabVines = true;

        [SerializeField] private float _vineReleaseHoldTime = 0.5f;

        [SerializeField] private float _vineCatchTranslationDuration = 0.1f;

        [Header("Pushing Pulling")]
        [SerializeField] private float _pushPullForce;
        
        [SerializeField] private float _pushableCheckYOffset = 0.1f;
        [SerializeField] private float _pushableCheckDistance = 0.5f;
        
        [SerializeField] private bool _useManualGrabForPushables = true;
        [SerializeField] private float _pushIntentDelay = 0.15f;
        [SerializeField] private float _pushForceRampTime = 0.35f;
        [SerializeField] private float _pushSnapSpeed = 20f;
        
        [Header("Knockback")]
        [SerializeField] private float _knockbackAirAcceleration = 3f;
        
    }
}
