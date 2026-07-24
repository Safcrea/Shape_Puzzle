using System;

namespace ToyPuzzle
{
    [Serializable]
    public sealed class PieceProgressData
    {
        public string pieceId;
        public string targetSlotId;
        public float normalizedX;
        public float normalizedY;
        public bool snapped;
    }

    [Serializable]
    public sealed class LevelProgressData
    {
        public int levelNumber;
        public bool completed;
        public int completionCount;
        public int bestMoveCount;
        public float bestCompletionSeconds;
        public int totalHintsUsed;
        public int inProgressMoveCount;
        public float inProgressElapsedSeconds;
        public int inProgressHintsUsed;
        public PieceProgressData[] pieceProgress = Array.Empty<PieceProgressData>();
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public const int CurrentVersion = 4;

        public int version;
        public int highestUnlockedLevel;
        public LevelProgressData[] levelProgress;
        public bool soundEnabled;
        public bool musicEnabled;
        public bool hapticsEnabled;
        public bool reducedMotion;
        public string[] completedTutorialIds;
        public int totalHintUsage;
        public int lastSelectedLevel;

        public static PlayerSaveData CreateDefault()
        {
            return new PlayerSaveData
            {
                version = CurrentVersion,
                highestUnlockedLevel = 1,
                levelProgress = Array.Empty<LevelProgressData>(),
                soundEnabled = true,
                musicEnabled = true,
                hapticsEnabled = true,
                reducedMotion = false,
                completedTutorialIds = Array.Empty<string>(),
                totalHintUsage = 0,
                lastSelectedLevel = 1
            };
        }
    }
}
