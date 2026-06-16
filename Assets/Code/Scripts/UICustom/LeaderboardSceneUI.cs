using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards.Models;

namespace UICustom
{
    /// <summary>
    /// Controller for the Leaderboard scene.
    /// Fetches and displays paginated leaderboard entries (20 per page).
    /// </summary>
    public class LeaderboardSceneUI : MonoBehaviour
    {
        [Header("Entry List")]
        [Tooltip("Parent transform with a Vertical Layout Group for entry rows.")]
        [SerializeField] private RectTransform _entryListContent;
        [Tooltip("Prefab for a leaderboard entry row. Must have children named 'RankText', 'NameText', 'TimeText'.")]
        [SerializeField] private GameObject _entryRowPrefab;

        [Header("Pagination")]
        [SerializeField] private Button _prevPageButton;
        [SerializeField] private Button _nextPageButton;
        [SerializeField] private TextMeshProUGUI _pageIndicatorText;

        [Header("Player Info")]
        [Tooltip("Text showing the current player's own rank and time.")]
        [SerializeField] private TextMeshProUGUI _playerInfoText;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;
        [Tooltip("Scene name to return to when Back is pressed.")]
        [SerializeField] private string _returnSceneName = "Main_Level";

        [Header("Loading")]
        [SerializeField] private GameObject _loadingIndicator;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Colors")]
        [SerializeField] private Color _normalRowColor = new Color(1f, 1f, 1f, 0.05f);
        [SerializeField] private Color _highlightRowColor = new Color(0.3f, 0.8f, 1f, 0.15f);
        [SerializeField] private Color _normalTextColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color _highlightTextColor = new Color(0.3f, 0.9f, 1f, 1f);

        // ── Runtime state ──────────────────────────────────────────────
        private int _currentPage = 0;
        private int _totalEntries = 0;
        private int _totalPages = 1;
        private bool _isLoading = false;
        private string _currentPlayerId;
        private readonly List<GameObject> _activeRows = new List<GameObject>();

        private int TotalPages => Mathf.Max(1, Mathf.CeilToInt((float)_totalEntries / LeaderboardService.PAGE_SIZE));

        // ── Unity lifecycle ────────────────────────────────────────────

        private void Start()
        {
            // Get current player ID for highlighting
            try
            {
                _currentPlayerId = AuthenticationService.Instance.PlayerId;
            }
            catch
            {
                _currentPlayerId = "";
            }

            // Wire buttons
            if (_prevPageButton != null) _prevPageButton.onClick.AddListener(PreviousPage);
            if (_nextPageButton != null) _nextPageButton.onClick.AddListener(NextPage);
            if (_backButton != null) _backButton.onClick.AddListener(GoBack);

            // Load first page
            _currentPage = 0;
            FetchPage();
            FetchPlayerScore();
        }

        private void OnDestroy()
        {
            if (_prevPageButton != null) _prevPageButton.onClick.RemoveListener(PreviousPage);
            if (_nextPageButton != null) _nextPageButton.onClick.RemoveListener(NextPage);
            if (_backButton != null) _backButton.onClick.RemoveListener(GoBack);
        }

        // ── Pagination ─────────────────────────────────────────────────

        private void PreviousPage()
        {
            if (_currentPage > 0 && !_isLoading)
            {
                _currentPage--;
                FetchPage();
            }
        }

        private void NextPage()
        {
            if (_currentPage < _totalPages - 1 && !_isLoading)
            {
                _currentPage++;
                FetchPage();
            }
        }

        // ── Data fetching ──────────────────────────────────────────────

        private async void FetchPage()
        {
            _isLoading = true;
            SetLoading(true);

            try
            {
                int offset = _currentPage * LeaderboardService.PAGE_SIZE;
                var page = await LeaderboardService.GetScoresPageAsync(offset, LeaderboardService.PAGE_SIZE);

                _totalEntries = page.Total;
                _totalPages = TotalPages;

                ClearRows();
                PopulateRows(page.Results);
                UpdatePaginationUI();

                if (_statusText != null)
                {
                    _statusText.text = _totalEntries == 0 ? "No scores yet. Be the first!" : "";
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardSceneUI] Failed to fetch page: {e.Message}");
                if (_statusText != null)
                {
                    _statusText.text = "Failed to load leaderboard. Check your connection.";
                }
            }
            finally
            {
                _isLoading = false;
                SetLoading(false);
            }
        }

        private async void FetchPlayerScore()
        {
            try
            {
                var playerEntry = await LeaderboardService.GetPlayerScoreAsync();
                if (_playerInfoText != null)
                {
                    if (playerEntry != null)
                    {
                        string name = LeaderboardService.CleanPlayerName(playerEntry.PlayerName);
                        string time = SpeedrunTimer.FormatTime((float)playerEntry.Score);
                        _playerInfoText.text = $"Your Best: #{playerEntry.Rank + 1}  {name}  {time}";
                    }
                    else
                    {
                        _playerInfoText.text = "You haven't submitted a time yet.";
                    }
                }
            }
            catch
            {
                if (_playerInfoText != null)
                {
                    _playerInfoText.text = "You haven't submitted a time yet.";
                }
            }
        }

        // ── UI building ────────────────────────────────────────────────

        private void ClearRows()
        {
            foreach (var row in _activeRows)
            {
                if (row != null) Destroy(row);
            }
            _activeRows.Clear();
        }

        private void PopulateRows(System.Collections.Generic.List<LeaderboardEntry> entries)
        {
            if (_entryRowPrefab == null || _entryListContent == null) return;

            foreach (var entry in entries)
            {
                GameObject row = Instantiate(_entryRowPrefab, _entryListContent);
                row.SetActive(true);

                // Find child texts by name
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
                TextMeshProUGUI rankText = null;
                TextMeshProUGUI nameText = null;
                TextMeshProUGUI timeText = null;

                foreach (var t in texts)
                {
                    switch (t.gameObject.name)
                    {
                        case "RankText": rankText = t; break;
                        case "NameText": nameText = t; break;
                        case "TimeText": timeText = t; break;
                    }
                }

                bool isCurrentPlayer = entry.PlayerId == _currentPlayerId;
                Color textColor = isCurrentPlayer ? _highlightTextColor : _normalTextColor;

                if (rankText != null)
                {
                    rankText.text = $"#{entry.Rank + 1}";
                    rankText.color = textColor;
                }

                if (nameText != null)
                {
                    nameText.text = LeaderboardService.CleanPlayerName(entry.PlayerName);
                    nameText.color = textColor;
                }

                if (timeText != null)
                {
                    timeText.text = SpeedrunTimer.FormatTime((float)entry.Score);
                    timeText.color = textColor;
                }

                // Highlight current player's row background
                var rowImage = row.GetComponent<Image>();
                if (rowImage != null)
                {
                    rowImage.color = isCurrentPlayer ? _highlightRowColor : _normalRowColor;
                }

                _activeRows.Add(row);
            }
        }

        private void UpdatePaginationUI()
        {
            if (_pageIndicatorText != null)
            {
                _pageIndicatorText.text = $"Page {_currentPage + 1} / {_totalPages}";
            }

            if (_prevPageButton != null) _prevPageButton.interactable = _currentPage > 0;
            if (_nextPageButton != null) _nextPageButton.interactable = _currentPage < _totalPages - 1;
        }

        private void SetLoading(bool loading)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(loading);
            }
        }

        // ── Navigation ─────────────────────────────────────────────────

        private void GoBack()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_returnSceneName);
        }
    }
}
