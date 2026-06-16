using UnityEngine;
using TMPro;
using Player.GameState;

namespace UICustom
{
    [RequireComponent(typeof(TMP_Text))]
    public class DeathTimeCounterUI : MonoBehaviour
    {
        [Tooltip("The text format. {0} is replaced by Deaths, {1} is replaced by Time (mm:ss)")]
        [SerializeField] private string _formatString = "Deaths: {0} | Time: {1}";

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (GameStateManager.Instance == null) return;

            int deaths = GameStateManager.Instance.TotalDeaths;
            float time = GameStateManager.Instance.TotalTimePlayed;

            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            
            string timeString = $"{minutes:00}:{seconds:00}";

            _text.text = string.Format(_formatString, deaths, timeString);
        }
    }
}
