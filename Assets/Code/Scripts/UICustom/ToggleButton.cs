using UnityEngine;

namespace UICustom
{
    public class ToggleButton : MonoBehaviour
    {
        [SerializeField] private GameObject _toggleObject;

        public void Toggle()
        {
            _toggleObject.SetActive(!_toggleObject.gameObject.activeInHierarchy);
        }
    }
}
