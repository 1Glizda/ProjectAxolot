using Interfaces;
using UnityEngine;

namespace Platforming
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BoulderBehaviour : MonoBehaviour, IPushable
    {
        public Rigidbody2D Rb => _rb;
        public Vector2 Velocity => _rb.linearVelocity;
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private float _maxSpeed = 3f;
        
        private void Awake()
        {
            if (!_rb)
            {
                _rb = GetComponent<Rigidbody2D>();
            }
            _rb.bodyType = RigidbodyType2D.Dynamic;
        }
        
        public void ApplyPushForce(Vector2 force)
        {
            Vector2 dir = force.normalized;
            float forceMag = force.magnitude;
            
            float projectedVel = Vector2.Dot(_rb.linearVelocity, dir);

            float maxDeltaV = Mathf.Max(0, _maxSpeed - projectedVel);
            float desiredDeltaV = force.magnitude / _rb.mass * Time.fixedDeltaTime;
            float actualDeltaV = Mathf.Min(desiredDeltaV, maxDeltaV);

            if (actualDeltaV > 0)
            {
                Vector2 computedForce = dir * (actualDeltaV * _rb.mass / Time.fixedDeltaTime);
                _rb.AddForce(computedForce, ForceMode2D.Force);
            }
        }

        /*private void FixedUpdate()
        {
            if (Mathf.Abs(_rb.linearVelocityX) > _maxSpeed)
            {
                _rb.linearVelocityX = Mathf.Sign(_rb.linearVelocityX) * _maxSpeed;
            }
        }*/
    }
}
