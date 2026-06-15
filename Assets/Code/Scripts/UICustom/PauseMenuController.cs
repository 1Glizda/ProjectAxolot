using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UICustom
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject _pauseMenuPanel;

        [SerializeField] private Button _backButton;

        private PlayerController _playerController;
        private IPlayerInputHandler _inputHandler;
        private bool _isPaused = false;

        private void Start()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _playerController = playerObj.GetComponent<PlayerController>()
                    ?? playerObj.GetComponentInParent<PlayerController>()
                    ?? playerObj.GetComponentInChildren<PlayerController>();
                _inputHandler = playerObj.GetComponent<IPlayerInputHandler>() 
                    ?? playerObj.GetComponentInParent<IPlayerInputHandler>() 
                    ?? playerObj.GetComponentInChildren<IPlayerInputHandler>();
            }

            if (_inputHandler == null)
            {
                foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (mb is IPlayerInputHandler manager)
                    {
                        _inputHandler = manager;
                        break;
                    }
                }
            }

            if (_playerController == null)
            {
                _playerController = FindFirstObjectByType<PlayerController>();
            }

            if (_inputHandler != null && _inputHandler.PauseAction != null)
            {
                _inputHandler.PauseAction.performed += OnPauseToggle;
            }
            else
            {
                Debug.LogWarning("[PauseMenuController] Could not find IPlayerInputManager or PauseAction in the scene.");
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(ResumeGame);
            }

            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_inputHandler != null && _inputHandler.PauseAction != null)
            {
                _inputHandler.PauseAction.performed -= OnPauseToggle;
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(ResumeGame);
            }
        }

        private void OnPauseToggle(InputAction.CallbackContext context)
        {
            if (_playerController != null && _playerController.IsLocked)
            {
                return;
            }
            TogglePause();
        }

        private void ResumeGame()
        {
            if (_isPaused)
            {
                TogglePause();
            }
        }

        
        public void TogglePause()
        {
            _isPaused = !_isPaused;

            Time.timeScale = _isPaused ? 0f : 1f;

            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(_isPaused);
            }

            Debug.Log($"[PauseMenuController] Game is now {(_isPaused ? "PAUSED" : "RESUMED")}");
        }
    }
}
