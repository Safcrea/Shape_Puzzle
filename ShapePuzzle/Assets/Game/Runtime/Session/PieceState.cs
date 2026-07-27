using System;

namespace ToyPuzzle
{
    public sealed class PieceState
    {
        internal PieceState(
            PieceDefinition definition,
            PiecePose pose,
            bool isCorrect,
            bool isLocked,
            bool isReferenceAnchor = false)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Pose = pose;
            IsCorrect = isCorrect;
            IsLocked = isLocked;
            IsReferenceAnchor = isReferenceAnchor;
        }

        public PieceDefinition Definition { get; }
        public string PieceId => Definition.pieceId;
        public PiecePose Pose { get; internal set; }
        public bool IsCorrect { get; internal set; }
        public bool IsLocked { get; internal set; }
        public bool IsReferenceAnchor { get; }
    }
}
