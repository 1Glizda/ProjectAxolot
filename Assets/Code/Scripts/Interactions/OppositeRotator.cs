using UnityEngine;

namespace Interactions
{
    public class OppositeRotator : MonoBehaviour
    {
        [Header("Renderers")]
        [Tooltip("The first sprite to rotate.")]
        [SerializeField] private SpriteRenderer _sprite1;
        [Tooltip("The second sprite to rotate in the opposite direction.")]
        [SerializeField] private SpriteRenderer _sprite2;

        [Header("Speeds (Degrees per second)")]
        [Tooltip("Speed for the first sprite.")]
        [SerializeField] private float _speed1 = 90f;
        
        [Tooltip("Speed for the second sprite (it will automatically rotate in the opposite direction, so just set the positive speed you want).")]
        [SerializeField] private float _speed2 = 60f;

        private void Update()
        {
            if (_sprite1 != null)
            {
                // Rotate normally (counter-clockwise if positive, clockwise if negative)
                _sprite1.transform.Rotate(0f, 0f, _speed1 * Time.deltaTime);
            }

            if (_sprite2 != null)
            {
                // Multiply by -1 to force it to rotate in the opposite direction
                _sprite2.transform.Rotate(0f, 0f, -_speed2 * Time.deltaTime);
            }
        }
    }
}
