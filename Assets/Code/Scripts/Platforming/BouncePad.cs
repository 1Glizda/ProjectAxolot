using System;
using UnityEngine;

namespace Platforming
{
    public class BouncePad : MonoBehaviour
    {
        [SerializeField] private float _pushForce;
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.collider.CompareTag("Player"))
            {
                other.rigidbody.AddForce(transform.up * _pushForce, ForceMode2D.Impulse);
            }
        }
    }
}
