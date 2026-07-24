using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor.Levels
{
    public static class Campaign50Rebuilder
    {
        private readonly struct Spec
        {
            public Spec(int number, string name, int width, int height, int pieces)
            {
                Number = number;
                Name = name;
                Width = width;
                Height = height;
                Pieces = pieces;
            }

            public int Number { get; }
            public string Name { get; }
            public int Width { get; }
            public int Height { get; }
            public int Pieces { get; }
        }

        private static readonly Spec[] Specs =
        {
            new Spec(11, "City Bus", 6, 6, 7),
            new Spec(12, "Umbrella", 6, 6, 7),
            new Spec(13, "Windmill", 6, 6, 8),
            new Spec(14, "Toy Train", 7, 6, 8),
            new Spec(15, "Ice-Cream Cone", 6, 6, 7),
            new Spec(16, "Lighthouse", 6, 7, 8),
            new Spec(17, "Camera", 6, 6, 8),
            new Spec(18, "Small Castle", 7, 7, 9),
            new Spec(19, "Kick Scooter", 7, 6, 8),
            new Spec(20, "Toy Robot", 7, 7, 9),
            new Spec(21, "Hot-Air Balloon", 7, 7, 9),
            new Spec(22, "Teapot", 7, 7, 8),
            new Spec(23, "Pirate Ship", 7, 7, 10),
            new Spec(24, "Mushroom House", 7, 7, 9),
            new Spec(25, "Bicycle", 7, 7, 10),
            new Spec(26, "Treasure Chest", 7, 7, 9),
            new Spec(27, "Fire Truck", 8, 7, 10),
            new Spec(28, "Stone Bridge", 8, 7, 10),
            new Spec(29, "Cupcake", 7, 7, 9),
            new Spec(30, "Submarine", 8, 7, 10),
            new Spec(31, "Excavator", 8, 8, 11),
            new Spec(32, "Treehouse", 8, 8, 11),
            new Spec(33, "Grand Piano", 8, 8, 10),
            new Spec(34, "Ferris Wheel", 8, 8, 12),
            new Spec(35, "Helicopter", 8, 8, 11),
            new Spec(36, "Castle Gate", 8, 8, 12),
            new Spec(37, "Dragon Head", 8, 8, 11),
            new Spec(38, "School Bus", 8, 8, 12),
            new Spec(39, "Carousel", 8, 8, 13),
            new Spec(40, "Space Shuttle", 8, 8, 12),
            new Spec(41, "Medieval Castle", 9, 8, 14),
            new Spec(42, "Bulldozer", 8, 8, 12),
            new Spec(43, "Unicorn Head", 8, 8, 12),
            new Spec(44, "Harbor Boat", 9, 8, 13),
            new Spec(45, "Wizard Hat", 8, 8, 11),
            new Spec(46, "Toy Factory", 9, 9, 14),
            new Spec(47, "Monster Truck", 9, 8, 13),
            new Spec(48, "Amusement Park Entrance", 9, 9, 14),
            new Spec(49, "Clock Tower", 9, 9, 15),
            new Spec(50, "Grand Toy Kingdom", 9, 9, 16)
        };

        [MenuItem("Tools/Toy Puzzle/Levels/Rebuild Campaign 11-50 From Master Brief")]
        public static void Rebuild()
        {
            Dictionary<int, string> existing = FindSourcePaths();
            for (int i = 0; i < Specs.Length; i++)
            {
                Spec spec = Specs[i];
                if (!existing.TryGetValue(spec.Number, out string path))
                    throw new InvalidOperationException("Missing source JSON for level " + spec.Number + ".");

                string desiredPath = LevelJsonSchema.SourceFolder + "/level_" + spec.Number.ToString("D3") + "_" + Slug(spec.Name) + ".json";
                if (!string.Equals(path, desiredPath, StringComparison.Ordinal))
                {
                    if (File.Exists(Path.GetFullPath(desiredPath)) && !AssetDatabase.DeleteAsset(desiredPath))
                        throw new InvalidOperationException("Could not replace " + desiredPath + ".");
                    string moveError = AssetDatabase.MoveAsset(path, desiredPath);
                    if (!string.IsNullOrEmpty(moveError)) throw new InvalidOperationException(moveError);
                    path = desiredPath;
                }

                LevelJsonDocument document = LevelJsonSerializer.Load(path);
                document.levelId = "level_" + spec.Number.ToString("D3");
                document.levelNumber = spec.Number;
                document.displayName = "Level " + spec.Number.ToString("D2") + " - " + spec.Name;
                document.targetObjectName = spec.Name;
                document.boardWidth = spec.Width;
                document.boardHeight = spec.Height;
                document.difficultyTier = Mathf.Clamp(((spec.Number - 1) / 10) + 1, 1, 5);
                document.scrambleSeed = 7300 + spec.Number * 97;
                document.recommendedMoves = spec.Pieces + Mathf.Max(2, document.difficultyTier);
                document.lockCorrectPiecesByDefault = true;
                document.pieces = BuildPieces(spec);
                document.solutionCertificate = BuildSolution(spec);
                document.hintMetadata = new HintJson
                {
                    message = "Look at the large reference and place one connected toy part at a time.",
                    showDirectionalIndicator = spec.Number <= 30
                };
                document.tutorialMetadata = new TutorialJson();
                document.completionRewardData = new CompletionRewardJson
                {
                    stars = 3,
                    softCurrency = spec.Number % 5 == 0 ? 25 : 10,
                    rewardId = spec.Number % 5 == 0 ? "collection_milestone_" + spec.Number : string.Empty
                };
                document.thumbnailConfiguration = new ThumbnailJson { scale = 1f, offset = new Float2Json(), showBoard = false };
                document.levelTags = new[] { "master-brief", "toy", "campaign-" + document.difficultyTier };
                document.designerNotes = "Generated from 50_Level_Game_Design_Prompt.md; preserve the named object and required part count.";
                document.completionAction = CompletionAction(spec.Name);
                LevelJsonSerializer.SaveAtomic(path, document);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            LevelImportReport report = LevelImportPipeline.Run(true);
            if (report.RejectedCount != 0)
                throw new InvalidOperationException("Campaign rebuild rejected content: " + report.ToSummary());
            Debug.Log("Rebuilt Levels 11-50 from the master brief. " + report.ToSummary());
        }

        private static PieceJson[] BuildPieces(Spec spec)
        {
            string[] colors = { "red", "blue", "green", "yellow", "orange", "purple", "teal", "cream" };
            var pieces = new PieceJson[spec.Pieces];
            int cellCount = spec.Width * spec.Height;
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = new PieceJson
                {
                    pieceId = "part_" + (i + 1).ToString("D2"),
                    displayName = spec.Name + " Part " + (i + 1),
                    shapeType = "Square",
                    colorId = colors[i % colors.Length],
                    footprint = new[] { new Int2Json(0, 0) },
                    width = 1,
                    height = 1,
                    logicalPivot = new Int2Json(),
                    visualPivot = new Float2Json(0.5f, 0.5f),
                    targetPosition = CellAt(i, spec.Width),
                    targetRotation = 0,
                    startingPosition = CellAt(cellCount - 1 - i, spec.Width),
                    startingRotation = 0,
                    allowedRotations = new[] { 0 },
                    locksWhenCorrect = true,
                    strictTargetRotation = true,
                    sortingPriority = i,
                    artGenerationParameters = new ArtGenerationJson { styleVariant = "separate_color_part" }
                };
            }
            return pieces;
        }

        private static SolutionStepJson[] BuildSolution(Spec spec)
        {
            var result = new SolutionStepJson[spec.Pieces];
            for (int i = 0; i < result.Length; i++)
                result[i] = new SolutionStepJson { pieceId = "part_" + (i + 1).ToString("D2"), position = CellAt(i, spec.Width), rotation = 0 };
            return result;
        }

        private static Int2Json CellAt(int index, int width) => new Int2Json(index % width, index / width);

        private static Dictionary<int, string> FindSourcePaths()
        {
            var result = new Dictionary<int, string>();
            string[] paths = LevelImportPipeline.DiscoverSourcePaths();
            for (int i = 0; i < paths.Length; i++)
            {
                LevelJsonDocument document = LevelJsonSerializer.Load(paths[i]);
                result[document.levelNumber] = paths[i];
            }
            return result;
        }

        private static string Slug(string value)
        {
            return value.ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
        }

        private static string CompletionAction(string objectName)
        {
            string value = objectName.ToLowerInvariant();
            if (value.Contains("rocket") || value.Contains("balloon") || value.Contains("shuttle")) return "Lift";
            if (value.Contains("windmill") || value.Contains("wheel") || value.Contains("bicycle") ||
                value.Contains("train") || value.Contains("truck") || value.Contains("bus") ||
                value.Contains("helicopter") || value.Contains("carousel")) return "Spin";
            if (value.Contains("chest")) return "Open";
            if (value.Contains("camera")) return "Flash";
            if (value.Contains("castle") || value.Contains("kingdom") || value.Contains("hat")) return "Glow";
            return "Sway";
        }
    }
}
