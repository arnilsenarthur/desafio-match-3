using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Settings
{
    public class SettingSliderRowView : SettingRowBase
    {
        private const float SliderSoundMinInterval = 0.12f;

        [SerializeField]
        private SettingId _settingId;

        [SerializeField]
        private Slider _slider;

        [SerializeField]
        private TMP_Text _valueLabel;

        private bool _suppressCallbacks;

        private void Awake()
        {
            if (_slider != null)
            {
                _slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        private void OnDestroy()
        {
            if (_slider != null)
            {
                _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
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
            if (_slider == null)
            {
                return;
            }

            _suppressCallbacks = true;
            switch (_settingId)
            {
                case SettingId.VfxVolume:
                    _slider.SetValueWithoutNotify(SettingsService.VfxVolume);
                    UpdateValueLabel(SettingsService.VfxVolume);
                    break;
                case SettingId.MusicVolume:
                    _slider.SetValueWithoutNotify(SettingsService.MusicVolume);
                    UpdateValueLabel(SettingsService.MusicVolume);
                    break;
                case SettingId.AnimationSpeed:
                    _slider.SetValueWithoutNotify(SettingsService.AnimationSpeed);
                    UpdateValueLabel(SettingsService.AnimationSpeed);
                    break;
            }

            _suppressCallbacks = false;
        }

        private void OnSliderValueChanged(float value)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            AudioService.PlaySfxRateLimited(AudioKeys.UiCheck, SliderSoundMinInterval);

            switch (_settingId)
            {
                case SettingId.VfxVolume:
                    SettingsService.SetVfxVolume(value);
                    UpdateValueLabel(SettingsService.VfxVolume);
                    break;
                case SettingId.MusicVolume:
                    SettingsService.SetMusicVolume(value);
                    UpdateValueLabel(SettingsService.MusicVolume);
                    break;
                case SettingId.AnimationSpeed:
                    SettingsService.SetAnimationSpeed(Mathf.RoundToInt(value));
                    UpdateValueLabel(SettingsService.AnimationSpeed);
                    break;
            }
        }

        private void UpdateValueLabel(float value)
        {
            if (_valueLabel == null)
            {
                return;
            }

            _valueLabel.text = _settingId == SettingId.AnimationSpeed
                ? SettingsService.AnimationSpeedMultiplier.ToString("0.##")
                : value.ToString("0.##");
        }
    }
}
