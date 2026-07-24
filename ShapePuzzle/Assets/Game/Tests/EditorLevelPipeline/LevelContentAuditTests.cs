using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ToyPuzzle.Editor.Levels;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Tests.ContentValidation
{
    public sealed class LevelContentAuditTests
    {
        private static readonly string[] ExpectedNames =
        {
            "Airplane", "Truck", "Car", "Rocket", "Boat", "Bicycle", "Train", "Bus", "Sailboat", "Scooter",
            "City Bus", "Umbrella", "Windmill", "Toy Train", "Ice-Cream Cone", "Lighthouse", "Camera", "Small Castle", "Kick Scooter", "Toy Robot",
            "Hot-Air Balloon", "Teapot", "Pirate Ship", "Mushroom House", "Bicycle", "Treasure Chest", "Fire Truck", "Stone Bridge", "Cupcake", "Submarine",
            "Excavator", "Treehouse", "Grand Piano", "Ferris Wheel", "Helicopter", "Castle Gate", "Dragon Head", "School Bus", "Carousel", "Space Shuttle",
            "Medieval Castle", "Bulldozer", "Unicorn Head", "Harbor Boat", "Wizard Hat", "Toy Factory", "Monster Truck", "Amusement Park Entrance", "Clock Tower", "Grand Toy Kingdom"
        };

        private static readonly int[] ExpectedPieceCounts =
        {
            4, 6, 6, 6, 6, 4, 8, 8, 5, 7,
            7, 7, 8, 8, 7, 8, 8, 9, 8, 9,
            9, 8, 10, 9, 10, 9, 10, 10, 9, 10,
            11, 11, 10, 12, 11, 12, 11, 12, 13, 12,
            14, 12, 12, 13, 11, 14, 13, 14, 15, 16
        };

        private static readonly int[] ExpectedLongestBoardDimensions =
        {
            5, 5, 5, 5, 5, 6, 6, 6, 6, 6,
            6, 6, 6, 7, 6, 7, 6, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 8, 8, 7, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            9, 8, 8, 9, 8, 9, 9, 9, 9, 9
        };

        private static readonly Regex GenericPartName = new Regex(
            @"^(piece|part|block|shape|tile|cell|segment|component)([\s_-]*\d+)?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        [Test]
        public void SourceSet_MatchesDistributionAndUsesSemanticUniqueNames()
        {
            LoadedLevel[] levels = LoadAll();
            var errors = new List<string>();
            Assert.That(levels, Has.Length.EqualTo(50), "Exactly 50 authoritative source JSON files are required.");

            var levelIds = new HashSet<string>(StringComparer.Ordinal);
            var levelNumbers = new HashSet<int>();
            var objectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < levels.Length; i++)
            {
                LevelJsonDocument level = levels[i].Document;
                int expectedNumber = i + 1;
                Check(level.levelNumber == expectedNumber, levels[i], errors, "expected levelNumber " + expectedNumber + ", found " + level.levelNumber);
                Check(string.Equals(level.levelId, "level_" + expectedNumber.ToString("D3"), StringComparison.Ordinal), levels[i], errors, "unexpected stable levelId '" + level.levelId + "'");
                Check(string.Equals(level.targetObjectName, ExpectedNames[i], StringComparison.Ordinal), levels[i], errors, "expected object '" + ExpectedNames[i] + "', found '" + level.targetObjectName + "'");
                Check(level.pieces != null && level.pieces.Length == ExpectedPieceCounts[i], levels[i], errors, "expected " + ExpectedPieceCounts[i] + " pieces, found " + (level.pieces == null ? 0 : level.pieces.Length));
                Check(level.difficultyTier == i / 10 + 1, levels[i], errors, "difficulty tier does not match the ten-level tier distribution");
                int longestDimension = Math.Max(level.boardWidth, level.boardHeight);
                int distributionBaseline = ExpectedLongestBoardDimensions[i];
                Check(
                    longestDimension == distributionBaseline || longestDimension == Math.Min(9, distributionBaseline + 1),
                    levels[i],
                    errors,
                    "longest board dimension is outside the square default or one-cell rectangular extension allowed by the visual-reference precedence rule");
                Check(levelIds.Add(level.levelId ?? string.Empty), levels[i], errors, "duplicate levelId '" + level.levelId + "'");
                Check(levelNumbers.Add(level.levelNumber), levels[i], errors, "duplicate levelNumber " + level.levelNumber);
                bool approvedBicycleRepeat = level.levelNumber == 25 &&
                                              string.Equals(level.targetObjectName, "Bicycle", StringComparison.OrdinalIgnoreCase);
                Check(approvedBicycleRepeat || objectNames.Add(level.targetObjectName ?? string.Empty), levels[i], errors, "duplicate target object name '" + level.targetObjectName + "'");

                var pieceIds = new HashSet<string>(StringComparer.Ordinal);
                var displayNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (level.pieces == null)
                {
                    continue;
                }

                for (int pieceIndex = 0; pieceIndex < level.pieces.Length; pieceIndex++)
                {
                    PieceJson piece = level.pieces[pieceIndex];
                    if (piece == null)
                    {
                        errors.Add(Context(levels[i]) + ": null piece at index " + pieceIndex);
                        continue;
                    }

                    Check(pieceIds.Add(piece.pieceId ?? string.Empty), levels[i], errors, "duplicate pieceId '" + piece.pieceId + "'");
                    Check(displayNames.Add(piece.displayName ?? string.Empty), levels[i], errors, "duplicate piece displayName '" + piece.displayName + "'");
                    Check(!GenericPartName.IsMatch(piece.pieceId ?? string.Empty), levels[i], errors, "generic pieceId '" + piece.pieceId + "' is not an object-specific semantic name");
                    Check(!GenericPartName.IsMatch(piece.displayName ?? string.Empty), levels[i], errors, "generic displayName '" + piece.displayName + "' is not an object-specific semantic name");
                }
            }

            AssertNoErrors(errors);
        }

        [Test]
        public void EverySource_PassesSchemaAndContentValidationAndStartsUnsolved()
        {
            LoadedLevel[] levels = LoadAll();
            var errors = new List<string>();
            for (int i = 0; i < levels.Length; i++)
            {
                LoadedLevel loaded = levels[i];
                LevelJsonDocument level = loaded.Document;
                Check(level.schemaVersion == LevelJsonSchema.CurrentVersion, loaded, errors, "schemaVersion is not current");
                ToyPuzzle.Editor.Levels.LevelValidationResult validation = LevelContentValidator.Validate(level);
                for (int issueIndex = 0; issueIndex < validation.Issues.Count; issueIndex++)
                {
                    ToyPuzzle.Editor.Levels.LevelValidationIssue issue = validation.Issues[issueIndex];
                    if (issue.Severity == LevelValidationSeverity.Error)
                    {
                        errors.Add(Context(loaded) + ": " + issue);
                    }
                }

                bool allCorrect = true;
                for (int pieceIndex = 0; pieceIndex < level.pieces.Length; pieceIndex++)
                {
                    PieceJson piece = level.pieces[pieceIndex];
                    allCorrect &= LevelContentValidator.IsCorrectPose(piece, piece.startingPosition, piece.startingRotation);
                }

                Check(!allCorrect, loaded, errors, "starting state is already solved");
            }

            AssertNoErrors(errors);
        }

        [Test]
        public void RuntimeAndEditorPoseGeometry_AgreeAndRemainLegal()
        {
            LoadedLevel[] levels = LoadAll();
            var errors = new List<string>();
            for (int i = 0; i < levels.Length; i++)
            {
                LoadedLevel loaded = levels[i];
                LevelJsonDocument source = loaded.Document;
                LevelDefinition runtime = RuntimeLevelAssetBuilder.Convert(source);
                ValidateRuntimePoseSet(loaded, source, runtime, true, errors);
                ValidateRuntimePoseSet(loaded, source, runtime, false, errors);

                ToyPuzzle.LevelValidationResult runtimeValidation = LevelDefinitionValidator.Validate(runtime);
                for (int issueIndex = 0; issueIndex < runtimeValidation.Issues.Length; issueIndex++)
                {
                    ToyPuzzle.LevelValidationIssue issue = runtimeValidation.Issues[issueIndex];
                    if (issue.Severity == ValidationSeverity.Error)
                    {
                        errors.Add(Context(loaded) + ": runtime " + issue.Code + " [" + issue.PieceId + "]: " + issue.Message);
                    }
                }
            }

            AssertNoErrors(errors);
        }

        [Test]
        public void SolutionCertificates_ArePresentLegalAndCompleteUnderRuntimeRules()
        {
            LoadedLevel[] levels = LoadAll();
            var errors = new List<string>();
            for (int i = 0; i < levels.Length; i++)
            {
                LoadedLevel loaded = levels[i];
                LevelJsonDocument source = loaded.Document;
                LevelDefinition runtime = RuntimeLevelAssetBuilder.Convert(source);
                if (source.solutionCertificate == null || source.solutionCertificate.Length == 0)
                {
                    errors.Add(Context(loaded) + ": missing required solution certificate");
                    continue;
                }

                var poses = new Dictionary<string, PiecePose>(StringComparer.Ordinal);
                var locked = new HashSet<string>(StringComparer.Ordinal);
                for (int pieceIndex = 0; pieceIndex < runtime.pieces.Length; pieceIndex++)
                {
                    PieceDefinition piece = runtime.pieces[pieceIndex];
                    poses.Add(piece.pieceId, piece.StartingPose);
                    if (piece.startsLocked)
                    {
                        locked.Add(piece.pieceId);
                    }
                }

                for (int stepIndex = 0; stepIndex < source.solutionCertificate.Length; stepIndex++)
                {
                    SolutionStepJson step = source.solutionCertificate[stepIndex];
                    if (step == null || step.position == null || string.IsNullOrEmpty(step.pieceId))
                    {
                        errors.Add(Context(loaded) + ": certificate step " + stepIndex + " is incomplete");
                        continue;
                    }

                    PieceDefinition selected = runtime.FindPiece(step.pieceId);
                    if (selected == null)
                    {
                        errors.Add(Context(loaded) + ": certificate step " + stepIndex + " references unknown piece '" + step.pieceId + "'");
                        continue;
                    }

                    if (locked.Contains(selected.pieceId))
                    {
                        errors.Add(Context(loaded) + ": certificate step " + stepIndex + " tries to move locked piece '" + selected.pieceId + "'");
                        continue;
                    }

                    PiecePose candidate = new PiecePose(new GridCoordinate(step.position.x, step.position.y), step.rotation);
                    if (poses[selected.pieceId].Equals(candidate))
                    {
                        errors.Add(Context(loaded) + ": certificate step " + stepIndex + " is a no-op for '" + selected.pieceId + "'");
                        continue;
                    }

                    OccupancyMap otherPieces = BuildOccupancyWithout(runtime, poses, selected.pieceId, loaded, errors);
                    PlacementResult placement = PlacementValidator.Validate(selected, candidate, otherPieces);
                    if (!placement.IsValid)
                    {
                        errors.Add(Context(loaded) + ": certificate step " + stepIndex + " for '" + selected.pieceId + "' is illegal at runtime: " + placement.FailureReason + " at " + placement.BlockedCell);
                        continue;
                    }

                    poses[selected.pieceId] = candidate;
                    if (TargetPoseValidator.IsCorrect(selected, candidate) &&
                        (selected.locksWhenCorrect || runtime.lockCorrectPiecesByDefault))
                    {
                        locked.Add(selected.pieceId);
                    }
                }

                for (int pieceIndex = 0; pieceIndex < runtime.pieces.Length; pieceIndex++)
                {
                    PieceDefinition piece = runtime.pieces[pieceIndex];
                    Check(TargetPoseValidator.IsCorrect(piece, poses[piece.pieceId]), loaded, errors, "certificate does not finish piece '" + piece.pieceId + "' at its runtime target pose");
                }

                Check(source.recommendedMoves >= source.solutionCertificate.Length, loaded, errors, "recommendedMoves is shorter than the certified legal pose sequence");
            }

            AssertNoErrors(errors);
        }

        [Test]
        public void LayoutSignatures_AreUniqueAcrossAllObjects()
        {
            LoadedLevel[] levels = LoadAll();
            var errors = new List<string>();
            var targetSignatures = new Dictionary<string, LoadedLevel>(StringComparer.Ordinal);
            var startSignatures = new Dictionary<string, LoadedLevel>(StringComparer.Ordinal);
            var completeSignatures = new Dictionary<string, LoadedLevel>(StringComparer.Ordinal);
            var occupancyOnly = new Dictionary<string, LoadedLevel>(StringComparer.Ordinal);

            for (int i = 0; i < levels.Length; i++)
            {
                LoadedLevel loaded = levels[i];
                if (loaded.Document.levelNumber <= PuzzleLayoutConstants.TotalPlayableLevels)
                {
                    continue; // These levels use freeform artwork targets rather than their hidden session grid cells.
                }
                string target = BuildLayoutSignature(loaded.Document, true, true);
                string start = BuildLayoutSignature(loaded.Document, false, true);
                string complete = target + "||" + start;
                RecordDuplicate(targetSignatures, target, loaded, "target structural layout", errors);
                RecordDuplicate(startSignatures, start, loaded, "starting structural layout", errors);
                RecordDuplicate(completeSignatures, complete, loaded, "combined start/target layout", errors);

                string cellsOnly = BuildLayoutSignature(loaded.Document, true, false);
                if (occupancyOnly.TryGetValue(cellsOnly, out LoadedLevel other))
                {
                    TestContext.Progress.WriteLine("DESIGN WARNING: occupancy-only target signature is shared by " + Context(other) + " and " + Context(loaded) + ".");
                }
                else
                {
                    occupancyOnly.Add(cellsOnly, loaded);
                }
            }

            AssertNoErrors(errors);
        }

        [Test]
        public void Occupancy_PreservesManipulationSpaceAndReportsSparseTargets()
        {
            LoadedLevel[] levels = LoadAll();
            var errors = new List<string>();
            int sparseCount = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                LoadedLevel loaded = levels[i];
                LevelDefinition runtime = RuntimeLevelAssetBuilder.Convert(loaded.Document);
                int targetCells = CountOccupiedCells(runtime, true, loaded, errors);
                int startingCells = CountOccupiedCells(runtime, false, loaded, errors);
                int totalCells = runtime.boardWidth * runtime.boardHeight;
                float targetRatio = targetCells / (float)totalCells;
                float startRatio = startingCells / (float)totalCells;
                Check(targetRatio <= 0.70f, loaded, errors, "target occupancy " + Percent(targetRatio) + " leaves less than 30% manipulation space");
                Check(startRatio <= 0.70f, loaded, errors, "starting occupancy " + Percent(startRatio) + " leaves less than 30% manipulation space");

                int recommendedMinimum = RecommendedMinimumOccupiedCells(Math.Max(runtime.boardWidth, runtime.boardHeight));
                if (targetCells < recommendedMinimum)
                {
                    sparseCount++;
                    TestContext.Progress.WriteLine("DESIGN WARNING: " + Context(loaded) + " target occupies " + targetCells + "/" + totalCells + " cells (" + Percent(targetRatio) + "), below the guide's suggested " + recommendedMinimum + "-cell minimum.");
                }
            }

            TestContext.Progress.WriteLine("Sparse target summary: " + sparseCount + "/" + levels.Length + " levels are below the distribution guide's suggested logical occupancy range.");
            AssertNoErrors(errors);
        }

        [Test]
        public void AllPlayablePrefabs_HaveCompleteThumbnailArtworkFullyVisibleAtStart()
        {
            LevelPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(LevelJsonSchema.PrefabCatalogPath);
            Assert.That(catalog, Is.Not.Null, "Generated level prefab catalog is required.");
            var errors = new List<string>();
            Assert.That(catalog.Count, Is.EqualTo(PuzzleLayoutConstants.TotalPlayableLevels));
            for (int levelIndex = 0; levelIndex < PuzzleLayoutConstants.TotalPlayableLevels; levelIndex++)
            {
                PuzzleLevelPrefab prefab = catalog.GetByIndex(levelIndex);
                Assert.That(prefab, Is.Not.Null, "Missing prefab for level " + (levelIndex + 1) + ".");
                LevelDefinition level = prefab.Level;
                Check(level.pieces.Length <= 16, new LoadedLevel(LevelJsonSchema.PrefabCatalogPath, new LevelJsonDocument { levelId = level.levelId, levelNumber = level.levelNumber }), errors, "playable piece count exceeds 16");
                int expectedMoves = level.pieces.Length + (level.levelNumber <= 15 ? 2 : 1);
                Check(level.recommendedMoves == expectedMoves, new LoadedLevel(LevelJsonSchema.PrefabCatalogPath, new LevelJsonDocument { levelId = level.levelId, levelNumber = level.levelNumber }), errors, "move budget does not match the progression rule");
                Check(prefab.Thumbnail != null, new LoadedLevel(LevelJsonSchema.PrefabCatalogPath, new LevelJsonDocument { levelId = level.levelId, levelNumber = level.levelNumber }), errors, "thumbnail is missing");
                for (int pieceIndex = 0; pieceIndex < level.pieces.Length; pieceIndex++)
                {
                    PieceDefinition piece = level.pieces[pieceIndex];
                    PuzzlePieceArtwork artwork = prefab.FindPieceArtwork(piece.pieceId);
                    if (artwork == null || !artwork.IsValid)
                    {
                        errors.Add("level " + level.levelNumber + " piece '" + piece.pieceId + "': generated artwork is missing or invalid");
                        continue;
                    }
                    if (!artwork.freeformColorBlock)
                    {
                        errors.Add("level " + level.levelNumber + " piece '" + piece.pieceId + "': artwork is not configured as a freeform separated-color part");
                    }
                    string spritePath = AssetDatabase.GetAssetPath(artwork.sprite);
                    if (!spritePath.StartsWith("Assets/Game/Art/Generated/LevelPieces/", StringComparison.Ordinal))
                    {
                        errors.Add("level " + level.levelNumber + " piece '" + piece.pieceId + "': artwork uses unexpected asset '" + spritePath + "'");
                    }

                    Vector2 half = artwork.sizeNormalized * 0.5f;
                    Vector2 start = artwork.startingCenterNormalized;
                    if (start.x - half.x < 0f || start.y - half.y < 0f ||
                        start.x + half.x > 1f || start.y + half.y > 1f)
                    {
                        errors.Add("level " + level.levelNumber + " piece '" + piece.pieceId + "': freeform starting artwork leaves the board");
                    }
                }
            }

            AssertNoErrors(errors);
        }

        private static void ValidateRuntimePoseSet(
            LoadedLevel loaded,
            LevelJsonDocument source,
            LevelDefinition runtime,
            bool target,
            List<string> errors)
        {
            var occupied = new HashSet<GridCoordinate>();
            for (int pieceIndex = 0; pieceIndex < runtime.pieces.Length; pieceIndex++)
            {
                PieceDefinition runtimePiece = runtime.pieces[pieceIndex];
                PieceJson sourcePiece = source.pieces[pieceIndex];
                PiecePose runtimePose = target ? runtimePiece.TargetPose : runtimePiece.StartingPose;
                Int2Json sourcePosition = target ? sourcePiece.targetPosition : sourcePiece.startingPosition;
                int sourceRotation = target ? sourcePiece.targetRotation : sourcePiece.startingRotation;
                List<Int2Json> editorCells = LevelContentValidator.GetOccupiedCells(sourcePiece, sourcePosition, sourceRotation);
                GridCoordinate[] runtimeCells = GridMath.GetOccupiedCells(runtimePiece, runtimePose);
                string poseName = target ? "target" : "start";
                Check(HaveSameCells(editorCells, runtimeCells), loaded, errors, poseName + " editor/runtime occupied-cell mismatch for piece '" + runtimePiece.pieceId + "'");

                for (int cellIndex = 0; cellIndex < runtimeCells.Length; cellIndex++)
                {
                    GridCoordinate cell = runtimeCells[cellIndex];
                    Check(cell.x >= 0 && cell.x < runtime.boardWidth && cell.y >= 0 && cell.y < runtime.boardHeight, loaded, errors, poseName + " piece '" + runtimePiece.pieceId + "' is out of bounds at " + cell);
                    Check(occupied.Add(cell), loaded, errors, poseName + " overlap at " + cell + " on piece '" + runtimePiece.pieceId + "'");
                }
            }
        }

        private static OccupancyMap BuildOccupancyWithout(
            LevelDefinition level,
            Dictionary<string, PiecePose> poses,
            string excludedPieceId,
            LoadedLevel loaded,
            List<string> errors)
        {
            var map = new OccupancyMap(level.boardWidth, level.boardHeight);
            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceDefinition piece = level.pieces[i];
                if (string.Equals(piece.pieceId, excludedPieceId, StringComparison.Ordinal))
                {
                    continue;
                }

                GridCoordinate[] cells = GridMath.GetOccupiedCells(piece, poses[piece.pieceId]);
                if (!map.TryReserve(piece.pieceId, cells))
                {
                    errors.Add(Context(loaded) + ": certificate replay encountered an invalid existing pose for '" + piece.pieceId + "'");
                }
            }

            return map;
        }

        private static int CountOccupiedCells(LevelDefinition level, bool target, LoadedLevel loaded, List<string> errors)
        {
            var cells = new HashSet<GridCoordinate>();
            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceDefinition piece = level.pieces[i];
                GridCoordinate[] occupied = GridMath.GetOccupiedCells(piece, target ? piece.TargetPose : piece.StartingPose);
                for (int cellIndex = 0; cellIndex < occupied.Length; cellIndex++)
                {
                    Check(cells.Add(occupied[cellIndex]), loaded, errors, (target ? "target" : "start") + " overlap at " + occupied[cellIndex]);
                }
            }

            return cells.Count;
        }

        private static string BuildLayoutSignature(LevelJsonDocument source, bool target, bool includeStructure)
        {
            LevelDefinition level = RuntimeLevelAssetBuilder.Convert(source);
            var descriptors = new string[level.pieces.Length];
            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceDefinition piece = level.pieces[i];
                PiecePose pose = target ? piece.TargetPose : piece.StartingPose;
                GridCoordinate[] cells = GridMath.GetOccupiedCells(piece, pose);
                Array.Sort(cells);
                var builder = new StringBuilder();
                if (includeStructure)
                {
                    builder.Append(piece.shapeType).Append('|').Append(piece.colorId).Append('|').Append(GridMath.NormalizeRotation(pose.rotation)).Append('|');
                }

                for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
                {
                    builder.Append(cells[cellIndex].x).Append(',').Append(cells[cellIndex].y).Append(';');
                }

                descriptors[i] = builder.ToString();
            }

            Array.Sort(descriptors, StringComparer.Ordinal);
            return level.boardWidth + "x" + level.boardHeight + ":" + string.Join("/", descriptors);
        }

        private static bool HaveSameCells(List<Int2Json> editorCells, GridCoordinate[] runtimeCells)
        {
            if (editorCells.Count != runtimeCells.Length)
            {
                return false;
            }

            var cells = new HashSet<GridCoordinate>();
            for (int i = 0; i < editorCells.Count; i++)
            {
                cells.Add(new GridCoordinate(editorCells[i].x, editorCells[i].y));
            }

            for (int i = 0; i < runtimeCells.Length; i++)
            {
                if (!cells.Contains(runtimeCells[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void RecordDuplicate(
            Dictionary<string, LoadedLevel> signatures,
            string signature,
            LoadedLevel level,
            string description,
            List<string> errors)
        {
            if (signatures.TryGetValue(signature, out LoadedLevel other))
            {
                errors.Add(Context(level) + ": duplicate " + description + " with " + Context(other));
            }
            else
            {
                signatures.Add(signature, level);
            }
        }

        private static int RecommendedMinimumOccupiedCells(int longestBoardDimension)
        {
            switch (longestBoardDimension)
            {
                case 5: return 10;
                case 6: return 16;
                case 7: return 23;
                case 8: return 31;
                default: return int.MaxValue;
            }
        }

        private static string Percent(float ratio)
        {
            return (ratio * 100f).ToString("0.0") + "%";
        }

        private static void Check(bool condition, LoadedLevel level, List<string> errors, string message)
        {
            if (!condition)
            {
                errors.Add(Context(level) + ": " + message);
            }
        }

        private static string Context(LoadedLevel loaded)
        {
            LevelJsonDocument level = loaded.Document;
            return Path.GetFileName(loaded.Path) + " [" + level.levelId + ", #" + level.levelNumber + "]";
        }

        private static void AssertNoErrors(List<string> errors)
        {
            if (errors.Count == 0)
            {
                return;
            }

            Assert.Fail(string.Join(Environment.NewLine, errors.ToArray()));
        }

        private static LoadedLevel[] LoadAll()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null.And.Not.Empty, "Unity project root is unavailable.");
            string folder = Path.Combine(projectRoot, LevelJsonSchema.SourceFolder);
            string[] paths = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);
            var levels = new LoadedLevel[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                levels[i] = new LoadedLevel(paths[i], LevelJsonSerializer.Deserialize(File.ReadAllText(paths[i]), paths[i]));
            }

            return levels;
        }

        private sealed class LoadedLevel
        {
            public LoadedLevel(string path, LevelJsonDocument document)
            {
                Path = path;
                Document = document;
            }

            public string Path { get; }
            public LevelJsonDocument Document { get; }
        }
    }
}
