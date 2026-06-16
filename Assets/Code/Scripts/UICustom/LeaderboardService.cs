using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;

namespace UICustom
{
    /// <summary>
    /// Static wrapper around Unity Leaderboards API.
    /// Leaderboard ID: "GleamAGDgameY1" (lowest-to-highest, best score).
    /// </summary>
    public static class LeaderboardService
    {
        public const string LEADERBOARD_ID = "GleamAGDgameY1";
        public const int PAGE_SIZE = 20;

        /// <summary>
        /// Submit the player's completion time (in seconds) to the leaderboard.
        /// Since the leaderboard uses "best score" update type, only the lowest
        /// time will be kept automatically.
        /// </summary>
        public static async Task SubmitScoreAsync(float timeInSeconds)
        {
            try
            {
                double score = Math.Round(timeInSeconds, 3);
                var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(
                    LEADERBOARD_ID, score);
                Debug.Log($"[LeaderboardService] Score submitted: {score}s (Rank #{entry.Rank + 1})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] Submit failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Fetch a page of leaderboard entries.
        /// </summary>
        /// <param name="offset">Number of entries to skip (0-based).</param>
        /// <param name="limit">Number of entries to return.</param>
        /// <returns>A LeaderboardScoresPage with Results and Total.</returns>
        public static async Task<LeaderboardScoresPage> GetScoresPageAsync(int offset, int limit)
        {
            try
            {
                var options = new GetScoresOptions
                {
                    Offset = offset,
                    Limit = limit
                };
                return await LeaderboardsService.Instance.GetScoresAsync(LEADERBOARD_ID, options);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] GetScores failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get the current player's own leaderboard entry.
        /// Returns null if the player has no entry.
        /// </summary>
        public static async Task<LeaderboardEntry> GetPlayerScoreAsync()
        {
            try
            {
                return await LeaderboardsService.Instance.GetPlayerScoreAsync(LEADERBOARD_ID);
            }
            catch (LeaderboardsException e)
            {
                // Player has no entry yet — this is expected, not an error
                if (e.Message.Contains("not found") || e.Message.Contains("404"))
                {
                    return null;
                }
                Debug.LogError($"[LeaderboardService] GetPlayerScore failed: {e.Message}");
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardService] GetPlayerScore failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Strip the #XXXX suffix that Unity Authentication appends to player names.
        /// </summary>
        public static string CleanPlayerName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Unknown";
            int hashIndex = rawName.LastIndexOf('#');
            return hashIndex > 0 ? rawName.Substring(0, hashIndex) : rawName;
        }
    }
}
