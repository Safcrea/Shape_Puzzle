using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToyPuzzle
{
    public enum SaveLoadStatus
    {
        LoadedPrimary,
        LoadedAndMigrated,
        RecoveredFromBackup,
        CreatedDefault
    }

    public readonly struct SaveLoadResult
    {
        public SaveLoadResult(PlayerSaveData data, SaveLoadStatus status)
        {
            Data = data;
            Status = status;
        }

        public PlayerSaveData Data { get; }
        public SaveLoadStatus Status { get; }
    }

    public readonly struct SaveWriteResult
    {
        public SaveWriteResult(bool succeeded, string error)
        {
            Succeeded = succeeded;
            Error = error;
        }

        public bool Succeeded { get; }
        public string Error { get; }
    }

    public sealed class SaveService
    {
        private readonly ISaveStorage storage;
        private readonly int maximumLevel;

        public SaveService(ISaveStorage storage, int maximumLevel = 50)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            if (maximumLevel <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumLevel));
            }

            this.maximumLevel = maximumLevel;
        }

        public PlayerSaveData Data { get; private set; }

        public SaveLoadResult Load()
        {
            bool primaryExists = storage.TryReadPrimary(out string primaryJson);
            if (primaryExists && TryDeserialize(primaryJson, out PlayerSaveData primaryData, out bool primaryMigrated))
            {
                Data = primaryData;
                if (primaryMigrated)
                {
                    Save();
                }

                return new SaveLoadResult(
                    Data,
                    primaryMigrated ? SaveLoadStatus.LoadedAndMigrated : SaveLoadStatus.LoadedPrimary);
            }

            if (primaryExists)
            {
                storage.PreserveCorruptPrimary();
            }

            if (storage.TryReadBackup(out string backupJson) &&
                TryDeserialize(backupJson, out PlayerSaveData backupData, out _))
            {
                Data = backupData;
                Save();
                return new SaveLoadResult(Data, SaveLoadStatus.RecoveredFromBackup);
            }

            Data = PlayerSaveData.CreateDefault();
            Save();
            return new SaveLoadResult(Data, SaveLoadStatus.CreatedDefault);
        }

        public SaveWriteResult Save()
        {
            EnsureLoaded();
            try
            {
                string json = JsonUtility.ToJson(Data, true);
                storage.WriteAtomically(json);
                return new SaveWriteResult(true, null);
            }
            catch (Exception exception)
            {
                return new SaveWriteResult(false, exception.Message);
            }
        }

        public LevelProgressData GetLevelProgress(int levelNumber)
        {
            EnsureLoaded();
            if (Data.levelProgress == null)
            {
                return null;
            }

            for (int i = 0; i < Data.levelProgress.Length; i++)
            {
                if (Data.levelProgress[i].levelNumber == levelNumber)
                {
                    return Data.levelProgress[i];
                }
            }

            return null;
        }

        public void CompleteLevel(int levelNumber, int moveCount, float completionSeconds, int hintsUsed)
        {
            EnsureLoaded();
            ValidateLevelNumber(levelNumber);
            if (moveCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moveCount));
            }

            if (completionSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(completionSeconds));
            }

            if (hintsUsed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hintsUsed));
            }

            LevelProgressData progress = GetOrCreateLevelProgress(levelNumber);

            progress.completed = true;
            progress.completionCount++;
            progress.totalHintsUsed += hintsUsed;
            Data.totalHintUsage += hintsUsed;
            if (progress.bestMoveCount == 0 || moveCount < progress.bestMoveCount)
            {
                progress.bestMoveCount = moveCount;
            }

            if (progress.bestCompletionSeconds <= 0f || completionSeconds < progress.bestCompletionSeconds)
            {
                progress.bestCompletionSeconds = completionSeconds;
            }

            Data.highestUnlockedLevel = Math.Min(
                maximumLevel,
                Math.Max(Data.highestUnlockedLevel, levelNumber + 1));
            Data.lastSelectedLevel = Math.Min(Data.highestUnlockedLevel, maximumLevel);
            ClearPuzzleProgress(progress);
        }

        public void SetPuzzleProgress(
            int levelNumber,
            PieceProgressData[] pieces,
            int moveCount,
            float elapsedSeconds,
            int hintsUsed)
        {
            EnsureLoaded();
            ValidateLevelNumber(levelNumber);
            if (moveCount < 0) throw new ArgumentOutOfRangeException(nameof(moveCount));
            if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (hintsUsed < 0) throw new ArgumentOutOfRangeException(nameof(hintsUsed));

            pieces = pieces ?? Array.Empty<PieceProgressData>();
            var copied = new PieceProgressData[pieces.Length];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < pieces.Length; i++)
            {
                PieceProgressData piece = pieces[i];
                if (piece == null || string.IsNullOrWhiteSpace(piece.pieceId) || !ids.Add(piece.pieceId) ||
                    float.IsNaN(piece.normalizedX) || float.IsInfinity(piece.normalizedX) ||
                    float.IsNaN(piece.normalizedY) || float.IsInfinity(piece.normalizedY) ||
                    piece.normalizedX < 0f || piece.normalizedX > 1f || piece.normalizedY < 0f || piece.normalizedY > 1f)
                {
                    throw new ArgumentException("Puzzle progress contains an invalid or duplicate piece.", nameof(pieces));
                }
                copied[i] = new PieceProgressData
                {
                    pieceId = piece.pieceId,
                    targetSlotId = piece.targetSlotId,
                    normalizedX = piece.normalizedX,
                    normalizedY = piece.normalizedY,
                    snapped = piece.snapped
                };
            }

            LevelProgressData progress = GetOrCreateLevelProgress(levelNumber);
            progress.pieceProgress = copied;
            progress.inProgressMoveCount = moveCount;
            progress.inProgressElapsedSeconds = elapsedSeconds;
            progress.inProgressHintsUsed = hintsUsed;
        }

        public void ClearPuzzleProgress(int levelNumber)
        {
            EnsureLoaded();
            ValidateLevelNumber(levelNumber);
            LevelProgressData progress = GetLevelProgress(levelNumber);
            if (progress != null) ClearPuzzleProgress(progress);
        }

        public void SetSettings(bool sound, bool music, bool haptics, bool reducedMotion)
        {
            EnsureLoaded();
            Data.soundEnabled = sound;
            Data.musicEnabled = music;
            Data.hapticsEnabled = haptics;
            Data.reducedMotion = reducedMotion;
        }

        public void SetLastSelectedLevel(int levelNumber)
        {
            EnsureLoaded();
            ValidateLevelNumber(levelNumber);
            Data.lastSelectedLevel = Math.Min(levelNumber, Data.highestUnlockedLevel);
        }

        public void MarkTutorialCompleted(string tutorialId)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(tutorialId))
            {
                throw new ArgumentException("Tutorial ID is required.", nameof(tutorialId));
            }

            for (int i = 0; i < Data.completedTutorialIds.Length; i++)
            {
                if (string.Equals(Data.completedTutorialIds[i], tutorialId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            var expanded = new string[Data.completedTutorialIds.Length + 1];
            Array.Copy(Data.completedTutorialIds, expanded, Data.completedTutorialIds.Length);
            expanded[expanded.Length - 1] = tutorialId;
            Array.Sort(expanded, StringComparer.Ordinal);
            Data.completedTutorialIds = expanded;
        }

        public void ResetProgress()
        {
            EnsureLoaded();
            bool sound = Data.soundEnabled;
            bool music = Data.musicEnabled;
            bool haptics = Data.hapticsEnabled;
            bool reducedMotion = Data.reducedMotion;
            Data = PlayerSaveData.CreateDefault();
            SetSettings(sound, music, haptics, reducedMotion);
        }

        public void UnlockAllLevels()
        {
            EnsureLoaded();
            Data.highestUnlockedLevel = maximumLevel;
        }

        private bool TryDeserialize(string json, out PlayerSaveData data, out bool migrated)
        {
            data = null;
            migrated = false;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<PlayerSaveData>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (data == null || data.version <= 0 || data.version > PlayerSaveData.CurrentVersion)
            {
                data = null;
                return false;
            }

            while (data.version < PlayerSaveData.CurrentVersion)
            {
                if (data.version == 1)
                {
                    MigrateVersionOneToTwo(data);
                    migrated = true;
                    continue;
                }

                if (data.version == 2)
                {
                    MigrateVersionTwoToThree(data);
                    migrated = true;
                    continue;
                }

                if (data.version == 3)
                {
                    MigrateVersionThreeToFour(data);
                    migrated = true;
                    continue;
                }

                data = null;
                return false;
            }

            if (!ValidateAndNormalize(data))
            {
                data = null;
                return false;
            }

            return true;
        }

        private bool ValidateAndNormalize(PlayerSaveData data)
        {
            if (data.highestUnlockedLevel < 1 || data.lastSelectedLevel < 1 || data.totalHintUsage < 0)
            {
                return false;
            }

            data.highestUnlockedLevel = Math.Min(data.highestUnlockedLevel, maximumLevel);
            data.lastSelectedLevel = Math.Min(data.lastSelectedLevel, data.highestUnlockedLevel);
            data.levelProgress = data.levelProgress ?? Array.Empty<LevelProgressData>();
            data.completedTutorialIds = data.completedTutorialIds ?? Array.Empty<string>();

            var levelNumbers = new HashSet<int>();
            for (int i = 0; i < data.levelProgress.Length; i++)
            {
                LevelProgressData progress = data.levelProgress[i];
                if (progress == null || progress.levelNumber < 1 || progress.levelNumber > maximumLevel ||
                    progress.completionCount < 0 || progress.bestMoveCount < 0 ||
                    progress.bestCompletionSeconds < 0f || progress.totalHintsUsed < 0 ||
                    progress.inProgressMoveCount < 0 || progress.inProgressElapsedSeconds < 0f ||
                    float.IsNaN(progress.inProgressElapsedSeconds) || float.IsInfinity(progress.inProgressElapsedSeconds) ||
                    progress.inProgressHintsUsed < 0 ||
                    !levelNumbers.Add(progress.levelNumber))
                {
                    return false;
                }
                progress.pieceProgress = progress.pieceProgress ?? Array.Empty<PieceProgressData>();
                var pieceIds = new HashSet<string>(StringComparer.Ordinal);
                for (int pieceIndex = 0; pieceIndex < progress.pieceProgress.Length; pieceIndex++)
                {
                    PieceProgressData piece = progress.pieceProgress[pieceIndex];
                    if (piece == null || string.IsNullOrWhiteSpace(piece.pieceId) || !pieceIds.Add(piece.pieceId) ||
                        float.IsNaN(piece.normalizedX) || float.IsInfinity(piece.normalizedX) ||
                        float.IsNaN(piece.normalizedY) || float.IsInfinity(piece.normalizedY) ||
                        piece.normalizedX < 0f || piece.normalizedX > 1f || piece.normalizedY < 0f || piece.normalizedY > 1f)
                    {
                        return false;
                    }
                }
            }

            var tutorialIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < data.completedTutorialIds.Length; i++)
            {
                string tutorialId = data.completedTutorialIds[i];
                if (string.IsNullOrWhiteSpace(tutorialId) || !tutorialIds.Add(tutorialId))
                {
                    return false;
                }
            }

            return true;
        }

        private static void MigrateVersionOneToTwo(PlayerSaveData data)
        {
            data.version = 2;
            data.levelProgress = data.levelProgress ?? Array.Empty<LevelProgressData>();
            data.completedTutorialIds = data.completedTutorialIds ?? Array.Empty<string>();
            data.reducedMotion = false;
        }

        private static void MigrateVersionTwoToThree(PlayerSaveData data)
        {
            data.version = 3;
            data.levelProgress = data.levelProgress ?? Array.Empty<LevelProgressData>();
            for (int i = 0; i < data.levelProgress.Length; i++)
            {
                if (data.levelProgress[i] != null)
                    data.levelProgress[i].pieceProgress = data.levelProgress[i].pieceProgress ?? Array.Empty<PieceProgressData>();
            }
        }

        private static void MigrateVersionThreeToFour(PlayerSaveData data)
        {
            data.version = 4;
            data.levelProgress = data.levelProgress ?? Array.Empty<LevelProgressData>();
            var preservedProgress = new List<LevelProgressData>(data.levelProgress.Length);
            for (int i = 0; i < data.levelProgress.Length; i++)
            {
                LevelProgressData progress = data.levelProgress[i];
                if (progress == null || progress.levelNumber > 10) continue;
                preservedProgress.Add(progress);
                if (progress.pieceProgress == null) continue;
                for (int pieceIndex = 0; pieceIndex < progress.pieceProgress.Length; pieceIndex++)
                {
                    PieceProgressData piece = progress.pieceProgress[pieceIndex];
                    if (piece != null && string.IsNullOrEmpty(piece.targetSlotId)) piece.targetSlotId = piece.pieceId;
                }
            }

            data.levelProgress = preservedProgress.ToArray();
            int highestUnlocked = 1;
            for (int levelNumber = 1; levelNumber <= 10; levelNumber++)
            {
                LevelProgressData progress = preservedProgress.Find(item => item.levelNumber == levelNumber);
                if (progress == null || !progress.completed) break;
                highestUnlocked = levelNumber + 1;
            }

            data.highestUnlockedLevel = Mathf.Clamp(highestUnlocked, 1, 11);
            data.lastSelectedLevel = Mathf.Clamp(data.lastSelectedLevel, 1, data.highestUnlockedLevel);
        }

        private LevelProgressData GetOrCreateLevelProgress(int levelNumber)
        {
            LevelProgressData progress = GetLevelProgress(levelNumber);
            if (progress != null) return progress;
            progress = new LevelProgressData { levelNumber = levelNumber, pieceProgress = Array.Empty<PieceProgressData>() };
            var expanded = new LevelProgressData[Data.levelProgress.Length + 1];
            Array.Copy(Data.levelProgress, expanded, Data.levelProgress.Length);
            expanded[expanded.Length - 1] = progress;
            Array.Sort(expanded, (left, right) => left.levelNumber.CompareTo(right.levelNumber));
            Data.levelProgress = expanded;
            return progress;
        }

        private static void ClearPuzzleProgress(LevelProgressData progress)
        {
            progress.pieceProgress = Array.Empty<PieceProgressData>();
            progress.inProgressMoveCount = 0;
            progress.inProgressElapsedSeconds = 0f;
            progress.inProgressHintsUsed = 0;
        }

        private void ValidateLevelNumber(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > maximumLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            }
        }

        private void EnsureLoaded()
        {
            if (Data == null)
            {
                throw new InvalidOperationException("Load must be called before accessing save data.");
            }
        }
    }
}
