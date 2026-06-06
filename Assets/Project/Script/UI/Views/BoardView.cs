using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Views
{
    [DefaultExecutionOrder(-200)]
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
        private const string TilePoolObjectName = "TilePool";

        private readonly BoardViewAnimations _animations = new();

        [SerializeField]
        private TileSpotView _tileSpotPrefab;

        [SerializeField]
        private TutorialBoardHintsView _tutorialHints;

        private FlexibleGridLayout _boardContainer;
        private int _width;
        private int _height;
        private GameObject[] _tiles;
        private TileSpotView[] _tileSpots;

        [SerializeField]
        private TilePool _tilePool;
        private RectTransform _boardRect;
        private bool _tutorialEventsBound;
        private int _hoverIndex = -1;
        private int _selectedIndex = -1;
        private bool _interactionEnabled = true;
        private bool _tutorialGuideActive;
        private Vector2Int _tutorialSelectCell = BoardCell.Invalid;
        private Vector2Int _tutorialSwapTargetCell = BoardCell.Invalid;
        private Coroutine _tutorialLayoutCoroutine;
        private bool _layoutEventsSubscribed;

        public bool HasSelection => _selectedIndex >= 0;

        public bool IsReady => _boardContainer != null && _tilePool != null && _tileSpotPrefab != null;

        private void OnEnable() => EnsureInitialized();

        public bool Configure(GameConfig config)
        {
            EnsureInitialized();

            if (_boardContainer == null)
            {
                Debug.LogError("BoardView requires FlexibleGridLayout on the same GameObject.", this);
                return false;
            }

            if (_tileSpotPrefab == null)
            {
                Debug.LogError("BoardView is missing Tile Spot Prefab.", this);
                return false;
            }

            TilePool tilePool = GetOrCreateTilePool();
            if (tilePool == null)
            {
                Debug.LogError("BoardView could not create the tile pool.", this);
                return false;
            }

            TileDefinitions tiles = config?.Tiles;
            if (tiles == null || !tiles.IsConfigured)
            {
                Debug.LogError("GameConfig tile ids are missing or not configured.", this);
                return false;
            }

            if (!tilePool.HasPrefabs)
            {
                Debug.LogError("TilePool has no prefabs assigned. Configure them on the TilePool in the scene.", this);
                return false;
            }

            _tilePool = tilePool;

            if (!_tutorialEventsBound)
            {
                GameService.TutorialGuideChanged += OnTutorialGuideChanged;
                _tutorialEventsBound = true;
            }

            return true;
        }

        private void OnDisable() => CancelRunningAnimations();

        private void OnDestroy()
        {
            CancelRunningAnimations();

            if (_tutorialEventsBound)
            {
                GameService.TutorialGuideChanged -= OnTutorialGuideChanged;
                _tutorialEventsBound = false;
            }

            if (_boardContainer != null && _layoutEventsSubscribed)
            {
                _boardContainer.LayoutUpdated -= OnBoardLayoutUpdated;
                _layoutEventsSubscribed = false;
            }
        }

        public void CancelRunningAnimations()
        {
            StopTutorialLayoutCoroutine();
            KillBoardTweens();
        }

        private void StopTutorialLayoutCoroutine()
        {
            if (_tutorialLayoutCoroutine == null)
            {
                return;
            }

            StopCoroutine(_tutorialLayoutCoroutine);
            _tutorialLayoutCoroutine = null;
        }

        private void KillBoardTweens()
        {
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
                    string typeId = board.GetType(x, y);
                    GameObject currentTile = _tiles[index];

                    if (string.IsNullOrEmpty(typeId))
                    {
                        if (currentTile != null)
                        {
                            _tilePool.ReleaseObject(currentTile);
                            _tiles[index] = null;
                        }

                        continue;
                    }

                    if (!forceRecreate && currentTile != null)
                    {
                        PooledTile pooled = currentTile.GetComponent<PooledTile>();
                        if (pooled != null && pooled.TypeId == typeId)
                        {
                            _tileSpots[index].SnapTile(currentTile);
                            continue;
                        }

                        _tilePool.ReleaseObject(currentTile);
                    }
                    else if (currentTile != null)
                    {
                        _tilePool.ReleaseObject(currentTile);
                    }

                    GameObject tile = _tilePool.GetObject(typeId);
                    _tileSpots[index].SetTile(tile);
                    _tiles[index] = tile;
                }
            }
        }

        public void CreateBoard(BoardState board)
        {
            EnsureInitialized();

            if (!IsReady)
            {
                Debug.LogError("BoardView is not ready to create a board. Call Configure first.", this);
                return;
            }

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

                    string tileTypeId = board.GetType(x, y);
                    if (!string.IsNullOrEmpty(tileTypeId))
                    {
                        GameObject tile = _tilePool.GetObject(tileTypeId);
                        tileSpot.SetTile(tile);
                        _tiles[index] = tile;
                    }
                }
            }

            _animations.Bind(_tiles, _tileSpots, _tilePool, _width, gameObject, GetTargetScaleForIndex);
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
                        _tilePool.ReleaseObject(_tiles[i]);
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
            _animations.ClearBindings();
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
            ClearTutorialGuideHighlights();
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

            StopTutorialLayoutCoroutine();
            _tutorialLayoutCoroutine = StartCoroutine(ShowTutorialHintsAfterLayout(selectCell, swapTargetCell));
        }

        private IEnumerator ShowTutorialHintsAfterLayout(Vector2Int selectCell, Vector2Int swapTargetCell)
        {
            yield return null;

            if (!this)
            {
                yield break;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_boardRect);
            _tutorialHints?.ShowSwapHint(selectCell, swapTargetCell);
            _tutorialLayoutCoroutine = null;
        }

        private void EnsureInitialized()
        {
            if (_boardRect == null)
            {
                _boardRect = (RectTransform)transform;
            }

            if (_boardContainer == null)
            {
                _boardContainer = GetComponent<FlexibleGridLayout>();
            }

            if (_boardContainer != null && !_layoutEventsSubscribed)
            {
                _boardContainer.LayoutUpdated += OnBoardLayoutUpdated;
                _layoutEventsSubscribed = true;
            }

            GetOrCreateTilePool();

            if (_tutorialHints == null)
            {
                _tutorialHints = GetComponentInChildren<TutorialBoardHintsView>(true);
            }

            EnsureTutorialHintsSetup();
            _tutorialHints?.Hide(animated: false);
        }

        private TilePool GetOrCreateTilePool()
        {
            if (_tilePool != null)
            {
                return _tilePool;
            }

            const string poolName = TilePoolObjectName;
            Transform existing = transform.Find(poolName);
            if (existing != null && existing.TryGetComponent(out TilePool existingPool))
            {
                _tilePool = existingPool;
                return _tilePool;
            }

            if (!this)
            {
                return null;
            }

            GameObject poolObject = new GameObject(poolName);
            poolObject.transform.SetParent(transform, false);
            poolObject.transform.localPosition = Vector3.zero;
            SetIgnoreLayout(poolObject);
            _tilePool = poolObject.AddComponent<TilePool>();
            return _tilePool;
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
            ClearTutorialGuideHighlights();
            _tutorialGuideActive = false;
            _tutorialSelectCell = BoardCell.Invalid;
            _tutorialSwapTargetCell = BoardCell.Invalid;
            _tutorialHints?.Hide(animated: true);
        }

        private void ClearTutorialGuideHighlights()
        {
            int previousSelect = BoardCell.IsValid(_tutorialSelectCell) ? ToIndex(_tutorialSelectCell) : -1;
            int previousSwapTarget = BoardCell.IsValid(_tutorialSwapTargetCell) ? ToIndex(_tutorialSwapTargetCell) : -1;

            if (previousSelect >= 0)
            {
                ApplyScaleForIndex(previousSelect);
            }

            if (previousSwapTarget >= 0 && previousSwapTarget != previousSelect)
            {
                ApplyScaleForIndex(previousSwapTarget);
            }
        }

        public bool CanSelectTutorialCell(Vector2Int cell)
        {
            if (!_interactionEnabled)
            {
                return false;
            }

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

        public Tween CreateTile(List<AddedTileInfo> addedTiles) => _animations.CreateTile(addedTiles);

        public Tween DestroyTiles(List<Vector2Int> matchedPosition) =>
            _animations.DestroyTiles(matchedPosition);

        public Tween MoveTiles(List<MovedTileInfo> movedTiles) => _animations.MoveTiles(movedTiles);

        public Tween SwapTiles(Vector2Int from, Vector2Int to) => _animations.SwapTiles(from, to);

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
