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

        private void OnCollisionEnter2D(Collision2D other)
        {
            float vel = other.relativeVelocity.magnitude;
            float mass = other.rigidbody.mass;
            float momentum = vel * mass;

            if (momentum >= _breakForceThreshold)
            {
                _wallCollider.enabled = false;
                
                //TODO move logic to pebble maybe?
                foreach (var rb in _wallPebbles)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.AddForce(other.relativeVelocity.normalized * 20f, ForceMode2D.Impulse);
                }
            }
            
        }
    }
}
