using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace UICustom
{
    /// <summary>
    /// Initializes Unity Gaming Services and signs in anonymously on scene load.
    /// Generates a default player name if one hasn't been set.
    /// Place this on a persistent GameObject in your first scene.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class UGSBootstrap : MonoBehaviour
    {
        private const string PLAYER_NAME_PREF_KEY = "UGS_PlayerName";

        public static bool IsReady { get; private set; }

        /// <summary>
        /// The display name (without the #XXXX suffix Unity appends).
        /// </summary>
        public static string PlayerDisplayName { get; private set; } = "Player";

        private async void Awake()
        {
            if (IsReady) return;

            try
            {
                // 1. Initialize Unity Services
                await UnityServices.InitializeAsync();
                Debug.Log("[UGSBootstrap] Unity Services initialized.");

                // 2. Sign in anonymously
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[UGSBootstrap] Signed in. Player ID: {AuthenticationService.Instance.PlayerId}");
                }

                // 3. Set default player name if not already set
                await EnsurePlayerNameAsync();

                IsReady = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UGSBootstrap] Failed to initialize: {e.Message}");
            }
        }

        private async System.Threading.Tasks.Task EnsurePlayerNameAsync()
        {
            try
            {
                // Check if user already has a saved custom name
                string savedName = PlayerPrefs.GetString(PLAYER_NAME_PREF_KEY, "");

                if (!string.IsNullOrEmpty(savedName) && !savedName.StartsWith("Axolotl-"))
                {
                    // Player previously set a name — use it
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(savedName);
                    PlayerDisplayName = savedName;
                    Debug.Log($"[UGSBootstrap] Restored player name: {savedName}");
                }
                else
                {
                    // Generate a default name from the player ID
                    string playerId = AuthenticationService.Instance.PlayerId;
                    string shortId = playerId.Length >= 5 ? playerId.Substring(0, 5) : playerId;
                    string defaultName = $"Gleam-{shortId}";

                    await AuthenticationService.Instance.UpdatePlayerNameAsync(defaultName);
                    PlayerPrefs.SetString(PLAYER_NAME_PREF_KEY, defaultName);
                    PlayerPrefs.Save();
                    PlayerDisplayName = defaultName;
                    Debug.Log($"[UGSBootstrap] Generated default name: {defaultName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UGSBootstrap] Could not set player name: {e.Message}");
                PlayerDisplayName = "Player";
            }
        }

        /// <summary>
        /// Update the player's display name. Call this from the username UI.
        /// </summary>
        public static async System.Threading.Tasks.Task SetPlayerNameAsync(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;

            // Unity Auth doesn't allow spaces in names
            newName = newName.Replace(" ", "-");

            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
                PlayerPrefs.SetString(PLAYER_NAME_PREF_KEY, newName);
                PlayerPrefs.Save();
                PlayerDisplayName = newName;
                Debug.Log($"[UGSBootstrap] Player name updated to: {newName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UGSBootstrap] Failed to update name: {e.Message}");
            }
        }
    }
}
