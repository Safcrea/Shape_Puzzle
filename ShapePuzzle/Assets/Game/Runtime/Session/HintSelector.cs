using System;
using System.Collections.Generic;

namespace ToyPuzzle
{
    public static class HintSelector
    {
        public static PieceState Select(IReadOnlyList<PieceState> pieces, int seed, int hintIndex)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            var candidates = new List<PieceState>(pieces.Count);

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState piece = pieces[i];
                if (piece == null || piece.IsCorrect || piece.IsLocked)
                {
                    continue;
                }

                candidates.Add(piece);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            candidates.Sort((left, right) =>
            {
                uint leftRank = StableRank(left.PieceId, seed);
                uint rightRank = StableRank(right.PieceId, seed);
                int rankComparison = leftRank.CompareTo(rightRank);
                if (rankComparison != 0)
                {
                    return rankComparison;
                }

                return string.CompareOrdinal(left.PieceId, right.PieceId);
            });
            return candidates[PositiveModulo(hintIndex, candidates.Count)];
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static uint StableRank(string value, int seed)
        {
            unchecked
            {
                uint hash = 2166136261u ^ (uint)seed;
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash ^= value[i];
                        hash *= 16777619u;
                    }
                }

                return hash;
            }
        }
    }
}
