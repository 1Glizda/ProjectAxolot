using UnityEngine;

namespace Platforming
{
    public class SwingBone : MonoBehaviour
    {
        public Rigidbody2D Rb { get; private set; }
        public VineHelper VineHelper { get; private set; }
        
        public float BoneLength { get; private set; }
        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            VineHelper = transform.parent.GetComponent<VineHelper>();
            
        }
    }
}
