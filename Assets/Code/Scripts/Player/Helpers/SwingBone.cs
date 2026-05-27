using UnityEngine;

namespace Player.Helpers
{
    public class SwingBone : MonoBehaviour
    {
        public Rigidbody2D Rb { get; private set; }
        public VineHelper VineHelper { get; private set; }
        
        public float BoneLength { get; private set; }
        private int _pulseLayer;


        private void PulseInteract()
        {
            Vector2 dir =  Rb.position -  PlayerController.PlayerControllerContext.rb.position;
            Rb.AddForce(dir * 0.2f, ForceMode2D.Impulse);
        }

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            VineHelper = GetComponentInParent<VineHelper>();
            _pulseLayer = LayerMask.NameToLayer("Pulse");
        }

        private void OnParticleCollision(GameObject other)
        {
            if (other.gameObject.layer == _pulseLayer)
            {
                PulseInteract();
                Debug.Log("Pulse interacted with swing bone!");
            }
        }
    }
}
