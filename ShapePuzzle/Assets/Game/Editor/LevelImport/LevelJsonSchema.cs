using System;

namespace ToyPuzzle.Editor.Levels
{
    public static class LevelJsonSchema
    {
        public const int CurrentVersion = 1;
        public const int MinimumSupportedVersion = 0;
        public const string SourceFolder = "Assets/Game/Data/Levels/Source";
        public const string GeneratedFolder = "Assets/Game/Data/Levels/Generated";
        public const string CatalogPath = GeneratedFolder + "/LevelCatalog.asset";
        public const string PrefabFolder = "Assets/Game/Prefabs/Levels";
        public const string PrefabCatalogPath = GeneratedFolder + "/LevelPrefabCatalog.asset";
    }

    [Serializable]
    public sealed class LevelJsonDocument
    {
        public int schemaVersion = LevelJsonSchema.CurrentVersion;
        public string levelId;
        public int levelNumber = 1;
        public string displayName;
        public string targetObjectName;
        public int boardWidth = 6;
        public int boardHeight = 6;
        public int difficultyTier = 1;
        public int scrambleSeed;
        public string paletteId = "primary";
        public int recommendedMoves;
        public PieceJson[] pieces = Array.Empty<PieceJson>();
        public bool lockCorrectPiecesByDefault;
        public HintJson hintMetadata = new HintJson();
        public TutorialJson tutorialMetadata = new TutorialJson();
        public CompletionRewardJson completionRewardData = new CompletionRewardJson();
        public ThumbnailJson thumbnailConfiguration = new ThumbnailJson();
        public string[] levelTags = Array.Empty<string>();
        public string designerNotes;
        public SolutionStepJson[] solutionCertificate = Array.Empty<SolutionStepJson>();
    }

    [Serializable]
    public sealed class PieceJson
    {
        public string pieceId;
        public string displayName;
        public string shapeType = "RoundedRectangle";
        public string colorId = "red";
        public Int2Json[] footprint = Array.Empty<Int2Json>();
        public int width = 1;
        public int height = 1;
        public Int2Json logicalPivot = new Int2Json();
        public Float2Json visualPivot = new Float2Json(0.5f, 0.5f);
        public Float2Json[] customPolygonPoints = Array.Empty<Float2Json>();
        public Int2Json targetPosition = new Int2Json();
        public int targetRotation;
        public Int2Json startingPosition = new Int2Json();
        public int startingRotation;
        public int[] allowedRotations = { 0, 90, 180, 270 };
        public bool startsLocked;
        public bool locksWhenCorrect = true;
        public bool strictTargetRotation;
        public int sortingPriority;
        public DecorativeStudJson[] decorativeStuds = Array.Empty<DecorativeStudJson>();
        public RecessedHoleJson[] recessedHoles = Array.Empty<RecessedHoleJson>();
        public Float2Json visualOverhang = new Float2Json();
        public ArtGenerationJson artGenerationParameters = new ArtGenerationJson();
    }

    [Serializable]
    public sealed class Int2Json
    {
        public int x;
        public int y;

        public Int2Json() { }

        public Int2Json(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [Serializable]
    public sealed class Float2Json
    {
        public float x;
        public float y;

        public Float2Json() { }

        public Float2Json(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [Serializable]
    public sealed class DecorativeStudJson
    {
        public Float2Json position = new Float2Json();
        public float radius = 0.12f;
    }

    [Serializable]
    public sealed class RecessedHoleJson
    {
        public Float2Json position = new Float2Json();
        public float radius = 0.2f;
    }

    [Serializable]
    public sealed class ArtGenerationJson
    {
        public float cornerRadius = 0.18f;
        public float bevelSize = 0.08f;
        public bool insetPanel;
        public string styleVariant = "classic";
    }

    [Serializable]
    public sealed class HintJson
    {
        public string message;
        public bool showDirectionalIndicator = true;
    }

    [Serializable]
    public sealed class TutorialJson
    {
        public string tutorialId;
        public string message;
        public bool requireCompletion;
    }

    [Serializable]
    public sealed class CompletionRewardJson
    {
        public int stars = 1;
        public int softCurrency;
        public string rewardId;
    }

    [Serializable]
    public sealed class ThumbnailJson
    {
        public float scale = 1f;
        public Float2Json offset = new Float2Json();
        public bool showBoard;
    }

    [Serializable]
    public sealed class SolutionStepJson
    {
        public string pieceId;
        public Int2Json position = new Int2Json();
        public int rotation;
    }
}
