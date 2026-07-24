using NUnit.Framework;

namespace ToyPuzzle.Tests
{
    public sealed class PlacementAndLevelValidationTests
    {
        [Test]
        public void PlacementValidator_DistinguishesOverlapAndBounds()
        {
            var occupancy = new OccupancyMap(5, 5);
            PieceDefinition piece = TestLevelFactory.CreateSingleCellPiece(
                "candidate",
                GridCoordinate.Zero,
                GridCoordinate.Zero);
            occupancy.TryReserve("blocker", new[] { new GridCoordinate(2, 2) });

            PlacementResult overlap = PlacementValidator.Validate(
                piece,
                new PiecePose(new GridCoordinate(2, 2), 0),
                occupancy);
            PlacementResult bounds = PlacementValidator.Validate(
                piece,
                new PiecePose(new GridCoordinate(-1, 0), 0),
                occupancy);

            Assert.That(overlap.IsValid, Is.False);
            Assert.That(overlap.FailureReason, Is.EqualTo(PlacementFailureReason.Occupied));
            Assert.That(overlap.BlockingPieceId, Is.EqualTo("blocker"));
            Assert.That(bounds.FailureReason, Is.EqualTo(PlacementFailureReason.OutsideBoard));
        }

        [Test]
        public void LevelValidator_AcceptsIndependentRectangularDimensions()
        {
            LevelDefinition level = TestLevelFactory.CreateTwoPieceLevel();
            level.boardWidth = 5;
            level.boardHeight = 8;

            LevelValidationResult result = LevelDefinitionValidator.Validate(level);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void LevelValidator_RejectsOverlappingStartingLayout()
        {
            LevelDefinition level = TestLevelFactory.CreateTwoPieceLevel();
            level.pieces[1].startingPosition = level.pieces[0].startingPosition;

            LevelValidationResult result = LevelDefinitionValidator.Validate(level);

            Assert.That(result.IsValid, Is.False);
            bool containsOverlap = false;
            for (int i = 0; i < result.Issues.Length; i++)
            {
                if (result.Issues[i].Code.Contains("start.Occupied"))
                {
                    containsOverlap = true;
                    break;
                }
            }

            Assert.That(containsOverlap, Is.True);
        }
    }
}
