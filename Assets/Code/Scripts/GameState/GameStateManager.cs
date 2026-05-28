using UnityEngine;
using UnityEngine.Events;

namespace Player.GameState
{
    [DefaultExecutionOrder(-1)]
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance;
        
        //events
        public UnityEvent<int, int> onHpChange;
        public UnityEvent onDeath;
        
        
        //fields
        [Header("Health")]
        [SerializeField] private int _maxHp = 3;
        [SerializeField] private float _damageCooldown = 1f;
        private int _currentHp;
        private float _damageCooldownTimer;

        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            
            _currentHp = _maxHp;
        }

        private void Update()
        {
            if (_damageCooldownTimer > 0f)
            {
                _damageCooldownTimer -= Time.deltaTime;
            }
        }

        
        public void KillPlayer()
        {
            #if UNITY_EDITOR
            Debug.LogError("Player Killed", this);
            #endif
            ResetPlayer();
            onDeath?.Invoke();
        }
        
        public bool DamagePlayer(int damage, Collider2D hazardCollider = null)
        {
            
            if (_damageCooldownTimer > 0f) return false;
            int initialHp = _currentHp;
            
            _currentHp -= damage;
            if (_currentHp < 0) _currentHp = 0;
            _damageCooldownTimer = _damageCooldown;

            onHpChange?.Invoke(initialHp, _currentHp);

            if (_currentHp <= 0)
            {
                KillPlayer();
            }

            if (hazardCollider != null)
            {
                StartCoroutine(IgnoreCollisionRoutine(hazardCollider, _damageCooldown));
            }

            return true;
        }

        private System.Collections.IEnumerator IgnoreCollisionRoutine(Collider2D hazardCollider, float duration)
        {
            if (!hazardCollider) yield break;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (!playerObj) yield break;

            Collider2D[] playerColliders = playerObj.GetComponentsInChildren<Collider2D>();
            
            foreach (var pc in playerColliders)
            {
                if (pc && hazardCollider)
                {
                    Physics2D.IgnoreCollision(pc, hazardCollider, true);
                }
            }

            yield return new WaitForSeconds(duration);

            if (hazardCollider && playerObj)
            {
                foreach (var pc in playerColliders)
                {
                    if (pc && hazardCollider)
                    {
                        Physics2D.IgnoreCollision(pc, hazardCollider, false);
                    }
                }
            }
        }

        public void HealPlayer(int heal)
        {
            int initialHp = _currentHp;
            _currentHp += heal;
            if (_currentHp > _maxHp)
            {
                _currentHp = _maxHp;
            }
            onHpChange?.Invoke(initialHp, _currentHp);
        }

        private void ResetPlayer()
        {
            int previousHp = _currentHp;
            _currentHp = _maxHp;
            _damageCooldownTimer = _damageCooldown;
            onHpChange?.Invoke(previousHp, _currentHp);
        }
        
    }
}