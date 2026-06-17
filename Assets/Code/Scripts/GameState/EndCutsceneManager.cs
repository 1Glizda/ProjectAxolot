using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace GameState
{
    public class EndCutsceneManager : MonoBehaviour
    {
        [SerializeField] private float _initialVideoDelay = 2f;
        [SerializeField] private VideoPlayer _videoPlayer;
        
        public UnityEvent onDelayElapsed;
        public UnityEvent onVideoEnded;

        public void StartPlaying()
        {
            _ = PlaySequence();
        }
        
        private async Task PlaySequence()
        {
            await Awaitable.WaitForSecondsAsync(_initialVideoDelay);
            _videoPlayer.gameObject.SetActive(true);
            onDelayElapsed?.Invoke();
            _videoPlayer.Play();
            _videoPlayer.loopPointReached += OnVideoEnded;
        }

        private void OnVideoEnded(VideoPlayer source)
        {
            _videoPlayer.loopPointReached -= OnVideoEnded;
            _videoPlayer.gameObject.SetActive(false);
            
            onVideoEnded?.Invoke();
        }

        
    }
}
