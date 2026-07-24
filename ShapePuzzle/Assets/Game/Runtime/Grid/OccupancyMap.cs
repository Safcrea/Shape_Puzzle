using System;

namespace ToyPuzzle
{
    public sealed class OccupancyMap
    {
        private readonly string[] occupants;

        public OccupancyMap(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            occupants = new string[width * height];
        }

        public int Width { get; }
        public int Height { get; }

        public bool IsInside(GridCoordinate coordinate)
        {
            return coordinate.x >= 0 && coordinate.x < Width &&
                   coordinate.y >= 0 && coordinate.y < Height;
        }

        public string GetOccupant(GridCoordinate coordinate)
        {
            if (!IsInside(coordinate))
            {
                return null;
            }

            return occupants[ToIndex(coordinate)];
        }

        public bool CanReserve(string pieceId, GridCoordinate[] cells, out GridCoordinate blockedCell)
        {
            if (string.IsNullOrEmpty(pieceId))
            {
                throw new ArgumentException("A piece ID is required.", nameof(pieceId));
            }

            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            for (int i = 0; i < cells.Length; i++)
            {
                GridCoordinate cell = cells[i];
                if (!IsInside(cell))
                {
                    blockedCell = cell;
                    return false;
                }

                string occupant = occupants[ToIndex(cell)];
                if (!string.IsNullOrEmpty(occupant) && !string.Equals(occupant, pieceId, StringComparison.Ordinal))
                {
                    blockedCell = cell;
                    return false;
                }
            }

            blockedCell = default;
            return true;
        }

        public bool TryReserve(string pieceId, GridCoordinate[] cells)
        {
            if (!CanReserve(pieceId, cells, out _))
            {
                return false;
            }

            for (int i = 0; i < cells.Length; i++)
            {
                occupants[ToIndex(cells[i])] = pieceId;
            }

            return true;
        }

        public void Release(string pieceId)
        {
            if (string.IsNullOrEmpty(pieceId))
            {
                return;
            }

            for (int i = 0; i < occupants.Length; i++)
            {
                if (string.Equals(occupants[i], pieceId, StringComparison.Ordinal))
                {
                    occupants[i] = null;
                }
            }
        }

        public void Clear()
        {
            Array.Clear(occupants, 0, occupants.Length);
        }

        private int ToIndex(GridCoordinate coordinate)
        {
            return coordinate.y * Width + coordinate.x;
        }
    }
}
