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

        public static UGSBootstrap Instance { get; private set; }
        public static bool IsReady { get; private set; }

        /// <summary>
        /// The display name (without the #XXXX suffix Unity appends).
        /// </summary>
        public static string PlayerDisplayName { get; private set; } = "Player";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            IsReady = false;
            PlayerDisplayName = "Player";
        }

        // AutoInitialize removed to prevent initialization before Leaderboard package registers.
        // It will now be spawned explicitly by other scripts.

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (IsReady) return;

            try
            {
                Debug.Log($"[UGS Diagnostics] UGSBootstrap Awake started. Current UnityServices.State: {UnityServices.State}");

                // 1. Initialize Unity Services
                if (UnityServices.State == ServicesInitializationState.Initialized)
                {
                    Debug.LogError("[CRITICAL ERROR] UnityServices is ALREADY initialized when the game started! This means Domain Reloading is definitely DISABLED in your Editor. UGS Leaderboards will crash. You MUST turn off 'Enter Play Mode Options' in Project Settings.");
                }
                else
                {
                    Debug.Log("[UGS Diagnostics] Calling UnityServices.InitializeAsync()...");
                    await UnityServices.InitializeAsync();
                }
                Debug.Log($"[UGS Diagnostics] Unity Services initialized. State is now: {UnityServices.State}");

                // 2. Sign in anonymously
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("[UGS Diagnostics] Not signed in. Calling SignInAnonymouslyAsync()...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                Debug.Log($"[UGS Diagnostics] Signed in. Player ID: {AuthenticationService.Instance.PlayerId}");

                // 3. Set default player name if not already set
                await EnsurePlayerNameAsync();

                // Workaround: Give Unity Services internal message bus time to register all package instances
                await System.Threading.Tasks.Task.Delay(500);

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
