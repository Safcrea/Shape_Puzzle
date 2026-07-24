using System;
using System.Collections.Generic;

namespace ToyPuzzle.Editor.Levels
{
    public enum LevelValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class LevelValidationIssue
    {
        public readonly LevelValidationSeverity Severity;
        public readonly string Code;
        public readonly string Message;
        public readonly string PieceId;

        public LevelValidationIssue(LevelValidationSeverity severity, string code, string message, string pieceId = null)
        {
            Severity = severity;
            Code = code;
            Message = message;
            PieceId = pieceId;
        }

        public override string ToString()
        {
            string owner = string.IsNullOrEmpty(PieceId) ? string.Empty : " [" + PieceId + "]";
            return Severity + " " + Code + owner + ": " + Message;
        }
    }

    public sealed class LevelValidationResult
    {
        private readonly List<LevelValidationIssue> issues = new List<LevelValidationIssue>();
        public IReadOnlyList<LevelValidationIssue> Issues => issues;
        public bool IsValid { get; private set; } = true;

        public void Add(LevelValidationSeverity severity, string code, string message, string pieceId = null)
        {
            issues.Add(new LevelValidationIssue(severity, code, message, pieceId));
            if (severity == LevelValidationSeverity.Error)
            {
                IsValid = false;
            }
        }
    }

    public static class LevelContentValidator
    {
        private static readonly HashSet<string> ShapeTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "RoundedRectangle", "Square", "Rectangle", "Capsule", "Circle", "Ring", "Triangle",
            "Trapezoid", "Wedge", "Semicircle", "QuarterCircle", "LShape", "TShape", "UShape",
            "ZShape", "CrossShape", "Polyomino", "CustomGridFootprint", "CustomPolygon"
        };

        public static LevelValidationResult Validate(LevelJsonDocument level)
        {
            LevelValidationResult result = new LevelValidationResult();
            if (level == null)
            {
                result.Add(LevelValidationSeverity.Error, "LEVEL_NULL", "Level document is null.");
                return result;
            }

            if (level.schemaVersion != LevelJsonSchema.CurrentVersion)
                result.Add(LevelValidationSeverity.Error, "SCHEMA_VERSION", "Expected schema version " + LevelJsonSchema.CurrentVersion + ", found " + level.schemaVersion + ".");
            if (!IsStableId(level.levelId, "level_"))
                result.Add(LevelValidationSeverity.Error, "LEVEL_ID", "Level ID must begin with 'level_' and use lowercase letters, digits, or underscores.");
            if (level.levelNumber < 1)
                result.Add(LevelValidationSeverity.Error, "LEVEL_NUMBER", "Level number must be positive.");
            if (string.IsNullOrWhiteSpace(level.displayName))
                result.Add(LevelValidationSeverity.Error, "DISPLAY_NAME", "Display name is required.");
            if (string.IsNullOrWhiteSpace(level.targetObjectName))
                result.Add(LevelValidationSeverity.Error, "TARGET_NAME", "Target object name is required.");
            if (level.boardWidth < 5 || level.boardWidth > 9 || level.boardHeight < 5 || level.boardHeight > 9)
                result.Add(LevelValidationSeverity.Error, "BOARD_SIZE", "Board width and height must each be between 5 and 9.");
            if (level.difficultyTier < 1 || level.difficultyTier > 5)
                result.Add(LevelValidationSeverity.Error, "DIFFICULTY", "Difficulty tier must be between 1 and 5.");
            if (string.IsNullOrWhiteSpace(level.paletteId))
                result.Add(LevelValidationSeverity.Error, "PALETTE", "Palette ID is required.");
            if (level.recommendedMoves < 1)
                result.Add(LevelValidationSeverity.Error, "MOVE_COUNT", "Recommended moves must be positive.");
            if (level.pieces == null || level.pieces.Length == 0)
            {
                result.Add(LevelValidationSeverity.Error, "PIECES_EMPTY", "At least one required piece is needed.");
                return result;
            }

            HashSet<string> pieceIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<long> targetCells = new HashSet<long>();
            HashSet<long> startingCells = new HashSet<long>();
            bool allPiecesSolved = true;
            for (int index = 0; index < level.pieces.Length; index++)
            {
                PieceJson piece = level.pieces[index];
                if (piece == null)
                {
                    result.Add(LevelValidationSeverity.Error, "PIECE_NULL", "Piece at index " + index + " is null.");
                    continue;
                }

                ValidatePiece(level, piece, pieceIds, targetCells, startingCells, result);
                if (!IsCorrectPose(piece, piece.startingPosition, piece.startingRotation))
                {
                    allPiecesSolved = false;
                }
                else if (piece.startsLocked == false && level.levelNumber > 10)
                {
                    result.Add(LevelValidationSeverity.Warning, "STARTS_CORRECT", "Piece starts in its target pose on a non-tutorial level.", piece.pieceId);
                }
            }

            if (allPiecesSolved)
                result.Add(LevelValidationSeverity.Error, "ALREADY_SOLVED", "The starting layout is already solved.");

            int occupied = targetCells.Count;
            int totalCells = Math.Max(1, level.boardWidth * level.boardHeight);
            float ratio = (float)occupied / totalCells;
            if (ratio > 0.70f)
                result.Add(LevelValidationSeverity.Error, "FREE_SPACE", "Solved occupancy exceeds 70%, leaving insufficient manipulation space.");
            else if (ratio < 0.24f)
                result.Add(LevelValidationSeverity.Warning, "LOW_OCCUPANCY", "Solved silhouette occupies less than 24% of the board.");

            ValidateSolutionCertificate(level, result);
            return result;
        }

