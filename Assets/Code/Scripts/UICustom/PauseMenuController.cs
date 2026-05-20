using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UICustom
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [Tooltip("The UI panel that represents the Pause Menu overlay.")]
        [SerializeField] private GameObject _pauseMenuPanel;

        [Tooltip("The back/resume button that will unpause the game when clicked.")]
        [SerializeField] private Button _backButton;

        private IPlayerInputManager _inputManager;
        private bool _isPaused = false;

        private void Start()
        {
            // Find the player's input manager in the scene without referencing internal types
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _inputManager = playerObj.GetComponent<IPlayerInputManager>() 
                    ?? playerObj.GetComponentInParent<IPlayerInputManager>() 
                    ?? playerObj.GetComponentInChildren<IPlayerInputManager>();
            }

            if (_inputManager == null)
            {
                foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (mb is IPlayerInputManager manager)
                    {
                        _inputManager = manager;
                        break;
                    }
                }
            }

            if (_inputManager != null && _inputManager.PauseAction != null)
            {
                // Subscribe to the escape/cancel press event
                _inputManager.PauseAction.performed += OnPauseToggle;
            }
            else
            {
                Debug.LogWarning("[PauseMenuController] Could not find IPlayerInputManager or PauseAction in the scene.");
            }

            // Subscribe to the back/resume button click event
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(ResumeGame);
            }

            // Ensure the pause menu panel is hidden initially
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_inputManager != null && _inputManager.PauseAction != null)
            {
                // Unsubscribe from the event when this component is destroyed
                _inputManager.PauseAction.performed -= OnPauseToggle;
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(ResumeGame);
            }
        }

        private void OnPauseToggle(InputAction.CallbackContext context)
        {
            TogglePause();
        }

        private void ResumeGame()
        {
            if (_isPaused)
            {
                TogglePause();
            }
        }

        /// <summary>
        /// Toggles the pause state of the game, setting the time scale and showing/hiding the pause panel.
        /// </summary>
        public void TogglePause()
        {
            _isPaused = !_isPaused;

            // Pause/resume time
            Time.timeScale = _isPaused ? 0f : 1f;

            // Show/hide the pause panel
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(_isPaused);
            }

            Debug.Log($"[PauseMenuController] Game is now {(_isPaused ? "PAUSED" : "RESUMED")}");
        }
    }
}
