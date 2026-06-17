using System;
using System.Collections.Generic;
using LootLocker.Requests;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameState
{
    public class LeaderboardsHandler : MonoBehaviour
    {
        public static LeaderboardsHandler Instance;
        
        [SerializeField] private TMP_InputField _playerNameInput;
        [SerializeField] private GameObject _confirmButton;
        [SerializeField] private GameObject _lbDisplay;
        
        private LeaderboardEntry _localEntry = new();


        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        
        public void LogAnonymous()
        {
            LootLockerSDKManager.StartGuestSession((response) => {
                if (response.success)
                {
                    Debug.Log("Player logged in anonymously.");
                }});
        }

        public void PlaceholderNameInputField()
        {
            _playerNameInput.gameObject.SetActive(true);
            _confirmButton.gameObject.SetActive(true);
            
            _playerNameInput.text = $"Gleam#{Random.Range(1000, 9999)}";;
        }

        public void SubmitScore()
        {
            LeaderboardEntry entry = new LeaderboardEntry();

            int deaths = GameStateManager.Instance.TotalDeaths;
            float time = GameStateManager.Instance.TotalTimePlayed;

            entry.playerName = _playerNameInput.text;
            entry.timeScore = Mathf.CeilToInt(time);
            entry.deaths = deaths;

            string metadataString = JsonUtility.ToJson(entry);
            LootLockerSDKManager.SubmitScore(entry.playerName, entry.timeScore, "34934", metadataString, (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Player score submitted.");
                }
            });
            
            _confirmButton.SetActive(false);
            _playerNameInput.gameObject.SetActive(false);
            
            _lbDisplay.SetActive(true);
        }

        public static void GetLeaderboard(Action<List<LeaderboardEntry>> onLeaderboardFetched)
        {
            LootLockerSDKManager.GetScoreList("34934", 50, 0, (response) =>
            {
                List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

                if (response.success)
                {
                    foreach (var scoreEntry in response.items)
                    {
                        int score = scoreEntry.score;
                        if (!string.IsNullOrEmpty(scoreEntry.metadata))
                        {
                            LeaderboardEntry entry = JsonUtility.FromJson<LeaderboardEntry>(scoreEntry.metadata);
                            entries.Add(entry);
                        }
                    }
                }
                
                onLeaderboardFetched?.Invoke(entries);
            });
        }
        
    }

    [System.Serializable]
    public struct LeaderboardEntry
    {
        public string playerName;
        public int timeScore;
        public int deaths;
    }
    
}
