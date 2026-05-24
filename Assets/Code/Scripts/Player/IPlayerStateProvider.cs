using Interfaces;
using UnityEngine;

namespace Player
{
    public interface IPlayerStateProvider
    {
        public event System.Action OnJump;
        public event System.Action OnStartClimb;
        public event System.Action OnLand;
        public event System.Action OnGrabVine;
        public bool IsClimbing { get; }
        public bool IsJumping { get; }
        public float VerticalVelocity { get; }
        public float HorizontalVelocity { get; }
        
        public bool IsGrounded { get; }
        public bool IsInCoyoteTime { get; }
 
        public bool IsNearValidWall { get; }
        public bool IsFootNearValidWall { get; }
        public bool IsHeadBlocked { get; }
        public Vector2 WallHitNormal { get; }
        
        public bool IsFootNearPushable { get; }
        public IPushable Pushable { get; }
 
        public bool CanVault { get; }
        public Vector2 VaultTarget { get; }
 
 
        public PlayerGroundData GetGroundData();
        public void NotifyJump();
        public void NotifyStartClimb();
        public void NotifyGrabVine();
        

        
    }
}
