using UnityEngine;

namespace Player
{
    //player references container
    internal sealed class PlayerContext
    {

        public readonly IPlayerInputManager manager;
        public readonly IPlayerController controller;

        
        public readonly PlayerCollisionHandler collisionHandler;
        public readonly PlayerSettingsSo settings;
        public readonly Animator animator;
        public readonly SpriteRenderer spriteRenderer;
        public readonly Collider2D bodyCollider;
        public readonly Collider2D feetCollider;
        public readonly Rigidbody2D rb;
        public readonly HingeJoint2D swingHinge;

        public PlayerContext(
            IPlayerInputManager manager,
            IPlayerController controller,
            PlayerCollisionHandler collisionHandler,
            PlayerSettingsSo settings,
            Animator animator,
            SpriteRenderer spriteRenderer,
            Collider2D bodyCollider,
            Collider2D feetCollider,
            Rigidbody2D rb,
            HingeJoint2D swingHinge
        )
        {
            this.manager = manager;
            this.controller = controller;
            this.collisionHandler = collisionHandler;
            this.settings = settings;
            this.animator = animator;
            this.spriteRenderer = spriteRenderer;
            this.bodyCollider = bodyCollider;
            this.feetCollider = feetCollider;
            this.rb = rb;
            this.swingHinge = swingHinge;
        }
    }
}
