using System;
using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Gameplay;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class TutorialBoardHintsView : MonoBehaviour
    {
        private const float ArrowAppearDuration = 0.28f;
        private const float ArrowHideDuration = 0.22f;
        private const float HintMoveDuration = 0.35f;
        private const float HintOffsetY = 34f;
        private const float PulsePeakScale = 1.15f;
        private const float PulseHalfDuration = 0.4f;

        [SerializeField]
        private RectTransform _hintArrow;

        private BoardView _boardView;
        private RectTransform _overlayRect;
        private CanvasGroup _arrowCanvasGroup;
        private Sequence _pulseSequence;
        private Tween _moveTween;
        private Tween _visibilityTween;
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

            bool wasVisible = _hintArrow.gameObject.activeSelf;

            KillMotionTweens();
            _selectCell = selectCell;
            _swapTargetCell = swapTargetCell;
            _showingSwapTarget = false;
            gameObject.SetActive(true);

            if (wasVisible)
            {
                PlayHide(() =>
                {
                    if (!this)
                    {
                        return;
                    }

                    ShowAtCell(selectCell);
                });
                return true;
            }

            return ShowAtCell(selectCell);
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
            KillMotionTweens();

            if (!PlaceHintAtCell(_selectCell))
            {
                return;
            }

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
            return PlaceHintAtCell(cell);
        }

        public void Hide(bool animated = true)
        {
            if (!animated)
            {
                StopAllHintAnimations();
                gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf && (_hintArrow == null || !_hintArrow.gameObject.activeSelf))
            {
                StopAllHintAnimations();
                return;
            }

            gameObject.SetActive(true);
            PlayHide(() =>
            {
                if (!this)
                {
                    return;
                }

                StopAllHintAnimations();
                gameObject.SetActive(false);
            });
        }

        private bool ShowAtCell(Vector2Int cell)
        {
            if (!PlaceHintAtCell(cell))
            {
                return false;
            }

            PlayAppear(StartPulse);
            return true;
        }

        private void MoveHintToCell(Vector2Int cell)
        {
            if (!_boardView.TryGetCellAnchoredPosition(cell, _overlayRect, out Vector2 position))
            {
                return;
            }

            KillMotionTweens();
            _hintArrow.DOKill();

            Vector2 targetPosition = position + new Vector2(0f, HintOffsetY);
            float duration = SettingsService.ScaleDuration(HintMoveDuration);

            _moveTween = DOTween
                .To(() => _hintArrow.anchoredPosition, value => _hintArrow.anchoredPosition = value, targetPosition, duration)
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

        private bool PlaceHintAtCell(Vector2Int cell)
        {
            if (!_boardView.TryGetCellAnchoredPosition(cell, _overlayRect, out Vector2 position))
            {
                return false;
            }

            KillMotionTweens();
            _hintArrow.DOKill();
            _hintArrow.anchoredPosition = position + new Vector2(0f, HintOffsetY);
            _hintArrow.gameObject.SetActive(true);
            ResetArrowVisuals();
            return true;
        }

        private void PlayAppear(Action onComplete)
        {
            KillVisibilityTween();
            EnsureCanvasGroup();

            _hintArrow.localScale = Vector3.zero;
            _arrowCanvasGroup.alpha = 0f;

            float duration = SettingsService.ScaleDuration(ArrowAppearDuration);

            _visibilityTween = DOTween.Sequence()
                .Join(_hintArrow.DOScale(1f, duration).SetEase(Ease.OutBack))
                .Join(DOTween.To(() => _arrowCanvasGroup.alpha, value => _arrowCanvasGroup.alpha = value, 1f, duration))
                .SetTarget(_hintArrow)
                .OnComplete(() =>
                {
                    if (!this)
                    {
                        return;
                    }

                    _visibilityTween = null;
                    onComplete?.Invoke();
                });
        }

        private void PlayHide(Action onComplete)
        {
            KillVisibilityTween();
            KillMotionTweens();

            if (_hintArrow == null || !_hintArrow.gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureCanvasGroup();
            float duration = SettingsService.ScaleDuration(ArrowHideDuration);

            _visibilityTween = DOTween.Sequence()
                .Join(_hintArrow.DOScale(0f, duration).SetEase(Ease.InBack))
                .Join(DOTween.To(() => _arrowCanvasGroup.alpha, value => _arrowCanvasGroup.alpha = value, 0f, duration))
                .SetTarget(_hintArrow)
                .OnComplete(() =>
                {
                    if (!this)
                    {
                        return;
                    }

                    _visibilityTween = null;
                    _hintArrow.gameObject.SetActive(false);
                    ResetArrowVisuals();
                    onComplete?.Invoke();
                });
        }

        private void StartPulse()
        {
            _pulseSequence?.Kill();
            Transform target = _hintArrow.transform;
            target.DOKill();
            target.localScale = Vector3.one;

            float halfDuration = SettingsService.ScaleDuration(PulseHalfDuration);

            _pulseSequence = DOTween.Sequence();
            _pulseSequence.Append(target.DOScale(PulsePeakScale, halfDuration));
            _pulseSequence.Append(target.DOScale(1f, halfDuration));
            _pulseSequence.SetLoops(-1);
            _pulseSequence.SetEase(Ease.InOutSine);
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

        private void EnsureCanvasGroup()
        {
            if (_arrowCanvasGroup != null || _hintArrow == null)
            {
                return;
            }

            _arrowCanvasGroup = _hintArrow.GetComponent<CanvasGroup>();
            if (_arrowCanvasGroup == null)
            {
                _arrowCanvasGroup = _hintArrow.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void KillMotionTweens()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _pulseSequence?.Kill();
            _pulseSequence = null;
        }

        private void KillVisibilityTween()
        {
            _visibilityTween?.Kill();
            _visibilityTween = null;
        }

        private void ResetArrowVisuals()
        {
            if (_hintArrow == null)
            {
                return;
            }

            _hintArrow.localScale = Vector3.one;
            EnsureCanvasGroup();
            _arrowCanvasGroup.alpha = 1f;
        }

        private void StopAllHintAnimations()
        {
            KillVisibilityTween();
            KillMotionTweens();
            _selectCell = BoardCell.Invalid;
            _swapTargetCell = BoardCell.Invalid;
            _showingSwapTarget = false;

            if (_hintArrow != null)
            {
                _hintArrow.DOKill();
                ResetArrowVisuals();
                _hintArrow.gameObject.SetActive(false);
            }
        }
    }
}
