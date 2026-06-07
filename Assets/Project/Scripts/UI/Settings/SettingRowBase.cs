using Gazeus.DesafioMatch3.App;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Settings
{
    public abstract class SettingRowBase : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            SettingsService.Changed += OnSettingsChanged;
            Refresh();
        }

        protected virtual void OnDisable() => SettingsService.Changed -= OnSettingsChanged;

        protected virtual void OnSettingsChanged(SettingChangedEventArgs args) => Refresh();

        public abstract void Refresh();
    }
}
