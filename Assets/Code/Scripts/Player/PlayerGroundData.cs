using UnityEngine;

namespace Player
{
    public readonly struct PlayerGroundData
    {
        public readonly Vector2 slopeTangent;
        
        public PlayerGroundData(
            Vector2 slopeTangent
        )
        {
            this.slopeTangent = slopeTangent;
        }
    }
}
