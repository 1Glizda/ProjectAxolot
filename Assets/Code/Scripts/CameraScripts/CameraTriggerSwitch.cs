using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace CameraScripts
{
    public class CameraTriggerSwitch : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _targetCamera;
        [SerializeField] private bool _revertOnExit = true;
        [SerializeField] private float _exitDelay = 1f;

        private Coroutine _exitRoutine;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (_exitRoutine != null)
                {
                    StopCoroutine(_exitRoutine);
                    _exitRoutine = null;
                }

                _targetCamera.Priority = 11;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!_revertOnExit) return;

            if (collision.CompareTag("Player"))
            {
                if (_exitRoutine != null) StopCoroutine(_exitRoutine);
                _exitRoutine = StartCoroutine(DelayedPriorityDrop());
            }
        }

        private IEnumerator DelayedPriorityDrop()
        {
            yield return new WaitForSeconds(_exitDelay);
            _targetCamera.Priority = 9;
            _exitRoutine = null;
        }
    }
}
