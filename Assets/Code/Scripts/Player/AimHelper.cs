using Player.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class AimHelper : MonoBehaviour
    {
        public Vector3 MouseWorld => _mouseWorld;
        [SerializeField] private bool _debugMode; 
        
        
        private Vector2 _mousePosition;
        private Vector3 _mouseWorld;

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }
        
        private void Update()
        {
            //convert mouse pointer to world space
            _mousePosition = Mouse.current.position.ReadValue();
            _mouseWorld = _camera.ScreenToWorldPoint(new Vector3(_mousePosition.x, _mousePosition.y, Mathf.Abs(_camera.transform.position.z)));
            _mouseWorld.z = 0f;

            if (_debugMode)
            {
                Vector2 verticalStart = new Vector2(_mouseWorld.x, _mouseWorld.y - 0.2f);
                Vector2 verticalEnd = new Vector2(_mouseWorld.x, _mouseWorld.y + 0.2f);
                Vector2 horizontalStart = new Vector2(_mouseWorld.x-0.2f, _mouseWorld.y);
                Vector2 horizontalEnd =  new Vector2(_mouseWorld.x+0.2f, _mouseWorld.y);
                Debug.DrawLine(verticalStart, verticalEnd, Color.yellow);
                Debug.DrawLine(horizontalStart, horizontalEnd, Color.yellow);
            }
        }
    }
}
