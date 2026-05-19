using System.Collections;
using UnityEngine;

namespace Platforming
{
    public class GeyserBehaviour : MonoBehaviour
    {
        public enum GeyserState { Inactive, Active, Blocked }

        [Header("Components")]
        [SerializeField] private Transform _platform;
        [Tooltip("Contains the stream sprite and a trigger collider for KnockbackHazard")]
        [SerializeField] private GameObject _streamObject; 
        
        [Header("Settings")]
        [SerializeField] private float _activeHeight = 3f;
        [SerializeField] private float _boostedHeight = 5f;
        [SerializeField] private float _activeDuration = 2f;
        [SerializeField] private float _inactiveDuration = 2f;
        [SerializeField] private float _activationTranslationDuration = 0.5f;
        [SerializeField] private float _inactivationTranslationDuration = 1.0f;

        [Header("Linked Geyser (Optional)")]
        [Tooltip("If assigned, blocking this geyser will boost the linked one.")]
        [SerializeField] private GeyserBehaviour _linkedGeyser;

        public bool IsBoosted { get; set; }
        public GeyserState CurrentState { get; private set; } = GeyserState.Inactive;

        private int _blockingMovableCount = 0;
        private float _targetHeight = 0f;
        private Vector3 _platformInitialLocalPos;
        private Vector3 _streamInitialLocalPos;
        private int _movableLayerMask;

        private void Awake()
        {
            _movableLayerMask = LayerMask.GetMask("Movable");
        }

        private void Start()
        {
            if (_platform != null)
            {
                _platformInitialLocalPos = _platform.localPosition;
            }
            
            if (_streamObject != null)
            {
                _streamInitialLocalPos = _streamObject.transform.localPosition;
                _streamObject.SetActive(true);
            }

            StartCoroutine(GeyserLoop());
        }

        private IEnumerator GeyserLoop()
        {
            while (true)
            {
                if (_blockingMovableCount > 0)
                {
                    CurrentState = GeyserState.Blocked;
                    _targetHeight = 0f;
                    
                    if (_linkedGeyser != null)
                    {
                        _linkedGeyser.IsBoosted = true;
                    }
                    
                    while (_blockingMovableCount > 0)
                    {
                        yield return null; // Wait until unblocked
                    }
                    continue;
                }

                // Normal behavior: Inactive -> Active -> Inactive
                if (_linkedGeyser != null && CurrentState == GeyserState.Blocked)
                {
                     // Reset boost if we just unblocked
                    _linkedGeyser.IsBoosted = false;
                }

                CurrentState = GeyserState.Inactive;
                _targetHeight = 0f;
                
                float t = 0f;
                while (t < _inactiveDuration && _blockingMovableCount == 0)
                {
                    t += Time.deltaTime;
                    yield return null;
                }

                // Check again before becoming active in case it was blocked during the inactive phase
                if (_blockingMovableCount > 0) continue;

                CurrentState = GeyserState.Active;
                
                t = 0f;
                while (t < _activeDuration && _blockingMovableCount == 0)
                {
                    _targetHeight = IsBoosted ? _boostedHeight : _activeHeight;
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }

        private void FixedUpdate()
        {
            if (_platform != null)
            {
                Vector3 targetLocalPos = _platformInitialLocalPos + Vector3.up * _targetHeight;
                
                float targetMaxHeight = IsBoosted ? _boostedHeight : _activeHeight;
                float currentDuration = (CurrentState == GeyserState.Active) ? _activationTranslationDuration : _inactivationTranslationDuration;
                float speed = targetMaxHeight / Mathf.Max(0.001f, currentDuration);

                Rigidbody2D rb = _platform.GetComponent<Rigidbody2D>();
                Vector3 newLocalPos;
                if (rb != null)
                {
                    Vector3 currentWorldPos = rb.position;
                    Vector3 currentLocalPos = _platform.parent != null ? _platform.parent.InverseTransformPoint(currentWorldPos) : currentWorldPos;
                    
                    newLocalPos = Vector3.MoveTowards(currentLocalPos, targetLocalPos, speed * Time.fixedDeltaTime);
                    Vector3 newWorldPos = _platform.parent != null ? _platform.parent.TransformPoint(newLocalPos) : newLocalPos;
                    
                    Vector2 velocity = ((Vector2)newWorldPos - rb.position) / Time.fixedDeltaTime;
                    rb.linearVelocity = velocity;
                    
                    rb.MovePosition(newWorldPos);
                }
                else
                {
                    newLocalPos = Vector3.MoveTowards(_platform.localPosition, targetLocalPos, speed * Time.fixedDeltaTime);
                    _platform.localPosition = newLocalPos;
                }

                // Move the stream sprite object to match platform height
                if (_streamObject != null)
                {
                    float currentHeight = newLocalPos.y - _platformInitialLocalPos.y;
                    _streamObject.transform.localPosition = _streamInitialLocalPos + Vector3.up * currentHeight;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _movableLayerMask) != 0)
            {
                _blockingMovableCount++;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _movableLayerMask) != 0)
            {
                _blockingMovableCount--;
                if (_blockingMovableCount < 0) _blockingMovableCount = 0;
            }
        }
    }
}
