using UnityEngine;

namespace RollyPolly
{
    public partial class RollyPollyBehaviour
    {
        private void OnDrawGizmos()
        {
            // 1. Detection Range (translucent Cyan circle)
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // 2. Height Tolerance band (translucent Yellow wire cube, adjusted for asymmetric above/below bounds)
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
            float totalHeight = _detectionHeightTolerance + _aboveHeightTolerance;
            float centerY = transform.position.y + (_aboveHeightTolerance - _detectionHeightTolerance) * 0.5f;
            Vector3 size = new Vector3(_detectionRange * 2f, totalHeight, 0.1f);
            Gizmos.DrawWireCube(new Vector3(transform.position.x, centerY, transform.position.z), size);

            // 3. Patrol Bounds visualizer (Magenta vertical lines and transparent floor block)
            if (_leftPatrolBound != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 leftPos = _leftPatrolBound.position;
                Gizmos.DrawLine(new Vector3(leftPos.x, leftPos.y - 5f, leftPos.z), new Vector3(leftPos.x, leftPos.y + 5f, leftPos.z));
                Gizmos.DrawWireSphere(leftPos, 0.2f);
            }

            if (_rightPatrolBound != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 rightPos = _rightPatrolBound.position;
                Gizmos.DrawLine(new Vector3(rightPos.x, rightPos.y - 5f, rightPos.z), new Vector3(rightPos.x, rightPos.y + 5f, rightPos.z));
                Gizmos.DrawWireSphere(rightPos, 0.2f);
            }

            if (_leftPatrolBound != null && _rightPatrolBound != null)
            {
                float minX = Mathf.Min(_leftPatrolBound.position.x, _rightPatrolBound.position.x);
                float maxX = Mathf.Max(_leftPatrolBound.position.x, _rightPatrolBound.position.x);
                float midY = (_leftPatrolBound.position.y + _rightPatrolBound.position.y) * 0.5f;

                Gizmos.color = new Color(1f, 0f, 1.5f, 0.15f);
                Gizmos.DrawCube(new Vector3((minX + maxX) * 0.5f, midY, 0f), new Vector3(maxX - minX, 0.2f, 0.1f));
            }
        }
    }
}
