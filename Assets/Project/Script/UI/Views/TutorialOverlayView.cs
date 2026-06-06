using Gazeus.DesafioMatch3.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gazeus.DesafioMatch3.UI.Views
{
    public class TutorialOverlayView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _bodyText;

        [SerializeField]
        private TMP_Text _stepCounterText;

        [SerializeField]
        private Image _stepImage;

        [SerializeField]
        private GameObject _continueButtonRoot;

        [SerializeField]
        private GameObject _skipButtonRoot;

        private string _titleKey;
        private string _bodyKey;
        private int _stepIndex;
        private int _stepCount;
        private bool _showStepCounter;

        private void Awake() => ConfigureRaycasts();

        private void OnEnable() => LocalizationService.LanguageChanged += OnLanguageChanged;

        private void OnDisable() => LocalizationService.LanguageChanged -= OnLanguageChanged;

        public void ShowIntro(string titleKey, string bodyKey)
        {
            gameObject.SetActive(true);

            _titleKey = titleKey;
            _bodyKey = bodyKey;
            _showStepCounter = false;

            SetStepCounterVisible(false);
            SetContinueVisible(true);
            SetSkipVisible(true);
            ApplyStepImage(null);
            ApplyLocalizedText();
        }

        public void ShowPracticeStep(
            int stepIndex,
            int stepCount,
            string titleKey,
            string bodyKey,
            Sprite illustrationSprite)
        {
            gameObject.SetActive(true);

            _titleKey = titleKey;
            _bodyKey = bodyKey;
            _stepIndex = stepIndex;
            _stepCount = stepCount;
            _showStepCounter = true;

            SetContinueVisible(false);
            SetSkipVisible(true);
            ApplyStepImage(illustrationSprite);
            ApplyLocalizedText();
        }

        public void HideContinue() => SetContinueVisible(false);

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnLanguageChanged()
        {
            if (!this)
            {
                return;
            }

            ApplyLocalizedText();
        }

        private void ApplyLocalizedText()
        {
            if (!this)
            {
                return;
            }

            if (_titleText != null && !string.IsNullOrEmpty(_titleKey))
            {
                _titleText.text = LocalizationService.Localize(_titleKey);
            }

            if (_bodyText != null && !string.IsNullOrEmpty(_bodyKey))
            {
                _bodyText.text = LocalizationService.Localize(_bodyKey);
            }

            if (_stepCounterText != null)
            {
                _stepCounterText.gameObject.SetActive(_showStepCounter);
                if (_showStepCounter)
                {
                    _stepCounterText.text = LocalizationService.Localize(
                        LocKeys.TutorialStepCounter,
                        _stepIndex,
                        _stepCount);
                }
            }
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

        private void ApplyStepImage(Sprite sprite)
        {
            if (_stepImage == null)
            {
                return;
            }

            if (sprite == null)
            {
                _stepImage.gameObject.SetActive(false);
                return;
            }

            _stepImage.gameObject.SetActive(true);
            _stepImage.sprite = sprite;
        }
    }
}
