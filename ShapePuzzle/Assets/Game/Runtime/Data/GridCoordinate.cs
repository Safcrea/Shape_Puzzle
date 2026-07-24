using System;
using UnityEngine;

namespace ToyPuzzle
{
    [Serializable]
    public struct GridCoordinate : IEquatable<GridCoordinate>, IComparable<GridCoordinate>
    {
        public int x;
        public int y;

        public GridCoordinate(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static GridCoordinate Zero => new GridCoordinate(0, 0);

        public bool Equals(GridCoordinate other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public int CompareTo(GridCoordinate other)
        {
            int yComparison = y.CompareTo(other.y);
            return yComparison != 0 ? yComparison : x.CompareTo(other.x);
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public static GridCoordinate operator +(GridCoordinate left, GridCoordinate right)
        {
            return new GridCoordinate(left.x + right.x, left.y + right.y);
        }

        public static GridCoordinate operator -(GridCoordinate left, GridCoordinate right)
        {
            return new GridCoordinate(left.x - right.x, left.y - right.y);
        }

        public static bool operator ==(GridCoordinate left, GridCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridCoordinate left, GridCoordinate right)
        {
            return !left.Equals(right);
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }

        public static GridCoordinate FromVector2Int(Vector2Int value)
        {
            return new GridCoordinate(value.x, value.y);
        }
    }

    [Serializable]
    public struct FloatCoordinate
    {
        public float x;
        public float y;

        public FloatCoordinate(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }

    [Serializable]
    public struct PiecePose : IEquatable<PiecePose>
    {
        public GridCoordinate position;
        public int rotation;

        public PiecePose(GridCoordinate position, int rotation)
        {
            this.position = position;
            this.rotation = GridMath.NormalizeRotation(rotation);
        }

        public bool Equals(PiecePose other)
        {
            return position == other.position && rotation == other.rotation;
        }

        public override bool Equals(object obj)
        {
            return obj is PiecePose other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (position.GetHashCode() * 397) ^ rotation;
            }
        }

        public override string ToString()
        {
            return $"{position} @ {rotation}°";
        }
    }
}
