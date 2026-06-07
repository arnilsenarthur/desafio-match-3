using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Localization;

namespace Gazeus.DesafioMatch3.UI.Settings
{
    public class LanguageSettingRowView : SettingButtonRowView
    {
        protected override string GetSelectedValue() => SettingsService.Language;

        protected override void SelectValue(string value) => SettingsService.SetLanguage(value);

        protected override void RefreshOptionCaption(ButtonOption option)
        {
            if (option.Caption == null || string.IsNullOrEmpty(option.Value))
            {
                return;
            }

            option.Caption.text = LocalizationService.GetLanguageDisplayName(option.Value);
        }

        protected override void OnSettingsChanged(SettingChangedEventArgs args)
        {
            if (args.Id == SettingId.Language)
            {
                Refresh();
            }
        }
    }
}
