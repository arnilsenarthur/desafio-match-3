using System;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Gazeus.DesafioMatch3;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class BoardView : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public event Action<Vector2Int> TileClicked;

        private const float NormalScale = 1f;
        private const float HoverScale = 1.1f;
        private const float SelectedScale = 1.15f;
        private const float TutorialHintScale = 1.1f;
        private const float ClickPunchScale = 0.92f;
        private const float ScaleTweenDuration = 0.12f;
        private const float ClickPunchDuration = 0.08f;

        [SerializeField]
        private TileSpotView _tileSpotPrefab;

        [SerializeField]
        private TutorialBoardHintsView _tutorialHints;

        private FlexibleGridLayout _boardContainer;
        private int _width;
        private int _height;
        private GameObject[] _tiles;
        private TileSpotView[] _tileSpots;
        private TileObjectPool _tilePool;
        private Transform _poolRoot;
        private RectTransform _boardRect;
        private GameEvents _gameEvents;
        private int _hoverIndex = -1;
        private int _selectedIndex = -1;
        private bool _interactionEnabled = true;
        private bool _tutorialGuideActive;
        private Vector2Int _tutorialSelectCell = BoardCell.Invalid;
        private Vector2Int _tutorialSwapTargetCell = BoardCell.Invalid;

        public bool HasSelection => _selectedIndex >= 0;

        private void Awake()
        {
            _boardRect = (RectTransform)transform;
            _boardContainer = GetComponent<FlexibleGridLayout>();
            _boardContainer.LayoutUpdated += OnBoardLayoutUpdated;

            GameObject poolObject = new GameObject("TilePool");
            poolObject.transform.SetParent(transform, false);
            _poolRoot = poolObject.transform;
            SetIgnoreLayout(poolObject);

            if (_tutorialHints == null)
            {
                _tutorialHints = GetComponentInChildren<TutorialBoardHintsView>(true);
            }

            EnsureTutorialHintsSetup();
            _tutorialHints?.Hide();
        }

        public void Configure(GameConfig config, GameEvents gameEvents = null)
        {
            _tilePool = new TileObjectPool(config.TileTypeRegistry.GetPrefabLookupTable(), _poolRoot);

            if (_gameEvents != null)
            {
                _gameEvents.TutorialGuideChanged -= OnTutorialGuideChanged;
            }

            _gameEvents = gameEvents;

            if (_gameEvents != null)
            {
                _gameEvents.TutorialGuideChanged += OnTutorialGuideChanged;
            }
        }

        private void OnDestroy()
        {
            if (_gameEvents != null)
            {
                _gameEvents.TutorialGuideChanged -= OnTutorialGuideChanged;
            }

            if (_boardContainer != null)
            {
                _boardContainer.LayoutUpdated -= OnBoardLayoutUpdated;
            }

            if (_tiles != null)
            {
                for (int i = 0; i < _tiles.Length; i++)
                {
                    if (_tiles[i] != null)
                    {
                        _tiles[i].transform.DOKill();
                    }
                }
            }

            if (transform != null)
            {
                DOTween.Kill(transform, true);
            }
        }

        public bool TryGetSelectedCell(out Vector2Int cell)
        {
            if (_selectedIndex < 0)
            {
                cell = BoardCell.Invalid;
                return false;
            }

            cell = IndexToCell(_selectedIndex);
            return true;
        }

        public bool IsSelectedCell(Vector2Int cell) => _selectedIndex == ToIndex(cell);

        public void SelectCell(Vector2Int cell)
        {
            ClearSelection(keepTutorialSwapHint: _tutorialGuideActive && cell == _tutorialSelectCell);
            _selectedIndex = ToIndex(cell);
            ApplyScaleForIndex(_selectedIndex);

            if (_tutorialGuideActive && cell == _tutorialSelectCell)
            {
                _tutorialHints?.OnTutorialSelectCellChosen();
            }
        }

        public void SyncFromState(BoardState board) => RefreshAllTilesFromState(board, forceRecreate: false);

        public void RebuildFromState(BoardState board) => RefreshAllTilesFromState(board, forceRecreate: true);

        private void RefreshAllTilesFromState(BoardState board, bool forceRecreate)
        {
            if (_tiles == null || board.Width != _width || board.Height != _height)
            {
                return;
            }

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

                    if (!forceRecreate && currentTile != null)
                    {
                        PooledTile pooled = currentTile.GetComponent<PooledTile>();
                        if (pooled != null && pooled.TypeIndex == type)
                        {
                            _tileSpots[index].SnapTile(currentTile);
                            continue;
                        }

                        _tilePool.Release(currentTile);
                    }
                    else if (currentTile != null)
                    {
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

        public void ClearSelection(bool keepTutorialSwapHint = false)
        {
            if (_selectedIndex < 0)
            {
                return;
            }

            int previousSelected = _selectedIndex;
            _selectedIndex = -1;
            ApplyScaleForIndex(previousSelected);

            if (_tutorialGuideActive && !keepTutorialSwapHint)
            {
                _tutorialHints?.ResetToSelectPhase();
            }
        }

        private void OnTutorialGuideChanged(TutorialGuideEventArgs args)
        {
            if (!args.IsActive)
            {
                ClearTutorialGuide();
                return;
            }

            ActivateTutorialGuide(args.SelectCell, args.SwapTargetCell);
        }

        private void ActivateTutorialGuide(Vector2Int selectCell, Vector2Int swapTargetCell)
        {
            ClearTutorialGuide();
            _tutorialGuideActive = true;
            _tutorialSelectCell = selectCell;
            _tutorialSwapTargetCell = swapTargetCell;

            ApplyScaleForIndex(ToIndex(_tutorialSelectCell));
            ApplyScaleForIndex(ToIndex(_tutorialSwapTargetCell));
            ShowTutorialHints(selectCell, swapTargetCell);
        }

        private void ShowTutorialHints(Vector2Int selectCell, Vector2Int swapTargetCell)
        {
            if (_tutorialHints == null)
            {
                return;
            }

            EnsureTutorialHintsSetup();
            _tutorialHints.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_boardRect);

            if (_tutorialHints.ShowSwapHint(selectCell, swapTargetCell))
            {
                return;
            }

            StartCoroutine(ShowTutorialHintsAfterLayout(selectCell, swapTargetCell));
        }

        private System.Collections.IEnumerator ShowTutorialHintsAfterLayout(
            Vector2Int selectCell,
            Vector2Int swapTargetCell)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_boardRect);
            _tutorialHints?.ShowSwapHint(selectCell, swapTargetCell);
        }

        private void EnsureTutorialHintsSetup()
        {
            if (_tutorialHints == null)
            {
                return;
            }

            SetIgnoreLayout(_tutorialHints.gameObject);
        }

        private static void SetIgnoreLayout(GameObject target)
        {
            LayoutElement layoutElement = target.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = target.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
        }

        private void ClearTutorialGuide()
        {
            int previousSelect = BoardCell.IsValid(_tutorialSelectCell) ? ToIndex(_tutorialSelectCell) : -1;
            int previousSwapTarget = BoardCell.IsValid(_tutorialSwapTargetCell) ? ToIndex(_tutorialSwapTargetCell) : -1;
            _tutorialGuideActive = false;
            _tutorialSelectCell = BoardCell.Invalid;
            _tutorialSwapTargetCell = BoardCell.Invalid;

            if (previousSelect >= 0)
            {
                ApplyScaleForIndex(previousSelect);
            }

            if (previousSwapTarget >= 0 && previousSwapTarget != previousSelect)
            {
                ApplyScaleForIndex(previousSwapTarget);
            }

            _tutorialHints?.Hide();
        }

        public bool CanSelectTutorialCell(Vector2Int cell)
        {
            if (!_tutorialGuideActive)
            {
                return true;
            }

            if (!HasSelection)
            {
                return cell == _tutorialSelectCell;
            }

            if (!TryGetSelectedCell(out Vector2Int selectedCell))
            {
                return false;
            }

            if (cell == selectedCell)
            {
                return true;
            }

            return selectedCell == _tutorialSelectCell && cell == _tutorialSwapTargetCell;
        }

        public bool TryGetCellAnchoredPosition(Vector2Int cell, RectTransform relativeTo, out Vector2 anchoredPosition)
        {
            anchoredPosition = default;

            if (relativeTo == null || !IsOnBoard(cell))
            {
                return false;
            }

            Vector3 worldCenter;

            if (_tileSpots != null)
            {
                RectTransform tileRect = _tileSpots[ToIndex(cell)].transform as RectTransform;
                if (tileRect != null)
                {
                    worldCenter = tileRect.TransformPoint(tileRect.rect.center);
                }
                else if (_boardContainer != null &&
                         _boardContainer.TryGetCellCenterLocal(cell, out Vector2 boardLocal))
                {
                    worldCenter = _boardRect.TransformPoint(boardLocal);
                }
                else
                {
                    return false;
                }
            }
            else if (_boardContainer != null &&
                     _boardContainer.TryGetCellCenterLocal(cell, out Vector2 boardLocal))
            {
                worldCenter = _boardRect.TransformPoint(boardLocal);
            }
            else
            {
                return false;
            }

            Camera eventCamera = GetEventCamera(relativeTo);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                relativeTo,
                screenPoint,
                eventCamera,
                out anchoredPosition);
        }

        private static Camera GetEventCamera(RectTransform relativeTo)
        {
            Canvas canvas = relativeTo.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
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

            if (!TryGetCellFromPointer(eventData, out Vector2Int cell, out int index))
            {
                return;
            }

            if (!CanSelectTutorialCell(cell))
            {
                return;
            }

            PunchTile(index);
            TileClicked?.Invoke(cell);
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

                if (tile == null)
                {
                    continue;
                }

                sequence.Join(tile.transform.DOScale(0f, 0.2f).OnComplete(() => _tilePool.Release(tile)));
            }

            if (matchedPosition.Count == 0)
            {
                sequence.AppendInterval(0.01f);
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

        public Tween SwapTiles(Vector2Int from, Vector2Int to)
        {
            int fromIndex = ToIndex(from);
            int toIndex = ToIndex(to);

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

            if (_tutorialGuideActive && _tutorialHints != null && _tutorialHints.gameObject.activeSelf)
            {
                _tutorialHints.RefreshSwapHint(_tutorialSelectCell, _tutorialSwapTargetCell);
            }
        }

        private bool TryGetCellFromPointer(PointerEventData eventData, out Vector2Int cell, out int index)
        {
            cell = BoardCell.Invalid;
            index = -1;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _boardRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return false;
            }

            if (!_boardContainer.TryGetCellCoordinates(localPoint, out int x, out int y))
            {
                return false;
            }

            cell = BoardCell.At(x, y);
            index = ToIndex(cell);
            return true;
        }

        private void UpdateHover(PointerEventData eventData)
        {
            if (!TryGetCellFromPointer(eventData, out Vector2Int cell, out int index))
            {
                ClearHover();
                return;
            }

            if (!CanSelectTutorialCell(cell))
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

            if ((BoardCell.IsValid(_tutorialSelectCell) && index == ToIndex(_tutorialSelectCell)) ||
                (BoardCell.IsValid(_tutorialSwapTargetCell) && index == ToIndex(_tutorialSwapTargetCell)))
            {
                return TutorialHintScale;
            }

            if (index == _hoverIndex)
            {
                return HoverScale;
            }

            return NormalScale;
        }

        private int ToIndex(Vector2Int cell) => cell.y * _width + cell.x;

        private int ToIndex(int x, int y) => y * _width + x;

        private Vector2Int IndexToCell(int index) => BoardCell.At(index % _width, index / _width);

        private bool IsOnBoard(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < _width && cell.y < _height;
    }
}
