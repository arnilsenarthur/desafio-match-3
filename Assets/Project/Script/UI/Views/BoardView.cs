using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class BoardView : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public event Action<int, int> TileClicked;

        private const float NormalScale = 1f;
        private const float HoverScale = 1.1f;
        private const float SelectedScale = 1.15f;
        private const float ClickPunchScale = 0.92f;
        private const float ScaleTweenDuration = 0.12f;
        private const float ClickPunchDuration = 0.08f;

        [SerializeField]
        private TileSpotView _tileSpotPrefab;

        private FlexibleGridLayout _boardContainer;
        private int _width;
        private int _height;
        private GameObject[] _tiles;
        private TileSpotView[] _tileSpots;
        private TileObjectPool _tilePool;
        private Transform _poolRoot;
        private RectTransform _boardRect;
        private int _hoverIndex = -1;
        private int _selectedIndex = -1;
        private bool _interactionEnabled = true;

        public bool HasSelection => _selectedIndex >= 0;

        private void Awake()
        {
            _boardRect = (RectTransform)transform;
            _boardContainer = GetComponent<FlexibleGridLayout>();
            _boardContainer.LayoutUpdated += OnBoardLayoutUpdated;

            GameObject poolObject = new GameObject("TilePool");
            poolObject.transform.SetParent(transform, false);
            _poolRoot = poolObject.transform;
        }

        public void Configure(GameConfig config)
        {
            _tilePool = new TileObjectPool(config.TilePrefabRepository.TileTypePrefabList, _poolRoot);
        }

        private void OnDestroy()
        {
            _boardContainer.LayoutUpdated -= OnBoardLayoutUpdated;
        }

        public bool TryGetSelectedCell(out int x, out int y)
        {
            if (_selectedIndex < 0)
            {
                x = -1;
                y = -1;
                return false;
            }

            IndexToCell(_selectedIndex, out x, out y);
            return true;
        }

        public bool IsSelectedCell(int x, int y) => _selectedIndex == ToIndex(x, y);

        public void SelectCell(int x, int y)
        {
            ClearSelection();
            _selectedIndex = ToIndex(x, y);
            ApplyScaleForIndex(_selectedIndex);
        }

        public void SyncFromState(BoardState board)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int index = ToIndex(x, y);
                    int type = board.GetType(x, y);
                    GameObject currentTile = _tiles[index];

                    if (type < 0)
                    {
                        if (currentTile != null)
                        {
                            _tilePool.Release(currentTile);
                            _tiles[index] = null;
                        }

                        continue;
                    }

                    if (currentTile != null)
                    {
                        PooledTile pooled = currentTile.GetComponent<PooledTile>();
                        if (pooled != null && pooled.TypeIndex == type)
                        {
                            _tileSpots[index].SnapTile(currentTile);
                            continue;
                        }

                        _tilePool.Release(currentTile);
                    }

                    GameObject tile = _tilePool.Get(type);
                    _tileSpots[index].SetTile(tile);
                    _tiles[index] = tile;
                }
            }
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

        public void ClearBoard()
        {
            ClearSelection();
            ClearHover();

            if (_tiles != null)
            {
                for (int i = 0; i < _tiles.Length; i++)
                {
                    if (_tiles[i] != null)
                    {
                        _tilePool.Release(_tiles[i]);
                    }
                }
            }

            if (_tileSpots != null)
            {
                for (int i = 0; i < _tileSpots.Length; i++)
                {
                    if (_tileSpots[i] != null)
                    {
                        Destroy(_tileSpots[i].gameObject);
                    }
                }
            }

            _tiles = null;
            _tileSpots = null;
            _width = 0;
            _height = 0;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (_interactionEnabled == enabled)
            {
                return;
            }

            _interactionEnabled = enabled;

            if (!enabled)
            {
                ClearHover();
                ClearSelection();
            }
        }

        public void ClearSelection()
        {
            if (_selectedIndex < 0)
            {
                return;
            }

            int previousSelected = _selectedIndex;
            _selectedIndex = -1;
            ApplyScaleForIndex(previousSelected);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!_interactionEnabled)
            {
                return;
            }

            UpdateHover(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ClearHover();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactionEnabled)
            {
                return;
            }

            if (!TryGetCellFromPointer(eventData, out int x, out int y, out int index))
            {
                return;
            }

            PunchTile(index);
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
                sequence.Join(tile.transform.DOScale(GetTargetScaleForIndex(index), 0.2f));
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

        private bool TryGetCellFromPointer(PointerEventData eventData, out int x, out int y, out int index)
        {
            x = -1;
            y = -1;
            index = -1;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _boardRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            if (!_boardContainer.TryGetCellCoordinates(localPoint, out x, out y))
            {
                return false;
            }

            index = ToIndex(x, y);
            return true;
        }

        private void UpdateHover(PointerEventData eventData)
        {
            if (!TryGetCellFromPointer(eventData, out _, out _, out int index))
            {
                ClearHover();
                return;
            }

            if (index == _hoverIndex)
            {
                return;
            }

            int previousHover = _hoverIndex;
            _hoverIndex = index;

            if (previousHover >= 0)
            {
                ApplyScaleForIndex(previousHover);
            }

            ApplyScaleForIndex(_hoverIndex);
        }

        private void ClearHover()
        {
            if (_hoverIndex < 0)
            {
                return;
            }

            int previousHover = _hoverIndex;
            _hoverIndex = -1;
            ApplyScaleForIndex(previousHover);
        }

        private void PunchTile(int index)
        {
            if (index < 0 || _tiles == null || _tiles[index] == null)
            {
                return;
            }

            Transform tileTransform = _tiles[index].transform;
            tileTransform.DOKill();
            tileTransform
                .DOScale(ClickPunchScale, ClickPunchDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => ApplyScaleForIndex(index));
        }

        private void ApplyScaleForIndex(int index)
        {
            if (index < 0 || _tiles == null || _tiles[index] == null)
            {
                return;
            }

            float targetScale = GetTargetScaleForIndex(index);
            Transform tileTransform = _tiles[index].transform;
            tileTransform.DOKill();
            tileTransform.DOScale(targetScale, ScaleTweenDuration).SetEase(Ease.OutQuad);
        }

        private float GetTargetScaleForIndex(int index)
        {
            if (index == _selectedIndex)
            {
                return SelectedScale;
            }

            if (index == _hoverIndex)
            {
                return HoverScale;
            }

            return NormalScale;
        }

        private int ToIndex(int x, int y) => y * _width + x;

        private void IndexToCell(int index, out int x, out int y)
        {
            x = index % _width;
            y = index / _width;
        }
    }
}
