using System;
using System.Collections.Generic;

namespace ToyPuzzle
{
    public enum ValidationSeverity
    {
        Warning,
        Error
    }

    public sealed class LevelValidationIssue
    {
        public LevelValidationIssue(string code, string message, ValidationSeverity severity, string pieceId = null)
        {
            Code = code;
            Message = message;
            Severity = severity;
            PieceId = pieceId;
        }

        public string Code { get; }
        public string Message { get; }
        public ValidationSeverity Severity { get; }
        public string PieceId { get; }
    }

    public sealed class LevelValidationResult
    {
        private readonly LevelValidationIssue[] issues;

        public LevelValidationResult(LevelValidationIssue[] issues)
        {
            this.issues = issues ?? Array.Empty<LevelValidationIssue>();
        }

        public LevelValidationIssue[] Issues => issues;

        public bool IsValid
        {
            get
            {
                for (int i = 0; i < issues.Length; i++)
                {
                    if (issues[i].Severity == ValidationSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    public static class LevelDefinitionValidator
    {
        public static LevelValidationResult Validate(LevelDefinition level)
        {
            var issues = new List<LevelValidationIssue>();
            if (level == null)
            {
                issues.Add(Error("level.null", "Level data is missing."));
                return new LevelValidationResult(issues.ToArray());
            }

            if (level.schemaVersion <= 0)
            {
                issues.Add(Error("level.schemaVersion", "Schema version must be positive."));
            }

            if (string.IsNullOrWhiteSpace(level.levelId))
            {
                issues.Add(Error("level.id", "Level ID is required."));
            }

            if (level.levelNumber <= 0)
            {
                issues.Add(Error("level.number", "Level number must be positive."));
            }

            if (level.boardWidth < 5 || level.boardWidth > 8 || level.boardHeight < 5 || level.boardHeight > 8)
            {
                issues.Add(Error("board.size", "Board width and height must each be between 5 and 8."));
            }

            if (level.pieces == null || level.pieces.Length == 0)
            {
                issues.Add(Error("pieces.empty", "At least one piece is required."));
                return new LevelValidationResult(issues.ToArray());
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            OccupancyMap starts = CreateMap(level);
            OccupancyMap targets = CreateMap(level);

            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceDefinition piece = level.pieces[i];
                if (piece == null)
                {
                    issues.Add(Error("piece.null", $"Piece at index {i} is missing."));
                    continue;
                }

                string pieceId = piece.pieceId;
                if (string.IsNullOrWhiteSpace(pieceId))
                {
                    issues.Add(Error("piece.id", $"Piece at index {i} has no ID."));
                    continue;
                }

                if (!ids.Add(pieceId))
                {
                    issues.Add(Error("piece.id.duplicate", $"Piece ID '{pieceId}' is duplicated.", pieceId));
                }

                ValidatePieceShape(piece, issues);
                ValidatePose(piece, piece.StartingPose, starts, "start", issues);
                ValidatePose(piece, piece.TargetPose, targets, "target", issues);
            }

            return new LevelValidationResult(issues.ToArray());
        }

        private static OccupancyMap CreateMap(LevelDefinition level)
        {
            return new OccupancyMap(Math.Max(1, level.boardWidth), Math.Max(1, level.boardHeight));
        }

        private static void ValidatePieceShape(PieceDefinition piece, List<LevelValidationIssue> issues)
        {
            if (piece.footprint == null || piece.footprint.Length == 0)
            {
                issues.Add(Error("piece.footprint.empty", $"Piece '{piece.pieceId}' has no footprint.", piece.pieceId));
                return;
            }

            var cells = new HashSet<GridCoordinate>();
            for (int i = 0; i < piece.footprint.Length; i++)
            {
                if (!cells.Add(piece.footprint[i]))
                {
                    issues.Add(Error("piece.footprint.duplicate", $"Piece '{piece.pieceId}' contains a duplicate footprint cell.", piece.pieceId));
                }
            }

            if (!GridMath.IsQuarterTurn(piece.startingRotation) || !GridMath.IsQuarterTurn(piece.targetRotation))
            {
                issues.Add(Error("piece.rotation.quarter", $"Piece '{piece.pieceId}' rotations must be multiples of 90 degrees.", piece.pieceId));
            }

            if (piece.allowedRotations == null || piece.allowedRotations.Length == 0)
            {
                issues.Add(Error("piece.rotations.empty", $"Piece '{piece.pieceId}' has no allowed rotations.", piece.pieceId));
                return;
            }

            var rotations = new HashSet<int>();
            for (int i = 0; i < piece.allowedRotations.Length; i++)
            {
                int rotation = piece.allowedRotations[i];
                if (!GridMath.IsQuarterTurn(rotation))
                {
                    issues.Add(Error("piece.rotation.invalid", $"Piece '{piece.pieceId}' contains a non-quarter-turn rotation.", piece.pieceId));
                }
                else if (!rotations.Add(GridMath.NormalizeRotation(rotation)))
                {
                    issues.Add(new LevelValidationIssue(
                        "piece.rotation.duplicate",
                        $"Piece '{piece.pieceId}' contains an equivalent duplicate rotation.",
                        ValidationSeverity.Warning,
                        piece.pieceId));
                }
            }

            if (!piece.AllowsRotation(piece.startingRotation))
            {
                issues.Add(Error("piece.rotation.start", $"Piece '{piece.pieceId}' does not allow its starting rotation.", piece.pieceId));
            }

            if (!piece.AllowsRotation(piece.targetRotation))
            {
                issues.Add(Error("piece.rotation.target", $"Piece '{piece.pieceId}' does not allow its target rotation.", piece.pieceId));
            }
        }

        private static void ValidatePose(
            PieceDefinition piece,
            PiecePose pose,
            OccupancyMap map,
            string poseName,
            List<LevelValidationIssue> issues)
        {
            PlacementResult result;
            try
            {
                result = PlacementValidator.Validate(piece, pose, map);
            }
            catch (ArgumentException exception)
            {
                issues.Add(Error($"piece.{poseName}.invalid", exception.Message, piece.pieceId));
                return;
            }

            if (!result.IsValid)
            {
                issues.Add(Error(
                    $"piece.{poseName}.{result.FailureReason}",
                    $"Piece '{piece.pieceId}' has an invalid {poseName} pose: {result.FailureReason} at {result.BlockedCell}.",
                    piece.pieceId));
                return;
            }

            map.TryReserve(piece.pieceId, GridMath.GetOccupiedCells(piece, pose));
        }

        private static LevelValidationIssue Error(string code, string message, string pieceId = null)
        {
            return new LevelValidationIssue(code, message, ValidationSeverity.Error, pieceId);
        }
    }
}
