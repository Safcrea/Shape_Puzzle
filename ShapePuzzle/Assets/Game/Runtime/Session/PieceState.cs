using System;

namespace ToyPuzzle
{
    public sealed class PieceState
    {
        internal PieceState(PieceDefinition definition, PiecePose pose, bool isCorrect, bool isLocked)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Pose = pose;
            IsCorrect = isCorrect;
            IsLocked = isLocked;
        }

        public PieceDefinition Definition { get; }
        public string PieceId => Definition.pieceId;
        public PiecePose Pose { get; internal set; }
        public bool IsCorrect { get; internal set; }
        public bool IsLocked { get; internal set; }
    }
}
