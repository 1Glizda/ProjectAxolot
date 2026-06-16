namespace RollyPolly
{
    /// <summary>
    /// Centralizes magic numbers used across the RollyPollyBehaviour partial class files.
    /// </summary>
    public static class RollyPollyConstants
    {
        /// <summary>Vertical offset for "eye level" raycasts (both enemy and player).</summary>
        public const float EyeLevelOffset = 0.5f;

        /// <summary>Angle (degrees) above which a surface is considered a wall, not a slope.</summary>
        public const float WallAngleThreshold = 50f;

        /// <summary>How far down to raycast when checking for gaps between enemy and player.</summary>
        public const float GapCheckDepth = 2.5f;

        /// <summary>Horizontal step size for the gap-check sweep.</summary>
        public const float GapCheckStep = 0.5f;

        /// <summary>Hard timeout (seconds) for the Attack state before reverting to Patrol.</summary>
        public const float AttackTimeout = 5f;

        /// <summary>How long the yeet-and-kill spin lasts before the poof effect.</summary>
        public const float YeetDuration = 0.7f;

        /// <summary>Y coordinate used to hide a dead enemy offscreen.</summary>
        public const float OffscreenY = -9999f;

        /// <summary>How far ahead to raycast for geysers during patrol.</summary>
        public const float GeyserCheckDistance = 1.5f;

        /// <summary>Length of the downward ray used for ledge detection.</summary>
        public const float LedgeCheckRayLength = 0.6f;

        /// <summary>Length of the downward ray used for grounded checks.</summary>
        public const float GroundedRayLength = 0.25f;

        /// <summary>Lerp speed for smoothing the ground normal each frame.</summary>
        public const float NormalSmoothSpeed = 8f;

        /// <summary>Small upward offset for ground-check ray origins above the collider bottom.</summary>
        public const float GroundCheckOriginOffset = 0.1f;

        /// <summary>Length of the downward ray used for fetching the ground normal.</summary>
        public const float GroundNormalRayLength = 0.4f;

        /// <summary>Upward offset for ledge-check ray origins.</summary>
        public const float LedgeCheckOriginOffset = 0.15f;
    }
}
