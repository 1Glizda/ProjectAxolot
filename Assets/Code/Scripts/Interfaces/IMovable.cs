using UnityEngine;

namespace Interfaces
{
    public interface IPushable
    {
        public void ApplyPushForce(Vector2 force);
        public Vector2 Velocity { get; }
    }
}
