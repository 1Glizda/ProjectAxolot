using System;
using UnityEngine;

namespace UICustom
{
    /// <summary>
    /// Singleton that tracks speedrun elapsed time.
    /// Lives on a UI Canvas GameObject in the scene.
    /// The timer is started explicitly by MainMenuBehaviour.OnStartGame().
    /// </summary>
    public class SpeedrunTimer : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────
        public static SpeedrunTimer Instance { get; private set; }

        // ── Runtime state ──────────────────────────────────────────────
        private bool _isRunning;
        private float _elapsedTime;

        // ── Public read-only accessors ─────────────────────────────────
        public float ElapsedTime => _elapsedTime;
        public bool IsRunning => _isRunning;

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
            StartTimer();
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

        /// <summary>Pause the timer without resetting.</summary>
        public void PauseTimer()
        {
            _isRunning = false;
        }

        /// <summary>Full reset — clears elapsed time.</summary>
        public void ResetTimer()
        {
            _isRunning = false;
            _elapsedTime = 0f;
        }

        /// <summary>
        /// Submit the current elapsed time to the leaderboard.
        /// Call this when the level is completed.
        /// </summary>
        public async void SubmitTime()
        {
            PauseTimer();
            try
            {
                await LeaderboardService.SubmitScoreAsync(_elapsedTime);
                Debug.Log($"[SpeedrunTimer] Submitted time {FormatTime(_elapsedTime)} to leaderboard.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpeedrunTimer] Failed to submit time: {e.Message}");
            }
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
