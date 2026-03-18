using Player.StateMachine;
using UnityEngine;

namespace Player
{
    public interface IPlayerController
    {
        public bool IsGrounded { get; }
        public float DistanceToGround { get; }
        public bool IsInCoyoteTime { get; }

        public bool IsNearValidWall { get; }
        public bool IsFootNearValidWall { get; }
        public Vector2 WallHitNormal { get; }
        
        public PlayerGroundData GetGroundData();
        
        
        
    }
}
