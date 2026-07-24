using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ToyPuzzle.Editor.Levels;
using UnityEditor;
using UnityEngine;
using EditorValidationResult = ToyPuzzle.Editor.Levels.LevelValidationResult;

namespace ToyPuzzle.Tests.Editor
{
    public sealed class LevelPipelineTests
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

        [Test]
        public void SourceSet_HasExactlyFiftyOrderedIntentionalLevels()
        {
            LevelJsonDocument[] levels = LoadAll();
            Assert.That(levels, Has.Length.EqualTo(50));
            for (int i = 0; i < levels.Length; i++)
            {
                Assert.That(levels[i].levelNumber, Is.EqualTo(i + 1));
                Assert.That(levels[i].levelId, Is.EqualTo("level_" + (i + 1).ToString("D3")));
                Assert.That(levels[i].targetObjectName, Is.EqualTo(ExpectedNames[i]));
                Assert.That(levels[i].pieces, Has.Length.EqualTo(ExpectedPieceCounts[i]), levels[i].levelId);
                Assert.That(levels[i].difficultyTier, Is.EqualTo(i / 10 + 1), levels[i].levelId);
            }
        }

        [Test]
        public void GeneratedPrefabCatalog_HasFiftyOrderedEditableLevelPrefabs()
        {
            LevelPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(LevelJsonSchema.PrefabCatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(PuzzleLayoutConstants.TotalPlayableLevels));
            for (int i = 0; i < catalog.Count; i++)
            {
                GameObject prefab = catalog.GetPrefabByIndex(i);
                PuzzleLevelPrefab levelPrefab = catalog.GetByIndex(i);
                Assert.That(prefab, Is.Not.Null, "Missing prefab catalog entry " + i);
                Assert.That(levelPrefab, Is.Not.Null, AssetDatabase.GetAssetPath(prefab));
                Assert.That(levelPrefab.Level, Is.Not.Null);
                Assert.That(levelPrefab.Level.levelNumber, Is.EqualTo(i + 1));
                Assert.That(levelPrefab.Level.targetObjectName, Is.EqualTo(ExpectedNames[i]));
                Assert.That(levelPrefab.Level.pieces, Has.Length.EqualTo(ExpectedPieceCounts[i]));
                Assert.That(levelPrefab.SourceJsonPath, Is.Not.Empty);
            }
        }

        [Test]
        public void EverySource_IsCurrentValidAndStartsUnsolved()
        {
            LevelJsonDocument[] levels = LoadAll();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<int> numbers = new HashSet<int>();
            for (int i = 0; i < levels.Length; i++)
            {
                LevelJsonDocument level = levels[i];
                Assert.That(ids.Add(level.levelId), Is.True, "Duplicate ID " + level.levelId);
                Assert.That(numbers.Add(level.levelNumber), Is.True, "Duplicate number " + level.levelNumber);
                EditorValidationResult validation = LevelContentValidator.Validate(level);
                Assert.That(validation.IsValid, Is.True, level.levelId + Environment.NewLine + Join(validation));
                Assert.That(level.solutionCertificate, Is.Not.Null.And.Not.Empty, level.levelId + " needs a solvability certificate.");

                int occupiedCells = 0;
                int multiCellPieces = 0;
                for (int pieceIndex = 0; pieceIndex < level.pieces.Length; pieceIndex++)
                {
                    occupiedCells += level.pieces[pieceIndex].footprint.Length;
                    if (level.pieces[pieceIndex].footprint.Length > 1) multiCellPieces++;
                    Assert.That(level.pieces[pieceIndex].pieceId, Does.Not.StartWith("piece_"), level.levelId + " uses a generic part ID.");
                    Assert.That(level.pieces[pieceIndex].startingRotation, Is.Zero, level.levelId + " must not require player rotation.");
                    Assert.That(level.pieces[pieceIndex].targetRotation, Is.Zero, level.levelId + " must not require player rotation.");
                    Assert.That(level.pieces[pieceIndex].allowedRotations, Is.EqualTo(new[] { 0 }), level.levelId + " must be translation-only.");
                }
                // Levels 1-35 use freeform color masks. Their one-cell definitions are
                // bookkeeping poses only; the artwork metadata carries the true proportions.
                if (level.levelNumber > PuzzleLayoutConstants.TotalPlayableLevels)
                {
                    float occupancy = occupiedCells / (float)(level.boardWidth * level.boardHeight);
                    float[] minimumByTier = { 0f, 0.40f, 0.45f, 0.50f, 0.55f, 0.58f };
                    float[] maximumByTier = { 0f, 0.50f, 0.58f, 0.60f, 0.65f, 0.68f };
                    Assert.That(occupancy, Is.InRange(minimumByTier[level.difficultyTier], maximumByTier[level.difficultyTier]), level.levelId);
                    Assert.That(multiCellPieces, Is.GreaterThanOrEqualTo(level.pieces.Length / 2), level.levelId + " must be built from substantive multi-cell parts.");
                }

                bool allCorrect = true;
                for (int pieceIndex = 0; pieceIndex < level.pieces.Length; pieceIndex++)
                {
                    PieceJson piece = level.pieces[pieceIndex];
                    allCorrect &= LevelContentValidator.IsCorrectPose(piece, piece.startingPosition, piece.startingRotation);
                }
                Assert.That(allCorrect, Is.False, level.levelId + " must not start solved.");
            }
        }

