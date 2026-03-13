using Platforming;
using System;
using UnityEngine;

namespace Player
{
    public class PlayerCollisionHandler : MonoBehaviour
    {
        public bool CanVault => _canVault;
        public bool CanSwing => _canSwing;
        public VaultHelper VaultHelper => _vaultHelper;
        public SwingBone SwingBone => _swingBone;
        
        [SerializeField] private LayerMask _vaultLayer;
        [SerializeField] private LayerMask _swingLayer;

        private bool _canVault;
        private bool _canSwing;
        private VaultHelper _vaultHelper;
        [SerializeField] private SwingBone _swingBone;
        [SerializeField] private VineHelper _vineHelper;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((_vaultLayer.value & (1 << other.gameObject.layer)) != 0
                && other.gameObject.TryGetComponent<VaultHelper>(out var vaultHelper))
            {
                _canVault = true;
                _vaultHelper = vaultHelper;
            }

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
            if ((_vaultLayer.value & (1 << other.gameObject.layer)) != 0)
            {
                if (other.TryGetComponent<VaultHelper>(out var vaultHelper)
                    && vaultHelper == _vaultHelper)
                {
                    _canVault = false;
                    _vaultHelper = null;
                }
            }

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
