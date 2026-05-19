using System;
using System.Collections;
using UnityEngine;

namespace Player.AI
{
    public class AiBurrowController : MonoBehaviour
    {
        [Header("Burrow Timing")]
        [Tooltip("How long to wait for the burrow-down animation clip to finish.")]
        [SerializeField] private float burrowDownDuration = 0.4f;

        [Tooltip("Delay while underground before surfacing. 0 = instant teleport after burrow.")]
        [SerializeField] private float undergroundDelay = 0f;

        [Tooltip("How long to wait for the surface-up animation clip to finish.")]
        [SerializeField] private float surfaceUpDuration = 0.4f;

        [Tooltip("Delay after surfacing before the AI starts moving again. 0 = immediate.")]
        public float resumeDelay = 0f;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Rigidbody2D rb;

        [Header("Animator Parameters")]
        [SerializeField] private string burrowDownTrigger = "BurrowDown";
        [SerializeField] private string surfaceUpTrigger = "SurfaceUp";

        /// <summary>True while the AI is in any phase of the burrow sequence.</summary>
        public bool IsBurrowing { get; private set; }

        /// <summary>Fired when the full burrow sequence completes and the AI is ready to move again.</summary>
        public event Action OnBurrowComplete;

        private Coroutine _activeSequence;

        /// <summary>
        /// Starts the full burrow-teleport-surface sequence.
        /// </summary>
        /// <param name="destination">World position to teleport to.</param>
        public void StartBurrow(Vector2 destination)
        {
            if (IsBurrowing) return;

            if (_activeSequence != null)
                StopCoroutine(_activeSequence);

            _activeSequence = StartCoroutine(BurrowSequence(destination));
        }

        private IEnumerator BurrowSequence(Vector2 destination)
        {
            IsBurrowing = true;

            // --- Phase 1: Burrow Down ---
            // Freeze physics so the AI doesn't slide
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;

            // Fire the burrow-down animation and wait for it to play
            if (animator != null)
                animator.SetTrigger(burrowDownTrigger);

            yield return new WaitForSeconds(burrowDownDuration);

            // --- Phase 2: Underground ---
            // Hide the character and disable collision (invulnerable)
            spriteRenderer.enabled = false;
            if (bodyCollider != null) bodyCollider.enabled = false;

            // Teleport
            transform.position = destination;

            // Optional delay
            if (undergroundDelay > 0f)
                yield return new WaitForSeconds(undergroundDelay);

            // --- Phase 3: Surface Up ---
            // Show the character at the destination
            spriteRenderer.enabled = true;

            // Fire the surface-up animation and wait for it to play
            if (animator != null)
                animator.SetTrigger(surfaceUpTrigger);

            yield return new WaitForSeconds(surfaceUpDuration);

            // --- Phase 4: Post-Surface Wait ---
            if (resumeDelay > 0f)
                yield return new WaitForSeconds(resumeDelay);

            // --- Phase 5: Resume ---
            if (bodyCollider != null) bodyCollider.enabled = true;
            rb.simulated = true;

            IsBurrowing = false;
            OnBurrowComplete?.Invoke();
            _activeSequence = null;
        }
    }
}
