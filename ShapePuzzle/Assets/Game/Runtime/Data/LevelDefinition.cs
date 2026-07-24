using System;
using UnityEngine;

namespace ToyPuzzle
{
    public enum PieceShapeType
    {
        RoundedRectangle,
        Square,
        Rectangle,
        Capsule,
        Circle,
        Ring,
        Triangle,
        Trapezoid,
        Wedge,
        Semicircle,
        QuarterCircle,
        LShape,
        TShape,
        UShape,
        ZShape,
        CrossShape,
        Polyomino,
        CustomGridFootprint,
        CustomPolygon
    }

    [Serializable]
    public sealed class DecorativeStudData
    {
        public FloatCoordinate position;
        public float radius = 0.12f;
    }

    [Serializable]
    public sealed class RecessedHoleData
    {
        public FloatCoordinate position;
        public float radius = 0.2f;
    }

    [Serializable]
    public sealed class ArtGenerationData
    {
        public float cornerRadius = 0.18f;
        public float bevelSize = 0.08f;
        public bool insetPanel;
        public string styleVariant;
    }

    [Serializable]
    public sealed class PieceDefinition
    {
        public string pieceId;
        public string displayName;
        public PieceShapeType shapeType;
        public string colorId;
        public GridCoordinate[] footprint = Array.Empty<GridCoordinate>();
        public int width;
        public int height;
        public GridCoordinate logicalPivot;
        public FloatCoordinate visualPivot = new FloatCoordinate(0.5f, 0.5f);
        public FloatCoordinate[] customPolygonPoints = Array.Empty<FloatCoordinate>();
        public GridCoordinate targetPosition;
        public int targetRotation;
        public GridCoordinate startingPosition;
        public int startingRotation;
        public int[] allowedRotations = { 0, 90, 180, 270 };
        public bool startsLocked;
        public bool locksWhenCorrect;
        public bool requireExactTargetRotation;
        public int sortingPriority;
        public DecorativeStudData[] decorativeStuds = Array.Empty<DecorativeStudData>();
        public RecessedHoleData[] recessedHoles = Array.Empty<RecessedHoleData>();
        public FloatCoordinate visualOverhang;
        public ArtGenerationData artGeneration = new ArtGenerationData();

        public PiecePose StartingPose => new PiecePose(startingPosition, startingRotation);
        public PiecePose TargetPose => new PiecePose(targetPosition, targetRotation);

        public bool AllowsRotation(int rotation)
        {
            int normalized = GridMath.NormalizeRotation(rotation);
            if (allowedRotations == null || allowedRotations.Length == 0)
            {
                return normalized == GridMath.NormalizeRotation(startingRotation);
            }

            for (int i = 0; i < allowedRotations.Length; i++)
            {
                if (GridMath.NormalizeRotation(allowedRotations[i]) == normalized)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class HintMetadata
    {
        public string message;
        public bool showDirectionalIndicator = true;
    }

    [Serializable]
    public sealed class TutorialMetadata
    {
        public string tutorialId;
        public string message;
        public bool requireCompletion;
    }

    [Serializable]
    public sealed class CompletionRewardData
    {
        public int stars = 1;
        public int softCurrency;
        public string rewardId;
    }

    [Serializable]
    public sealed class ThumbnailConfiguration
    {
        public float scale = 1f;
        public FloatCoordinate offset;
        public bool showBoard;
    }

    [Serializable]
    public sealed class LevelDefinition
    {
        public int schemaVersion = 1;
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
        public PieceDefinition[] pieces = Array.Empty<PieceDefinition>();
        public bool lockCorrectPiecesByDefault;
        public HintMetadata hint = new HintMetadata();
        public TutorialMetadata tutorial = new TutorialMetadata();
        public CompletionRewardData completionReward = new CompletionRewardData();
        public ThumbnailConfiguration thumbnail = new ThumbnailConfiguration();
        public string[] tags = Array.Empty<string>();
        public string designerNotes;

        public PieceDefinition FindPiece(string pieceId)
        {
            if (pieces == null || string.IsNullOrEmpty(pieceId))
            {
                return null;
            }

            for (int i = 0; i < pieces.Length; i++)
            {
                PieceDefinition piece = pieces[i];
                if (piece != null && string.Equals(piece.pieceId, pieceId, StringComparison.Ordinal))
                {
                    return piece;
                }
            }

            return null;
        }
    }

}
