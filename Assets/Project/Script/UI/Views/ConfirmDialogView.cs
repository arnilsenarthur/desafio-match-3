using System;
using Gazeus.DesafioMatch3.Localization;
using TMPro;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class ConfirmDialogView : UiPanelView
    {
        public enum ConfirmAction
        {
            None,
            Restart,
            MainMenu
        }

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _messageText;

        [SerializeField]
        private TMP_Text _confirmButtonText;

        private ConfirmAction _pendingAction;
        private string _titleKey;
        private string _messageKey;
        private string _confirmLabelKey;
        private bool _hasActiveContent;

        public event Action<ConfirmAction> Confirmed;

        private void OnEnable() => LocalizationService.LanguageChanged += OnLanguageChanged;

        private void OnDisable() => LocalizationService.LanguageChanged -= OnLanguageChanged;

        public void ShowRestartConfirm() => Open(
            ConfirmAction.Restart,
            LocKeys.ConfirmRestartTitle,
            LocKeys.ConfirmRestartMessage,
            LocKeys.ConfirmRestartButton);

        public void ShowMainMenuConfirm() => Open(
            ConfirmAction.MainMenu,
            LocKeys.ConfirmMainMenuTitle,
            LocKeys.ConfirmMainMenuMessage,
            LocKeys.ConfirmMainMenuButton);

        public void Cancel()
        {
            _pendingAction = ConfirmAction.None;
            _hasActiveContent = false;
            Hide();
        }

        public void Confirm()
        {
            ConfirmAction action = _pendingAction;
            _pendingAction = ConfirmAction.None;
            _hasActiveContent = false;
            Hide();

            if (action != ConfirmAction.None)
            {
                Confirmed?.Invoke(action);
            }
        }

        private void Open(
            ConfirmAction action,
            string titleKey,
            string messageKey,
            string confirmLabelKey)
        {
            _pendingAction = action;
            _titleKey = titleKey;
            _messageKey = messageKey;
            _confirmLabelKey = confirmLabelKey;
            _hasActiveContent = true;
            ApplyText();
            base.Show();
        }

        protected override void OnBeforeShow()
        {
            if (_hasActiveContent)
            {
                ApplyText();
            }
        }

        private void OnLanguageChanged()
        {
            if (!this || !IsVisible || !_hasActiveContent)
            {
                return;
            }

            ApplyText();
        }

        private void ApplyText()
        {
            if (!_hasActiveContent)
            {
                return;
            }

            CacheTexts();

            if (_titleText != null)
            {
                _titleText.text = LocalizationService.Localize(_titleKey);
            }

            if (_messageText != null)
            {
                _messageText.text = LocalizationService.Localize(_messageKey);
            }

            if (_confirmButtonText != null)
            {
                _confirmButtonText.text = LocalizationService.Localize(_confirmLabelKey);
            }
        }

        private void CacheTexts()
        {
            if (_messageText == null)
            {
                Transform message = FindDeepChild(transform, "Message");
                if (message != null)
                {
                    message.TryGetComponent(out _messageText);
                }
            }

            if (_titleText == null)
            {
                Transform header = FindDeepChild(transform, "Header");
                if (header != null)
                {
                    _titleText = header.GetComponentInChildren<TMP_Text>(true);
                }
            }

            if (_confirmButtonText == null)
            {
                Transform confirmButton = FindDeepChild(transform, "ButtonRestart");
                if (confirmButton != null)
                {
                    _confirmButtonText = confirmButton.GetComponentInChildren<TMP_Text>(true);
                }
            }
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                Transform found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
