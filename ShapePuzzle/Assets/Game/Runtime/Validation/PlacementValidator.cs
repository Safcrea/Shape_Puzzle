using System;

namespace ToyPuzzle
{
    public enum PlacementFailureReason
    {
        None,
        MissingPiece,
        EmptyFootprint,
        UnsupportedRotation,
        OutsideBoard,
        Occupied
    }

    public readonly struct PlacementResult
    {
        public PlacementResult(
            bool isValid,
            PlacementFailureReason failureReason,
            GridCoordinate blockedCell,
            string blockingPieceId)
        {
            IsValid = isValid;
            FailureReason = failureReason;
            BlockedCell = blockedCell;
            BlockingPieceId = blockingPieceId;
        }

        public bool IsValid { get; }
        public PlacementFailureReason FailureReason { get; }
        public GridCoordinate BlockedCell { get; }
        public string BlockingPieceId { get; }

        public static PlacementResult Valid => new PlacementResult(true, PlacementFailureReason.None, default, null);
    }

    public static class PlacementValidator
    {
        public static PlacementResult Validate(
            PieceDefinition piece,
            PiecePose pose,
            OccupancyMap occupancy)
        {
            if (piece == null)
            {
                return new PlacementResult(false, PlacementFailureReason.MissingPiece, default, null);
            }

            if (occupancy == null)
            {
                throw new ArgumentNullException(nameof(occupancy));
            }

            if (piece.footprint == null || piece.footprint.Length == 0)
            {
                return new PlacementResult(false, PlacementFailureReason.EmptyFootprint, default, null);
            }

            if (!GridMath.IsQuarterTurn(pose.rotation) || !piece.AllowsRotation(pose.rotation))
            {
                return new PlacementResult(false, PlacementFailureReason.UnsupportedRotation, default, null);
            }

            GridCoordinate[] cells = GridMath.GetOccupiedCells(piece, pose);
            for (int i = 0; i < cells.Length; i++)
            {
                GridCoordinate cell = cells[i];
                if (!occupancy.IsInside(cell))
                {
                    return new PlacementResult(false, PlacementFailureReason.OutsideBoard, cell, null);
                }

                string occupant = occupancy.GetOccupant(cell);
                if (!string.IsNullOrEmpty(occupant) && !string.Equals(occupant, piece.pieceId, StringComparison.Ordinal))
                {
                    return new PlacementResult(false, PlacementFailureReason.Occupied, cell, occupant);
                }
            }

            return PlacementResult.Valid;
        }
    }

    public static class TargetPoseValidator
    {
        public static bool IsCorrect(PieceDefinition piece, PiecePose pose)
        {
            if (piece == null || pose.position != piece.targetPosition)
            {
                return false;
            }

            int currentRotation = GridMath.NormalizeRotation(pose.rotation);
            int targetRotation = GridMath.NormalizeRotation(piece.targetRotation);
            if (currentRotation == targetRotation)
            {
                return true;
            }

            return !piece.requireExactTargetRotation &&
                   GridMath.HaveEquivalentFootprints(piece, currentRotation, targetRotation);
        }
    }
}
