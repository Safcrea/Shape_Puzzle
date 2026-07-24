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
        [SerializeField, HideInInspector] private string sourceJsonPath;

        public LevelDefinition Level => level;
        public Sprite Thumbnail => thumbnail;
        public PuzzlePieceArtwork[] PieceArtwork => pieceArtwork;
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

        public void SetSourceJsonPath(string value)
        {
            sourceJsonPath = value ?? string.Empty;
        }
    }
}
