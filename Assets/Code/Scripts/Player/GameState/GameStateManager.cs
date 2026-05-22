using UnityEngine;
using UnityEngine.Events;

namespace Player.GameState
{
    [DefaultExecutionOrder(-1)]
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance;
        
        //events
        public UnityEvent<int> onHpChange;
        public UnityEvent onDeath;
        
        //exposed
        public PlayerUnlocks Unlocks => _unlocks;
        
        
        //fields
        [Header("Health")]
        [SerializeField] private int _maxHp = 5;
        [SerializeField] private float _damageCooldown = 1f;
        private int _currentHp;
        private float _damageCooldownTimer;
        
        [Header("Unlocks")]
        [SerializeField] private PlayerUnlocks _unlocks;


      
        
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

        
        #region Pulse Upgrades
        public void UnlockDirectionalPulse()
        {
            _unlocks.UnlockTier(1);
        }
        public void UnlockForcePulse()
        {
            _unlocks.UnlockTier(2);
        }
        public void UnlockHoldingPulse()
        {
            _unlocks.UnlockTier(3);
        }
        #endregion

        
        public void KillPlayer()
        {
            Debug.LogError("Player Killed", this);
            ResetPlayer();
            onDeath?.Invoke();
        }
        
        public bool DamagePlayer(int damage, Collider2D hazardCollider = null)
        {
            if (_damageCooldownTimer > 0f) return false;

            _currentHp -= damage;
            _damageCooldownTimer = _damageCooldown;

            onHpChange?.Invoke(_currentHp);
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
            if (hazardCollider == null) yield break;

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) yield break;

            Collider2D[] playerColliders = playerObj.GetComponentsInChildren<Collider2D>();
            
            foreach (var pc in playerColliders)
            {
                if (pc != null && hazardCollider != null)
                {
                    Physics2D.IgnoreCollision(pc, hazardCollider, true);
                }
            }

            yield return new WaitForSeconds(duration);

            if (hazardCollider != null && playerObj != null)
            {
                foreach (var pc in playerColliders)
                {
                    if (pc != null && hazardCollider != null)
                    {
                        Physics2D.IgnoreCollision(pc, hazardCollider, false);
                    }
                }
            }
        }

        public void HealPlayer(int heal)
        {
            _currentHp += heal;
            onHpChange?.Invoke(_currentHp);
            if (_currentHp > _maxHp)
            {
                _currentHp = _maxHp;
            }
        }

        private void ResetPlayer()
        {
            _currentHp = _maxHp;
            _damageCooldownTimer = 0f;
            onHpChange?.Invoke(_currentHp);
        }
        
    }
}