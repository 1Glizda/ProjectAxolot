using UnityEngine;
using UnityEngine.Playables;

namespace UICustom
{
    public class MainMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _startGameSequence;
        [SerializeField] private GameObject _settingsMenu;
        [SerializeField] private GameObject _mainMenu;

        [Header("Debug Settings")]
        [SerializeField] private bool _debugMode;
        [SerializeField] private float _debugTimeScale = 20f;
        
        public void OnStartGame()
        {
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
            _mainMenu.SetActive(!toggle);
            _settingsMenu.SetActive(toggle);
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

