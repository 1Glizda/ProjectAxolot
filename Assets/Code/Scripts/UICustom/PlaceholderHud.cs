using TMPro;
using UnityEngine;

namespace UICustom
{
    public class PlaceholderHud : MonoBehaviour
    {
        
        [SerializeField] private TMP_Text  _hpText;

        public void UpdateHp(int previous, int hp)
        {
            _hpText.text = $"Hp:{hp}";
        }
    }
}
