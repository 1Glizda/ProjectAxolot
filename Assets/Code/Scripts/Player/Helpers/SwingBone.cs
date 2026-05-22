using UnityEngine;

namespace Player.Helpers
{
    internal class SwingBone : MonoBehaviour
    {
        public Rigidbody2D Rb { get; private set; }
        public VineHelper VineHelper { get; private set; }
        
        public float BoneLength { get; private set; }
        private int _pulseLayer;


        private void PulseInteract(){
            Rb.AddForce(Vector2.up * 5f, ForceMode2D.Impulse);
        }

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            VineHelper = GetComponentInParent<VineHelper>();
            _pulseLayer = LayerMask.NameToLayer("Pulse");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == _pulseLayer)
            {
                PulseInteract();
                Debug.Log("Pulse interacted with swing bone!");
            }
        }
    }
}
