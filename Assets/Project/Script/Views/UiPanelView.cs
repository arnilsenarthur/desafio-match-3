using UnityEngine;

namespace Gazeus.DesafioMatch3.Views
{
    public class UiPanelView : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
