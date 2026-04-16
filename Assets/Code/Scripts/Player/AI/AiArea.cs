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

        [SerializeField] List<AiPoint> points;
        
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
            
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            foreach(var anchor in anchorPoints)
            {
                if (anchor != null)
                {
                    Gizmos.DrawSphere(anchor.position, 0.3f);
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