using System.Collections;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.UI;
using Gazeus.DesafioMatch3.UI.Views;
using UnityEngine;

namespace Gazeus.DesafioMatch3.UI.Controllers
{
    [DefaultExecutionOrder(50)]
    public class GameplayTutorialController : MonoBehaviour
    {
        [SerializeField]
        private GameController _gameController;

        [SerializeField]
        private GameConfig _gameConfig;

        [SerializeField]
        private TutorialOverlayView _overlay;

        private TutorialStepDefinition[] _steps;
        private int _stepIndex;
        private bool _waitingForStepComplete;
        private bool _showingIntro;
        private Coroutine _advanceCoroutine;

        public bool IsActive { get; private set; }

        public bool IsShowingIntro => _showingIntro;

        private bool ShouldShowTutorial => _gameConfig != null && _gameConfig.ShowTutorialOnNextMatch;

        private void Awake()
        {
            if (_gameController == null)
            {
                _gameController = GetComponent<GameController>();
            }

            if (_overlay == null)
            {
                _overlay = GetComponentInChildren<TutorialOverlayView>(true);
            }
        }

        public bool TryBeginInteractiveTutorial()
        {
            if (_gameController == null)
            {
                _gameController = GetComponent<GameController>();
            }

            if (_gameConfig == null || _overlay == null || _gameController == null)
            {
                Debug.LogError(
                    "Tutorial cannot start: assign GameConfig, TutorialOverlayView on TutorialPanel, and GameController.");
                return false;
            }

            if (!ShouldShowTutorial)
            {
                return false;
            }

            TileTypeRegistry registry = _gameConfig.TileTypeRegistry;
            if (registry == null || !registry.IsConfigured)
            {
                Debug.LogError("Tutorial cannot start: Tile Type Registry is missing or not configured.");
                return false;
            }

            _steps = TutorialLayouts.BuildSteps(registry, _gameConfig.BoardWidth, _gameConfig.BoardHeight);
            _stepIndex = 0;
            _showingIntro = true;
            _waitingForStepComplete = false;
            IsActive = true;

            _gameController.GameService.EnterTutorialMode();
            _gameController.RefreshTutorialBoard();
            _gameController.SetPaused(false);
            _gameController.SetInteractionLocked(true);
            _gameController.BeginTutorialSession();

            _overlay.ShowIntro(UiText.TutorialIntroTitle, UiText.TutorialIntroBody);
            return true;
        }

        public void ContinueFromIntro()
        {
            if (!IsActive || !_showingIntro)
            {
                return;
            }

            _showingIntro = false;
            ShowCurrentStep();
        }

        public void OnPracticeSwapCompleted()
        {
            if (!IsActive || _showingIntro || _waitingForStepComplete)
            {
                return;
            }

            _waitingForStepComplete = true;
            _gameController.SetInteractionLocked(true);

            if (_advanceCoroutine != null)
            {
                StopCoroutine(_advanceCoroutine);
            }

            _advanceCoroutine = StartCoroutine(AdvanceAfterDelay(0.85f));
        }

        public void SkipTutorial()
        {
            if (!IsActive)
            {
                return;
            }

            CompleteTutorial();
        }

        private IEnumerator AdvanceAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _advanceCoroutine = null;
            _waitingForStepComplete = false;
            _stepIndex++;

            if (_stepIndex >= _steps.Length)
            {
                CompleteTutorial();
                yield break;
            }

            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            TutorialStepDefinition step = _steps[_stepIndex];
            _gameController.ApplyTutorialStep(step);
            _overlay.ShowPracticeStep(_stepIndex + 1, _steps.Length, step.Title, step.Instruction);
            _gameController.SetInteractionLocked(false);
            _gameController.SetPaused(false);
            _gameController.SetTutorialGuide(step.SelectCell, step.SwapTargetCell);
        }

        private void CompleteTutorial()
        {
            if (_advanceCoroutine != null)
            {
                StopCoroutine(_advanceCoroutine);
                _advanceCoroutine = null;
            }

            IsActive = false;
            _showingIntro = false;
            _waitingForStepComplete = false;
            _overlay.Hide();
            _gameController.ClearTutorialGuide();
            _gameController.GameService.ExitTutorialMode();
            _gameController.FinishTutorialAndStartMatch();
        }

        private void OnDestroy()
        {
            if (_advanceCoroutine != null)
            {
                StopCoroutine(_advanceCoroutine);
            }

        }
    }
}