        public static bool IsCorrectPose(PieceJson piece, Int2Json position, int rotation)
        {
            if (piece == null || position == null || piece.targetPosition == null)
                return false;
            if (position.x != piece.targetPosition.x || position.y != piece.targetPosition.y)
                return false;
            if (piece.strictTargetRotation)
                return NormalizeRotation(rotation) == NormalizeRotation(piece.targetRotation);
            return AreFootprintsEquivalent(piece, rotation, piece.targetRotation);
        }

        public static List<Int2Json> GetOccupiedCells(PieceJson piece, Int2Json position, int rotation)
        {
            List<Int2Json> cells = new List<Int2Json>();
            if (piece == null || position == null || piece.footprint == null || piece.logicalPivot == null)
                return cells;

            int turns = NormalizeRotation(rotation) / 90;
            int[] rotatedX = new int[piece.footprint.Length];
            int[] rotatedY = new int[piece.footprint.Length];
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            for (int i = 0; i < piece.footprint.Length; i++)
            {
                Int2Json source = piece.footprint[i];
                if (source == null)
                    continue;
                int x = source.x - piece.logicalPivot.x;
                int y = source.y - piece.logicalPivot.y;
                for (int turn = 0; turn < turns; turn++)
                {
                    int oldX = x;
                    x = y;
                    y = -oldX;
                }
                int localX = piece.logicalPivot.x + x;
                int localY = piece.logicalPivot.y + y;
                rotatedX[i] = localX;
                rotatedY[i] = localY;
                minX = Math.Min(minX, localX);
                minY = Math.Min(minY, localY);
            }

            for (int i = 0; i < piece.footprint.Length; i++)
            {
                if (piece.footprint[i] == null) continue;
                cells.Add(new Int2Json(position.x + rotatedX[i] - minX, position.y + rotatedY[i] - minY));
            }

            return cells;
        }

        public static int NormalizeRotation(int rotation)
        {
            int normalized = rotation % 360;
            return normalized < 0 ? normalized + 360 : normalized;
        }

