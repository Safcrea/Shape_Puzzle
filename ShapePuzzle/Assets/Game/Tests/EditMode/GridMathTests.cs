using NUnit.Framework;

namespace ToyPuzzle.Tests
{
    public sealed class GridMathTests
    {
        [Test]
        public void RotatedFootprint_NormalizesCellsAndPivot()
        {
            var cells = new[]
            {
                new GridCoordinate(0, 0),
                new GridCoordinate(1, 0),
                new GridCoordinate(2, 0)
            };

            RotatedFootprint result = GridMath.GetRotatedFootprint(cells, new GridCoordinate(1, 0), 90);

            Assert.That(result.Width, Is.EqualTo(1));
            Assert.That(result.Height, Is.EqualTo(3));
            Assert.That(result.Pivot, Is.EqualTo(new GridCoordinate(0, 1)));
            Assert.That(result.Cells, Is.EqualTo(new[]
            {
                new GridCoordinate(0, 0),
                new GridCoordinate(0, 1),
                new GridCoordinate(0, 2)
            }));
        }

        [Test]
        public void RotatePoseKeepingPivot_PreservesWorldPivot()
        {
            var piece = new PieceDefinition
            {
                pieceId = "line",
                footprint = new[]
                {
                    new GridCoordinate(0, 0),
                    new GridCoordinate(1, 0),
                    new GridCoordinate(2, 0)
                },
                logicalPivot = new GridCoordinate(1, 0)
            };
            var original = new PiecePose(new GridCoordinate(1, 1), 0);

            PiecePose rotated = GridMath.RotatePoseKeepingPivot(piece, original, 90);

            Assert.That(rotated, Is.EqualTo(new PiecePose(new GridCoordinate(2, 0), 90)));
            Assert.That(GridMath.GetOccupiedCells(piece, rotated), Is.EqualTo(new[]
            {
                new GridCoordinate(2, 0),
                new GridCoordinate(2, 1),
                new GridCoordinate(2, 2)
            }));
        }

        [TestCase(-90, 270)]
        [TestCase(360, 0)]
        [TestCase(450, 90)]
        public void NormalizeRotation_UsesCanonicalQuarterTurns(int input, int expected)
        {
            Assert.That(GridMath.NormalizeRotation(input), Is.EqualTo(expected));
        }
    }
}
