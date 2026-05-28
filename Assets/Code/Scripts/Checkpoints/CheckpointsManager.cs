using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Player.GameState
{
    [DefaultExecutionOrder(-5)]
    public class CheckpointsManager : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private Checkpoint _startingCheckpoint;

        [Header("Death Transition")]
        [Tooltip("Seconds to wait after death before resetting the scene. Use this for a fade-out or death animation.")]
        [SerializeField] private float _preResetDelay = 0.5f;
        [Tooltip("Seconds to wait after the scene is reset before re-enabling the player. Use this for a fade-in.")]
        [SerializeField] private float _postResetDelay = 0.3f;
        [Tooltip("Seconds to wait after the fade-in finishes before fully unlocking the player controls.")]
        [SerializeField] private float _postReviveDelay = 0.2f;

        [Header("Events")]
        [Tooltip("Fired immediately on death, before any delay. Hook up your screen fade-out here.")]
        public UnityEvent OnDeathStart;
        [Tooltip("Fired after the Pre Reset Delay completes, right before the room is actually reset.")]
        public UnityEvent OnDeathFinished;
        [Tooltip("Fired after the scene has been reset and the player has been teleported. Hook up your fade-in here.")]
        public UnityEvent OnReviveStart;
        [Tooltip("Fired after the post-reset delay finishes. Use this when the fade-in is complete.")]
        public UnityEvent OnReviveFinished;
        [Tooltip("Fired after the post-revive delay finishes. Use this to unlock the player controls slightly later.")]
        public UnityEvent OnPlayerUnlocked;

        private Checkpoint _currentCheckpoint;

        private static HashSet<Interfaces.IResettable> _resettables = new HashSet<Interfaces.IResettable>();

        public static void RegisterResettable(Interfaces.IResettable resettable)
        {
            if (resettable != null) _resettables.Add(resettable);
        }

        public static void UnregisterResettable(Interfaces.IResettable resettable)
        {
            if (resettable != null) _resettables.Remove(resettable);
        }

        private void Awake()
        {
            // Static fields survive between editor Play sessions, so we must clear
            // stale entries every time the scene starts to prevent ghost references
            // from accumulating across play sessions or scene reloads.
            _resettables.Clear();

            _currentCheckpoint = _startingCheckpoint;
            if (_playerController == null)
            {
                _playerController = FindFirstObjectByType<PlayerController>();
            }

            Checkpoint[] checkpoints = FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Checkpoint checkpoint in checkpoints)
            {
                checkpoint.Initialize(this);
            }
        }

        private void OnEnable()
        {
            GameStateManager.Instance.onDeath.AddListener(OnDeath);
        }

        private void OnDisable()
        {
            GameStateManager.Instance.onDeath.RemoveListener(OnDeath);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Checkpoints"))
            {
                if (other.gameObject.TryGetComponent<Checkpoint>(out var checkpoint))
                {
                    _currentCheckpoint = checkpoint;
                    checkpoint.NotifyEnable();
                }
            }
        }

        private void OnDeath()
        {
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            Checkpoint respawnAt = _currentCheckpoint ? _currentCheckpoint : _startingCheckpoint;

            if (respawnAt == null)
            {
                Debug.LogError("[CheckpointsManager] OnDeath: no checkpoint to respawn at — assign _startingCheckpoint in the inspector!", this);
                yield break;
            }

            // 1. Notify UI immediately so it can start fading out
            OnDeathStart?.Invoke();

            // 2. Wait — gives time for fade-out animation AND lets physics settle
            //    naturally without forcing SyncTransforms
            if (_preResetDelay > 0f)
                yield return new WaitForSeconds(_preResetDelay);

            OnDeathFinished?.Invoke();

            // 3. Reset the world while the screen is black / faded
            foreach (var resettable in _resettables.ToList())
            {
                if (resettable != null && resettable is MonoBehaviour mb && mb != null)
                {
                    resettable.TriggerReset();
                }
            }

            // 4. Teleport the player (TeleportRoutine already waits a WaitForFixedUpdate internally)
            _playerController.Teleport(respawnAt.transform.position);

            // 5. Scene is clean — tell UI to fade back in
            OnReviveStart?.Invoke();

            // 6. Wait for a post-reset window — physics bodies fully settle across
            //    multiple fixed update frames and UI finishes fading in
            if (_postResetDelay > 0f)
                yield return new WaitForSeconds(_postResetDelay);

            // 7. Fade-in is complete
            OnReviveFinished?.Invoke();

            // 8. Wait for a small buffer before fully unlocking controls
            if (_postReviveDelay > 0f)
                yield return new WaitForSeconds(_postReviveDelay);

            OnPlayerUnlocked?.Invoke();
        }
    }
}
