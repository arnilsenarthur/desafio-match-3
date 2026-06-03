using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Models;
using Gazeus.DesafioMatch3.ScriptableObjects;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gazeus.DesafioMatch3.Views
{
    public class BoardView : MonoBehaviour, IPointerClickHandler
    {
        public event Action<int, int> TileClicked;

        [SerializeField] 
        private FlexibleGridLayout _boardContainer;
        [SerializeField] 
        private TilePrefabRepository _tilePrefabRepository;
        [SerializeField] 
        private TileSpotView _tileSpotPrefab;

        private int _width;
        private int _height;
        private GameObject[] _tiles;
        private TileSpotView[] _tileSpots;
        private TileObjectPool _tilePool;
        private Transform _poolRoot;
        private RectTransform _boardRect;

        private void Awake()
        {
            _boardRect = _boardContainer.transform as RectTransform;
            _boardContainer.LayoutUpdated += OnBoardLayoutUpdated;

            GameObject poolObject = new GameObject("TilePool");
            poolObject.transform.SetParent(transform, false);
            _poolRoot = poolObject.transform;
            _tilePool = new TileObjectPool(_tilePrefabRepository.TileTypePrefabList, _poolRoot);
        }

        private void OnDestroy()
        {
            _boardContainer.LayoutUpdated -= OnBoardLayoutUpdated;
        }

        public void CreateBoard(BoardState board)
        {
            _width = board.Width;
            _height = board.Height;
            _boardContainer.Rows = _height;
            _boardContainer.Columns = _width;

            int cellCount = _width * _height;
            _tiles = new GameObject[cellCount];
            _tileSpots = new TileSpotView[cellCount];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int index = ToIndex(x, y);

                    TileSpotView tileSpot = Instantiate(_tileSpotPrefab, _boardContainer.transform, false);
                    _tileSpots[index] = tileSpot;

                    int tileTypeIndex = board.GetType(x, y);
                    if (tileTypeIndex > -1)
                    {
                        GameObject tile = _tilePool.Get(tileTypeIndex);
                        tileSpot.SetTile(tile);
                        _tiles[index] = tile;
                    }
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _boardRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            if (!_boardContainer.TryGetCellCoordinates(localPoint, out int x, out int y))
            {
                return;
            }

            TileClicked?.Invoke(x, y);
        }

        public Tween CreateTile(List<AddedTileInfo> addedTiles)
        {
            Sequence sequence = DOTween.Sequence();
            foreach (var addedTileInfo in addedTiles)
            {
                Vector2Int position = addedTileInfo.Position;
                int index = ToIndex(position.x, position.y);

                GameObject tile = _tilePool.Get(addedTileInfo.Type);
                _tileSpots[index].SetTile(tile);
                _tiles[index] = tile;

                tile.transform.localScale = Vector3.zero;
                sequence.Join(tile.transform.DOScale(1f, 0.2f));
            }

            return sequence;
        }

        public Tween DestroyTiles(List<Vector2Int> matchedPosition)
        {
            Sequence sequence = DOTween.Sequence();
            foreach (var position in matchedPosition)
            {
                int index = ToIndex(position.x, position.y);
                GameObject tile = _tiles[index];
                _tiles[index] = null;

                sequence.Join(tile.transform.DOScale(0f, 0.2f).OnComplete(() => _tilePool.Release(tile)));
            }

            return sequence;
        }

        public Tween MoveTiles(List<MovedTileInfo> movedTiles)
        {
            Sequence sequence = DOTween.Sequence();
            foreach (var movedTileInfo in movedTiles)
            {
                Vector2Int from = movedTileInfo.From;
                Vector2Int to = movedTileInfo.To;

                int fromIndex = ToIndex(from.x, from.y);
                int toIndex = ToIndex(to.x, to.y);

                GameObject tile = _tiles[fromIndex];
                sequence.Join(_tileSpots[toIndex].AnimatedSetTile(tile));

                _tiles[toIndex] = tile;
                _tiles[fromIndex] = null;
            }

            return sequence;
        }

        public Tween SwapTiles(int fromX, int fromY, int toX, int toY)
        {
            int fromIndex = ToIndex(fromX, fromY);
            int toIndex = ToIndex(toX, toY);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(_tileSpots[fromIndex].AnimatedSetTile(_tiles[toIndex]));
            sequence.Join(_tileSpots[toIndex].AnimatedSetTile(_tiles[fromIndex]));

            (_tiles[toIndex], _tiles[fromIndex]) = (_tiles[fromIndex], _tiles[toIndex]);

            return sequence;
        }

        private void OnBoardLayoutUpdated()
        {
            if (_tiles == null)
            {
                return;
            }

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i] == null)
                {
                    continue;
                }

                _tileSpots[i].SnapTile(_tiles[i]);
            }
        }

        private int ToIndex(int x, int y) => y * _width + x;
    }
}
