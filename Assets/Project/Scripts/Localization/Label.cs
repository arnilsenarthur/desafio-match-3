using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Localization
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class Label : MonoBehaviour
    {
        private TMP_Text _text;
        private string _key;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }

            if (_text == null)
            {
                return;
            }

            _key = _text.text?.Trim();
            LocalizationService.Register(this);
        }

        private void OnDisable()
        {
            LocalizationService.Unregister(this);

            if (_text != null)
            {
                _text.text = _key;
            }
        }

        internal void ApplyText()
        {
            if (!this || string.IsNullOrEmpty(_key) || _text == null)
            {
                return;
            }

            _text.text = LocalizationService.Localize(_key);
        }
    }
}
