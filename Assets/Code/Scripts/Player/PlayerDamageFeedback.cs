using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Player.GameState;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal;

namespace Player
{
    /// <summary>
    /// Drives three damage feedback effects on hit:
    ///   1. Sprite color flash  — tints all child SpriteRenderers via MaterialPropertyBlock
    ///   2. Vignette pulse      — animates a URP Volume's Vignette intensity
    ///   3. Cinemachine impulse — fires a CinemachineImpulseSource
    /// Subscribe to GameStateManager.onHpChange automatically on Enable.
    /// </summary>
    public class PlayerDamageFeedback : MonoBehaviour
    {
        [Header("Sprite Color Flash")]
        [Tooltip("Root of the player's sprite hierarchy. All child SpriteRenderers will be tinted.")]
        [SerializeField] private Transform _spriteRoot;
        [SerializeField] private Color _flashColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private float _flashDuration = 0.12f;
        [SerializeField] private float _flashFadeOut = 0.25f;

        [Header("Vignette Pulse")]
        [SerializeField] private Volume _postProcessVolume;
        [SerializeField] private float _vignetteIntensityPeak = 0.55f;
        [SerializeField] private float _vignetteFadeIn = 0.05f;
        [SerializeField] private float _vignetteFadeOut = 0.4f;

        [Header("Camera Shake")]
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        private SpriteRenderer[] _renderers;

        private Vignette _vignette;
        private float _vignetteBaseIntensity;

        private Coroutine _flashCoroutine;
        private Coroutine _vignetteCoroutine;

        private void Awake()
        {
            if (_spriteRoot != null)
            {
                _renderers = _spriteRoot.GetComponentsInChildren<SpriteRenderer>();
                
                // Clear any existing/stuck MaterialPropertyBlocks which distort SpriteSkin skeletal joints
                foreach (var sr in _renderers)
                {
                    if (sr != null)
                    {
                        sr.SetPropertyBlock(null);
                    }
                }
            }

            if (_postProcessVolume != null && _postProcessVolume.profile.TryGet(out _vignette))
                _vignetteBaseIntensity = _vignette.intensity.value;
        }

        private void OnEnable()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.onHpChange.AddListener(OnHpChange);
        }

        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.onHpChange.RemoveListener(OnHpChange);
        }

        private void OnHpChange(int previous, int current)
        {
            if (current >= previous) return; // healing — skip

            if (_renderers != null && _renderers.Length > 0)
            {
                if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                _flashCoroutine = StartCoroutine(FlashSprites());
            }

            if (_vignette != null)
            {
                if (_vignetteCoroutine != null) StopCoroutine(_vignetteCoroutine);
                _vignetteCoroutine = StartCoroutine(PulseVignette());
            }

            _impulseSource?.GenerateImpulse();
        }

        // ── Sprite flash ──────────────────────────────────────────────────────

        private IEnumerator FlashSprites()
        {
            // Hold flash color
            SetTint(_flashColor);
            yield return new WaitForSeconds(_flashDuration);

            // Fade back to white (no tint)
            float elapsed = 0f;
            while (elapsed < _flashFadeOut)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _flashFadeOut);
                SetTint(Color.Lerp(_flashColor, Color.white, t));
                yield return null;
            }

            SetTint(Color.white);
            _flashCoroutine = null;
        }

        private void SetTint(Color color)
        {
            foreach (var sr in _renderers)
            {
                if (sr == null) continue;
                sr.color = color;
            }
        }

        // ── Vignette pulse ────────────────────────────────────────────────────

        private IEnumerator PulseVignette()
        {
            // Fade in
            float elapsed = 0f;
            float startIntensity = _vignette.intensity.value;
            while (elapsed < _vignetteFadeIn)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _vignetteFadeIn);
                _vignette.intensity.Override(Mathf.Lerp(startIntensity, _vignetteIntensityPeak, t));
                yield return null;
            }

            _vignette.intensity.Override(_vignetteIntensityPeak);

            // Fade out
            elapsed = 0f;
            while (elapsed < _vignetteFadeOut)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _vignetteFadeOut);
                _vignette.intensity.Override(Mathf.Lerp(_vignetteIntensityPeak, _vignetteBaseIntensity, t));
                yield return null;
            }

            _vignette.intensity.Override(_vignetteBaseIntensity);
            _vignetteCoroutine = null;
        }
    }
}
