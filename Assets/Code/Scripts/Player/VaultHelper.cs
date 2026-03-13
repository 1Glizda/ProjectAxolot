using UnityEngine;

namespace Player
{
    public class VaultHelper : MonoBehaviour
    {
        public Transform VaultApex => _vaultApex;
        [SerializeField] private Transform _vaultApex;
        public Transform VaultTarget => _vaultTarget;
        [SerializeField] private Transform _vaultTarget;
    }
}
