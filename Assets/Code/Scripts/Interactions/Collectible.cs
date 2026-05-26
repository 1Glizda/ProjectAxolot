using UnityEngine;

namespace Interactions
{
    public class Collectible : MonoBehaviour
    {
        private bool _isCollected;

        private void Start()
        {
            if (CollectibleTracker.Instance != null)
            {
                CollectibleTracker.Instance.RegisterCollectible();
            }
            else
            {
                Debug.LogWarning("No CollectibleTracker instance found in scene to register this collectible.", this);
            }
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (_isCollected) return;

            if (collider.CompareTag("Player") || (collider.attachedRigidbody != null && collider.attachedRigidbody.CompareTag("Player")))
            {
                _isCollected = true;
                
                if (CollectibleTracker.Instance != null)
                {
                    CollectibleTracker.Instance.Collect();
                }

                // Deactivate the collectible upon collection
                gameObject.SetActive(false);
            }
        }
    }
}
