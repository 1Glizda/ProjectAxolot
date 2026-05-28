using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Player.GameState
{
    public class CheckpointsManager : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private Checkpoint _startingCheckpoint;
        
        private Checkpoint _currentCheckpoint;
        
        private static HashSet<Interfaces.IResettable> _resettables = new HashSet<Interfaces.IResettable>();

        public static void RegisterResettable(Interfaces.IResettable resettable)
        {
            if (resettable != null) _resettables.Add(resettable);
        }

        public static void UnregisterResettable(Interfaces.IResettable resettable)
        {
            if (resettable != null) _resettables.Remove(resettable);
        }


        private void Awake()
        {
            _currentCheckpoint = _startingCheckpoint;
            if (_playerController == null)
            {
                _playerController = FindFirstObjectByType<PlayerController>();
            }

            Checkpoint[] checkpoints = FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Checkpoint checkpoint in checkpoints)
            {
                checkpoint.Initialize(this);
            }
        }
        
        private void OnEnable()
        {
            GameStateManager.Instance.onDeath.AddListener(OnDeath);
        }

        private void OnDisable()
        {
            GameStateManager.Instance.onDeath.RemoveListener(OnDeath);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Checkpoints"))
            {
                if (other.gameObject.TryGetComponent<Checkpoint>(out var checkpoint))
                {
                    _currentCheckpoint = checkpoint;
                    checkpoint.NotifyEnable();
                }
            }
        }
        
        private void OnDeath()
        {
            Debug.Log($"[CheckpointsManager] OnDeath fired. Current checkpoint: '{(_currentCheckpoint ? _currentCheckpoint.name : "none")}'  Starting: '{(_startingCheckpoint ? _startingCheckpoint.name : "none")}'", this);
            if ( _playerController)
            {
                Checkpoint respawnAt = _currentCheckpoint ? _currentCheckpoint : _startingCheckpoint;

                if (respawnAt == null)
                {
                    Debug.LogError("[CheckpointsManager] OnDeath: no checkpoint to respawn at — assign _startingCheckpoint in the inspector!", this);
                    return;
                }

                _playerController.Teleport(respawnAt.transform.position);
                
                // Reset all globally registered objects. 
                // We use .ToList() to create a snapshot because TriggerReset might spawn/destroy 
                // objects, which modifies the _resettables HashSet during iteration.
                foreach (var resettable in _resettables.ToList())
                {
                    // Double check in case an object was destroyed but failed to unregister
                    if (resettable != null && resettable is MonoBehaviour mb && mb != null)
                    {
                        resettable.TriggerReset();
                    }
                }
            }
        }
    }
}
