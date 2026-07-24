using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor.Levels
{
    public static class FourColorBlockLevelConverter
    {
        public static string ConvertAndRebuild(IReadOnlyDictionary<int, string[]> pieceIdsByLevel)
        {
            if (pieceIdsByLevel == null) throw new ArgumentNullException(nameof(pieceIdsByLevel));
            string[] paths = LevelImportPipeline.DiscoverSourcePaths();
            var pending = new List<PendingLevel>();
            int totalParts = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                LevelJsonDocument document = LevelJsonSerializer.Load(paths[i]);
                if (document.levelNumber < 1 || document.levelNumber > PuzzleLayoutConstants.TotalPlayableLevels) continue;
                if (!pieceIdsByLevel.TryGetValue(document.levelNumber, out string[] pieceIds) || pieceIds == null || pieceIds.Length == 0)
                    throw new InvalidOperationException("No generated separated-part IDs were supplied for level " + document.levelNumber + ".");
                if (pieceIds.Length * 2 > document.boardWidth * document.boardHeight)
                    throw new InvalidOperationException("Level " + document.levelNumber + " needs at least two hidden cells per separated part.");

                document.pieces = BuildPieces(document.boardWidth, document.boardHeight, pieceIds);
                document.solutionCertificate = BuildSolutionCertificate(document.boardWidth, pieceIds);
                document.recommendedMoves = pieceIds.Length + (document.levelNumber <= 15 ? 2 : 1);
                document.lockCorrectPiecesByDefault = true;
                LevelValidationResult validation = LevelContentValidator.Validate(document);
                if (!validation.IsValid)
                    throw new InvalidOperationException("Separated-part conversion failed validation for level " + document.levelNumber + ": " + DescribeErrors(validation));
                pending.Add(new PendingLevel(paths[i], document));
                totalParts += pieceIds.Length;
            }

            if (pending.Count != PuzzleLayoutConstants.TotalPlayableLevels)
                throw new InvalidOperationException("Expected " + PuzzleLayoutConstants.TotalPlayableLevels + " source levels to convert, found " + pending.Count + ".");
            for (int i = 0; i < pending.Count; i++) LevelJsonSerializer.SaveAtomic(pending[i].path, pending[i].document);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            LevelImportReport report = LevelImportPipeline.Run(true);
            if (report.RejectedCount != 0) throw new InvalidOperationException("Level rebuild rejected separated-part content. " + report.ToSummary());

            string message = "Converted levels 1-" + PuzzleLayoutConstants.TotalPlayableLevels + " to " + totalParts + " separate color-part definitions and rebuilt " + report.ImportedCount + " level assets.";
            Debug.Log(message);
            return message;
        }

        private static PieceJson[] BuildPieces(int boardWidth, int boardHeight, string[] pieceIds)
        {
            var pieces = new PieceJson[pieceIds.Length];
            int cellCount = boardWidth * boardHeight;
            for (int i = 0; i < pieces.Length; i++)
            {
                string pieceId = pieceIds[i];
                string colorId = ColorFromPieceId(pieceId);
                Int2Json target = CellAt(i, boardWidth);
                Int2Json start = CellAt(cellCount - 1 - i, boardWidth);
                pieces[i] = new PieceJson
                {
                    pieceId = pieceId,
                    displayName = DisplayName(pieceId),
                    shapeType = "Square",
                    colorId = colorId,
                    footprint = new[] { new Int2Json(0, 0) },
                    width = 1,
                    height = 1,
                    logicalPivot = new Int2Json(0, 0),
                    visualPivot = new Float2Json(0.5f, 0.5f),
                    customPolygonPoints = Array.Empty<Float2Json>(),
                    targetPosition = target,
                    targetRotation = 0,
                    startingPosition = start,
                    startingRotation = 0,
                    allowedRotations = new[] { 0 },
                    startsLocked = false,
                    locksWhenCorrect = true,
                    strictTargetRotation = true,
                    sortingPriority = i,
                    decorativeStuds = Array.Empty<DecorativeStudJson>(),
                    recessedHoles = Array.Empty<RecessedHoleJson>(),
                    visualOverhang = new Float2Json(),
                    artGenerationParameters = new ArtGenerationJson
                    {
                        cornerRadius = 0.18f,
                        bevelSize = 0.08f,
                        insetPanel = false,
                        styleVariant = "separate_color_part"
                    }
                };
            }
            return pieces;
        }

        private static SolutionStepJson[] BuildSolutionCertificate(int boardWidth, string[] pieceIds)
        {
            var steps = new SolutionStepJson[pieceIds.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                steps[i] = new SolutionStepJson
                {
                    pieceId = pieceIds[i],
                    position = CellAt(i, boardWidth),
                    rotation = 0
                };
            }
            return steps;
        }

        private static Int2Json CellAt(int index, int boardWidth)
        {
            return new Int2Json(index % boardWidth, index / boardWidth);
        }

        private static string ColorFromPieceId(string pieceId)
        {
            int split = pieceId == null ? -1 : pieceId.IndexOf('_');
            if (split <= 0) throw new InvalidOperationException("Separated part ID does not start with a color: " + pieceId);
            string color = pieceId.Substring(0, split);
            if (color != "red" && color != "blue" && color != "green" && color != "yellow" &&
                color != "orange" && color != "purple" && color != "teal" && color != "cream")
                throw new InvalidOperationException("Unsupported separated part color in ID: " + pieceId);
            return color;
        }

        private static string DisplayName(string pieceId)
        {
            string[] words = (pieceId ?? string.Empty).Split('_');
            if (words.Length < 3) return pieceId ?? string.Empty;
            string color = char.ToUpperInvariant(words[0][0]) + words[0].Substring(1);
            int number = int.TryParse(words[words.Length - 1], out int parsed) ? parsed : 1;
            return color + " Molded Part " + number;
        }

        private static string DescribeErrors(LevelValidationResult validation)
        {
            var messages = new List<string>();
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                LevelValidationIssue issue = validation.Issues[i];
                if (issue.Severity == LevelValidationSeverity.Error) messages.Add(issue.ToString());
            }
            return string.Join(" | ", messages);
        }

        private readonly struct PendingLevel
        {
            public PendingLevel(string path, LevelJsonDocument document)
            {
                this.path = path;
                this.document = document;
            }

            public readonly string path;
            public readonly LevelJsonDocument document;
        }
    }
}
