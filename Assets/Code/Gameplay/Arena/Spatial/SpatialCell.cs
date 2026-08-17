using System;
using UnityEngine;

namespace Code.Gameplay
{
    public readonly struct SpatialCell : IEquatable<SpatialCell>
    {
        public int X { get; }
        public int Y { get; }

        public SpatialCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(SpatialCell other)
        {
            return X == other.X &&
                   Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is SpatialCell other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    X * 73856093 ^
                    Y * 19349663;
            }
        }

        public static bool operator ==(
            SpatialCell left,
            SpatialCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            SpatialCell left,
            SpatialCell right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
