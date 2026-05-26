using UnityEngine;
using System.Collections.Generic;
using Interfaces;

namespace Player.GameState
{
    public class Checkpoint : MonoBehaviour
    {
        
        // Accepts any MonoBehaviour that implements IResettable (e.g. ResetObject)
        [SerializeField] private List<MonoBehaviour> _stateSave = new List<MonoBehaviour>();

        private CheckpointsManager _manager;
        
        public void Initialize(CheckpointsManager manager)
        {
            _manager = manager;
        }

        
        public void NotifyEnable()
        {
            //TODO effects or idk
        }
        
        public void ResetSavedObjects()
        {
            foreach (var behaviour in _stateSave)
            {
                if (behaviour is IResettable resettable)
                {
                    resettable.TriggerReset();
                }
            }
        }
        
        
    }
}