using CameraScripts;
using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private float _smoothMin;
    [SerializeField] private float _smoothMax;
    [SerializeField] private float _playerSpeedMax;
    [SerializeField] private Rigidbody2D _rb;
    
    [Header("Zoom (Z Axis)")]
    [SerializeField] private float _zoomSpeed = 2f;
    [SerializeField] private float _minZoomZ = -5f;
    [SerializeField] private float _maxZoomZ = -20f; 
    
    [Header("Offset (Y Axis)")]
    [SerializeField] private float _yOffsetSpeed = 2f;
    
    private Vector3 _offset;
    private float _targetZ;
    private float _defaultZ;
    private float _targetY;
    private float _defaultY;
    
    public static CameraFollow Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        _offset =  transform.position - _player.position;
        _defaultZ = _offset.z;
        _targetZ = _defaultZ;
        _defaultY = _offset.y;
        _targetY = _defaultY;
    }
    private void LateUpdate()
    {
        float t = Mathf.InverseLerp(0f, _playerSpeedMax, _rb.linearVelocity.magnitude);
        float smooth = 1/Mathf.Lerp(_smoothMin, _smoothMax, t);
        
        _offset.z = Mathf.Lerp(_offset.z, _targetZ, _zoomSpeed * Time.deltaTime);
        _offset.y = Mathf.Lerp(_offset.y, _targetY, _yOffsetSpeed * Time.deltaTime);

        this.transform.position = Vector3.Lerp(transform.position, _player.position + _offset, smooth*Time.deltaTime);
    }
    
    private CameraZoomZone _activeZoneZ;
    private CameraZoomZone _activeZoneY;
    
    public void ApplyZoneSettings(bool affectZ, float targetZ, bool affectY, float targetY, CameraZoomZone callerZone)
    {
        if (affectZ)
        {
            _activeZoneZ = callerZone;
            _targetZ = Mathf.Clamp(targetZ, _maxZoomZ, _minZoomZ); 
        }
        
        if (affectY)
        {
            _activeZoneY = callerZone;
            _targetY = targetY;
        }
    }

    public void RevertZoneSettings(CameraZoomZone callerZone)
    {
        if (_activeZoneZ == callerZone || _activeZoneZ == null)
        {
            _targetZ = _defaultZ;
            if (_activeZoneZ == callerZone) _activeZoneZ = null;
        }
        
        if (_activeZoneY == callerZone || _activeZoneY == null)
        {
            _targetY = _defaultY;
            if (_activeZoneY == callerZone) _activeZoneY = null;
        }
    }
}
