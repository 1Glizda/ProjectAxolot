using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UICustom
{
    /// <summary>
    /// UI overlay that renders the speedrun timer, checkpoint counter,
    /// and split list — styled like a compact LiveSplit panel.
    /// 
    /// Attach this to a Canvas that is set to Screen Space – Overlay
    /// so it stays visible at all times during gameplay.
    /// </summary>
    public class SpeedrunTimerUI : MonoBehaviour
    {
        // ── Inspector references ───────────────────────────────────────
        [Header("Timer Display")]
        [Tooltip("The large live-updating timer text (MM:SS.mmm).")]
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("Checkpoint Counter")]
        [Tooltip("Text showing how many checkpoints have been reached.")]
        [SerializeField] private TextMeshProUGUI _checkpointCounterText;

        [Header("Split List")]
        [Tooltip("Parent transform where split row instances are spawned.")]
        [SerializeField] private RectTransform _splitListContent;

        [Tooltip("Prefab for a single split row. Must contain two TMP_Text children named 'SplitName' and 'SplitTime'.")]
        [SerializeField] private GameObject _splitRowPrefab;

        [Header("Animation")]
        [Tooltip("Duration of the highlight flash when a new split appears.")]
        [SerializeField] private float _highlightDuration = 0.6f;
        [Tooltip("Highlight color for new split rows.")]
        [SerializeField] private Color _highlightColor = new Color(0.2f, 1f, 0.4f, 1f);
        [Tooltip("Normal text color for split rows after highlight.")]
        [SerializeField] private Color _normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);

        // ── Runtime ────────────────────────────────────────────────────
        private SpeedrunTimer _timer;
        private readonly List<GameObject> _splitRows = new List<GameObject>();

        // ── Unity lifecycle ────────────────────────────────────────────

        private void Start()
        {
            _timer = SpeedrunTimer.Instance;

            if (_timer != null)
            {
                _timer.OnSplitRegistered.AddListener(OnSplitRegistered);

                // Rebuild any splits that were registered before this UI initialised
                foreach (var existingSplit in _timer.Splits)
                {
                    CreateSplitRow(existingSplit, animate: false);
                }
            }
            else
            {
                Debug.LogWarning("[SpeedrunTimerUI] No SpeedrunTimer instance found in scene.");
            }

            UpdateCounterText();
        }

        private void OnDestroy()
        {
            if (_timer != null)
            {
                _timer.OnSplitRegistered.RemoveListener(OnSplitRegistered);
            }
        }

        private void Update()
        {
            if (_timer == null || !_timer.IsRunning) return;

            // Update the live timer every frame
            if (_timerText != null)
            {
                _timerText.text = SpeedrunTimer.FormatTime(_timer.ElapsedTime);
            }
        }

        // ── Split event handler ────────────────────────────────────────

        private void OnSplitRegistered(SpeedrunTimer.SplitData split)
        {
            CreateSplitRow(split, animate: true);
            UpdateCounterText();
        }

        // ── UI helpers ─────────────────────────────────────────────────

        private void UpdateCounterText()
        {
            if (_checkpointCounterText == null) return;

            int count = _timer != null ? _timer.CheckpointCount : 0;
            _checkpointCounterText.text = $"CP {count}";
        }

        private void CreateSplitRow(SpeedrunTimer.SplitData split, bool animate)
        {
            if (_splitRowPrefab == null || _splitListContent == null) return;

            GameObject row = Instantiate(_splitRowPrefab, _splitListContent);
            row.SetActive(true);

            // Find child texts by name convention
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI nameText = null;
            TextMeshProUGUI timeText = null;

            foreach (var t in texts)
            {
                if (t.gameObject.name == "SplitName") nameText = t;
                else if (t.gameObject.name == "SplitTime") timeText = t;
            }

            // Friendly display name: strip common prefixes and clean up
            string displayName = split.Name;

            if (nameText != null)
            {
                nameText.text = displayName;
                nameText.color = animate ? _highlightColor : _normalColor;
            }

            if (timeText != null)
            {
                timeText.text = SpeedrunTimer.FormatTime(split.SplitTime);
                timeText.color = animate ? _highlightColor : _normalColor;
            }

            _splitRows.Add(row);

            if (animate)
            {
                StartCoroutine(HighlightRow(nameText, timeText));
            }
        }

        private IEnumerator HighlightRow(TextMeshProUGUI nameText, TextMeshProUGUI timeText)
        {
            float elapsed = 0f;

            while (elapsed < _highlightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _highlightDuration);
                Color current = Color.Lerp(_highlightColor, _normalColor, t);

                if (nameText != null) nameText.color = current;
                if (timeText != null) timeText.color = current;

                yield return null;
            }

            if (nameText != null) nameText.color = _normalColor;
            if (timeText != null) timeText.color = _normalColor;
        }
    }
}
