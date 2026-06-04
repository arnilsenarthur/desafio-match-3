using Gazeus.DesafioMatch3.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class TutorialOverlayView : MonoBehaviour
    {
        [SerializeField]
        private float _bottomBarHeight = 300f;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _bodyText;

        [SerializeField]
        private TMP_Text _stepCounterText;

        [SerializeField]
        private GameObject _continueButtonRoot;

        [SerializeField]
        private GameObject _skipButtonRoot;

        private void Awake()
        {
            ApplyBottomBarLayout();
            ConfigureRaycasts();
        }

        public void ShowIntro(string title, string body)
        {
            gameObject.SetActive(true);
            ApplyBottomBarLayout();
            ApplyBottomBarContentLayout();

            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_bodyText != null)
            {
                _bodyText.text = body;
            }

            SetStepCounterVisible(false);
            SetContinueVisible(true);
            SetSkipVisible(true);
        }

        public void ShowPracticeStep(int stepIndex, int stepCount, string title, string body)
        {
            gameObject.SetActive(true);
            ApplyBottomBarLayout();
            ApplyBottomBarContentLayout();

            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_bodyText != null)
            {
                _bodyText.text = body;
            }

            if (_stepCounterText != null)
            {
                _stepCounterText.gameObject.SetActive(true);
                _stepCounterText.text = UiText.TutorialStepCounter(stepIndex, stepCount);
            }

            SetContinueVisible(false);
            SetSkipVisible(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ApplyBottomBarLayout()
        {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, _bottomBarHeight);
        }

        private void ApplyBottomBarContentLayout()
        {
            LayoutBarElement(_titleText != null ? _titleText.rectTransform : null, 0.5f, 1f, new Vector2(0f, -12f), new Vector2(900f, 52f));
            LayoutBarElement(
                _stepCounterText != null ? _stepCounterText.rectTransform : null,
                0.5f,
                1f,
                new Vector2(0f, -58f),
                new Vector2(320f, 36f));
            LayoutBarElement(_bodyText != null ? _bodyText.rectTransform : null, 0.5f, 0.5f, new Vector2(0f, 6f), new Vector2(920f, 110f));
            LayoutBarElement(
                _continueButtonRoot != null ? _continueButtonRoot.transform as RectTransform : null,
                0.3f,
                0f,
                new Vector2(0f, 56f),
                new Vector2(340f, 64f));
            LayoutBarElement(
                _skipButtonRoot != null ? _skipButtonRoot.transform as RectTransform : null,
                0.7f,
                0f,
                new Vector2(0f, 56f),
                new Vector2(340f, 64f));
        }

        private static void LayoutBarElement(RectTransform rectTransform, float anchorX, float anchorY, Vector2 anchoredPosition, Vector2 size)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(anchorX, anchorY);
            rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        private void ConfigureRaycasts()
        {
            if (TryGetComponent<Image>(out Image panelImage))
            {
                panelImage.raycastTarget = false;
            }

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic.GetComponentInParent<Button>(true) != null)
                {
                    continue;
                }

                graphic.raycastTarget = false;
            }
        }

        private void SetStepCounterVisible(bool visible)
        {
            if (_stepCounterText != null)
            {
                _stepCounterText.gameObject.SetActive(visible);
            }
        }

        private void SetContinueVisible(bool visible)
        {
            if (_continueButtonRoot != null)
            {
                _continueButtonRoot.SetActive(visible);
            }
        }

        private void SetSkipVisible(bool visible)
        {
            if (_skipButtonRoot != null)
            {
                _skipButtonRoot.SetActive(visible);
            }
        }
    }
}
