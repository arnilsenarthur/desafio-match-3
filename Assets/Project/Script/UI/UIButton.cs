using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UIButton : Button
    {
        [SerializeField]
        private Sprite _defaultSprite;

        [SerializeField]
        private Sprite _pressedSprite;

        [SerializeField]
        private Vector2 _pressedOffset;

        private Image _image;
        private RectTransform _rectTransform;
        private bool _offsetApplied;
        private bool _pressedVisualActive;

        protected override void Awake()
        {
            transition = Transition.ColorTint;
            CacheReferences();
            base.Awake();
        }

        protected override void OnDisable()
        {
            ReleaseExtraVisuals(force: true);
            base.OnDisable();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (!IsActive())
            {
                ReleaseExtraVisuals(force: true);
                return;
            }

            if (state == SelectionState.Pressed && IsInteractable())
            {
                ApplyPressedVisual();
            }
            else
            {
                ReleaseExtraVisuals();
            }
        }

        private void CacheReferences()
        {
            _image = targetGraphic as Image;

            if (_image == null)
            {
                TryGetComponent(out _image);
            }

            _rectTransform = transform as RectTransform;

            if (_defaultSprite == null && _image != null)
            {
                _defaultSprite = _image.sprite;
            }
        }

        private void ApplyPressedVisual()
        {
            if (_pressedVisualActive)
            {
                return;
            }

            _pressedVisualActive = true;

            if (_image != null && _pressedSprite != null)
            {
                _image.sprite = _pressedSprite;
            }

            ApplyOffset();
        }

        private void ReleaseExtraVisuals(bool force = false)
        {
            if (!force && !_pressedVisualActive && !_offsetApplied)
            {
                return;
            }

            _pressedVisualActive = false;

            if (_image != null && _defaultSprite != null)
            {
                _image.sprite = _defaultSprite;
            }

            RemoveOffset();
        }

        private void ApplyOffset()
        {
            if (_offsetApplied || _rectTransform == null || _pressedOffset == Vector2.zero)
            {
                return;
            }

            _rectTransform.anchoredPosition += _pressedOffset;
            _offsetApplied = true;
        }

        private void RemoveOffset()
        {
            if (!_offsetApplied || _rectTransform == null)
            {
                return;
            }

            _rectTransform.anchoredPosition -= _pressedOffset;
            _offsetApplied = false;
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            transition = Transition.ColorTint;

            Image image = targetGraphic as Image;
            if (image != null)
            {
                _defaultSprite = image.sprite;
            }
        }
#endif
    }
}
