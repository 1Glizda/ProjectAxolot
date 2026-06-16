using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UICustom
{
    public class MainMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _startGameSequence;
        [SerializeField] private GameObject _settingsMenu;
        [SerializeField] private GameObject _mainMenu;

        [Header("Leaderboard UI")]
        [Tooltip("The leaderboard button in the main menu to hide when game starts.")]
        [SerializeField] private GameObject _leaderboardButton;

        [Header("Username")]
        [Tooltip("Input field for the player's display name.")]
        [SerializeField] private TMP_InputField _usernameInput;
        [Tooltip("Button to confirm the username change.")]
        [SerializeField] private Button _usernameConfirmButton;
        [Tooltip("Text showing the current username.")]
        [SerializeField] private TextMeshProUGUI _currentUsernameText;

        [Header("Debug Settings")]
        [SerializeField] private bool _debugMode;
        [SerializeField] private float _debugTimeScale = 20f;

        private bool _isStarting;

        private void Awake()
        {
            EnsureUGSBootstrap();
        }

        private void EnsureUGSBootstrap()
        {
            if (UGSBootstrap.Instance == null)
            {
                GameObject go = new GameObject("UGSBootstrap (Auto)");
                go.AddComponent<UGSBootstrap>();
                DontDestroyOnLoad(go);
            }
        }

        private void Start()
        {
            // Wire username confirm button
            if (_usernameConfirmButton != null)
            {
                _usernameConfirmButton.onClick.AddListener(OnConfirmUsername);
            }

            // Show current name
            UpdateUsernameDisplay();
        }

        private void OnDestroy()
        {
            if (_usernameConfirmButton != null)
            {
                _usernameConfirmButton.onClick.RemoveListener(OnConfirmUsername);
            }
        }
        
        public void OnStartGame()
        {
            if (_isStarting) return;
            _isStarting = true;

            // Instantly hide the leaderboard button
            if (_leaderboardButton != null)
            {
                _leaderboardButton.SetActive(false);
            }

            if (_debugMode)
            {
                Time.timeScale = _debugTimeScale;
                _startGameSequence.stopped += OnSequenceStopped;
            }
            
            _startGameSequence.Play();

            // Start the speedrun timer the moment the player hits Start
            SpeedrunTimer.Instance?.StartTimer();
        }

        private void OnSequenceStopped(PlayableDirector director)
        {
            Time.timeScale = 1f;
            _startGameSequence.stopped -= OnSequenceStopped;
        }

        public void ToggleSettingsMenu(bool toggle)
        {
            _mainMenu.SetActive(!toggle);
            _settingsMenu.SetActive(toggle);
        }

        /// <summary>
        /// Load the Leaderboard scene. Wire this to a "Leaderboard" button in the menu.
        /// </summary>
        public void OnLeaderboard()
        {
            SceneManager.LoadScene("Leaderboard");
        }

        /// <summary>
        /// Called when the player confirms their username.
        /// </summary>
        private async void OnConfirmUsername()
        {
            if (_usernameInput == null) return;

            string newName = _usernameInput.text.Trim();
            if (string.IsNullOrEmpty(newName)) return;

            if (_usernameConfirmButton != null) _usernameConfirmButton.interactable = false;

            await UGSBootstrap.SetPlayerNameAsync(newName);
            UpdateUsernameDisplay();

            if (_usernameConfirmButton != null) _usernameConfirmButton.interactable = true;
            if (_usernameInput != null) _usernameInput.text = "";
        }

        private void UpdateUsernameDisplay()
        {
            if (_currentUsernameText != null)
            {
                _currentUsernameText.text = $"Playing as: {UGSBootstrap.PlayerDisplayName}";
            }
        }
        
        public void OnQuit()
        {
            Debug.Log("Quitting.", this);
            
            #if !UNITY_EDITOR
            Application.Quit();
            #endif
        }
    }
}
