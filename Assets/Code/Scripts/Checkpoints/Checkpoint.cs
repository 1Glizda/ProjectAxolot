using UnityEngine;
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

        [Header("Events")]
        [Tooltip("Fired immediately when the checkpoint is reached/revealed.")]
        public UnityEvent OnCheckpointReveal;

        private CheckpointsManager _manager;
        private Color[] _initialColors;
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
        }

        public void Initialize(CheckpointsManager manager)
        {
            _manager = manager;
        }

        public void NotifyEnable()
        {
            if (IsActivated) return;
            IsActivated = true;
            
            OnCheckpointReveal?.Invoke();

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