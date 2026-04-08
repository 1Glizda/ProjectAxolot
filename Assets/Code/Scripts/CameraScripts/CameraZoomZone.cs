using UnityEngine;
using System.Threading.Tasks;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraZoomZone : MonoBehaviour
{
    [Header("Zoom (Z Axis)")]
    [Tooltip("Check to affect the camera's Z zoom level.")]
    [SerializeField] private bool _affectZoom = true;
    [Tooltip("The Z offset the camera should transition to when player enters this zone.")]
    [SerializeField] private float _targetZoomZ = -15f;
    
    [Header("Offset (Y Axis)")]
    [Tooltip("Check to affect the camera's Y offset.")]
    [SerializeField] private bool _affectYOffset = false;
    [Tooltip("The Y offset the camera should transition to when player enters this zone.")]
    [SerializeField] private float _targetOffsetY = 2f;
    
    [Header("Settings")]
    [Tooltip("If true, resets the camera back to default when leaving the zone.")]
    [SerializeField] private bool _revertOnExit = true;

    private BoxCollider2D _col;

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.ApplyZoneSettings(_affectZoom, _targetZoomZ, _affectYOffset, _targetOffsetY, this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_revertOnExit && other.CompareTag("Player"))
        {
            if (!other.enabled || !other.gameObject.activeInHierarchy)
            {
                _ = WaitAndCheckExitAsync(other);
                return;
            }

            RevertZoom();
        }
    }

    private async Task WaitAndCheckExitAsync(Collider2D other)
    {
        while (other != null && (!other.gameObject.activeInHierarchy || !other.enabled))
        {
            await Task.Yield();
        }

        await Awaitable.FixedUpdateAsync(); 

        if (other != null && _col != null)
        {
            if (!_col.IsTouching(other))
            {
                RevertZoom();
            }
        }
    }

    private void RevertZoom()
    {
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.RevertZoneSettings(this);
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if(col != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        }
    }
}
