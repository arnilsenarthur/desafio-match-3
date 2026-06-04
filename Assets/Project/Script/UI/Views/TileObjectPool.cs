using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class TileObjectPool
    {
        private readonly GameObject[] _prefabs;
        private readonly Stack<GameObject>[] _stacks;
        private readonly Transform _poolRoot;

        public TileObjectPool(GameObject[] prefabs, Transform poolRoot)
        {
            _prefabs = prefabs;
            _poolRoot = poolRoot;
            _stacks = new Stack<GameObject>[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
            {
                _stacks[i] = new Stack<GameObject>();
            }
        }

        public GameObject Get(int typeIndex)
        {
            if (_stacks[typeIndex].Count > 0)
            {
                GameObject tile = _stacks[typeIndex].Pop();
                tile.SetActive(true);
                tile.transform.localScale = Vector3.one;
                DisableRaycast(tile);
                return tile;
            }

            GameObject instance = Object.Instantiate(_prefabs[typeIndex]);
            PooledTile pooled = instance.GetComponent<PooledTile>();
            if (pooled == null)
            {
                pooled = instance.AddComponent<PooledTile>();
            }

            pooled.TypeIndex = typeIndex;
            DisableRaycast(instance);
            return instance;
        }

        public void Release(GameObject tile)
        {
            if (tile == null)
            {
                return;
            }

            PooledTile pooled = tile.GetComponent<PooledTile>();
            int typeIndex = pooled != null ? pooled.TypeIndex : 0;

            tile.transform.DOKill();
            tile.SetActive(false);
            tile.transform.SetParent(_poolRoot, false);
            _stacks[typeIndex].Push(tile);
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
