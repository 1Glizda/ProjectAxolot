using UnityEngine;
using UnityEngine.Playables;

namespace UICustom
{
    public class MainMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _startGameSequence;
        [SerializeField] private GameObject _settingsMenu;
        [SerializeField] private GameObject _mainMenu;
        [SerializeField] private GameObject _creditsReel;

        [Header("Debug Settings")]
        [SerializeField] private bool _debugMode;
        [SerializeField] private float _debugTimeScale = 20f;

        private bool _isStarting;
        private bool _hasStarted;

        public bool HasStarted => _hasStarted;
        
        public void OnStartGame()
        {
            if (_isStarting) return;
            _isStarting = true;
            _hasStarted = true;

            if (_debugMode)
            {
                Time.timeScale = _debugTimeScale;
                _startGameSequence.stopped += OnSequenceStopped;
            }
            
            _startGameSequence.Play();
        }

        private void OnSequenceStopped(PlayableDirector director)
        {
            Time.timeScale = 1f;
            _startGameSequence.stopped -= OnSequenceStopped;
        }

        public void ToggleSettingsMenu(bool toggle)
        {
            if (!_hasStarted)
            {
                _mainMenu.SetActive(!toggle);
            }
            _settingsMenu.SetActive(toggle);
        }

        public void ToggleCreditsReel(bool toggle)
        {
            if (!_hasStarted)
            {
                _mainMenu.SetActive(!toggle);
            }
            _creditsReel.SetActive(toggle);
        }

        public bool CloseActiveSubMenu()
        {
            if (_settingsMenu != null && _settingsMenu.activeSelf)
            {
                ToggleSettingsMenu(false);
                return true;
            }
            if (_creditsReel != null && _creditsReel.activeSelf)
            {
                ToggleCreditsReel(false);
                return true;
            }
            return false;
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

