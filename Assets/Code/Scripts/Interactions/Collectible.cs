using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace Interactions
{
    public class Collectible : MonoBehaviour
    {
        [Header("Sound")]
        [Tooltip("One-shot clip played when the item is collected.")]
        [SerializeField] private AudioClip _collectClip;
        [Range(0f, 1f)]
        [SerializeField] private float _collectVolume = 0.8f;
        [Tooltip("Assign the SFX mixer group so volume can be controlled from settings.")]
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [Header("Pitch Variation")]
        [SerializeField] private float _minPitch = 0.9f;
        [SerializeField] private float _maxPitch = 1.1f;

        [Header("Components")]
        [Tooltip("The SpriteRenderer to hide when collected.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [Tooltip("The Collider to disable when collected.")]
        [SerializeField] private Collider2D _triggerCollider;
        [Tooltip("An optional extra GameObject to disable when collected.")]
        [SerializeField] private GameObject _objectToDisable;

        [Header("Events")]
        public UnityEvent OnCollected;

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

                // Play collection sound before deactivating
                PlayCollectSound();

                // Hide the sprite and disable the collider instead of deactivating the whole object
                if (_spriteRenderer != null) _spriteRenderer.enabled = false;
                if (_triggerCollider != null) _triggerCollider.enabled = false;
                
                // Disable the extra GameObject if referenced
                if (_objectToDisable != null) _objectToDisable.SetActive(false);

                // Fire the Unity Event
                OnCollected?.Invoke();
            }
        }

        /// <summary>
        /// Plays the collection sound at this position.
        /// Uses PlayClipAtPoint so the sound survives the gameObject being deactivated.
        /// Routes through the SFX mixer group if assigned.
        /// </summary>
        private void PlayCollectSound()
        {
            if (_collectClip == null) return;

            // PlayClipAtPoint creates a temporary "One shot audio" GameObject
            AudioSource.PlayClipAtPoint(_collectClip, transform.position, _collectVolume);

            // Route the temporary AudioSource through the SFX mixer if assigned
            if (_sfxMixerGroup != null)
            {
                GameObject tempAudio = GameObject.Find("One shot audio");
                if (tempAudio != null)
                {
                    AudioSource source = tempAudio.GetComponent<AudioSource>();
                    if (source != null)
                        source.outputAudioMixerGroup = _sfxMixerGroup;
                }
            }
        }

        /// <summary>
        /// Plays the collection sound at this position with slight pitch variation.
        /// Can be called publicly.
        /// </summary>
        public void PlayCollectSoundWithPitch()
        {
            if (_collectClip == null) return;

            // Create a temporary GameObject to safely modify pitch and mixer
            GameObject tempAudio = new GameObject("CollectibleAudio");
            tempAudio.transform.position = transform.position;

            AudioSource source = tempAudio.AddComponent<AudioSource>();
            source.clip = _collectClip;
            source.volume = _collectVolume;
            source.pitch = Random.Range(_minPitch, _maxPitch);
            source.spatialBlend = 1f; // Make it 3D like PlayClipAtPoint

            if (_sfxMixerGroup != null)
            {
                source.outputAudioMixerGroup = _sfxMixerGroup;
            }

            source.Play();

            // Destroy the temporary GameObject after the sound finishes playing
            Destroy(tempAudio, _collectClip.length / source.pitch);
        }
    }
}
