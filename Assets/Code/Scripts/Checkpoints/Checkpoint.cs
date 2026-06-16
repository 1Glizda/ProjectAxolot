using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using Interactions;
using UnityEngine.Events;

namespace Player.GameState
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Activation Effect")]
        [Tooltip("List of sprites to fade in when activated.")]
        [SerializeField] private SpriteRenderer[] _spriteRenderers;
        [SerializeField] private float _fadeInDuration = 0.5f;
        [Tooltip("Delay in seconds between starting the fade of each sprite in the list.")]
        [SerializeField] private float _delayBetweenSprites = 0.2f;
        [Tooltip("Optional particle system to play when the checkpoint is first activated.")]
        [SerializeField] private ParticleSystem _activationParticles;

        [Header("Sound")]
        [Tooltip("One-shot clip played when the checkpoint is reached.")]
        [SerializeField] public AudioClip _activationClip;
        [Range(0f, 1f)]
        [SerializeField] private float _activationVolume = 0.8f;
        [Tooltip("Assign the SFX mixer group so volume can be controlled from settings.")]
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [Tooltip("0 for 2D (full volume everywhere), 1 for 3D (attenuates with distance). Default is 2D for checkpoints.")]
        [Range(0f, 1f)]
        [SerializeField] private float _spatialBlend = 0f;

        [Header("Events")]
        [Tooltip("Fired immediately when the checkpoint is reached/revealed.")]
        public UnityEvent OnCheckpointReveal;

        private CheckpointsManager _manager;
        private Color[] _initialColors;
        private AudioSource _audioSource;
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            if (_spriteRenderers != null && _spriteRenderers.Length > 0)
            {
                _initialColors = new Color[_spriteRenderers.Length];
                for (int i = 0; i < _spriteRenderers.Length; i++)
                {
                    if (_spriteRenderers[i] != null)
                    {
                        _initialColors[i] = _spriteRenderers[i].color;
                        // Start fully transparent so the sprite is hidden until activated
                        var clear = _initialColors[i];
                        clear.a = 0f;
                        _spriteRenderers[i].color = clear;
                    }
                }
            }

            // Set up audio source for checkpoint activation sound
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = _spatialBlend;
            _audioSource.maxDistance = 30f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            if (_sfxMixerGroup != null) _audioSource.outputAudioMixerGroup = _sfxMixerGroup;
        }

        public void Initialize(CheckpointsManager manager)
        {
            _manager = manager;
        }

        public void NotifyEnable()
        {
            Debug.Log($"[Checkpoint] NotifyEnable() called on {gameObject.name}. IsActivated: {IsActivated}");
            if (IsActivated) return;
            IsActivated = true;
            
            if (_activationParticles != null)
            {
                _activationParticles.Play();
            }
            
            OnCheckpointReveal?.Invoke();

            // Play activation sound
            if (_activationClip != null && _audioSource != null)
            {
                Debug.Log($"[Checkpoint] Playing clip {_activationClip.name} with volume {_activationVolume} (MixerGroup: {_audioSource.outputAudioMixerGroup})");
                _audioSource.PlayOneShot(_activationClip, _activationVolume);
            }
            else
            {
                Debug.LogWarning($"[Checkpoint] Cannot play sound on {gameObject.name}. Clip is null: {_activationClip == null}, AudioSource is null: {_audioSource == null}");
            }

            if (_spriteRenderers != null && _spriteRenderers.Length > 0)
                StartCoroutine(FadeInSequence());
        }

        private IEnumerator FadeInSequence()
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    StartCoroutine(FadeInSingle(i));
                }
                
                if (_delayBetweenSprites > 0f)
                {
                    yield return new WaitForSeconds(_delayBetweenSprites);
                }
            }
        }

        private IEnumerator FadeInSingle(int index)
        {
            float elapsed = 0f;
            var clear = _initialColors[index];
            clear.a = 0f;

            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeInDuration);
                _spriteRenderers[index].color = Color.Lerp(clear, _initialColors[index], t);
                yield return null;
            }

            _spriteRenderers[index].color = _initialColors[index];
        }

    }
}