using UnityEngine;

namespace Player.GameState
{
    [System.Serializable]
    public class PlayerUnlocks
    {
        public bool IsDirectionalUnlocked => _isDirectionalUnlocked;
        public bool IsForceUnlocked => _isForceUnlocked;
        public bool IsHoldingUnlocked => _isHoldingUnlocked;
        
        [SerializeField] private bool _isDirectionalUnlocked;
        [SerializeField] private bool _isForceUnlocked;
        [SerializeField] private bool _isHoldingUnlocked;

        public void UnlockTier(int tier)
        {
            switch (tier)
            {
                case 1: _isDirectionalUnlocked = true; break;
                case 2: _isForceUnlocked = true; break;
                case 3: _isHoldingUnlocked = true; break;
            }
        }

    }
}
