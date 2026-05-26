using UnityEngine;

namespace Interfaces
{
    /// <summary>
    /// Centralised ScriptableObject holding ALL environment SFX clips and volumes.
    /// Create via Assets > Create > Environment Sound Profile.
    /// Assign a single instance to every environment sound controller in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "Environment Sound Profile", menuName = "Environment Sound Profile")]
    public class EnvironmentSoundProfileSo : ScriptableObject
    {
        // ─── Water Droplets ────────────────────────────────────────
        [Header("Water Droplets")]
        [Tooltip("Looping ambient drip clip.")]
        public AudioClip waterDropletsLoop;
        [Range(0f, 1f)] public float waterDropletsVolume = 0.4f;

        // ─── Wind ──────────────────────────────────────────────────
        [Header("Wind")]
        [Tooltip("First wind whoosh clip.")]
        public AudioClip windWhoosh1;
        [Tooltip("Second wind whoosh clip.")]
        public AudioClip windWhoosh2;
        [Range(0f, 1f)] public float windVolume = 0.5f;
        [Tooltip("Minimum seconds between wind whooshes.")]
        public float windMinInterval = 3f;
        [Tooltip("Maximum seconds between wind whooshes.")]
        public float windMaxInterval = 8f;
        [Range(0f, 0.3f)] public float windPitchVariance = 0.1f;

        // ─── Rock Explosion ────────────────────────────────────────
        [Header("Rock Explosion")]
        [Tooltip("One-shot clip for breakable wall explosion.")]
        public AudioClip rockExplosionClip;
        [Range(0f, 1f)] public float rockExplosionVolume = 0.8f;
        [Range(0f, 0.3f)] public float rockExplosionPitchVariance = 0.05f;

        // ─── Geyser ───────────────────────────────────────────────
        [Header("Geyser")]
        [Tooltip("One-shot played when geyser erupts.")]
        public AudioClip geyserOnClip;
        [Tooltip("One-shot played when geyser deactivates.")]
        public AudioClip geyserOffClip;
        [Tooltip("Optional looping rumble while geyser is active.")]
        public AudioClip geyserActiveLoop;
        [Range(0f, 1f)] public float geyserOneShotVolume = 0.7f;
        [Range(0f, 1f)] public float geyserLoopVolume = 0.4f;

        // ─── Mushroom ──────────────────────────────────────────────
        [Header("Mushroom")]
        [Tooltip("One-shot played on mushroom explosion.")]
        public AudioClip mushroomExplosionClip;
        [Tooltip("Looping spore cloud sound while exploded.")]
        public AudioClip mushroomCloudLoop;
        [Range(0f, 1f)] public float mushroomExplosionVolume = 0.8f;
        [Range(0f, 1f)] public float mushroomCloudVolume = 0.3f;

        // ─── Moss ──────────────────────────────────────────────────
        [Header("Moss")]
        [Tooltip("One-shot played on contact with moss.")]
        public AudioClip mossContactClip;
        [Tooltip("Optional ambient loop while on moss.")]
        public AudioClip mossAmbientLoop;
        [Range(0f, 1f)] public float mossContactVolume = 0.6f;
        [Range(0f, 1f)] public float mossAmbientVolume = 0.3f;

        // ─── Spike / Damage ────────────────────────────────────────
        [Header("Spike (Player Damage)")]
        [Tooltip("One-shot played when player is hit by a spike/hazard.")]
        public AudioClip spikeDamageClip;
        [Range(0f, 1f)] public float spikeDamageVolume = 0.8f;
        [Range(0f, 0.3f)] public float spikePitchVariance = 0.05f;
    }
}
