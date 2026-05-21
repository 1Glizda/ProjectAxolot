using UnityEngine;

namespace Player.GameState
{
    public class Checkpoint : MonoBehaviour
    {
        
        [SerializeField] private GameObject[] _stateSave;

        private CheckpointsManager _manager;
        
        public void Initialize(CheckpointsManager manager)
        {
            _manager = manager;
        }

        
        public void NotifyEnable()
        {
            //TODO effects or idk
        }
        
        
        
    }
}