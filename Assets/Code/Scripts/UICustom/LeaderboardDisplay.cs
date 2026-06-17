using GameState;
using UnityEngine;

namespace UICustom
{
    public class LeaderboardDisplay : MonoBehaviour
    {
        [SerializeField] private LbEntry _entry;
        [SerializeField] private RectTransform _entriesParent;


        private void OnEnable()
        {
            InitializeDisplay();
        }
        
        
        public void InitializeDisplay()
        {
            foreach (var child in _entriesParent.GetComponentsInChildren<RectTransform>())
            {
                if (child == _entriesParent) continue;
                if (child.gameObject)
                {
                    Destroy(child.gameObject);
                }
            }

            
            LeaderboardsHandler.GetLeaderboard((entries) =>
            {
                
                Debug.Log($"{entries.Count} entries");
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var lbEntry = Instantiate(_entry, _entriesParent);
                    lbEntry.position.text = $"{i+1}";
                    lbEntry.playerName.text = $"{entry.playerName}";
                    lbEntry.score.text = FormatTimeScore(entry.timeScore);
                    lbEntry.deaths.text = $"{entry.deaths}";
                }

            });

        }

        private string FormatTimeScore(int time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
