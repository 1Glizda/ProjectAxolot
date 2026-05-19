using UnityEngine;
using UnityEngine.Events;
using Player;

namespace Interactions
{
    [RequireComponent(typeof(Collider2D))]
    public class PulseUnlockTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, this trigger will destroy itself after successfully enabling the Pulse action.")]
        [SerializeField] private bool _destroyOnTrigger = true;

        [Header("Events")]
        [Tooltip("Triggered when the Pulse action is successfully unlocked/enabled.")]
        [SerializeField] private UnityEvent _onPulseEnabled;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // Find the player's IPlayerInputManager starting from the collider
                IPlayerInputManager inputManager = other.GetComponent<IPlayerInputManager>() 
                    ?? other.GetComponentInParent<IPlayerInputManager>() 
                    ?? other.GetComponentInChildren<IPlayerInputManager>();

                if (inputManager != null && inputManager.PulseAction != null)
                {
                    if (!inputManager.PulseAction.enabled)
                    {
                        inputManager.PulseAction.Enable();
                        Debug.Log($"[PulseUnlockTrigger] Player's Pulse action has been enabled!");
                    }
                    else
                    {
                        Debug.Log("[PulseUnlockTrigger] Player's Pulse action was already enabled.");
                    }

                    _onPulseEnabled?.Invoke();

                    if (_destroyOnTrigger)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.LogWarning("[PulseUnlockTrigger] Player entered trigger, but IPlayerInputManager or PulseAction could not be found.");
                }
            }
        }
    }
}