        [Test]
        public void Serializer_IsDeterministicAcrossRoundTrip()
        {
            LevelJsonDocument[] levels = LoadAll();
            for (int i = 0; i < levels.Length; i++)
            {
                string first = LevelJsonSerializer.Serialize(levels[i]);
                LevelJsonDocument roundTrip = LevelJsonSerializer.Deserialize(first, levels[i].levelId);
                string second = LevelJsonSerializer.Serialize(roundTrip);
                Assert.That(second, Is.EqualTo(first), levels[i].levelId);
            }
        }

        [Test]
        public void RuntimeConversion_PreservesAuthoritativeGameplayFields()
        {
            LevelJsonDocument[] levels = LoadAll();
            for (int i = 0; i < levels.Length; i++)
            {
                LevelJsonDocument source = levels[i];
                LevelDefinition runtime = RuntimeLevelAssetBuilder.Convert(source);
                Assert.That(runtime.levelId, Is.EqualTo(source.levelId));
                Assert.That(runtime.levelNumber, Is.EqualTo(source.levelNumber));
                Assert.That(runtime.boardWidth, Is.EqualTo(source.boardWidth));
                Assert.That(runtime.boardHeight, Is.EqualTo(source.boardHeight));
                Assert.That(runtime.pieces, Has.Length.EqualTo(source.pieces.Length));
                for (int pieceIndex = 0; pieceIndex < source.pieces.Length; pieceIndex++)
                {
                    Assert.That(runtime.pieces[pieceIndex].pieceId, Is.EqualTo(source.pieces[pieceIndex].pieceId));
                    Assert.That(runtime.pieces[pieceIndex].footprint, Has.Length.EqualTo(source.pieces[pieceIndex].footprint.Length));
                    Assert.That(runtime.pieces[pieceIndex].requireExactTargetRotation, Is.EqualTo(source.pieces[pieceIndex].strictTargetRotation));
                }
            }
        }

        [Test]
        public void EditorOccupiedCells_MatchRuntimeNormalizedRotationSemantics()
        {
            LevelJsonDocument[] levels = LoadAll();
            for (int levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                LevelJsonDocument source = levels[levelIndex];
                LevelDefinition runtime = RuntimeLevelAssetBuilder.Convert(source);
                for (int pieceIndex = 0; pieceIndex < source.pieces.Length; pieceIndex++)
                {
                    PieceJson editorPiece = source.pieces[pieceIndex];
                    PieceDefinition runtimePiece = runtime.pieces[pieceIndex];
                    for (int rotationIndex = 0; rotationIndex < editorPiece.allowedRotations.Length; rotationIndex++)
                    {
                        int rotation = editorPiece.allowedRotations[rotationIndex];
                        List<Int2Json> editorCells = LevelContentValidator.GetOccupiedCells(editorPiece, editorPiece.startingPosition, rotation);
                        GridCoordinate[] runtimeCells = GridMath.GetOccupiedCells(runtimePiece, new PiecePose(runtimePiece.startingPosition, rotation));
                        HashSet<string> editorSet = new HashSet<string>();
                        HashSet<string> runtimeSet = new HashSet<string>();
                        for (int i = 0; i < editorCells.Count; i++) editorSet.Add(editorCells[i].x + "," + editorCells[i].y);
                        for (int i = 0; i < runtimeCells.Length; i++) runtimeSet.Add(runtimeCells[i].x + "," + runtimeCells[i].y);
                        Assert.That(editorSet.SetEquals(runtimeSet), Is.True, source.levelId + "/" + editorPiece.pieceId + " @ " + rotation);
                    }
                }
            }
        }

        [Test]
        public void SchemaVersion_RejectsUnsupportedFutureContent()
        {
            LevelJsonDocument level = LoadAll()[0];
            level.schemaVersion = LevelJsonSchema.CurrentVersion + 1;
            EditorValidationResult validation = LevelContentValidator.Validate(level);
            Assert.That(validation.IsValid, Is.False);
            Assert.Throws<InvalidOperationException>(() => LevelSchemaMigrator.MigrateToCurrent(level));
        }

        [Test]
        public void RotationEquivalence_UsesFootprintUnlessStrictVisualRotationIsEnabled()
        {
            PieceJson symmetric = new PieceJson
            {
                footprint = new[] { new Int2Json(0, 0) },
                targetPosition = new Int2Json(2, 2),
                targetRotation = 0,
                logicalPivot = new Int2Json(),
                strictTargetRotation = false
            };
            Assert.That(LevelContentValidator.IsCorrectPose(symmetric, new Int2Json(2, 2), 90), Is.True);
            symmetric.strictTargetRotation = true;
            Assert.That(LevelContentValidator.IsCorrectPose(symmetric, new Int2Json(2, 2), 90), Is.False);
        }

        private static LevelJsonDocument[] LoadAll()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null.And.Not.Empty);
            string folder = Path.Combine(projectRoot, LevelJsonSchema.SourceFolder);
            string[] paths = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);
            LevelJsonDocument[] levels = new LevelJsonDocument[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                levels[i] = LevelJsonSerializer.Deserialize(File.ReadAllText(paths[i]), paths[i]);
            }
            return levels;
        }

        private static string Join(EditorValidationResult validation)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < validation.Issues.Count; i++) builder.AppendLine(validation.Issues[i].ToString());
            return builder.ToString();
        }
    }
}
