using UnityEngine;

namespace UICustom
{
    public class ToggleObject : MonoBehaviour
    {
        [SerializeField] private GameObject _toggle;

        public void DoToggleObject()
        {
            _toggle.SetActive(!_toggle.activeInHierarchy);
        }
    }
}
