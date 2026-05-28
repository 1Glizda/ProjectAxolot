using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace UICustom
{
    /// <summary>
    /// Controls SFX and Ambient volume via two UI Sliders connected to an AudioMixer.
    /// Attach to the Settings panel. Assign the mixer and sliders in the Inspector.
    ///
    /// Setup:
    /// 1. Create an AudioMixer with exposed parameters named "SFXVolume" and "AmbientVolume"
    /// 2. Add two Sliders to your settings UI
    /// 3. Drag the AudioMixer and Sliders into this component's fields
    /// </summary>
    public class AudioSettingsController : MonoBehaviour
    {
        [Header("Mixer")]
        [Tooltip("The master AudioMixer containing the SFX and Ambient groups.")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("UI Sliders")]
        [Tooltip("Slider that controls the SFX volume (0 to 1).")]
        [SerializeField] private Slider sfxSlider;

        [Tooltip("Slider that controls the Ambient volume (0 to 1).")]
        [SerializeField] private Slider ambientSlider;

        [Header("Mixer Parameter Names")]
        [Tooltip("The exposed parameter name for SFX volume on the AudioMixer.")]
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        [Tooltip("The exposed parameter name for Ambient volume on the AudioMixer.")]
        [SerializeField] private string ambientVolumeParam = "AmbientVolume";

        private const string SfxPrefKey = "AudioSettings_SFXVolume";
        private const string AmbientPrefKey = "AudioSettings_AmbientVolume";

        private void Start()
        {
            // Load saved preferences (default to full volume)
            float savedSfx = PlayerPrefs.GetFloat(SfxPrefKey, 1f);
            float savedAmbient = PlayerPrefs.GetFloat(AmbientPrefKey, 1f);

            if (sfxSlider != null)
            {
                sfxSlider.minValue = 0.0001f;
                sfxSlider.maxValue = 1f;
                sfxSlider.value = savedSfx;
                sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            }

            if (ambientSlider != null)
            {
                ambientSlider.minValue = 0.0001f;
                ambientSlider.maxValue = 1f;
                ambientSlider.value = savedAmbient;
                ambientSlider.onValueChanged.AddListener(SetAmbientVolume);
            }

            // Apply saved volumes immediately
            ApplyVolume(sfxVolumeParam, savedSfx);
            ApplyVolume(ambientVolumeParam, savedAmbient);
        }

        private void OnDestroy()
        {
            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);

            if (ambientSlider != null)
                ambientSlider.onValueChanged.RemoveListener(SetAmbientVolume);
        }

        /// <summary>
        /// Called by the SFX slider's OnValueChanged event.
        /// </summary>
        public void SetSFXVolume(float value)
        {
            ApplyVolume(sfxVolumeParam, value);
            PlayerPrefs.SetFloat(SfxPrefKey, value);
        }

        /// <summary>
        /// Called by the Ambient slider's OnValueChanged event.
        /// </summary>
        public void SetAmbientVolume(float value)
        {
            ApplyVolume(ambientVolumeParam, value);
            PlayerPrefs.SetFloat(AmbientPrefKey, value);
        }

        /// <summary>
        /// Converts a linear slider value (0–1) to decibels and applies it to the mixer.
        /// Uses a logarithmic scale so the slider feels natural:
        ///   0.0001 → -80 dB (silence)
        ///   1.0    →   0 dB (full volume)
        /// </summary>
        private void ApplyVolume(string paramName, float linearValue)
        {
            if (audioMixer == null)
            {
                Debug.LogWarning("[AudioSettingsController] AudioMixer is not assigned!", this);
                return;
            }

            // Clamp to avoid log(0)
            linearValue = Mathf.Clamp(linearValue, 0.0001f, 1f);
            float dB = Mathf.Log10(linearValue) * 20f;
            bool success = audioMixer.SetFloat(paramName, dB);

            if (!success)
            {
                Debug.LogError($"[AudioSettingsController] SetFloat FAILED for param '{paramName}'. " +
                               $"Make sure the parameter is exposed in the AudioMixer and the name matches exactly.", this);
            }
            else
            {
                Debug.Log($"[AudioSettingsController] Set '{paramName}' to {dB:F1} dB (slider: {linearValue:F3})", this);
            }
        }
    }
}
