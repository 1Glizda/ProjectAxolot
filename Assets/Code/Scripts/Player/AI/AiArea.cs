using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Player.AI.Navigation
{
    
    [RequireComponent(typeof(BoxCollider2D))]
    public class AiArea : MonoBehaviour
    {
        [Tooltip("The patrol points within this area where the AI can navigate.")]
        public Transform[] anchorPoints;

<<<<<<< Updated upstream
        [SerializeField] List<AiPoint> points;
        
=======
        [Tooltip("When the player enters this zone, the AI will burrow-teleport to the first anchor point instead of walking.")]
        public bool teleportToNextZone;

>>>>>>> Stashed changes
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                AiManager.Instance.SetActiveArea(this);
            }
        }

        private void OnValidate()
        {
            Debug.Log("OnValidate");
        }
        
        
        [ContextMenu("Do Something")]
        public void Test()
        {
            Debug.Log("Test",this);
        }
        private void OnDrawGizmos()
        {
            if (anchorPoints == null) return;
            
            // Draw anchor points
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            foreach(var anchor in anchorPoints)
            {
                if (anchor != null)
                {
                    Gizmos.DrawSphere(anchor.position, 0.3f);
                }
            }

            // Draw teleport indicator if enabled
            if (teleportToNextZone)
            {
                Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.4f);
                BoxCollider2D box = GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
                }
            }
        }
    }
}

[System.Serializable]
public class AiPoint
{
    public UnityEvent onArrive;
    public int speed;
    public Transform pos;
    public float num2;
}