using Gazeus.DesafioMatch3.UI.Settings;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class SettingsPanelView : UiPanelView
    {
        private SettingRowBase[] _rows;

        protected override void OnBeforeShow()
        {
            transform.SetAsLastSibling();
            RefreshRows();
        }

        private void RefreshRows()
        {
            if (_rows == null || _rows.Length == 0)
            {
                _rows = GetComponentsInChildren<SettingRowBase>(true);
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].Refresh();
            }
        }
    }
}
