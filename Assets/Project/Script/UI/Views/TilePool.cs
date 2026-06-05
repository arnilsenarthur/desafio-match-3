using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class TilePool : Pool<string, PooledTile>
    {
        public void Configure(IReadOnlyDictionary<string, GameObject> prefabs, int prewarmPerType = 0)
        {
            ClearAll();

            if (prefabs == null)
            {
                return;
            }

            foreach (KeyValuePair<string, GameObject> entry in prefabs)
            {
                if (!string.IsNullOrEmpty(entry.Key) && entry.Value != null)
                {
                    Register(entry.Key, entry.Value, prewarmPerType);
                }
            }
        }

        public GameObject GetObject(string typeId) => Get(typeId).gameObject;

        public void ReleaseObject(GameObject tile)
        {
            if (tile != null && tile.TryGetComponent(out PooledTile pooled))
            {
                Release(pooled);
            }
        }

        protected override void OnAcquire(PooledTile instance)
        {
            base.OnAcquire(instance);
            instance.transform.localScale = Vector3.one;
            DisableRaycast(instance.gameObject);
        }

        protected override void OnRelease(PooledTile instance)
        {
            instance.transform.DOKill(true);
            instance.transform.localScale = Vector3.one;
            base.OnRelease(instance);
        }

        private static void DisableRaycast(GameObject tile)
        {
            if (tile.TryGetComponent(out Graphic graphic))
            {
                graphic.raycastTarget = false;
            }
        }
    }
}
