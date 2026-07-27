using NUnit.Framework;

namespace ToyPuzzle.Tests
{
    public sealed class PuzzleSessionTests
    {
        [Test]
        public void ValidMove_LocksCorrectPieceAndUndoRestoresEveryState()
        {
            var session = new PuzzleSession(TestLevelFactory.CreateTwoPieceLevel());

            PuzzleActionResult move = session.TryMove("a", new GridCoordinate(1, 0));

            Assert.That(move.Succeeded, Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(1));
            Assert.That(session.TryGetPiece("a", out PieceState movedPiece), Is.True);
            Assert.That(movedPiece.IsCorrect, Is.True);
            Assert.That(movedPiece.IsLocked, Is.True);
            Assert.That(session.Occupancy.GetOccupant(new GridCoordinate(1, 0)), Is.EqualTo("a"));

            Assert.That(session.TryUndo(out MoveRecord record), Is.True);
            Assert.That(record.PieceId, Is.EqualTo("a"));
            Assert.That(movedPiece.Pose.position, Is.EqualTo(new GridCoordinate(0, 0)));
            Assert.That(movedPiece.IsCorrect, Is.False);
            Assert.That(movedPiece.IsLocked, Is.False);
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(session.Occupancy.GetOccupant(new GridCoordinate(0, 0)), Is.EqualTo("a"));
            Assert.That(session.Occupancy.GetOccupant(new GridCoordinate(1, 0)), Is.Null);
        }

        [Test]
        public void InvalidMove_DoesNotChangePoseCountOrHistory()
        {
            var session = new PuzzleSession(TestLevelFactory.CreateTwoPieceLevel());

            PuzzleActionResult result = session.TryMove("a", new GridCoordinate(2, 0));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Placement.FailureReason, Is.EqualTo(PlacementFailureReason.Occupied));
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.TryGetPiece("a", out PieceState piece), Is.True);
            Assert.That(piece.Pose.position, Is.EqualTo(GridCoordinate.Zero));
        }

        [Test]
        public void HintSelection_IsDeterministicAndSkipsCorrectLockedPiece()
        {
            var first = new PuzzleSession(TestLevelFactory.CreateTwoPieceLevel());
            var second = new PuzzleSession(TestLevelFactory.CreateTwoPieceLevel());
            first.TryMove("a", new GridCoordinate(1, 0));
            second.TryMove("a", new GridCoordinate(1, 0));

            PieceState firstHint = first.RequestHint();
            PieceState secondHint = second.RequestHint();

            Assert.That(firstHint.PieceId, Is.EqualTo("b"));
            Assert.That(secondHint.PieceId, Is.EqualTo(firstHint.PieceId));
            Assert.That(first.HintUsageCount, Is.EqualTo(1));
        }

        [Test]
        public void Pause_StopsTimerAndRejectsActions()
        {
            var session = new PuzzleSession(TestLevelFactory.CreateTwoPieceLevel());
            session.AdvanceTime(1.5f);
            session.SetPaused(true);
            session.AdvanceTime(4f);

            PuzzleActionResult result = session.TryMove("a", new GridCoordinate(1, 0));

            Assert.That(session.ElapsedSeconds, Is.EqualTo(1.5f));
            Assert.That(result.Failure, Is.EqualTo(PuzzleActionFailure.SessionPaused));
        }

        [Test]
        public void Rotation_PreservesPivotAndCommitsTargetPose()
        {
            var line = new PieceDefinition
            {
                pieceId = "line",
                footprint = new[]
                {
                    new GridCoordinate(0, 0),
                    new GridCoordinate(1, 0),
                    new GridCoordinate(2, 0)
                },
                logicalPivot = new GridCoordinate(1, 0),
                startingPosition = new GridCoordinate(1, 1),
                startingRotation = 0,
                targetPosition = new GridCoordinate(2, 0),
                targetRotation = 90,
                allowedRotations = new[] { 0, 90, 180, 270 }
            };
            var level = new LevelDefinition
            {
                levelId = "rotation_test",
                boardWidth = 5,
                boardHeight = 5,
                pieces = new[]
                {
                    line,
                    TestLevelFactory.CreateSingleCellPiece(
                        "other",
                        new GridCoordinate(4, 4),
                        new GridCoordinate(3, 4))
                }
            };
            var session = new PuzzleSession(level);

            PuzzleActionResult result = session.TryRotate("line");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.TryGetPiece("line", out PieceState state), Is.True);
            Assert.That(state.Pose, Is.EqualTo(new PiecePose(new GridCoordinate(2, 0), 90)));
            Assert.That(state.IsCorrect, Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(1));
        }

        [Test]
        public void SymmetricDuplicateRotation_IsNotCountedUnlessExactRotationIsRequired()
        {
            LevelDefinition level = TestLevelFactory.CreateTwoPieceLevel();
            level.pieces[0].allowedRotations = new[] { 0, 90 };
            var session = new PuzzleSession(level);

            PuzzleActionResult result = session.TryRotate("a");

            Assert.That(result.Failure, Is.EqualTo(PuzzleActionFailure.NoChange));
            Assert.That(session.MoveCount, Is.Zero);
        }

        [Test]
        public void ReferenceAnchor_StartsSolvedLockedAndIsExcludedFromProgressActions()
        {
            LevelDefinition level = TestLevelFactory.CreateTwoPieceLevel();
            var session = new PuzzleSession(level, "a");

            Assert.That(session.ReferenceAnchorPieceId, Is.EqualTo("a"));
            Assert.That(session.TryGetPiece("a", out PieceState anchor), Is.True);
            Assert.That(anchor.IsReferenceAnchor, Is.True);
            Assert.That(anchor.Pose, Is.EqualTo(anchor.Definition.TargetPose));
            Assert.That(anchor.IsCorrect, Is.True);
            Assert.That(anchor.IsLocked, Is.True);
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(session.RequestHint().PieceId, Is.EqualTo("b"));
            Assert.That(session.TryUndo(out _), Is.False);

            Assert.That(
                session.RestoreProgress(
                    new System.Collections.Generic.HashSet<string>(),
                    3,
                    2f,
                    1),
                Is.True);
            Assert.That(anchor.IsCorrect, Is.True);
            Assert.That(anchor.IsLocked, Is.True);
            Assert.That(anchor.Pose, Is.EqualTo(anchor.Definition.TargetPose));
            Assert.That(session.MoveCount, Is.EqualTo(3));
        }
    }
}
