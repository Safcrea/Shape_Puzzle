namespace ToyPuzzle.Tests
{
    internal static class TestLevelFactory
    {
        public static LevelDefinition CreateTwoPieceLevel()
        {
            return new LevelDefinition
            {
                levelId = "test_level",
                levelNumber = 1,
                boardWidth = 5,
                boardHeight = 5,
                scrambleSeed = 1234,
                pieces = new[]
                {
                    CreateSingleCellPiece("a", new GridCoordinate(0, 0), new GridCoordinate(1, 0), true),
                    CreateSingleCellPiece("b", new GridCoordinate(2, 0), new GridCoordinate(3, 0), false)
                }
            };
        }

        public static PieceDefinition CreateSingleCellPiece(
            string id,
            GridCoordinate start,
            GridCoordinate target,
            bool locksWhenCorrect = false)
        {
            return new PieceDefinition
            {
                pieceId = id,
                displayName = id,
                shapeType = PieceShapeType.Square,
                footprint = new[] { GridCoordinate.Zero },
                logicalPivot = GridCoordinate.Zero,
                startingPosition = start,
                targetPosition = target,
                startingRotation = 0,
                targetRotation = 0,
                allowedRotations = new[] { 0 },
                locksWhenCorrect = locksWhenCorrect
            };
        }
    }
}