        private static void ValidatePiece(
            LevelJsonDocument level,
            PieceJson piece,
            HashSet<string> pieceIds,
            HashSet<long> targetCells,
            HashSet<long> startingCells,
            LevelValidationResult result)
        {
            if (!IsStableId(piece.pieceId, null))
                result.Add(LevelValidationSeverity.Error, "PIECE_ID", "Piece ID must use lowercase letters, digits, or underscores.", piece.pieceId);
            else if (!pieceIds.Add(piece.pieceId))
                result.Add(LevelValidationSeverity.Error, "PIECE_ID_DUPLICATE", "Piece ID is duplicated.", piece.pieceId);
            if (string.IsNullOrWhiteSpace(piece.displayName))
                result.Add(LevelValidationSeverity.Error, "PIECE_NAME", "Piece display name is required.", piece.pieceId);
            if (!ShapeTypes.Contains(piece.shapeType ?? string.Empty))
                result.Add(LevelValidationSeverity.Error, "SHAPE_TYPE", "Unsupported shape type '" + piece.shapeType + "'.", piece.pieceId);
            if (string.IsNullOrWhiteSpace(piece.colorId))
                result.Add(LevelValidationSeverity.Error, "COLOR_ID", "Color ID is required.", piece.pieceId);
            if (piece.footprint == null || piece.footprint.Length == 0)
            {
                result.Add(LevelValidationSeverity.Error, "FOOTPRINT_EMPTY", "Footprint must contain at least one cell.", piece.pieceId);
                return;
            }

            HashSet<long> localCells = new HashSet<long>();
            for (int i = 0; i < piece.footprint.Length; i++)
            {
                Int2Json cell = piece.footprint[i];
                if (cell == null)
                {
                    result.Add(LevelValidationSeverity.Error, "FOOTPRINT_NULL", "Footprint contains a null cell.", piece.pieceId);
                    continue;
                }
                if (!localCells.Add(CellKey(cell.x, cell.y)))
                    result.Add(LevelValidationSeverity.Error, "FOOTPRINT_DUPLICATE", "Footprint repeats cell (" + cell.x + ", " + cell.y + ").", piece.pieceId);
            }

            if (piece.logicalPivot == null || piece.visualPivot == null || piece.targetPosition == null || piece.startingPosition == null)
            {
                result.Add(LevelValidationSeverity.Error, "COORDINATE_NULL", "Pivot and pose coordinates are required.", piece.pieceId);
                return;
            }

            if (piece.allowedRotations == null || piece.allowedRotations.Length == 0)
                result.Add(LevelValidationSeverity.Error, "ROTATIONS_EMPTY", "At least one allowed rotation is required.", piece.pieceId);
            else
            {
                HashSet<int> rotations = new HashSet<int>();
                for (int i = 0; i < piece.allowedRotations.Length; i++)
                {
                    int rotation = piece.allowedRotations[i];
                    if (rotation != NormalizeRotation(rotation) || rotation % 90 != 0)
                        result.Add(LevelValidationSeverity.Error, "ROTATION_VALUE", "Allowed rotations must be one of 0, 90, 180, 270.", piece.pieceId);
                    if (!rotations.Add(NormalizeRotation(rotation)))
                        result.Add(LevelValidationSeverity.Error, "ROTATION_DUPLICATE", "Allowed rotations contain a duplicate.", piece.pieceId);
                }
                if (!rotations.Contains(NormalizeRotation(piece.targetRotation)))
                    result.Add(LevelValidationSeverity.Error, "TARGET_ROTATION", "Target rotation is not allowed.", piece.pieceId);
                if (!rotations.Contains(NormalizeRotation(piece.startingRotation)))
                    result.Add(LevelValidationSeverity.Error, "START_ROTATION", "Starting rotation is not allowed.", piece.pieceId);
            }

            AddPoseCells(level, piece, piece.targetPosition, piece.targetRotation, targetCells, "TARGET", result);
            AddPoseCells(level, piece, piece.startingPosition, piece.startingRotation, startingCells, "START", result);
            if (piece.startsLocked && !IsCorrectPose(piece, piece.startingPosition, piece.startingRotation))
                result.Add(LevelValidationSeverity.Error, "LOCKED_INCORRECT", "A starting locked piece must already be correct.", piece.pieceId);
            if (piece.customPolygonPoints != null && piece.shapeType == "CustomPolygon" && piece.customPolygonPoints.Length < 3)
                result.Add(LevelValidationSeverity.Error, "CUSTOM_POLYGON", "CustomPolygon requires at least three polygon points.", piece.pieceId);
        }

        private static void AddPoseCells(LevelJsonDocument level, PieceJson piece, Int2Json position, int rotation, HashSet<long> occupancy, string poseName, LevelValidationResult result)
        {
            List<Int2Json> cells = GetOccupiedCells(piece, position, rotation);
            for (int i = 0; i < cells.Count; i++)
            {
                Int2Json cell = cells[i];
                if (cell.x < 0 || cell.x >= level.boardWidth || cell.y < 0 || cell.y >= level.boardHeight)
                    result.Add(LevelValidationSeverity.Error, poseName + "_OUT_OF_BOUNDS", poseName + " cell (" + cell.x + ", " + cell.y + ") is outside the board.", piece.pieceId);
                if (!occupancy.Add(CellKey(cell.x, cell.y)))
                    result.Add(LevelValidationSeverity.Error, poseName + "_OVERLAP", poseName + " cell (" + cell.x + ", " + cell.y + ") overlaps another piece.", piece.pieceId);
            }
        }

        private static void ValidateSolutionCertificate(LevelJsonDocument level, LevelValidationResult result)
        {
            if (level.solutionCertificate == null || level.solutionCertificate.Length == 0)
                return;

            Dictionary<string, Int2Json> positions = new Dictionary<string, Int2Json>(StringComparer.Ordinal);
            Dictionary<string, int> rotations = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceJson piece = level.pieces[i];
                positions[piece.pieceId] = new Int2Json(piece.startingPosition.x, piece.startingPosition.y);
                rotations[piece.pieceId] = piece.startingRotation;
            }

