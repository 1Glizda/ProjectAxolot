using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace UI
{
    [RequireComponent(typeof(VideoPlayer))]
    public class SplashVideoController : MonoBehaviour
    {
        [Tooltip("The exact name of the scene to load after the video finishes")]
        [SerializeField] private string _nextSceneName = "MainMenu";
        
        [Tooltip("If true, any key press or mouse click will skip the intro video")]
        [SerializeField] private bool _allowSkip = true;

        private VideoPlayer _videoPlayer;

        private void Start()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            
            // Subscribe to the loopPointReached event (which fires when the video ends)
            _videoPlayer.loopPointReached += OnVideoFinished;
        }

        private void Update()
        {
            if (!_allowSkip) return;

            // Let the player press any key/click to skip the intro
            if (Input.anyKeyDown)
            {
                LoadNextScene();
            }
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            LoadNextScene();
        }

        private void LoadNextScene()
        {
            // Unsubscribe to be safe
            _videoPlayer.loopPointReached -= OnVideoFinished;
            SceneManager.LoadScene(_nextSceneName);
        }
    }
}
