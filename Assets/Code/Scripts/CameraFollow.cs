using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private float _smoothMin;
    [SerializeField] private float _smoothMax;
    [SerializeField] private float _playerSpeedMax;
    [SerializeField] private Rigidbody2D _rb;
    
    private Vector3 _offset;

    private void Awake()
    {
        _offset =  transform.position - _player.position;
    }
    private void LateUpdate()
    {
        float t = Mathf.InverseLerp(0f, _playerSpeedMax, _rb.linearVelocity.magnitude);
        float smooth = 1/Mathf.Lerp(_smoothMin, _smoothMax, t);
        this.transform.position = Vector3.Lerp(transform.position, _player.position + _offset, smooth*Time.deltaTime);
    }
}
