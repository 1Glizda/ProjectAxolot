using UnityEngine;

namespace Player.AI.Navigation
{
    public class AiManager : MonoBehaviour
    {
        public static AiManager Instance { get; private set; }

        public AiArea ActiveArea { get; private set; }
        public delegate void AiAreaChangedHandler(AiArea newArea);
        public event AiAreaChangedHandler OnAreaChanged;

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
        }

        public void SetActiveArea(AiArea area)
        {
            if (ActiveArea != area)
            {
                ActiveArea = area;
                OnAreaChanged?.Invoke(area);
            }
        }
    }
}
