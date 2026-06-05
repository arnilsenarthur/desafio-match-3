using System.Collections;
using Gazeus.DesafioMatch3.App;
using Gazeus.DesafioMatch3.Data;
using Gazeus.DesafioMatch3.Gameplay;
using Gazeus.DesafioMatch3.Localization;
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
        private TutorialOverlayView _overlay;

        private TutorialStepDefinition[] _steps;
        private int _stepIndex;
        private bool _waitingForStepComplete;
        private bool _showingIntro;
        private Coroutine _advanceCoroutine;

        public bool IsActive { get; private set; }

        public bool IsShowingIntro => _showingIntro;

        private GameConfig Config => _gameController != null ? _gameController.Config : null;


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

        private void OnDisable() => StopAdvanceCoroutine();

        private void OnDestroy() => StopAdvanceCoroutine();

        public bool TryBeginInteractiveTutorial()
        {
            if (_gameController == null)
            {
                _gameController = GetComponent<GameController>();
            }

            GameConfig config = Config;
            if (config == null || _overlay == null || _gameController == null)
            {
                Debug.LogError(
                    "Tutorial cannot start: assign GameController, TutorialOverlayView on TutorialPanel.");
                return false;
            }

            if (!SettingsService.PlayTutorialNextTime)
            {
                return false;
            }

            TileTypeRegistry registry = config.TileTypeRegistry;
            if (registry == null || !registry.IsConfigured)
            {
                Debug.LogError("Tutorial cannot start: Tile Type Registry is missing or not configured.");
                return false;
            }

            _steps = TutorialLayouts.BuildSteps(registry, config.BoardWidth, config.BoardHeight);
            _stepIndex = 0;
            _showingIntro = true;
            _waitingForStepComplete = false;
            IsActive = true;

            if (!GameService.IsActive)
            {
                return false;
            }

            GameService.EnterTutorialMode();
            _gameController.RefreshTutorialBoard();
            _gameController.SetPaused(false);
            _gameController.SetInteractionLocked(true);
            _gameController.BeginTutorialSession();

            SettingsService.ConsumePlayTutorialNextTime();
            _overlay.ShowIntro(LocKeys.TutorialIntroTitle, LocKeys.TutorialIntroBody);
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
            StopAdvanceCoroutine();
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

            if (!this || !isActiveAndEnabled || _gameController == null)
            {
                yield break;
            }

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
            if (_gameController == null || _overlay == null || _steps == null)
            {
                return;
            }

            TutorialStepDefinition step = _steps[_stepIndex];
            _gameController.ApplyTutorialStep(step);
            _overlay.ShowPracticeStep(_stepIndex + 1, _steps.Length, step.Title, step.Instruction);
            _gameController.SetInteractionLocked(false);
            _gameController.SetPaused(false);
            _gameController.SetTutorialGuide(step.SelectCell, step.SwapTargetCell);
        }

        private void CompleteTutorial()
        {
            StopAdvanceCoroutine();

            IsActive = false;
            _showingIntro = false;
            _waitingForStepComplete = false;
            _overlay?.Hide();

            if (_gameController == null)
            {
                return;
            }

            _gameController.ClearTutorialGuide();
            if (GameService.IsActive)
            {
                GameService.ExitTutorialMode();
            }
            _gameController.FinishTutorialAndStartMatch();
        }

        private void StopAdvanceCoroutine()
        {
            if (_advanceCoroutine == null)
            {
                return;
            }

            StopCoroutine(_advanceCoroutine);
            _advanceCoroutine = null;
        }
    }
}
