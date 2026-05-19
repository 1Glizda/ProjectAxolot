using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Player.AI.Navigation
{
    
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider2D))]
    public class AiArea : MonoBehaviour
    {
        [Tooltip("The patrol points within this area where the AI can navigate.")]
        public List<Transform> anchorPoints = new List<Transform>();

        [Tooltip("When the player enters this zone, the AI will burrow-teleport to the first anchor point instead of walking.")]
        public bool teleportToNextZone;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                AiManager.Instance.SetActiveArea(this);
            }
        }

        private void OnValidate()
        {
            SyncAnchorPoints();
        }

        private void OnTransformChildrenChanged()
        {
            SyncAnchorPoints();
        }

        private void SyncAnchorPoints()
        {
            // Auto-populate / sync with children to allow easy drag-and-drop ordering in the inspector
            if (anchorPoints == null)
            {
                anchorPoints = new List<Transform>();
            }

            // 1. Gather all current children transforms
            List<Transform> currentChildren = new List<Transform>();
            foreach (Transform child in transform)
            {
                currentChildren.Add(child);
            }

            bool changed = false;

            // 2. Remove any transforms that are null or no longer children of this GameObject
            for (int i = anchorPoints.Count - 1; i >= 0; i--)
            {
                if (anchorPoints[i] == null || !currentChildren.Contains(anchorPoints[i]))
                {
                    anchorPoints.RemoveAt(i);
                    changed = true;
                }
            }

            // 3. Add any new children that aren't already in the list (maintaining existing ordering)
            foreach (Transform child in currentChildren)
            {
                if (!anchorPoints.Contains(child))
                {
                    anchorPoints.Add(child);
                    changed = true;
                }
            }

#if UNITY_EDITOR
            if (changed && !Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                }
            }
#endif
        }

        [ContextMenu("Reset Order to Hierarchy")]
        public void ResetOrderToHierarchy()
        {
            anchorPoints = new List<Transform>();
            foreach (Transform child in transform)
            {
                anchorPoints.Add(child);
            }
            Debug.Log($"[AiArea] Reset anchor points order to match Hierarchy of {gameObject.name}.", this);
        }
        
        
        [ContextMenu("Do Something")]
        public void Test()
        {
            Debug.Log("Test",this);
        }
        private void OnDrawGizmos()
        {
            if (anchorPoints == null) return;

            for (int i = 0; i < anchorPoints.Count; i++)
            {
                var anchor = anchorPoints[i];
                if (anchor == null) continue;

                // Draw anchor sphere (green for normal, magenta for teleport zones)
                Gizmos.color = teleportToNextZone
                    ? new Color(0.8f, 0.2f, 0.8f, 0.6f)
                    : new Color(0.2f, 0.8f, 0.2f, 0.6f);
                Gizmos.DrawSphere(anchor.position, 0.3f);

                // Draw path line to next anchor
                if (i < anchorPoints.Count - 1 && anchorPoints[i + 1] != null)
                {
                    Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
                    Gizmos.DrawLine(anchor.position, anchorPoints[i + 1].position);
                }

#if UNITY_EDITOR
                // Draw numbered label at each anchor
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 14;
                UnityEditor.Handles.Label(anchor.position + Vector3.up * 0.5f, $"[{i}]", style);
#endif
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