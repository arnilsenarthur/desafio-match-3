using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Settings
{
    public class SettingToggleRowView : SettingRowBase
    {
        [SerializeField]
        private SettingId _settingId;

        [SerializeField]
        private Toggle _toggle;

        private bool _suppressCallbacks;

        private void Awake()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }

        private void OnDestroy()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }

        protected override void OnSettingsChanged(SettingChangedEventArgs args)
        {
            if (args.Id == _settingId)
            {
                Refresh();
            }
        }

        public override void Refresh()
        {
            if (_toggle == null)
            {
                return;
            }

            _suppressCallbacks = true;
            if (_settingId == SettingId.PlayTutorialNextTime)
            {
                _toggle.SetIsOnWithoutNotify(SettingsService.PlayTutorialNextTime);
            }

            _suppressCallbacks = false;
        }

        private void OnToggleValueChanged(bool value)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            AudioService.PlaySfx(AudioKeys.UICheck);

            if (_settingId == SettingId.PlayTutorialNextTime)
            {
                SettingsService.SetPlayTutorialNextTime(value);
            }
        }
    }
}
