using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor.Levels
{
    public interface IRuntimeLevelAssetBuilder
    {
        RuntimeLevelData Build(LevelJsonDocument source, string sourcePath);
    }

    public interface ILevelCatalogBuilder
    {
        LevelCatalog Build(IReadOnlyList<RuntimeLevelData> levels);
    }

    public sealed class RuntimeLevelAssetBuilder : IRuntimeLevelAssetBuilder
    {
        public RuntimeLevelData Build(LevelJsonDocument source, string sourcePath)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            EnsureFolder(LevelJsonSchema.GeneratedFolder);
            string assetPath = LevelJsonSchema.GeneratedFolder + "/Level_" + source.levelNumber.ToString("D3") + ".asset";
            string existingGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrEmpty(existingGuid) && !AssetDatabase.DeleteAsset(assetPath))
                throw new InvalidOperationException("Could not replace runtime level asset at " + assetPath + ".");

            RuntimeLevelData asset = ScriptableObject.CreateInstance<RuntimeLevelData>();
            AssetDatabase.CreateAsset(asset, assetPath);
            if (MonoScript.FromScriptableObject(asset) == null)
                throw new InvalidOperationException("Unity could not resolve the RuntimeLevelData MonoScript for " + assetPath + ".");

            asset.SetLevel(Convert(source));
            EditorUtility.SetDirty(asset);
            return asset;
        }

        public static LevelDefinition Convert(LevelJsonDocument source)
        {
            LevelDefinition destination = new LevelDefinition
            {
                schemaVersion = source.schemaVersion,
                levelId = source.levelId,
                levelNumber = source.levelNumber,
                displayName = source.displayName,
                targetObjectName = source.targetObjectName,
                boardWidth = source.boardWidth,
                boardHeight = source.boardHeight,
                difficultyTier = source.difficultyTier,
                scrambleSeed = source.scrambleSeed,
                paletteId = source.paletteId,
                recommendedMoves = source.recommendedMoves,
                lockCorrectPiecesByDefault = source.lockCorrectPiecesByDefault,
                pieces = ConvertPieces(source.pieces),
                hint = new HintMetadata
                {
                    message = source.hintMetadata.message,
                    showDirectionalIndicator = source.hintMetadata.showDirectionalIndicator
                },
                tutorial = new TutorialMetadata
                {
                    tutorialId = source.tutorialMetadata.tutorialId,
                    message = source.tutorialMetadata.message,
                    requireCompletion = source.tutorialMetadata.requireCompletion
                },
                completionReward = new CompletionRewardData
                {
                    stars = source.completionRewardData.stars,
                    softCurrency = source.completionRewardData.softCurrency,
                    rewardId = source.completionRewardData.rewardId
                },
                thumbnail = new ThumbnailConfiguration
                {
                    scale = source.thumbnailConfiguration.scale,
                    offset = Convert(source.thumbnailConfiguration.offset),
                    showBoard = source.thumbnailConfiguration.showBoard
                },
                tags = source.levelTags == null ? Array.Empty<string>() : (string[])source.levelTags.Clone(),
                designerNotes = source.designerNotes,
                completionAction = string.IsNullOrEmpty(source.completionAction) ? "Bounce" : source.completionAction
            };
            return destination;
        }

        private static PieceDefinition[] ConvertPieces(PieceJson[] source)
        {
            if (source == null) return Array.Empty<PieceDefinition>();
            PieceDefinition[] destination = new PieceDefinition[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                PieceJson piece = source[i];
                if (!Enum.TryParse(piece.shapeType, false, out PieceShapeType shapeType))
                    throw new InvalidOperationException("Unknown shapeType '" + piece.shapeType + "' on piece " + piece.pieceId + ".");
                destination[i] = new PieceDefinition
                {
                    pieceId = piece.pieceId,
                    displayName = piece.displayName,
                    shapeType = shapeType,
                    colorId = piece.colorId,
                    interchangeableGroupId = piece.interchangeableGroupId,
                    footprint = Convert(piece.footprint),
                    width = piece.width,
                    height = piece.height,
                    logicalPivot = Convert(piece.logicalPivot),
                    visualPivot = Convert(piece.visualPivot),
                    customPolygonPoints = Convert(piece.customPolygonPoints),
                    targetPosition = Convert(piece.targetPosition),
                    targetRotation = piece.targetRotation,
                    startingPosition = Convert(piece.startingPosition),
                    startingRotation = piece.startingRotation,
                    allowedRotations = piece.allowedRotations == null ? Array.Empty<int>() : (int[])piece.allowedRotations.Clone(),
                    startsLocked = piece.startsLocked,
                    locksWhenCorrect = piece.locksWhenCorrect,
                    requireExactTargetRotation = piece.strictTargetRotation,
                    sortingPriority = piece.sortingPriority,
                    decorativeStuds = Convert(piece.decorativeStuds),
                    recessedHoles = Convert(piece.recessedHoles),
                    visualOverhang = Convert(piece.visualOverhang),
                    artGeneration = Convert(piece.artGenerationParameters)
                };
            }
            return destination;
        }

        private static GridCoordinate Convert(Int2Json value) => value == null ? GridCoordinate.Zero : new GridCoordinate(value.x, value.y);
        private static FloatCoordinate Convert(Float2Json value) => value == null ? new FloatCoordinate() : new FloatCoordinate(value.x, value.y);

        private static GridCoordinate[] Convert(Int2Json[] values)
        {
            if (values == null) return Array.Empty<GridCoordinate>();
            GridCoordinate[] result = new GridCoordinate[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = Convert(values[i]);
            return result;
        }

        private static FloatCoordinate[] Convert(Float2Json[] values)
        {
            if (values == null) return Array.Empty<FloatCoordinate>();
            FloatCoordinate[] result = new FloatCoordinate[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = Convert(values[i]);
            return result;
        }

        private static DecorativeStudData[] Convert(DecorativeStudJson[] values)
        {
            if (values == null) return Array.Empty<DecorativeStudData>();
            DecorativeStudData[] result = new DecorativeStudData[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                DecorativeStudJson value = values[i];
                result[i] = new DecorativeStudData { position = Convert(value?.position), radius = value?.radius ?? 0.12f };
            }
            return result;
        }

        private static RecessedHoleData[] Convert(RecessedHoleJson[] values)
        {
            if (values == null) return Array.Empty<RecessedHoleData>();
            RecessedHoleData[] result = new RecessedHoleData[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                RecessedHoleJson value = values[i];
                result[i] = new RecessedHoleData { position = Convert(value?.position), radius = value?.radius ?? 0.2f };
            }
            return result;
        }

        private static ArtGenerationData Convert(ArtGenerationJson value)
        {
            if (value == null) return new ArtGenerationData();
            return new ArtGenerationData
            {
                cornerRadius = value.cornerRadius,
                bevelSize = value.bevelSize,
                insetPanel = value.insetPanel,
                styleVariant = value.styleVariant
            };
        }

        internal static void EnsureFolder(string assetFolder)
        {
            string[] segments = assetFolder.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }

    public sealed class LevelCatalogBuilder : ILevelCatalogBuilder
    {
        public LevelCatalog Build(IReadOnlyList<RuntimeLevelData> levels)
        {
            RuntimeLevelAssetBuilder.EnsureFolder(LevelJsonSchema.GeneratedFolder);
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelJsonSchema.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                AssetDatabase.CreateAsset(catalog, LevelJsonSchema.CatalogPath);
            }

            RuntimeLevelData[] ordered = new RuntimeLevelData[levels.Count];
            for (int i = 0; i < levels.Count; i++) ordered[i] = levels[i];
            Array.Sort(ordered, CompareLevels);
            catalog.SetLevels(ordered);
            EditorUtility.SetDirty(catalog);
            LevelPrefabAssetBuilder.BuildAll(ordered);
            return catalog;
        }

        private static int CompareLevels(RuntimeLevelData left, RuntimeLevelData right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            int leftNumber = left.Level == null ? int.MaxValue : left.Level.levelNumber;
            int rightNumber = right.Level == null ? int.MaxValue : right.Level.levelNumber;
            return leftNumber.CompareTo(rightNumber);
        }
    }
}
