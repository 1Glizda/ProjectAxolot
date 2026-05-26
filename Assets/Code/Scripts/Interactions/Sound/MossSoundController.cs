using Interfaces;
using UnityEngine;

namespace Interactions.Sound
{
    /// <summary>
    /// Moss sound controller. Attach to moss objects (MossBehaviour / TemporaryMossBehaviour).
    /// Plays a one-shot squish when the player contacts the moss, and optionally
    /// loops an ambient sound while the player stays on it.
    /// All audio clips are centralised in the EnvironmentSoundProfileSo.
    /// </summary>
    public class MossSoundController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnvironmentSoundProfileSo soundProfile;

        [Header("Spatial")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float maxDistance = 15f;

        private AudioSource _oneShotSource;
        private AudioSource _loopSource;
        private int _playersOnMoss;

        private void Awake()
        {
            _oneShotSource = gameObject.AddComponent<AudioSource>();
            _oneShotSource.playOnAwake = false;
            _oneShotSource.spatialBlend = spatialBlend;
            _oneShotSource.maxDistance = maxDistance;
            _oneShotSource.rolloffMode = AudioRolloffMode.Linear;

            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
            _loopSource.spatialBlend = spatialBlend;
            _loopSource.maxDistance = maxDistance;
            _loopSource.rolloffMode = AudioRolloffMode.Linear;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!IsPlayerOrAi(other.collider)) return;
            HandleContact();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerOrAi(other)) return;
            HandleContact();
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (!IsPlayerOrAi(other.collider)) return;
            HandleExit();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerOrAi(other)) return;
            HandleExit();
        }

        private void HandleContact()
        {
            if (soundProfile == null) return;

            _playersOnMoss++;

            // Play contact one-shot
            if (soundProfile.mossContactClip != null)
                _oneShotSource.PlayOneShot(soundProfile.mossContactClip, soundProfile.mossContactVolume);

            // Start ambient loop if not already playing
            if (soundProfile.mossAmbientLoop != null && !_loopSource.isPlaying)
            {
                _loopSource.clip = soundProfile.mossAmbientLoop;
                _loopSource.volume = soundProfile.mossAmbientVolume;
                _loopSource.Play();
            }
        }

        private void HandleExit()
        {
            _playersOnMoss = Mathf.Max(0, _playersOnMoss - 1);

            if (_playersOnMoss == 0 && _loopSource.isPlaying)
            {
                _loopSource.Stop();
            }
        }

        private bool IsPlayerOrAi(Collider2D col)
        {
            return col.CompareTag("Player") || col.CompareTag("AI");
        }
    }
}
