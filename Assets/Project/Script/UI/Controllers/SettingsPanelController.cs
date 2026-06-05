using Gazeus.DesafioMatch3.UI.Settings;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Controllers
{
    public class SettingsPanelController : MonoBehaviour
    {
        private SettingRowBase[] _rows;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake() => _rows = GetComponentsInChildren<SettingRowBase>(true);

        public void Show()
        {
            RefreshRows();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

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
