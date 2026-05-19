using UnityEngine;
using UnityEngine.Playables;

namespace UICustom
{
    public class MainMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private PlayableDirector _startGameSequence;
        [SerializeField] private GameObject _settingsMenu;
        [SerializeField] private GameObject _mainMenu;
        
        
        public void OnStartGame()
        {
            _startGameSequence.Play();
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
