using DG.Tweening;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Views
{
    public class TileSpotView : MonoBehaviour
    {
        public Tween AnimatedSetTile(GameObject tile)
        {
            Transform tileTransform = tile.transform;
            tileTransform.SetParent(transform, true);
            tileTransform.DOKill();

            return tileTransform.DOMove(transform.position, 0.3f);
        }

        public void SetTile(GameObject tile)
        {
            RectTransform tileRect = (RectTransform)tile.transform;
            tileRect.SetParent((RectTransform)transform, false);
            tileRect.anchoredPosition = Vector2.zero;
        }
    }
}
