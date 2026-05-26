using UnityEngine;

namespace Interactions
{
    public class MossBehaviour : PulseLightUpBehaviour
    {
        [Header("Moss Settings")]
        [SerializeField] private int _climbableLayer = 15;
        
        private void Awake()
        {
            _ = base.FadeIn();
            gameObject.layer = _climbableLayer;
        }

        public override void PulseInteract()
        {
            // Do nothing, permanent moss is already lit and active
        }
    }
}
