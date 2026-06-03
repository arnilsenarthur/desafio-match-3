using DG.Tweening;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Views
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
                MoveDuration).SetTarget(tileRect);
        }

        public void SetTile(GameObject tile)
        {
            SnapTile(tile);
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
