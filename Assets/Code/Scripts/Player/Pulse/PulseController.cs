using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInputManager = Code.Scripts.Player.Input.PlayerInputManager;

namespace Player.Pulse
{
    public class PulseController : MonoBehaviour
    {
        [SerializeField] private bool _debugMode;
        [SerializeField] private PlayerInputManager _playerInputManager;
        [SerializeField] private float _pulseDistance;
        [SerializeField] private LayerMask _layerMask;
        
        [Header("Pulse Settings")]
        [SerializeField] private GameObject _pulseObject;
        
        
        private InputAction _pulseAction;
        private InputAction _pointAction;

        private void Awake()
        {
            _pointAction = _playerInputManager.PointAction;
            _pulseAction = _playerInputManager.PulseAction;
            _pulseAction.performed += OnPulsePerformed;
        }
        
        
        private void OnPulsePerformed(InputAction.CallbackContext obj)
        {
            
            if (!Camera.main) return;
            //Step 1: Get mouse position to world
            Vector3 screenPos = (Vector3) _pointAction.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Plane worldPlane = new Plane(Vector3.back, Vector3.zero);

            if (worldPlane.Raycast(ray, out float enter))
            {
                Vector2 dir =  ray.GetPoint(enter) - transform.position;
                dir.Normalize();
                FirePulse(dir);
            }
        }
        
        
        private void FirePulse(Vector2 initialDir)
        {
           GameObject pulse = Instantiate(_pulseObject, transform.position, Quaternion.identity);
           pulse.GetComponent<PulseBehaviour>().Initialize(initialDir);
        }
    }
}
