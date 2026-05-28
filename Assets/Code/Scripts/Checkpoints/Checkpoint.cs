using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Interactions;

namespace Player.GameState
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Activation Effect")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _fadeInDuration = 0.5f;

        private CheckpointsManager _manager;
        private Color _initialColor;
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            if (_spriteRenderer != null)
            {
                _initialColor = _spriteRenderer.color;
                // Start fully transparent so the sprite is hidden until activated
                var clear = _initialColor;
                clear.a = 0f;
                _spriteRenderer.color = clear;
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

            if (_spriteRenderer != null)
                StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            var clear = _initialColor;
            clear.a = 0f;

            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeInDuration);
                _spriteRenderer.color = Color.Lerp(clear, _initialColor, t);
                yield return null;
            }

            _spriteRenderer.color = _initialColor;
        }

    }
}