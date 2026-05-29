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
        
        [Tooltip("Optional GameObject that will be activated when the wall breaks (e.g. a particle effect or hidden path).")]
        [SerializeField] private GameObject _objectToActivateOnBreak;

        /// <summary>Fired when the wall breaks. Subscribe from sound/VFX controllers.</summary>
        public event System.Action OnBreak;

        private class ChildState
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public int Layer;
            public Rigidbody2D Rb;
            public RigidbodyType2D InitialBodyType;
            public bool InitialSimulated;
        }

        private Dictionary<Transform, ChildState> _childStates = new Dictionary<Transform, ChildState>();

        private void Awake()
        {
            Player.GameState.CheckpointsManager.RegisterResettable(this);

            foreach (var obj in GetComponentsInChildren<Transform>(true))
            {
                var rb = obj.GetComponent<Rigidbody2D>();
                _childStates[obj] = new ChildState
                {
                    LocalPosition = obj.localPosition,
                    LocalRotation = obj.localRotation,
                    Layer = obj.gameObject.layer,
                    Rb = rb,
                    InitialBodyType = rb != null ? rb.bodyType : RigidbodyType2D.Dynamic,
                    InitialSimulated = rb != null ? rb.simulated : false
                };
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

            if (_objectToActivateOnBreak != null)
                _objectToActivateOnBreak.SetActive(false);

            // Restore original transforms, layers, and rigidbodies
            foreach (var kvp in _childStates)
            {
                var obj = kvp.Key;
                var state = kvp.Value;

                if (obj == null) continue;

                obj.gameObject.layer = state.Layer;
                obj.localPosition = state.LocalPosition;
                obj.localRotation = state.LocalRotation;

                if (state.Rb != null)
                {
                    state.Rb.bodyType = state.InitialBodyType;
                    state.Rb.simulated = state.InitialSimulated;
                    state.Rb.linearVelocity = Vector2.zero;
                    state.Rb.angularVelocity = 0f;
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

            if (_objectToActivateOnBreak != null)
                _objectToActivateOnBreak.SetActive(true);

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
