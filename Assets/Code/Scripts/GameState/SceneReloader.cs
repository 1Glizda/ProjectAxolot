using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameState
{
    public class SceneReloader : MonoBehaviour
    {
        /// <summary>
        /// Reloads the currently active scene.
        /// Call this manually via UnityEvents or UI buttons.
        /// </summary>
        public void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }

        /// <summary>
        /// Loads a specific scene by its Build Index (e.g. 0 for Main Menu).
        /// </summary>
        public void LoadSceneByIndex(int buildIndex)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(buildIndex);
        }

        /// <summary>
        /// Loads a specific scene by its name.
        /// </summary>
        public void LoadSceneByName(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
