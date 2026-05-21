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
            if ( _playerController)
            {
                Vector2 position = _currentCheckpoint ?  _currentCheckpoint.transform.position : _startingCheckpoint.transform.position;
                _playerController.Teleport(position);
            }
        }
    }
}
