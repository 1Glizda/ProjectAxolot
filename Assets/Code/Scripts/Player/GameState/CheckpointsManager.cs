using System;
using UnityEngine;
using UnityEngine.Events;

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
        }
        
        private void OnEnable()
        {
            GameStateManager.Instance.onDeath.AddListener(OnDeath);
        }

        private void OnDisable()
        {
            GameStateManager.Instance.onDeath.RemoveListener(OnDeath);
        }

        private void OnCollisionEnter2D(Collision2D other)
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
            if (_currentCheckpoint && _playerController)
            {
                _playerController.Teleport(_currentCheckpoint.transform.position);
            }
        }
    }
}
