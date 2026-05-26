using TMPro;
using UnityEngine;

namespace Interactions
{
    public class CollectibleTracker : MonoBehaviour
    {
        public static CollectibleTracker Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private TMP_Text _debugTrackerText;

        private int _totalCollectibles;
        private int _gatheredCollectibles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            UpdateUI();
        }

        public void RegisterCollectible()
        {
            _totalCollectibles++;
            UpdateUI();
        }

        public void Collect()
        {
            _gatheredCollectibles++;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_debugTrackerText != null)
            {
                _debugTrackerText.text = $"{_gatheredCollectibles}/{_totalCollectibles} collectibles found.";
            }
        }
    }
}
