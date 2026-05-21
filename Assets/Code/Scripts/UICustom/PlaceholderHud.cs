using TMPro;
using UnityEngine;

namespace Code.Scripts.UICustom
{
    public class PlaceholderHud : MonoBehaviour
    {
        
        [SerializeField] private TMP_Text  _hpText;

        public void UpdateHp(int hp)
        {
            _hpText.text = $"Hp:{hp}";
        }
    }
}
