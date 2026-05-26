using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace CameraCustom
{
    /// <summary>
    /// Tracks the nearest point on a global SplineContainer to a target (player)
    /// and updates its own position. This object can then be used in a Cinemachine Target Group.
    /// </summary>
    public class SplineCameraBias : MonoBehaviour
    {
        [Tooltip("The global spline path you want the camera to bias towards.")]
        [SerializeField] private SplineContainer _globalSpline;
        
        [Tooltip("The player or object to track.")]
        [SerializeField] private Transform _playerTarget;

        private void Update()
        {
            if (_globalSpline == null || _playerTarget == null) return;

            // Convert the player's world position into the Spline's local space
            float3 localTarget = _globalSpline.transform.InverseTransformPoint(_playerTarget.position);

            float minDistanceSq = float.MaxValue;
            float3 bestLocalPoint = localTarget; // default fallback

            // SplineContainer can hold multiple splines, check all of them to find the closest point
            foreach (var spline in _globalSpline.Splines)
            {
                SplineUtility.GetNearestPoint(spline, localTarget, out float3 nearest, out float t);
                
                float distSq = math.distancesq(localTarget, nearest);
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    bestLocalPoint = nearest;
                }
            }

            // Convert the nearest local point back to world space and update position
            transform.position = _globalSpline.transform.TransformPoint(bestLocalPoint);
        }
        
        // Optional helper to easily assign via script if needed
        public void SetTargets(SplineContainer spline, Transform player)
        {
            _globalSpline = spline;
            _playerTarget = player;
        }
    }
}
