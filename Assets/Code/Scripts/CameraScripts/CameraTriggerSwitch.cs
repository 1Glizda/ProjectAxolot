using Unity.Cinemachine;
using UnityEngine;

namespace CameraScripts
{
    public class CameraTriggerSwitch : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _targetCamera;
        [Tooltip("If false, the camera stays active even after you leave the trigger zone.")]
        [SerializeField] private bool _revertOnExit = true;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                _targetCamera.Priority = 11;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!_revertOnExit) return;

            if (collision.CompareTag("Player"))
            {
                _targetCamera.Priority = 9;
            }
        }
    }
}
