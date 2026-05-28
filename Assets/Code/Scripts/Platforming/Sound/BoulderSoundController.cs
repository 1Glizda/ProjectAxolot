using UnityEngine;
using UnityEngine.Audio;

namespace Platforming.Sound
{
    /// <summary>
    /// Boulder movement sound controller. Attach alongside a BoulderBehaviour component.
    /// Plays a looping movement sound while the boulder is moving above the velocity threshold.
    /// </summary>
    public class BoulderSoundController : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("Drop your movement sound here!")]
        public AudioClip movementSound;

        public float movementThreshold = 0.1f;

        [Header("Spatial")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float maxDistance = 25f;

        [Header("Mixer")]
        [Tooltip("Assign the SFX mixer group so volume can be controlled from settings.")]
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        private AudioSource audioSource;
        private Rigidbody2D rb;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = movementSound;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = spatialBlend;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            if (sfxMixerGroup != null) audioSource.outputAudioMixerGroup = sfxMixerGroup;

            rb = GetComponent<Rigidbody2D>();

            if (rb == null)
                Debug.LogWarning($"[{gameObject.name}] BoulderSoundController: No Rigidbody2D found. Sound will not play.", this);
            if (movementSound == null)
                Debug.LogWarning($"[{gameObject.name}] BoulderSoundController: No movementSound clip assigned.", this);
        }

        void Update()
        {
            if (rb == null) return;

            if (rb.linearVelocity.magnitude > movementThreshold)
            {
                PlaySound();
            }
            else
            {
                StopSound();
            }
        }

        public void PlaySound()
        {
            if (audioSource != null && !audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        public void StopSound()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}