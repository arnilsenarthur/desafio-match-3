using DG.Tweening;
using Gazeus.DesafioMatch3.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class TutorialBoardHintsView : MonoBehaviour
    {
        private const float ArrowPopDuration = 0.28f;
        private const float HintMoveDuration = 0.35f;
        private const float HintOffsetY = 34f;

        [SerializeField]
        private RectTransform _hintArrow;

        private BoardView _boardView;
        private RectTransform _overlayRect;
        private Sequence _pulseSequence;
        private Tween _moveTween;
        private Vector2Int _selectCell = BoardCell.Invalid;
        private Vector2Int _swapTargetCell = BoardCell.Invalid;
        private bool _showingSwapTarget;

        private void OnDisable() => StopAllHintAnimations();

        private void OnDestroy() => StopAllHintAnimations();

        public bool ShowSwapHint(Vector2Int selectCell, Vector2Int swapTargetCell)
        {
            EnsureInitialized();

            if (_boardView == null || _hintArrow == null || _overlayRect == null)
            {
                return false;
            }

            StopAllHintAnimations();
            _selectCell = selectCell;
            _swapTargetCell = swapTargetCell;
            _showingSwapTarget = false;

            gameObject.SetActive(true);

            if (!PlaceHintAtCell(selectCell, animateIn: true))
            {
                return false;
            }

            StartPulse();
            return true;
        }

        public void OnTutorialSelectCellChosen()
        {
            if (_showingSwapTarget || !BoardCell.IsValid(_swapTargetCell))
            {
                return;
            }

            _showingSwapTarget = true;
            MoveHintToCell(_swapTargetCell);
        }

        public void ResetToSelectPhase()
        {
            if (!BoardCell.IsValid(_selectCell) || !gameObject.activeSelf)
            {
                return;
            }

            _showingSwapTarget = false;
            PlaceHintAtCell(_selectCell, animateIn: false);
            StartPulse();
        }

        public bool RefreshSwapHint(Vector2Int selectCell, Vector2Int swapTargetCell)
        {
            EnsureInitialized();

            if (_boardView == null || _hintArrow == null || _overlayRect == null)
            {
                return false;
            }

            _selectCell = selectCell;
            _swapTargetCell = swapTargetCell;
            Vector2Int cell = _showingSwapTarget ? swapTargetCell : selectCell;

            if (!PlaceHintAtCell(cell, animateIn: false))
            {
                return false;
            }

            return true;
        }

        public void Hide()
        {
            StopAllHintAnimations();
            gameObject.SetActive(false);
        }

        private void MoveHintToCell(Vector2Int cell)
        {
            if (!_boardView.TryGetCellAnchoredPosition(cell, _overlayRect, out Vector2 position))
            {
                return;
            }

            _moveTween?.Kill();
            _pulseSequence?.Kill();

            _hintArrow.DOKill();
            Vector2 targetPosition = position + new Vector2(0f, HintOffsetY);

            _moveTween = DOTween
                .To(() => _hintArrow.anchoredPosition, value => _hintArrow.anchoredPosition = value, targetPosition, HintMoveDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(_hintArrow)
                .OnComplete(() =>
                {
                    if (!this)
                    {
                        return;
                    }

                    _moveTween = null;
                    StartPulse();
                });
        }

        private bool PlaceHintAtCell(Vector2Int cell, bool animateIn)
        {
            if (!_boardView.TryGetCellAnchoredPosition(cell, _overlayRect, out Vector2 position))
            {
                return false;
            }

            _moveTween?.Kill();
            _moveTween = null;

            _hintArrow.DOKill();
            _hintArrow.anchoredPosition = position + new Vector2(0f, HintOffsetY);
            _hintArrow.gameObject.SetActive(true);

            if (animateIn)
            {
                _hintArrow.localScale = Vector3.zero;
                _hintArrow.DOScale(1f, ArrowPopDuration).SetEase(Ease.OutBack);
            }
            else
            {
                _hintArrow.localScale = Vector3.one;
            }

            return true;
        }

        private void EnsureInitialized()
        {
            if (_overlayRect == null)
            {
                _overlayRect = transform as RectTransform;
            }

            if (_boardView == null)
            {
                _boardView = GetComponentInParent<BoardView>();
            }
        }

        private void StartPulse()
        {
            _pulseSequence?.Kill();
            Transform target = _hintArrow.transform;
            target.DOKill();
            target.localScale = Vector3.one;
            _pulseSequence = DOTween.Sequence();
            _pulseSequence.Append(target.DOScale(1.15f, 0.4f));
            _pulseSequence.Append(target.DOScale(1f, 0.4f));
            _pulseSequence.SetLoops(-1);
            _pulseSequence.SetEase(Ease.InOutSine);
        }

        private void StopAllHintAnimations()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _pulseSequence?.Kill();
            _pulseSequence = null;
            _selectCell = BoardCell.Invalid;
            _swapTargetCell = BoardCell.Invalid;
            _showingSwapTarget = false;

            if (_hintArrow != null)
            {
                _hintArrow.transform.DOKill();
                _hintArrow.transform.localScale = Vector3.one;
                _hintArrow.gameObject.SetActive(false);
            }
        }
    }
}
