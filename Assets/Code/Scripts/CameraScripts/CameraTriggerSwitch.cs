using Unity.Cinemachine;
using UnityEngine;

namespace CameraScripts
{
    public class CameraTriggerSwitch : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _targetCamera;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                _targetCamera.Priority = 11;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                _targetCamera.Priority = 9;
            }
        }
    }
}
