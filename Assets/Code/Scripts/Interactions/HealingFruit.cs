using Player.GameState;
using UnityEngine;

namespace Interactions
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class HealingFruit : MonoBehaviour
    {
        [SerializeField] private int _healingAmount = 1;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }
        
        private void OnParticleCollision(GameObject other)
        {
            if (other.layer == LayerMask.NameToLayer("Pulse"))
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                GameStateManager.Instance.HealPlayer(_healingAmount);
                gameObject.SetActive(false);
            }
        }
    }
}
