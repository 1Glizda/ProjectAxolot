using Interfaces;
using Player.Helpers;
using UnityEngine;

namespace Player
{
    public class PlayerCollisionHandler : MonoBehaviour
    {
        public bool CanSwing => _canSwing;
        
        public SwingBone SwingBone => _swingBone;
        
        [SerializeField] private LayerMask _swingLayer;
        private bool _canSwing;
        
        [SerializeField] private SwingBone _swingBone;
        [SerializeField] private VineHelper _vineHelper;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((_swingLayer.value & (1 << other.gameObject.layer)) != 0
                && other.TryGetComponent<SwingBone>(out var bone))
            {
                _canSwing = true;
                
                if (_swingBone == null)
                {
                    _swingBone = bone;
                }
                else
                {
                    float d1 = Vector2.Distance(_swingBone.transform.position, transform.position);
                    float d2 =  Vector2.Distance(bone.transform.position, transform.position);
                    _swingBone = d2 < d1 ? bone : _swingBone;
                }
            }

        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if ((_swingLayer.value & (1 << other.gameObject.layer)) != 0
                && other.TryGetComponent<SwingBone>(out var bone)
                && bone == _swingBone)
            {
                _canSwing = false;
                _swingBone = null;
            }
        }

        public void StoppedSwinging()
        {
            _canSwing = false;
            _swingBone = null;
        }
    }
}
