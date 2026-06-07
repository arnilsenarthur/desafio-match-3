using System;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Settings
{
    public class SettingButtonRowView : SettingRowBase
    {
        [Serializable]
        public struct ButtonOption
        {
            public string Value;
            public Button Button;
            public TMP_Text Caption;
        }

        [SerializeField]
        private ButtonOption[] _options = Array.Empty<ButtonOption>();

        private void Awake()
        {
            for (int i = 0; i < _options.Length; i++)
            {
                ButtonOption option = _options[i];
                if (option.Button == null || string.IsNullOrEmpty(option.Value))
                {
                    continue;
                }

                string captured = option.Value;
                option.Button.onClick.AddListener(() => OnOptionClicked(captured));
            }
        }

        protected virtual string GetSelectedValue() => string.Empty;

        protected virtual void SelectValue(string value) { }

        protected virtual void RefreshOptionCaption(ButtonOption option) { }

        public override void Refresh()
        {
            string selected = GetSelectedValue();
            for (int i = 0; i < _options.Length; i++)
            {
                ButtonOption option = _options[i];
                if (option.Button == null)
                {
                    continue;
                }

                bool isSelected = !string.IsNullOrEmpty(option.Value) && option.Value == selected;
                option.Button.interactable = !isSelected;
                RefreshOptionCaption(option);
            }
        }

        private void OnOptionClicked(string value)
        {
            if (string.IsNullOrEmpty(value) || value == GetSelectedValue())
            {
                return;
            }

            SelectValue(value);
            AudioService.PlaySfx(AudioKeys.UiCheck);
            Refresh();
        }
    }
}
