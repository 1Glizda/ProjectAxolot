using Interfaces;
using UnityEngine;

namespace Player
{
    public interface IPlayerController
    {
        public bool IsGrounded { get; }
        public bool IsInCoyoteTime { get; }

        public bool IsNearValidWall { get; }
        public bool IsFootNearValidWall { get; }
        public Vector2 WallHitNormal { get; }
        
        public bool IsFootNearPushable { get; }
        public IPushable Pushable { get; }

        public bool CanVault { get; }
        public Vector2 VaultTarget { get; }


        public PlayerGroundData GetGroundData();
        
        
        
    }
}
