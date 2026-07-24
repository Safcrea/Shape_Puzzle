using System.Collections;
using UnityEngine;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    public sealed class PuzzleAppController : MonoBehaviour
    {
        [SerializeField] private LevelPrefabCatalog levelCatalog;
        [SerializeField] private Transform levelInstanceRoot;
        [SerializeField] private PuzzleGameController gameController;
        [SerializeField] private ToyUIController uiController;
        [SerializeField] private LevelSelectView levelSelectView;
        [SerializeField] private AudioService audioService;
        [SerializeField] private HapticService hapticService;
        [SerializeField] private ToyEffectPool effectPool;
        [SerializeField] private TutorialOverlayController tutorialOverlay;

        private SaveService _saveService;
        private int _currentLevelIndex;
        private GameObject _activeLevelInstance;
        private int _lastDisplayedSecond = -1;
        private int _lastDisplayedMoveCount = -1;
        private Coroutine _completionSequence;

        public LevelPrefabCatalog Catalog => levelCatalog;
        public PlayerSaveData SaveData => _saveService == null ? null : _saveService.Data;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            _saveService = new SaveService(FileSaveStorage.CreateDefault(), levelCatalog == null ? 50 : Mathf.Max(1, levelCatalog.Count));
            _saveService.Load();
            _currentLevelIndex = Mathf.Clamp(_saveService.Data.lastSelectedLevel - 1, 0, Mathf.Max(0, GetLevelCount() - 1));
            BindEvents(true);
            ApplySettings();
            UpdateHomeProgress();
            if (uiController != null) uiController.ShowHome();
        }

        private void OnDestroy()
        {
            SaveCurrentPuzzleProgress();
            BindEvents(false);
            if (_saveService != null && _saveService.Data != null) _saveService.Save();
        }

        private void Update()
        {
            PuzzleSession session = gameController == null ? null : gameController.Session;
            if (session == null || uiController == null) return;
            int second = Mathf.Max(0, Mathf.FloorToInt(session.ElapsedSeconds));
            if (second == _lastDisplayedSecond && session.MoveCount == _lastDisplayedMoveCount) return;
            RefreshGameplayStatus();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveCurrentPuzzleProgress();
        }

        private void OnApplicationQuit()
        {
            SaveCurrentPuzzleProgress();
        }

        private void BindEvents(bool bind)
        {
            if (uiController != null)
            {
                if (bind)
                {
                    uiController.PlayRequested += PlayLatest;
                    uiController.LevelSelectRequested += ShowLevelSelect;
                    uiController.HomeRequested += ReturnHome;
                    uiController.UndoRequested += Undo;
                    uiController.HintRequested += Hint;
                    uiController.PauseRequested += Pause;
                    uiController.ResumeRequested += Resume;
                    uiController.RestartRequested += Restart;
                    uiController.ReplayRequested += Replay;
                    uiController.NextLevelRequested += NextLevel;
                    uiController.ResetProgressRequested += ResetProgress;
                    uiController.MusicChanged += SetMusic;
                    uiController.SoundChanged += SetSound;
                    uiController.HapticsChanged += SetHaptics;
                    uiController.ReducedMotionChanged += SetReducedMotion;
                }
                else
                {
                    uiController.PlayRequested -= PlayLatest;
                    uiController.LevelSelectRequested -= ShowLevelSelect;
                    uiController.HomeRequested -= ReturnHome;
                    uiController.UndoRequested -= Undo;
                    uiController.HintRequested -= Hint;
                    uiController.PauseRequested -= Pause;
                    uiController.ResumeRequested -= Resume;
                    uiController.RestartRequested -= Restart;
                    uiController.ReplayRequested -= Replay;
                    uiController.NextLevelRequested -= NextLevel;
                    uiController.ResetProgressRequested -= ResetProgress;
                    uiController.MusicChanged -= SetMusic;
                    uiController.SoundChanged -= SetSound;
                    uiController.HapticsChanged -= SetHaptics;
                    uiController.ReducedMotionChanged -= SetReducedMotion;
                }
            }

            if (gameController != null)
            {
                if (bind)
                {
                    gameController.SessionChanged += HandleSessionChanged;
                    gameController.LevelCompleted += HandleLevelCompleted;
                    gameController.ValidAction += HandleValidAction;
                    gameController.InvalidAction += HandleInvalidAction;
                    gameController.HintUsed += HandleHintUsed;
                    gameController.FreeformProgressChanged += HandleFreeformProgressChanged;
                }
                else
                {
                    gameController.SessionChanged -= HandleSessionChanged;
                    gameController.LevelCompleted -= HandleLevelCompleted;
                    gameController.ValidAction -= HandleValidAction;
                    gameController.InvalidAction -= HandleInvalidAction;
                    gameController.HintUsed -= HandleHintUsed;
                    gameController.FreeformProgressChanged -= HandleFreeformProgressChanged;
                }
            }

            if (tutorialOverlay != null)
            {
                if (bind) tutorialOverlay.Skipped += CompleteCurrentTutorial;
                else tutorialOverlay.Skipped -= CompleteCurrentTutorial;
            }
        }

        private void PlayLatest()
        {
            int latest = Mathf.Clamp(_saveService.Data.lastSelectedLevel - 1, 0, Mathf.Max(0, GetLevelCount() - 1));
            LoadLevel(latest);
        }

        private void ShowLevelSelect()
        {
            SaveCurrentPuzzleProgress();
            if (levelSelectView != null) levelSelectView.Rebuild(levelCatalog, _saveService.Data, LoadLevel);
        }

        private void ReturnHome()
        {
            CancelCompletionSequence();
            SaveCurrentPuzzleProgress();
            if (gameController != null) gameController.SetPaused(true);
            if (tutorialOverlay != null) tutorialOverlay.Hide();
            UpdateHomeProgress();
        }

        private void LoadLevel(int index)
        {
            LoadLevel(index, false);
        }

        private void LoadLevel(int index, bool bypassLock)
        {
            if (levelCatalog == null || gameController == null || index < 0 || index >= GetLevelCount()) return;
            CancelCompletionSequence();
            SaveCurrentPuzzleProgress();
            PuzzleLevelPrefab levelPrefab = levelCatalog.GetByIndex(index);
            if (levelPrefab == null || levelPrefab.Level == null) return;
            if (!bypassLock && levelPrefab.Level.levelNumber > _saveService.Data.highestUnlockedLevel) return;
            _currentLevelIndex = index;
            _saveService.SetLastSelectedLevel(levelPrefab.Level.levelNumber);
            _saveService.Save();
            PuzzleLevelPrefab activeLevel = InstantiateLevel(index);
            if (activeLevel == null) return;
            gameController.LoadLevel(activeLevel);
            LevelProgressData savedProgress = _saveService.GetLevelProgress(activeLevel.Level.levelNumber);
            if (savedProgress != null && savedProgress.pieceProgress != null && savedProgress.pieceProgress.Length > 0 &&
                !gameController.RestoreFreeformProgress(savedProgress))
            {
                _saveService.ClearPuzzleProgress(activeLevel.Level.levelNumber);
                _saveService.Save();
            }
            UpdateHomeProgress();
            ShowTutorial(activeLevel.Level);
            if (uiController != null) uiController.ShowGameplay();
            RefreshGameplayStatus();
        }

        private PuzzleLevelPrefab InstantiateLevel(int index)
        {
            GameObject prefab = levelCatalog == null ? null : levelCatalog.GetPrefabByIndex(index);
            if (prefab == null) return null;
            if (_activeLevelInstance != null) Destroy(_activeLevelInstance);
            Transform parent = levelInstanceRoot == null ? transform : levelInstanceRoot;
            _activeLevelInstance = Instantiate(prefab, parent, false);
            _activeLevelInstance.name = prefab.name;
            return _activeLevelInstance.GetComponent<PuzzleLevelPrefab>();
        }

        private void Undo()
        {
            if (gameController != null) gameController.Undo();
        }

        private void Hint()
        {
            if (gameController != null) gameController.Hint();
        }

        private void Pause()
        {
            if (gameController != null) gameController.SetPaused(true);
            if (tutorialOverlay != null) tutorialOverlay.Hide();
        }

        private void Resume()
        {
            if (gameController != null) gameController.SetPaused(false);
            if (gameController != null && gameController.Session != null) ShowTutorial(gameController.Session.Level);
        }

        private void Restart()
        {
            if (gameController == null || gameController.Session == null) return;
            _saveService.ClearPuzzleProgress(gameController.Session.Level.levelNumber);
            gameController.ResetLevel();
            SaveCurrentPuzzleProgress();
        }

        private void Replay()
        {
            LoadLevel(_currentLevelIndex);
        }

        private void NextLevel()
        {
            int count = GetLevelCount();
            if (count <= 0) return;
            LoadLevel((_currentLevelIndex + 1) % count, true);
        }

        private void ResetProgress()
        {
            _saveService.ResetProgress();
            _saveService.Save();
            _currentLevelIndex = 0;
            UpdateHomeProgress();
            ShowLevelSelect();
        }

        private void HandleSessionChanged(PuzzleSession session)
        {
            if (uiController != null) uiController.SetUndoAvailable(session != null && session.CanUndo);
            RefreshGameplayStatus();
        }

        private void HandleLevelCompleted(PuzzleSession session)
        {
            if (session == null) return;
            _saveService.CompleteLevel(session.Level.levelNumber, session.MoveCount, session.ElapsedSeconds, session.HintUsageCount);
            _saveService.Save();
            UpdateHomeProgress();
            if (_completionSequence != null) StopCoroutine(_completionSequence);
            _completionSequence = StartCoroutine(CompletionSequence(session.Level.levelNumber));
        }

        private IEnumerator CompletionSequence(int completedLevelNumber)
        {
            // 0.00 final snap is committed by PuzzleGameController.
            if (effectPool != null) effectPool.PlayCelebration(Vector2.zero);

            yield return new WaitForSecondsRealtime(0.10f);
            if (gameController != null) gameController.PlayCompletionRipple();
            if (hapticService != null) hapticService.Play(HapticCue.Correct);

            yield return new WaitForSecondsRealtime(0.30f);
            if (gameController != null) gameController.PlayWholeObjectBounce();
            if (hapticService != null) hapticService.Play(HapticCue.Completion);

            yield return new WaitForSecondsRealtime(0.30f);
            if (gameController != null) gameController.PlayObjectAction();

            yield return new WaitForSecondsRealtime(0.65f);
            if (gameController != null) gameController.PlayCompletionPop();

            yield return new WaitForSecondsRealtime(0.15f);
            _completionSequence = null;
            int count = GetLevelCount();
            if (count > 0 && _currentLevelIndex + 1 < count)
            {
                LoadLevel(_currentLevelIndex + 1, true);
            }
            else
            {
                if (gameController != null) gameController.SetPaused(true);
                UpdateHomeProgress();
                if (uiController != null) uiController.ShowHome();
            }
        }

        private void CancelCompletionSequence()
        {
            if (_completionSequence == null) return;
            StopCoroutine(_completionSequence);
            _completionSequence = null;
        }

        private void HandleFreeformProgressChanged()
        {
            SaveCurrentPuzzleProgress();
        }

        private void SaveCurrentPuzzleProgress()
        {
            if (_saveService == null || _saveService.Data == null || gameController == null || gameController.Session == null)
                return;
            PuzzleSession session = gameController.Session;
            if (session.IsComplete)
            {
                _saveService.ClearPuzzleProgress(session.Level.levelNumber);
                _saveService.Save();
                return;
            }

            PieceProgressData[] pieces = gameController.CaptureFreeformProgress();
            if (pieces.Length == 0) return;
            _saveService.SetPuzzleProgress(
                session.Level.levelNumber,
                pieces,
                session.MoveCount,
                session.ElapsedSeconds,
                session.HintUsageCount);
            _saveService.Save();
        }

        private void ApplySettings()
        {
            PlayerSaveData data = _saveService.Data;
            if (audioService != null)
            {
                audioService.SetSoundEnabled(data.soundEnabled);
                audioService.SetMusicEnabled(data.musicEnabled);
            }
            if (hapticService != null) hapticService.SetEnabled(data.hapticsEnabled);
            if (effectPool != null) effectPool.SetReducedMotion(data.reducedMotion);
            if (tutorialOverlay != null) tutorialOverlay.SetReducedMotion(data.reducedMotion);
            if (uiController != null) uiController.SetSettings(data.musicEnabled, data.soundEnabled, data.hapticsEnabled, data.reducedMotion);
        }

        private void SetMusic(bool value)
        {
            _saveService.Data.musicEnabled = value;
            if (audioService != null) audioService.SetMusicEnabled(value);
            SaveSettings();
        }

        private void SetSound(bool value)
        {
            _saveService.Data.soundEnabled = value;
            if (audioService != null) audioService.SetSoundEnabled(value);
            SaveSettings();
        }

        private void SetHaptics(bool value)
        {
            _saveService.Data.hapticsEnabled = value;
            if (hapticService != null) hapticService.SetEnabled(value);
            SaveSettings();
        }

        private void SetReducedMotion(bool value)
        {
            _saveService.Data.reducedMotion = value;
            if (effectPool != null) effectPool.SetReducedMotion(value);
            if (tutorialOverlay != null) tutorialOverlay.SetReducedMotion(value);
            SaveSettings();
        }

        private void ShowTutorial(LevelDefinition level)
        {
            if (tutorialOverlay == null || level == null || level.tutorial == null || string.IsNullOrEmpty(level.tutorial.tutorialId)) return;
            string id = level.tutorial.tutorialId;
            string[] completed = _saveService.Data.completedTutorialIds;
            for (int i = 0; i < completed.Length; i++)
            {
                if (string.Equals(completed[i], id, System.StringComparison.Ordinal)) return;
            }
            tutorialOverlay.Show(level.tutorial.message, string.Equals(id, "drag", System.StringComparison.Ordinal));
        }

        private void HandleValidAction(PuzzleActionType action)
        {
            string id = CurrentTutorialId();
            if (id == "drag" && action == PuzzleActionType.Move) CompleteCurrentTutorial();
        }

        private void HandleInvalidAction()
        {
            if (CurrentTutorialId() == "overlap") CompleteCurrentTutorial();
        }

        private void HandleHintUsed()
        {
            if (CurrentTutorialId() == "hint") CompleteCurrentTutorial();
        }

        private string CurrentTutorialId()
        {
            if (gameController == null || gameController.Session == null || gameController.Session.Level.tutorial == null) return string.Empty;
            return gameController.Session.Level.tutorial.tutorialId ?? string.Empty;
        }

        private void CompleteCurrentTutorial()
        {
            string id = CurrentTutorialId();
            if (string.IsNullOrEmpty(id)) return;
            _saveService.MarkTutorialCompleted(id);
            _saveService.Save();
            if (tutorialOverlay != null) tutorialOverlay.Hide();
        }

        private void SaveSettings()
        {
            PlayerSaveData data = _saveService.Data;
            _saveService.SetSettings(data.soundEnabled, data.musicEnabled, data.hapticsEnabled, data.reducedMotion);
            _saveService.Save();
        }

        private int GetLevelCount()
        {
            return levelCatalog == null ? 0 : levelCatalog.Count;
        }

        private void RefreshGameplayStatus()
        {
            if (uiController == null || gameController == null || gameController.Session == null) return;
            PuzzleSession session = gameController.Session;
            _lastDisplayedSecond = Mathf.Max(0, Mathf.FloorToInt(session.ElapsedSeconds));
            _lastDisplayedMoveCount = session.MoveCount;
            uiController.SetGameplayStatus(
                session.Level.levelNumber,
                session.MoveCount,
                gameController.MoveBudget,
                session.ElapsedSeconds,
                gameController.HasSnapPower);
        }

        private void UpdateHomeProgress()
        {
            if (uiController == null || _saveService == null || _saveService.Data == null) return;
            PlayerSaveData data = _saveService.Data;
            int completed = 0;
            if (data.levelProgress != null)
            {
                for (int i = 0; i < data.levelProgress.Length; i++)
                {
                    if (data.levelProgress[i] != null && data.levelProgress[i].completed) completed++;
                }
            }

            int total = Mathf.Max(1, GetLevelCount());
            uiController.SetHomeProgress(Mathf.Clamp(data.lastSelectedLevel, 1, total), completed, total);
        }
    }
}
