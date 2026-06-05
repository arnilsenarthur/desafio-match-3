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
            if (tile == null)
            {
                return DOTween.Sequence().AppendInterval(0.01f);
            }

            RectTransform tileRect = (RectTransform)tile.transform;
            RectTransform spotRect = (RectTransform)transform;

            DOTween.Kill(tileRect);

            Vector3 startWorldPosition = tileRect.position;
            tileRect.SetParent(spotRect, true);
            tileRect.position = startWorldPosition;

            Vector3 targetWorldPosition = spotRect.TransformPoint(spotRect.rect.center);
            float duration = SettingsService.ScaleDuration(MoveDuration);

            return tileRect
                .DOMove(targetWorldPosition, duration)
                .SetTarget(tileRect)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => SnapTile(tile));
        }

        public void SetTile(GameObject tile)
        {
            SnapTile(tile);
            if (tile != null)
            {
                tile.transform.localScale = Vector3.one;
            }
        }

        public void SnapTile(GameObject tile)
        {
            if (tile == null)
            {
                return;
            }

            RectTransform tileRect = (RectTransform)tile.transform;
            DOTween.Kill(tileRect);

            tileRect.SetParent((RectTransform)transform, false);
            tileRect.anchorMin = Vector2.zero;
            tileRect.anchorMax = Vector2.one;
            tileRect.offsetMin = Vector2.zero;
            tileRect.offsetMax = Vector2.zero;
            tileRect.anchoredPosition = Vector2.zero;
            tileRect.localScale = Vector3.one;
        }
    }
}
