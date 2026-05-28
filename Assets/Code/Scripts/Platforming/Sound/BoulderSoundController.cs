using UnityEngine;
using UnityEngine.Audio;

public class BoulderSoundController : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Drop your movement sound here!")]
    public AudioClip movementSound;
    
    public float movementThreshold = 0.1f;

    [Header("Mixer")]
    [Tooltip("Assign the SFX mixer group so volume can be controlled from settings.")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    private AudioSource audioSource;
    private Rigidbody rb;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // Safely add it if it's missing
            audioSource = gameObject.AddComponent<AudioSource>(); 
        }
        
        audioSource.clip = movementSound;
        audioSource.loop = true;
        audioSource.spatialBlend = 1.0f;
        if (sfxMixerGroup != null) audioSource.outputAudioMixerGroup = sfxMixerGroup;

        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb != null)
        {
            if (rb.linearVelocity.magnitude > movementThreshold)
            {
                PlaySound();
            }
            else
            {
                StopSound();
            }
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