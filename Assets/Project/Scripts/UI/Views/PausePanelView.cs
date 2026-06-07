using Gazeus.DesafioMatch3.Audio;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class PausePanelView : UIPanelView
    {
        protected override string GetOpenSoundKey() => AudioKeys.UIPause;
    }
}
