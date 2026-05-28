using UnityEngine;

namespace Interactions
{
    /// <summary>
    /// A simple component that smoothly bobs the GameObject up and down in a sine wave.
    /// </summary>
    public class Levitator : MonoBehaviour
    {
        [Header("Levitation Settings")]
        [Tooltip("How fast the object bobs up and down.")]
        [SerializeField] private float _speed = 2f;
        
        [Tooltip("How high and low the object goes from its original position.")]
        [SerializeField] private float _amplitude = 0.5f;

        [Header("Optional Randomization")]
        [Tooltip("If true, the object will start at a random point in its animation cycle. Useful if you have many levitating objects so they don't all bob in perfect sync.")]
        [SerializeField] private bool _randomizeStartPhase = true;

        private Vector3 _originalLocalPos;
        private float _timeOffset;

        private void Start()
        {
            // Cache the starting position so it always bobs relative to where you placed it
            _originalLocalPos = transform.localPosition;
            
            if (_randomizeStartPhase)
            {
                // Start at a random point in the sine wave
                _timeOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            // Calculate the new Y offset using a smooth sine wave
            float yOffset = Mathf.Sin(Time.time * _speed + _timeOffset) * _amplitude;
            
            // Apply it to the cached original position
            transform.localPosition = _originalLocalPos + new Vector3(0f, yOffset, 0f);
        }
    }
}
