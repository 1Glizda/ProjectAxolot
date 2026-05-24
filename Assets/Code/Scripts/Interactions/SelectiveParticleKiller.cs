using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class SelectiveParticleKiller : MonoBehaviour
{
    [Tooltip("The layer(s) that should instantly kill the particles.")]
    [SerializeField] private LayerMask _lethalLayer;

    private ParticleSystem _ps;
    private List<ParticleCollisionEvent> _collisionEvents;
    private ParticleSystem.Particle[] _particles;

    private void Start()
    {
        _ps = GetComponent<ParticleSystem>();
        _collisionEvents = new List<ParticleCollisionEvent>();
        _particles = new ParticleSystem.Particle[_ps.main.maxParticles];
    }

    private void OnParticleCollision(GameObject other)
    {
        // 1. Check if the object we hit matches the layer (using .value for safety)
        if (((1 << other.layer) & _lethalLayer.value) != 0)
        {
            int numCollisionEvents = _ps.GetCollisionEvents(other, _collisionEvents);
            int numParticlesAlive = _ps.GetParticles(_particles);
            
            bool isLocalSpace = _ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;

            for (int i = 0; i < numCollisionEvents; i++)
            {
                // FORCE TO 2D: Ignore Z-axis on the collision point
                Vector2 collisionPos = _collisionEvents[i].intersection;

                float closestDist = float.MaxValue;
                int closestIndex = -1;

                for (int j = 0; j < numParticlesAlive; j++)
                {
                    // FORCE TO 2D: Ignore Z-axis on the particle position
                    Vector2 particleWorldPos = isLocalSpace 
                        ? transform.TransformPoint(_particles[j].position) 
                        : _particles[j].position;

                    // Calculate distance purely based on X and Y
                    float dist = Vector2.SqrMagnitude(particleWorldPos - collisionPos);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestIndex = j;
                    }
                }

                // Kill the closest particle
                if (closestIndex != -1)
                {
                    _particles[closestIndex].remainingLifetime = 0f; 
                }
            }

            // Apply back to the system
            _ps.SetParticles(_particles, numParticlesAlive);
        }
    }
}