using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Gazeus.DesafioMatch3.App;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class DifficultyPanelView : UiPanelView
    {
        [SerializeField]
        private RectTransform[] _enterElements;

        [SerializeField]
        private Vector2 _slideOffset = new(-120f, 0f);

        [SerializeField]
        private float _enterDuration = 0.35f;

        [SerializeField]
        private float _enterStagger = 0.08f;

        [SerializeField]
        private Ease _enterEase = Ease.OutCubic;

        private readonly List<EnterTargetState> _enterTargetStates = new();
        private Coroutine _enterCoroutine;
        private Tween _enterTween;

        protected override void OnBeforeShow()
        {
            StopEnterAnimation();
            _enterCoroutine = StartCoroutine(PlayEnterAnimationRoutine());
        }

        protected override void OnAfterHide()
        {
            StopEnterAnimation();
            ResetEnterTargets();
        }

        private IEnumerator PlayEnterAnimationRoutine()
        {
            yield return null;

            if (!this || !isActiveAndEnabled)
            {
                yield break;
            }

            PlayEnterAnimation();
            _enterCoroutine = null;
        }

        private void PlayEnterAnimation()
        {
            ResetEnterTargets();

            if (_enterElements == null || _enterElements.Length == 0)
            {
                return;
            }

            float duration = SettingsService.ScaleDuration(_enterDuration);
            float stagger = SettingsService.ScaleDuration(_enterStagger);
            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < _enterElements.Length; i++)
            {
                RectTransform target = _enterElements[i];
                if (target == null)
                {
                    continue;
                }

                CanvasGroup group = EnsureCanvasGroup(target.gameObject);
                Vector2 restPosition = target.anchoredPosition;
                _enterTargetStates.Add(new EnterTargetState(target, restPosition, group));

                target.anchoredPosition = restPosition + _slideOffset;
                group.alpha = 0f;

                float delay = i * stagger;
                sequence.Insert(
                    delay,
                    DOTween.To(() => target.anchoredPosition, value => target.anchoredPosition = value, restPosition, duration)
                        .SetEase(_enterEase));
                sequence.Insert(
                    delay,
                    DOTween.To(() => group.alpha, value => group.alpha = value, 1f, duration).SetEase(_enterEase));
            }

            _enterTween = sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void StopEnterAnimation()
        {
            if (_enterCoroutine != null)
            {
                StopCoroutine(_enterCoroutine);
                _enterCoroutine = null;
            }

            _enterTween?.Kill();
            _enterTween = null;
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (!target.TryGetComponent(out CanvasGroup group))
            {
                group = target.AddComponent<CanvasGroup>();
            }

            return group;
        }

        private void ResetEnterTargets()
        {
            for (int i = 0; i < _enterTargetStates.Count; i++)
            {
                EnterTargetState state = _enterTargetStates[i];
                if (state.Transform == null)
                {
                    continue;
                }

                state.Transform.anchoredPosition = state.RestPosition;

                if (state.Group != null)
                {
                    state.Group.alpha = 1f;
                }
            }

            _enterTargetStates.Clear();
        }

        private readonly struct EnterTargetState
        {
            public EnterTargetState(RectTransform transform, Vector2 restPosition, CanvasGroup group)
            {
                Transform = transform;
                RestPosition = restPosition;
                Group = group;
            }

            public RectTransform Transform { get; }
            public Vector2 RestPosition { get; }
            public CanvasGroup Group { get; }
        }
    }
}
