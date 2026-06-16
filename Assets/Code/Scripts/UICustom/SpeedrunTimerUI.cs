using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UICustom
{
    /// <summary>
    /// Minimal UI overlay that shows only the live speedrun timer.
    /// Press T to toggle the panel visibility.
    /// </summary>
    public class SpeedrunTimerUI : MonoBehaviour
    {
        // ── Inspector references ───────────────────────────────────────
        [Header("Timer Display")]
        [Tooltip("The live-updating timer text (MM:SS.mmm).")]
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("Panel")]
        [Tooltip("The root GameObject of the timer panel. Toggled on/off with T key.")]
        [SerializeField] private GameObject _timerPanel;

        // ── Runtime ────────────────────────────────────────────────────
        private SpeedrunTimer _timer;

        // ── Unity lifecycle ────────────────────────────────────────────

        private void Start()
        {
            _timer = SpeedrunTimer.Instance;

            if (_timer == null)
            {
                Debug.LogWarning("[SpeedrunTimerUI] No SpeedrunTimer instance found in scene.");
            }

            // Start visible by default
            if (_timerPanel != null)
            {
                _timerPanel.SetActive(true);
            }
        }

        private void Update()
        {
            // ── T key toggle ───────────────────────────────────────────
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                if (_timerPanel != null)
                {
                    _timerPanel.SetActive(!_timerPanel.activeSelf);
                }
            }

            // ── Live timer update ──────────────────────────────────────
            if (_timer == null)
            {
                _timer = SpeedrunTimer.Instance;
            }
            if (_timer == null || _timerText == null) return;

            _timerText.text = SpeedrunTimer.FormatTime(_timer.ElapsedTime);
        }
    }
}
