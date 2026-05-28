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
                respawnAt.ResetSavedObjects();
            }
        }
    }
}
