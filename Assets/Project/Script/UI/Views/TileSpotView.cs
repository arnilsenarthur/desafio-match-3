using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class TileSpotView : MonoBehaviour
    {
        private const float MoveDuration = 0.3f;

        public Tween AnimatedSetTile(GameObject tile)
        {
            RectTransform tileRect = (RectTransform)tile.transform;
            RectTransform spotRect = (RectTransform)transform;

            tileRect.SetParent(spotRect, true);
            DOTween.Kill(tileRect);

            return DOTween.To(
                () => tileRect.anchoredPosition,
                value => tileRect.anchoredPosition = value,
                Vector2.zero,
                SettingsService.ScaleDuration(MoveDuration))
                .SetTarget(tileRect)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void SetTile(GameObject tile)
        {
            SnapTile(tile);
            tile.transform.localScale = Vector3.one;
        }

        public void SnapTile(GameObject tile)
        {
            RectTransform tileRect = (RectTransform)tile.transform;
            DOTween.Kill(tileRect);

            tileRect.SetParent((RectTransform)transform, false);
            tileRect.anchoredPosition = Vector2.zero;
        }
    }
}
