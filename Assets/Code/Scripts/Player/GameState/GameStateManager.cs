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
        private int _currentHp;
        
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
        
        public void DamagePlayer(int damage)
        {
            _currentHp -= damage;
            onHpChange?.Invoke(_currentHp);
            if (_currentHp <= 0)
            {
                KillPlayer();
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
            onHpChange?.Invoke(_currentHp);
        }
        
    }
}