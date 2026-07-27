using System;
using UnityEngine;

namespace ToyPuzzle
{
    [Serializable]
    public sealed class PuzzlePieceArtwork
    {
        public string pieceId;
        public Sprite sprite;
        public Vector2 sizeInCells = Vector2.one;
        public Vector2 offsetFromTargetPivotInCells;
        public int bakedTargetRotation;
        public bool freeformColorBlock;
        public Vector2 targetCenterNormalized = new Vector2(0.5f, 0.5f);
        public Vector2 startingCenterNormalized = new Vector2(0.5f, 0.5f);
        public Vector2 sizeNormalized = Vector2.one;
        [Range(0.01f, 0.25f)] public float snapDistanceNormalized = 0.065f;

        public bool IsValid => !string.IsNullOrEmpty(pieceId) && sprite != null &&
                               (freeformColorBlock
                                   ? sizeNormalized.x > 0f && sizeNormalized.y > 0f
                                   : sizeInCells.x > 0f && sizeInCells.y > 0f);
    }

    [DisallowMultipleComponent]
    public sealed class PuzzleLevelPrefab : MonoBehaviour
    {
        [SerializeField] private LevelDefinition level = new LevelDefinition();
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private PuzzlePieceArtwork[] pieceArtwork = Array.Empty<PuzzlePieceArtwork>();
        [SerializeField, HideInInspector] private string referenceAnchorPieceId;
        [SerializeField, HideInInspector] private string sourceJsonPath;

        public LevelDefinition Level => level;
        public Sprite Thumbnail => thumbnail;
        public PuzzlePieceArtwork[] PieceArtwork => pieceArtwork;
        public string ReferenceAnchorPieceId =>
            !string.IsNullOrEmpty(referenceAnchorPieceId)
                ? referenceAnchorPieceId
                : ResolveReferenceAnchorPieceId();
        public string SourceJsonPath => sourceJsonPath;

        public void SetLevel(LevelDefinition value)
        {
            level = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void SetThumbnail(Sprite value)
        {
            thumbnail = value;
        }

        public void SetPieceArtwork(PuzzlePieceArtwork[] value)
        {
            pieceArtwork = value ?? Array.Empty<PuzzlePieceArtwork>();
        }

        public void SetReferenceAnchorPieceId(string value)
        {
            referenceAnchorPieceId = value ?? string.Empty;
        }

        public PuzzlePieceArtwork FindPieceArtwork(string pieceId)
        {
            if (pieceArtwork == null || string.IsNullOrEmpty(pieceId)) return null;
            for (int i = 0; i < pieceArtwork.Length; i++)
            {
                PuzzlePieceArtwork candidate = pieceArtwork[i];
                if (candidate != null && string.Equals(candidate.pieceId, pieceId, StringComparison.Ordinal)) return candidate;
            }
            return null;
        }

        public string ResolveReferenceAnchorPieceId()
        {
            if (pieceArtwork == null || pieceArtwork.Length == 0) return string.Empty;
            string bestId = string.Empty;
            float bestDistance = float.MaxValue;
            float bestArea = float.MinValue;
            for (int i = 0; i < pieceArtwork.Length; i++)
            {
                PuzzlePieceArtwork candidate = pieceArtwork[i];
                if (candidate == null || !candidate.IsValid || !candidate.freeformColorBlock) continue;
                float distance = (candidate.targetCenterNormalized - new Vector2(0.5f, 0.5f)).sqrMagnitude;
                float area = candidate.sizeNormalized.x * candidate.sizeNormalized.y;
                bool closer = distance < bestDistance - 0.000001f;
                bool sameDistance = Mathf.Abs(distance - bestDistance) <= 0.000001f;
                bool larger = area > bestArea + 0.000001f;
                bool stableTie = sameDistance && Mathf.Abs(area - bestArea) <= 0.000001f &&
                                 (string.IsNullOrEmpty(bestId) ||
                                  string.CompareOrdinal(candidate.pieceId, bestId) < 0);
                if (!closer && !(sameDistance && larger) && !stableTie) continue;
                bestId = candidate.pieceId;
                bestDistance = distance;
                bestArea = area;
            }
            return bestId;
        }

        public void SetSourceJsonPath(string value)
        {
            sourceJsonPath = value ?? string.Empty;
        }
    }
}
