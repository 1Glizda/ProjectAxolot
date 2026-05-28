using System;
using System.Collections.Generic;
using UnityEngine;

using Interfaces;
using Player.GameState;

namespace Platforming
{
    public class BreakableWall : MonoBehaviour, IResettable
    {

        [SerializeField] private Collider2D _wallCollider;
        [SerializeField] private float _breakForceThreshold;

        [Tooltip("Optional sprite that will be hidden when the wall breaks.")]
        [SerializeField] private SpriteRenderer _spriteToHideOnBreak;

        /// <summary>Fired when the wall breaks. Subscribe from sound/VFX controllers.</summary>
        public event System.Action OnBreak;

        private Dictionary<Transform, int> _initialLayers = new Dictionary<Transform, int>();

        private void Awake()
        {
            Player.GameState.CheckpointsManager.RegisterResettable(this);

            foreach (var obj in GetComponentsInChildren<Transform>(true))
            {
                _initialLayers[obj] = obj.gameObject.layer;
            }
        }

        private void OnDestroy()
        {
            CheckpointsManager.UnregisterResettable(this);
        }

        public void TriggerReset()
        {
            _wallCollider.enabled = true;

            if (_spriteToHideOnBreak != null)
                _spriteToHideOnBreak.enabled = true;

            // Restore original layers
            foreach (var obj in GetComponentsInChildren<Transform>(true))
            {
                if (_initialLayers.TryGetValue(obj, out int initialLayer))
                {
                    obj.gameObject.layer = initialLayer;
                }
            }
        }

        public void TryBreak(float force, Vector2 direction)
        {
            if (force >= _breakForceThreshold)
            {
                Break(direction);
            }
        }

        public void Break(Vector2 direction)
        {
            _wallCollider.enabled = false;

            if (_spriteToHideOnBreak != null)
                _spriteToHideOnBreak.enabled = false;

            foreach (var obj in GetComponentsInChildren<Transform>())
            {
                obj.gameObject.layer = LayerMask.NameToLayer("Clutter");
            }

            OnBreak?.Invoke();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            float vel = other.relativeVelocity.magnitude;
            float mass = other.rigidbody.mass;
            float momentum = vel * mass;

            if (momentum >= _breakForceThreshold)
            {
                Break(other.relativeVelocity.normalized);
            }
        }
    }
}
