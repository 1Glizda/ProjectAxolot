using UnityEngine;

namespace Player
{
    //player references container
    internal sealed class PlayerContext
    {

        public readonly IPlayerInputManager manager;
        public readonly IPlayerStateProvider stateProvider;

        
        public readonly PlayerCollisionHandler collisionHandler;
        public readonly PlayerSettingsSo settings;
        public readonly GameObject spriteObject;
        public readonly Collider2D bodyCollider;
        public readonly Collider2D feetCollider;
        public readonly Rigidbody2D rb;
        public readonly HingeJoint2D swingHinge;

        public Vector2 PendingKnockbackVelocity;

        public PlayerContext(
            IPlayerInputManager manager,
            IPlayerStateProvider stateProvider,
            PlayerCollisionHandler collisionHandler,
            PlayerSettingsSo settings,
            GameObject spriteObject,
            Collider2D bodyCollider,
            Collider2D feetCollider,
            Rigidbody2D rb,
            HingeJoint2D swingHinge
        )
        {
            this.manager = manager;
            this.stateProvider = stateProvider;
            this.collisionHandler = collisionHandler;
            this.settings = settings;
            this.spriteObject = spriteObject;
            this.bodyCollider = bodyCollider;
            this.feetCollider = feetCollider;
            this.rb = rb;
            this.swingHinge = swingHinge;
        }
    }
}
