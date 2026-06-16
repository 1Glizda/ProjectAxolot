using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UICustom
{
    /// <summary>
    /// Singleton that tracks speedrun elapsed time and records checkpoint splits.
    /// Lives on a UI Canvas GameObject in the scene.
    /// The timer starts automatically when this component awakens (scene load).
    /// </summary>
    public class SpeedrunTimer : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────
        public static SpeedrunTimer Instance { get; private set; }

        // ── Split data ─────────────────────────────────────────────────
        [Serializable]
        public struct SplitData
        {
            /// <summary>Display name taken from the checkpoint GameObject.</summary>
            public string Name;
            /// <summary>Total elapsed time at the moment the checkpoint was reached.</summary>
            public float SplitTime;
            /// <summary>Time between this split and the previous one (or start).</summary>
            public float SegmentTime;
        }

        // ── Events ─────────────────────────────────────────────────────
        [Header("Events")]
        [Tooltip("Fired every time a new checkpoint split is recorded.")]
        public UnityEvent<SplitData> OnSplitRegistered;

        // ── Runtime state ──────────────────────────────────────────────
        private readonly List<SplitData> _splits = new List<SplitData>();
        private bool _isRunning;
        private float _elapsedTime;

        // ── Public read-only accessors ─────────────────────────────────
        public IReadOnlyList<SplitData> Splits => _splits;
        public float ElapsedTime => _elapsedTime;
        public bool IsRunning => _isRunning;
        public int CheckpointCount => _splits.Count;

        // ── Unity lifecycle ────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Timer is started explicitly via StartTimer() when the player hits Start Game.
            // Do NOT auto-start here.
        }

        private void Update()
        {
            if (_isRunning)
            {
                _elapsedTime += Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────

        /// <summary>Start (or resume) the timer.</summary>
        public void StartTimer()
        {
            _isRunning = true;
        }

        /// <summary>Pause the timer without clearing splits.</summary>
        public void PauseTimer()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Register a checkpoint split. Called automatically by Checkpoint.NotifyEnable().
        /// Only the first call per checkpoint name is recorded (duplicate protection).
        /// </summary>
        public void RegisterCheckpoint(string checkpointName)
        {
            // Guard against duplicate splits (shouldn't happen because NotifyEnable
            // is guarded by IsActivated, but belt-and-braces)
            for (int i = 0; i < _splits.Count; i++)
            {
                if (_splits[i].Name == checkpointName) return;
            }

            float previousSplit = _splits.Count > 0 ? _splits[_splits.Count - 1].SplitTime : 0f;

            var split = new SplitData
            {
                Name = checkpointName,
                SplitTime = _elapsedTime,
                SegmentTime = _elapsedTime - previousSplit
            };

            _splits.Add(split);
            Debug.Log($"[SpeedrunTimer] Split #{_splits.Count}: {split.Name} @ {FormatTime(split.SplitTime)} (segment {FormatTime(split.SegmentTime)})");
            OnSplitRegistered?.Invoke(split);
        }

        /// <summary>Full reset — clears elapsed time and all recorded splits.</summary>
        public void ResetTimer()
        {
            _isRunning = false;
            _elapsedTime = 0f;
            _splits.Clear();
        }

        // ── Formatting helper ──────────────────────────────────────────

        /// <summary>Format seconds into MM:SS.mmm</summary>
        public static string FormatTime(float totalSeconds)
        {
            if (totalSeconds < 0f) totalSeconds = 0f;
            int minutes = (int)(totalSeconds / 60f);
            int seconds = (int)(totalSeconds % 60f);
            int millis = (int)((totalSeconds * 1000f) % 1000f);
            return $"{minutes:00}:{seconds:00}.{millis:000}";
        }
    }
}