            for (int i = 0; i < level.solutionCertificate.Length; i++)
            {
                SolutionStepJson step = level.solutionCertificate[i];
                if (step == null || string.IsNullOrEmpty(step.pieceId) || step.position == null)
                {
                    result.Add(LevelValidationSeverity.Error, "CERTIFICATE_STEP", "Solution certificate contains an incomplete step.");
                    continue;
                }

                PieceJson piece = FindPiece(level, step.pieceId);
                if (piece == null)
                {
                    result.Add(LevelValidationSeverity.Error, "CERTIFICATE_PIECE", "Solution certificate references unknown piece '" + step.pieceId + "'.");
                    continue;
                }
                if (!IsAllowed(piece, step.rotation))
                    result.Add(LevelValidationSeverity.Error, "CERTIFICATE_ROTATION", "Solution step uses a disallowed rotation.", piece.pieceId);

                HashSet<long> occupied = new HashSet<long>();
                for (int pieceIndex = 0; pieceIndex < level.pieces.Length; pieceIndex++)
                {
                    PieceJson other = level.pieces[pieceIndex];
                    if (string.Equals(other.pieceId, piece.pieceId, StringComparison.Ordinal)) continue;
                    List<Int2Json> otherCells = GetOccupiedCells(other, positions[other.pieceId], rotations[other.pieceId]);
                    for (int cellIndex = 0; cellIndex < otherCells.Count; cellIndex++)
                        occupied.Add(CellKey(otherCells[cellIndex].x, otherCells[cellIndex].y));
                }

                List<Int2Json> candidateCells = GetOccupiedCells(piece, step.position, step.rotation);
                bool legal = candidateCells.Count > 0;
                for (int cellIndex = 0; cellIndex < candidateCells.Count; cellIndex++)
                {
                    Int2Json cell = candidateCells[cellIndex];
                    if (cell.x < 0 || cell.x >= level.boardWidth || cell.y < 0 || cell.y >= level.boardHeight || occupied.Contains(CellKey(cell.x, cell.y)))
                    {
                        legal = false;
                        break;
                    }
                }

                if (!legal)
                {
                    result.Add(LevelValidationSeverity.Error, "CERTIFICATE_ILLEGAL_MOVE", "Solution step " + i + " overlaps another piece or leaves the board.", piece.pieceId);
                    continue;
                }

                positions[piece.pieceId] = new Int2Json(step.position.x, step.position.y);
                rotations[piece.pieceId] = step.rotation;
            }

            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceJson piece = level.pieces[i];
                if (!IsCorrectPose(piece, positions[piece.pieceId], rotations[piece.pieceId]))
                    result.Add(LevelValidationSeverity.Error, "CERTIFICATE_INCOMPLETE", "Solution certificate does not end this required piece at its target pose.", piece.pieceId);
            }
        }

        private static PieceJson FindPiece(LevelJsonDocument level, string pieceId)
        {
            for (int i = 0; i < level.pieces.Length; i++)
                if (level.pieces[i] != null && string.Equals(level.pieces[i].pieceId, pieceId, StringComparison.Ordinal))
                    return level.pieces[i];
            return null;
        }

        private static bool IsAllowed(PieceJson piece, int rotation)
        {
            int normalized = NormalizeRotation(rotation);
            for (int i = 0; i < piece.allowedRotations.Length; i++)
                if (NormalizeRotation(piece.allowedRotations[i]) == normalized)
                    return true;
            return false;
        }

        private static bool AreFootprintsEquivalent(PieceJson piece, int leftRotation, int rightRotation)
        {
            List<Int2Json> left = GetOccupiedCells(piece, new Int2Json(), leftRotation);
            List<Int2Json> right = GetOccupiedCells(piece, new Int2Json(), rightRotation);
            if (left.Count != right.Count)
                return false;
            HashSet<long> cells = new HashSet<long>();
            for (int i = 0; i < left.Count; i++) cells.Add(CellKey(left[i].x, left[i].y));
            for (int i = 0; i < right.Count; i++) if (!cells.Contains(CellKey(right[i].x, right[i].y))) return false;
            return true;
        }

        private static bool IsStableId(string value, string requiredPrefix)
        {
            if (string.IsNullOrEmpty(value) || (requiredPrefix != null && !value.StartsWith(requiredPrefix, StringComparison.Ordinal)))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (!(character == '_' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9'))
                    return false;
            }
            return true;
        }

        private static long CellKey(int x, int y)
        {
            return ((long)x << 32) ^ (uint)y;
        }
    }
}
