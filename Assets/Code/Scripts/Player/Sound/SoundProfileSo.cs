using UnityEngine;

namespace Player.Sound
{
    /// <summary>
    /// Holds all audio clip references and tuning values for a character.
    /// Create separate instances for the Player and the AI via Assets > Create > Sound Profile.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSoundProfile", menuName = "Sound Profile")]
    public class SoundProfileSo : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Single footstep clip (used if array is empty).")]
        [UnityEngine.Serialization.FormerlySerializedAs("footstepLoop")]
        public AudioClip footstepClip;
        [Tooltip("Array of footstep clips for random variety.")]
        public AudioClip[] footstepClips;
        [Range(0f, 1f)] public float footstepVolume = 0.6f;

        [Header("Climbing")]
        [Tooltip("Looping clip played while climbing moss/walls.")]
        public AudioClip climbLoop;
        [Range(0f, 1f)] public float climbVolume = 0.5f;

        [Header("Idle")]
        [Tooltip("Periodic chirp / shuffle clips played at random while idle.")]
        public AudioClip[] idleChirps;
        [Range(0f, 1f)] public float idleChirpVolume = 0.4f;
        [Tooltip("Minimum seconds of idle before the first chirp plays.")]
        public float idleChirpInitialDelay = 3f;
        [Tooltip("Minimum seconds between chirps.")]
        public float idleChirpMinInterval = 4f;
        [Tooltip("Maximum seconds between chirps.")]
        public float idleChirpMaxInterval = 8f;

        [Header("Jump (Player only)")]
        public AudioClip jumpClip;
        [Range(0f, 1f)] public float jumpVolume = 0.7f;

        [Header("Landing (Player only)")]
        public AudioClip landClip;
        [Range(0f, 1f)] public float landVolume = 0.7f;

        [Header("Pulse (Player only)")]
        [Tooltip("Single fallback pulse sound (deprecated).")]
        public AudioClip pulseClip;
        [Tooltip("List of pulse sounds played sequentially (1-2-1-2-1-2...).")]
        public AudioClip[] pulseClips;
        [Range(0f, 1f)] public float pulseVolume = 0.8f;

        [Header("Singing (AI only)")]
        [Tooltip("Random singing clips played when the AI is off-camera.")]
        public AudioClip[] singingClips;
        [Range(0f, 1f)] public float singingVolume = 0.5f;
        [Tooltip("Minimum seconds between singing attempts.")]
        public float singingMinDelay = 8f;
        [Tooltip("Maximum seconds between singing attempts.")]
        public float singingMaxDelay = 20f;

        [Header("AI Actions")]
        [Tooltip("Looping clip for AI running/walking movement.")]
        public AudioClip runLoop;
        [Range(0f, 1f)] public float runVolume = 0.6f;
        [Tooltip("One-shot clip for AI eating.")]
        public AudioClip eatClip;
        [Range(0f, 1f)] public float eatVolume = 0.7f;
        [Tooltip("One-shot clip for AI tongue.")]
        public AudioClip tongueClip;
        [Range(0f, 1f)] public float tongueVolume = 0.7f;

        [Header("Pitch Variance")]
        [Tooltip("How much to randomize the pitch of one-shot SFX (0 = none, 0.1 = ±10%).")]
        [Range(0f, 0.3f)] public float pitchVariance = 0.05f;
    }
}
