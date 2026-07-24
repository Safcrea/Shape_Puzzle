using System;
using UnityEngine;

namespace ToyPuzzle
{
    public readonly struct RotatedFootprint
    {
        public RotatedFootprint(GridCoordinate[] cells, GridCoordinate pivot, int width, int height)
        {
            Cells = cells;
            Pivot = pivot;
            Width = width;
            Height = height;
        }

        public GridCoordinate[] Cells { get; }
        public GridCoordinate Pivot { get; }
        public int Width { get; }
        public int Height { get; }
    }

    public static class GridMath
    {
        public static int NormalizeRotation(int rotation)
        {
            int normalized = rotation % 360;
            if (normalized < 0)
            {
                normalized += 360;
            }

            return normalized;
        }

        public static bool IsQuarterTurn(int rotation)
        {
            return NormalizeRotation(rotation) % 90 == 0;
        }

        /// <summary>
        /// Rotates a coordinate clockwise in a grid whose positive Y axis points up.
        /// </summary>
        public static GridCoordinate RotateClockwise(GridCoordinate point, GridCoordinate pivot, int rotation)
        {
            int normalized = NormalizeRotation(rotation);
            if (!IsQuarterTurn(normalized))
            {
                throw new ArgumentException("Grid rotations must be multiples of 90 degrees.", nameof(rotation));
            }

            int dx = point.x - pivot.x;
            int dy = point.y - pivot.y;
            switch (normalized)
            {
                case 0:
                    return point;
                case 90:
                    return new GridCoordinate(pivot.x + dy, pivot.y - dx);
                case 180:
                    return new GridCoordinate(pivot.x - dx, pivot.y - dy);
                case 270:
                    return new GridCoordinate(pivot.x - dy, pivot.y + dx);
                default:
                    throw new ArgumentOutOfRangeException(nameof(rotation));
            }
        }

        public static RotatedFootprint GetRotatedFootprint(PieceDefinition piece, int rotation)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            return GetRotatedFootprint(piece.footprint, piece.logicalPivot, rotation);
        }

        public static RotatedFootprint GetRotatedFootprint(
            GridCoordinate[] sourceCells,
            GridCoordinate logicalPivot,
            int rotation)
        {
            if (sourceCells == null || sourceCells.Length == 0)
            {
                return new RotatedFootprint(Array.Empty<GridCoordinate>(), logicalPivot, 0, 0);
            }

            int normalizedRotation = NormalizeRotation(rotation);
            if (!IsQuarterTurn(normalizedRotation))
            {
                throw new ArgumentException("Grid rotations must be multiples of 90 degrees.", nameof(rotation));
            }

            var rotated = new GridCoordinate[sourceCells.Length];
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            for (int i = 0; i < sourceCells.Length; i++)
            {
                GridCoordinate cell = RotateClockwise(sourceCells[i], logicalPivot, normalizedRotation);
                rotated[i] = cell;
                minX = Math.Min(minX, cell.x);
                minY = Math.Min(minY, cell.y);
                maxX = Math.Max(maxX, cell.x);
                maxY = Math.Max(maxY, cell.y);
            }

            for (int i = 0; i < rotated.Length; i++)
            {
                rotated[i] = new GridCoordinate(rotated[i].x - minX, rotated[i].y - minY);
            }

            Array.Sort(rotated);
            GridCoordinate normalizedPivot = new GridCoordinate(logicalPivot.x - minX, logicalPivot.y - minY);
            return new RotatedFootprint(rotated, normalizedPivot, maxX - minX + 1, maxY - minY + 1);
        }

        public static GridCoordinate[] Translate(GridCoordinate[] cells, GridCoordinate position)
        {
            if (cells == null || cells.Length == 0)
            {
                return Array.Empty<GridCoordinate>();
            }

            var translated = new GridCoordinate[cells.Length];
            Translate(cells, position, translated);
            return translated;
        }

        public static void Translate(GridCoordinate[] cells, GridCoordinate position, GridCoordinate[] destination)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (destination == null || destination.Length < cells.Length)
            {
                throw new ArgumentException("Destination must be at least as large as the source footprint.", nameof(destination));
            }

            for (int i = 0; i < cells.Length; i++)
            {
                destination[i] = cells[i] + position;
            }
        }

        public static GridCoordinate[] GetOccupiedCells(PieceDefinition piece, PiecePose pose)
        {
            RotatedFootprint footprint = GetRotatedFootprint(piece, pose.rotation);
            return Translate(footprint.Cells, pose.position);
        }

        public static PiecePose RotatePoseKeepingPivot(PieceDefinition piece, PiecePose pose, int deltaRotation)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            int newRotation = NormalizeRotation(pose.rotation + deltaRotation);
            RotatedFootprint oldFootprint = GetRotatedFootprint(piece, pose.rotation);
            RotatedFootprint newFootprint = GetRotatedFootprint(piece, newRotation);
            GridCoordinate worldPivot = pose.position + oldFootprint.Pivot;
            GridCoordinate newPosition = worldPivot - newFootprint.Pivot;
            return new PiecePose(newPosition, newRotation);
        }

        public static bool HaveEquivalentFootprints(PieceDefinition piece, int firstRotation, int secondRotation)
        {
            RotatedFootprint first = GetRotatedFootprint(piece, firstRotation);
            RotatedFootprint second = GetRotatedFootprint(piece, secondRotation);
            if (first.Cells.Length != second.Cells.Length)
            {
                return false;
            }

            for (int i = 0; i < first.Cells.Length; i++)
            {
                if (first.Cells[i] != second.Cells[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static Vector2 GridToLocal(GridCoordinate coordinate, Vector2 gridOrigin, float cellSize)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            }

            return gridOrigin + new Vector2(coordinate.x * cellSize, coordinate.y * cellSize);
        }

        public static GridCoordinate LocalToNearestGrid(Vector2 localPosition, Vector2 gridOrigin, float cellSize)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            }

            Vector2 relative = (localPosition - gridOrigin) / cellSize;
            return new GridCoordinate(Mathf.RoundToInt(relative.x), Mathf.RoundToInt(relative.y));
        }

        public static bool TryLocalToGrid(
            Vector2 localPosition,
            Vector2 gridOrigin,
            float cellSize,
            float snapThresholdInCells,
            out GridCoordinate coordinate)
        {
            coordinate = LocalToNearestGrid(localPosition, gridOrigin, cellSize);
            Vector2 snapped = GridToLocal(coordinate, gridOrigin, cellSize);
            float threshold = Mathf.Max(0f, snapThresholdInCells) * cellSize;
            return Vector2.Distance(localPosition, snapped) <= threshold;
        }
    }
}
