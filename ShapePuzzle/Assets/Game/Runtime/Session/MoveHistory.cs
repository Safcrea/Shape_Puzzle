using System;
using System.Collections.Generic;

namespace ToyPuzzle
{
    public enum PuzzleActionType
    {
        Move,
        Rotate
    }

    public readonly struct MoveRecord
    {
        public MoveRecord(
            PuzzleActionType actionType,
            string pieceId,
            PiecePose previousPose,
            bool previousCorrect,
            bool previousLocked,
            PiecePose newPose,
            bool newCorrect,
            bool newLocked)
        {
            ActionType = actionType;
            PieceId = pieceId;
            PreviousPose = previousPose;
            PreviousCorrect = previousCorrect;
            PreviousLocked = previousLocked;
            NewPose = newPose;
            NewCorrect = newCorrect;
            NewLocked = newLocked;
        }

        public PuzzleActionType ActionType { get; }
        public string PieceId { get; }
        public PiecePose PreviousPose { get; }
        public bool PreviousCorrect { get; }
        public bool PreviousLocked { get; }
        public PiecePose NewPose { get; }
        public bool NewCorrect { get; }
        public bool NewLocked { get; }
    }

    public sealed class MoveHistory
    {
        public const int DefaultCapacity = 20;

        private readonly List<MoveRecord> records;

        public MoveHistory(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            Capacity = capacity;
            records = new List<MoveRecord>(capacity);
        }

        public int Capacity { get; }
        public int Count => records.Count;

        public void Push(MoveRecord record)
        {
            if (records.Count == Capacity)
            {
                records.RemoveAt(0);
            }

            records.Add(record);
        }

        public bool TryPop(out MoveRecord record)
        {
            if (records.Count == 0)
            {
                record = default;
                return false;
            }

            int lastIndex = records.Count - 1;
            record = records[lastIndex];
            records.RemoveAt(lastIndex);
            return true;
        }

        public void Clear()
        {
            records.Clear();
        }
    }
}
