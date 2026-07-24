using System;
using UnityEngine;
using UnityEngine.UI;

namespace ToyPuzzle
{
    public sealed class ToyUIController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private ScreenManager screenManager;
        [SerializeField] private Button playButton;
        [SerializeField] private Button levelsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button nextTestButton;
        [SerializeField] private Button levelSelectBackButton;
        [SerializeField] private Text homeProgressText;
        [SerializeField] private Text gameplayStatsText;
        [SerializeField] private Text hintButtonText;

        [Header("Settings")]
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Toggle reducedMotionToggle;
        [SerializeField] private Button settingsCloseButton;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private ToyTween tween;
        [SerializeField] private ToyEffectPool effectPool;
        [SerializeField] private AudioService audioService;

        [Header("Pause and completion")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button pauseExitButton;
        [SerializeField] private Button completionReplayButton;
        [SerializeField] private Button completionNextButton;
        [SerializeField] private Button completionLevelsButton;
        [SerializeField] private Text completionTitle;
        [SerializeField] private Text completionStats;

        [Header("Confirmations")]
        [SerializeField] private Button resetConfirmButton;
        [SerializeField] private Button resetCancelButton;
        [SerializeField] private Button progressResetConfirmButton;
        [SerializeField] private Button progressResetCancelButton;

        public event Action PlayRequested;
        public event Action LevelSelectRequested;
        public event Action HomeRequested;
        public event Action UndoRequested;
        public event Action HintRequested;
        public event Action RotateRequested;
        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action RestartRequested;
        public event Action ReplayRequested;
        public event Action NextLevelRequested;
        public event Action ResetProgressRequested;
        public event Action<bool> MusicChanged;
        public event Action<bool> SoundChanged;
        public event Action<bool> HapticsChanged;
        public event Action<bool> ReducedMotionChanged;

        private void Awake()
        {
            BindButtons(true);
        }

        private void OnDestroy()
        {
            BindButtons(false);
        }

        public void ShowHome() { screenManager.Show(GameScreenId.Home); }
        public void ShowGameplay() { screenManager.Show(GameScreenId.Gameplay); }
        public void ShowLevelSelect() { screenManager.Show(GameScreenId.LevelSelect); }

        public void SetHomeProgress(int currentLevel, int completedLevels, int totalLevels)
        {
            if (homeProgressText == null) return;
            homeProgressText.text = "LEVEL " + Mathf.Max(1, currentLevel) + "  •  " +
                                    Mathf.Max(0, completedLevels) + " / " + Mathf.Max(1, totalLevels) + " COMPLETE";
        }

        public void SetGameplayStatus(int levelNumber, int moves, int moveBudget, float seconds, bool snapPowerAvailable)
        {
            if (gameplayStatsText != null)
            {
                gameplayStatsText.text = "LEVEL " + Mathf.Max(1, levelNumber).ToString("00") +
                                         "  |  MOVES " + Mathf.Max(0, moves) + "/" + Mathf.Max(1, moveBudget) +
                                         "  |  TIME " + FormatTime(seconds);
            }
            if (hintButtonText != null) hintButtonText.text = snapPowerAvailable ? "POWER" : "HINT";
        }

        public void ShowCompletion(string objectName, int moves, int bestMoves, float seconds, float bestSeconds)
        {
            if (completionTitle != null) completionTitle.text = UIStrings.LevelComplete + "\n" + objectName;
            if (completionStats != null)
            {
                completionStats.text = "MOVES  " + moves + "   BEST  " + bestMoves + "\nTIME  " + FormatTime(seconds) + "   BEST  " + FormatTime(bestSeconds);
            }
            if (effectPool != null) effectPool.PlayCelebration(Vector2.zero);
            screenManager.Show(GameScreenId.Completion);
        }

        public void SetUndoAvailable(bool available)
        {
            if (undoButton != null) undoButton.interactable = available;
        }

        public void SetRotationAvailable(bool available)
        {
            if (rotateButton != null) rotateButton.interactable = available;
        }

        public void SetSettings(bool music, bool sound, bool haptics, bool reducedMotion)
        {
            if (musicToggle != null) musicToggle.SetIsOnWithoutNotify(music);
            if (soundToggle != null) soundToggle.SetIsOnWithoutNotify(sound);
            if (hapticsToggle != null) hapticsToggle.SetIsOnWithoutNotify(haptics);
            if (reducedMotionToggle != null) reducedMotionToggle.SetIsOnWithoutNotify(reducedMotion);
            if (screenManager != null) screenManager.SetReducedMotion(reducedMotion);
            if (tween != null) tween.SetReducedMotion(reducedMotion);
            if (effectPool != null) effectPool.SetReducedMotion(reducedMotion);
        }

        private void BindButtons(bool bind)
        {
            Bind(playButton, HandlePlay, bind);
            Bind(levelsButton, HandleLevels, bind);
            Bind(settingsButton, HandleSettings, bind);
            Bind(homeButton, HandleHome, bind);
            Bind(resetButton, HandleResetPrompt, bind);
            Bind(undoButton, HandleUndo, bind);
            Bind(hintButton, HandleHint, bind);
            Bind(rotateButton, HandleRotate, bind);
            Bind(pauseButton, HandlePause, bind);
            Bind(nextTestButton, HandleNext, bind);
            Bind(levelSelectBackButton, HandleHome, bind);
            Bind(settingsCloseButton, HandleCloseSettings, bind);
            Bind(resetProgressButton, HandleProgressResetPrompt, bind);
            Bind(resumeButton, HandleResume, bind);
            Bind(pauseRestartButton, HandleResetPrompt, bind);
            Bind(pauseSettingsButton, HandleSettings, bind);
            Bind(pauseExitButton, HandleLevels, bind);
            Bind(completionReplayButton, HandleReplay, bind);
            Bind(completionNextButton, HandleNext, bind);
            Bind(completionLevelsButton, HandleLevels, bind);
            Bind(resetConfirmButton, HandleRestartConfirmed, bind);
            Bind(resetCancelButton, HandleResetCancelled, bind);
            Bind(progressResetConfirmButton, HandleProgressResetConfirmed, bind);
            Bind(progressResetCancelButton, HandleProgressResetCancelled, bind);
            Bind(musicToggle, HandleMusic, bind);
            Bind(soundToggle, HandleSound, bind);
            Bind(hapticsToggle, HandleHaptics, bind);
            Bind(reducedMotionToggle, HandleReducedMotion, bind);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action, bool bind)
        {
            if (button == null) return;
            if (bind) button.onClick.AddListener(action); else button.onClick.RemoveListener(action);
        }

        private static void Bind(Toggle toggle, UnityEngine.Events.UnityAction<bool> action, bool bind)
        {
            if (toggle == null) return;
            if (bind) toggle.onValueChanged.AddListener(action); else toggle.onValueChanged.RemoveListener(action);
        }

        private void HandlePlay() { Click(); if (PlayRequested != null) PlayRequested(); screenManager.Show(GameScreenId.Gameplay); }
        private void HandleLevels() { Click(); if (LevelSelectRequested != null) LevelSelectRequested(); screenManager.Show(GameScreenId.LevelSelect); }
        private void HandleHome() { Click(); if (HomeRequested != null) HomeRequested(); screenManager.Show(GameScreenId.Home); }
        private void HandleSettings() { Click(); screenManager.Show(GameScreenId.Settings); }
        private void HandleCloseSettings() { Click(); screenManager.ClosePopup(GameScreenId.Settings); }
        private void HandleResetPrompt() { Click(); screenManager.Show(GameScreenId.ResetConfirmation); }
        private void HandleProgressResetPrompt() { Click(); screenManager.Show(GameScreenId.ProgressResetConfirmation); }
        private void HandleUndo() { if (UndoRequested != null) UndoRequested(); }
        private void HandleHint() { if (HintRequested != null) HintRequested(); }
        private void HandleRotate() { if (RotateRequested != null) RotateRequested(); }
        private void HandlePause() { Click(); if (PauseRequested != null) PauseRequested(); screenManager.Show(GameScreenId.Pause); }
        private void HandleResume() { Click(); screenManager.ClosePopup(GameScreenId.Pause); if (ResumeRequested != null) ResumeRequested(); }
        private void HandleReplay() { Click(); screenManager.ClosePopup(GameScreenId.Completion); if (ReplayRequested != null) ReplayRequested(); }
        private void HandleNext() { Click(); screenManager.ClosePopup(GameScreenId.Completion); if (NextLevelRequested != null) NextLevelRequested(); }
        private void HandleRestartConfirmed() { Click(); screenManager.ClosePopup(GameScreenId.ResetConfirmation); if (RestartRequested != null) RestartRequested(); }
        private void HandleResetCancelled() { Click(); screenManager.ClosePopup(GameScreenId.ResetConfirmation); }
        private void HandleProgressResetConfirmed() { Click(); screenManager.ClosePopup(GameScreenId.ProgressResetConfirmation); if (ResetProgressRequested != null) ResetProgressRequested(); }
        private void HandleProgressResetCancelled() { Click(); screenManager.ClosePopup(GameScreenId.ProgressResetConfirmation); }
        private void HandleMusic(bool value) { if (MusicChanged != null) MusicChanged(value); }
        private void HandleSound(bool value) { if (SoundChanged != null) SoundChanged(value); }
        private void HandleHaptics(bool value) { if (HapticsChanged != null) HapticsChanged(value); }
        private void HandleReducedMotion(bool value)
        {
            screenManager.SetReducedMotion(value);
            if (tween != null) tween.SetReducedMotion(value);
            if (effectPool != null) effectPool.SetReducedMotion(value);
            if (ReducedMotionChanged != null) ReducedMotionChanged(value);
        }

        private static string FormatTime(float seconds)
        {
            int value = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (value / 60).ToString("00") + ":" + (value % 60).ToString("00");
        }

        private void Click()
        {
            if (audioService != null) audioService.Play(ToyAudioCue.ButtonClick);
        }
    }
}
