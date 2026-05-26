using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platforming
{
    public class BreakableWall : MonoBehaviour
    {

        [SerializeField] private Collider2D _wallCollider;
        [SerializeField] private List<Rigidbody2D> _wallPebbles;
        [SerializeField] private float _breakForceThreshold;

        /// <summary>Fired when the wall breaks. Subscribe from sound/VFX controllers.</summary>
        public event System.Action OnBreak;

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

            foreach (var rb in _wallPebbles)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.AddForce(direction.normalized * 20f, ForceMode2D.Impulse);
            }

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
