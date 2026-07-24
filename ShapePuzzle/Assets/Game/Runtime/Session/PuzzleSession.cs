using System;
using System.Collections.Generic;

namespace ToyPuzzle
{
    public enum PuzzleActionFailure
    {
        None,
        SessionComplete,
        SessionPaused,
        PieceNotFound,
        PieceLocked,
        NoChange,
        InvalidPlacement,
        EmptyHistory
    }

    public readonly struct PuzzleActionResult
    {
        public PuzzleActionResult(bool succeeded, PuzzleActionFailure failure, PlacementResult placement)
        {
            Succeeded = succeeded;
            Failure = failure;
            Placement = placement;
        }

        public bool Succeeded { get; }
        public PuzzleActionFailure Failure { get; }
        public PlacementResult Placement { get; }

        public static PuzzleActionResult Success =>
            new PuzzleActionResult(true, PuzzleActionFailure.None, PlacementResult.Valid);
    }

    public sealed class PuzzleSession
    {
        private readonly Dictionary<string, PieceState> piecesById;
        private readonly List<PieceState> orderedPieces;
        private readonly OccupancyMap occupancy;
        private readonly MoveHistory history;

        public PuzzleSession(LevelDefinition level, int historyCapacity = MoveHistory.DefaultCapacity)
        {
            LevelValidationResult validation = LevelDefinitionValidator.Validate(level);
            if (!validation.IsValid)
            {
                throw new ArgumentException(BuildValidationMessage(validation), nameof(level));
            }

            Level = level;
            piecesById = new Dictionary<string, PieceState>(level.pieces.Length, StringComparer.Ordinal);
            orderedPieces = new List<PieceState>(level.pieces.Length);
            occupancy = new OccupancyMap(level.boardWidth, level.boardHeight);
            history = new MoveHistory(historyCapacity);
            Reset();
        }

        public event Action<PieceState> PieceChanged;
        public event Action StateChanged;
        public event Action Completed;

        public LevelDefinition Level { get; }
        public IReadOnlyList<PieceState> Pieces => orderedPieces;
        public OccupancyMap Occupancy => occupancy;
        public int MoveCount { get; private set; }
        public int HintUsageCount { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool IsComplete { get; private set; }
        public bool IsPaused { get; private set; }
        public bool CanUndo => !IsComplete && !IsPaused && history.Count > 0;

        public bool TryGetPiece(string pieceId, out PieceState state)
        {
            if (string.IsNullOrEmpty(pieceId))
            {
                state = null;
                return false;
            }

            return piecesById.TryGetValue(pieceId, out state);
        }

        public PuzzleActionResult TryMove(string pieceId, GridCoordinate newPosition)
        {
            if (!TryGetActionablePiece(pieceId, out PieceState state, out PuzzleActionResult failure))
            {
                return failure;
            }

            PiecePose candidate = new PiecePose(newPosition, state.Pose.rotation);
            return TryCommit(state, candidate, PuzzleActionType.Move);
        }

        public PuzzleActionResult TryRotate(string pieceId, int quarterTurns = 1)
        {
            if (!TryGetActionablePiece(pieceId, out PieceState state, out PuzzleActionResult failure))
            {
                return failure;
            }

            int delta = quarterTurns * 90;
            PiecePose candidate = GridMath.RotatePoseKeepingPivot(state.Definition, state.Pose, delta);
            return TryCommit(state, candidate, PuzzleActionType.Rotate);
        }

        public bool TryUndo(out MoveRecord undoneMove)
        {
            undoneMove = default;
            if (IsComplete || IsPaused || !history.TryPop(out undoneMove))
            {
                return false;
            }

            if (!piecesById.TryGetValue(undoneMove.PieceId, out PieceState state))
            {
                return false;
            }

            occupancy.Release(state.PieceId);
            state.Pose = undoneMove.PreviousPose;
            state.IsCorrect = undoneMove.PreviousCorrect;
            state.IsLocked = undoneMove.PreviousLocked;
            if (!occupancy.TryReserve(state.PieceId, GridMath.GetOccupiedCells(state.Definition, state.Pose)))
            {
                throw new InvalidOperationException("Undo history no longer describes a valid occupancy state.");
            }

            MoveCount = Math.Max(0, MoveCount - 1);
            PieceChanged?.Invoke(state);
            StateChanged?.Invoke();
            return true;
        }

        public PieceState RequestHint()
        {
            if (IsComplete || IsPaused)
            {
                return null;
            }

            PieceState selection = HintSelector.Select(orderedPieces, Level.scrambleSeed, HintUsageCount);
            if (selection != null)
            {
                HintUsageCount++;
                StateChanged?.Invoke();
            }

            return selection;
        }

        public bool RegisterFailedMove()
        {
            if (IsComplete || IsPaused) return false;
            MoveCount++;
            StateChanged?.Invoke();
            return true;
        }

        public void AdvanceTime(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(unscaledDeltaTime));
            }

            if (!IsComplete && !IsPaused)
            {
                ElapsedSeconds += unscaledDeltaTime;
            }
        }

        public void SetPaused(bool paused)
        {
            if (IsComplete || IsPaused == paused)
            {
                return;
            }

            IsPaused = paused;
            StateChanged?.Invoke();
        }

        public void Reset()
        {
            piecesById.Clear();
            orderedPieces.Clear();
            occupancy.Clear();
            history.Clear();
            MoveCount = 0;
            HintUsageCount = 0;
            ElapsedSeconds = 0f;
            IsPaused = false;

            for (int i = 0; i < Level.pieces.Length; i++)
            {
                PieceDefinition definition = Level.pieces[i];
                PiecePose pose = definition.StartingPose;
                var state = new PieceState(
                    definition,
                    pose,
                    TargetPoseValidator.IsCorrect(definition, pose),
                    definition.startsLocked);
                piecesById.Add(state.PieceId, state);
                orderedPieces.Add(state);
                if (!occupancy.TryReserve(state.PieceId, GridMath.GetOccupiedCells(definition, pose)))
                {
                    throw new InvalidOperationException($"Starting occupancy for '{state.PieceId}' is invalid.");
                }
            }

            IsComplete = AreAllPiecesCorrect();
            StateChanged?.Invoke();
        }

        public bool RestoreProgress(ISet<string> correctPieceIds, int moveCount, float elapsedSeconds, int hintsUsed)
        {
            if (correctPieceIds == null) throw new ArgumentNullException(nameof(correctPieceIds));
            if (moveCount < 0) throw new ArgumentOutOfRangeException(nameof(moveCount));
            if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (hintsUsed < 0) throw new ArgumentOutOfRangeException(nameof(hintsUsed));

            occupancy.Clear();
            history.Clear();
            for (int i = 0; i < orderedPieces.Count; i++)
            {
                PieceState state = orderedPieces[i];
                bool correct = correctPieceIds.Contains(state.PieceId);
                state.Pose = correct ? state.Definition.TargetPose : state.Definition.StartingPose;
                state.IsCorrect = correct && TargetPoseValidator.IsCorrect(state.Definition, state.Pose);
                state.IsLocked = state.IsCorrect &&
                                 (state.Definition.locksWhenCorrect || Level.lockCorrectPiecesByDefault);
                if (!occupancy.TryReserve(state.PieceId, GridMath.GetOccupiedCells(state.Definition, state.Pose)))
                {
                    Reset();
                    return false;
                }
            }

            MoveCount = moveCount;
            ElapsedSeconds = elapsedSeconds;
            HintUsageCount = hintsUsed;
            IsPaused = false;
            IsComplete = AreAllPiecesCorrect();
            StateChanged?.Invoke();
            return true;
        }

        private PuzzleActionResult TryCommit(PieceState state, PiecePose candidate, PuzzleActionType actionType)
        {
            if (state.Pose.Equals(candidate) ||
                (!state.Definition.requireExactTargetRotation && OccupiesSameCells(state, candidate)))
            {
                return new PuzzleActionResult(false, PuzzleActionFailure.NoChange, PlacementResult.Valid);
            }

            PlacementResult placement = PlacementValidator.Validate(state.Definition, candidate, occupancy);
            if (!placement.IsValid)
            {
                return new PuzzleActionResult(false, PuzzleActionFailure.InvalidPlacement, placement);
            }

            PiecePose previousPose = state.Pose;
            bool previousCorrect = state.IsCorrect;
            bool previousLocked = state.IsLocked;

            occupancy.Release(state.PieceId);
            if (!occupancy.TryReserve(state.PieceId, GridMath.GetOccupiedCells(state.Definition, candidate)))
            {
                occupancy.TryReserve(state.PieceId, GridMath.GetOccupiedCells(state.Definition, previousPose));
                throw new InvalidOperationException("A placement changed between validation and commit.");
            }

            state.Pose = candidate;
            state.IsCorrect = TargetPoseValidator.IsCorrect(state.Definition, candidate);
            state.IsLocked = state.IsCorrect &&
                             (state.Definition.locksWhenCorrect || Level.lockCorrectPiecesByDefault);
            history.Push(new MoveRecord(
                actionType,
                state.PieceId,
                previousPose,
                previousCorrect,
                previousLocked,
                state.Pose,
                state.IsCorrect,
                state.IsLocked));
            MoveCount++;

            PieceChanged?.Invoke(state);
            if (AreAllPiecesCorrect())
            {
                IsComplete = true;
                Completed?.Invoke();
            }

            StateChanged?.Invoke();
            return PuzzleActionResult.Success;
        }

        private bool TryGetActionablePiece(
            string pieceId,
            out PieceState state,
            out PuzzleActionResult failure)
        {
            if (IsComplete)
            {
                state = null;
                failure = new PuzzleActionResult(false, PuzzleActionFailure.SessionComplete, PlacementResult.Valid);
                return false;
            }

            if (IsPaused)
            {
                state = null;
                failure = new PuzzleActionResult(false, PuzzleActionFailure.SessionPaused, PlacementResult.Valid);
                return false;
            }

            if (!TryGetPiece(pieceId, out state))
            {
                failure = new PuzzleActionResult(false, PuzzleActionFailure.PieceNotFound, PlacementResult.Valid);
                return false;
            }

            if (state.IsLocked)
            {
                failure = new PuzzleActionResult(false, PuzzleActionFailure.PieceLocked, PlacementResult.Valid);
                return false;
            }

            failure = default;
            return true;
        }

        private bool OccupiesSameCells(PieceState state, PiecePose candidate)
        {
            GridCoordinate[] current = GridMath.GetOccupiedCells(state.Definition, state.Pose);
            GridCoordinate[] next = GridMath.GetOccupiedCells(state.Definition, candidate);
            if (current.Length != next.Length)
            {
                return false;
            }

            Array.Sort(current);
            Array.Sort(next);
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] != next[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreAllPiecesCorrect()
        {
            for (int i = 0; i < orderedPieces.Count; i++)
            {
                if (!orderedPieces[i].IsCorrect)
                {
                    return false;
                }
            }

            return orderedPieces.Count > 0;
        }

        private static string BuildValidationMessage(LevelValidationResult validation)
        {
            for (int i = 0; i < validation.Issues.Length; i++)
            {
                LevelValidationIssue issue = validation.Issues[i];
                if (issue.Severity == ValidationSeverity.Error)
                {
                    return $"Invalid level data ({issue.Code}): {issue.Message}";
                }
            }

            return "Invalid level data.";
        }
    }
}
