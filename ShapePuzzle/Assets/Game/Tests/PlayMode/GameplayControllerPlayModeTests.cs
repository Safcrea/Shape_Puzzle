using System;
using NUnit.Framework;
using UnityEngine;

namespace ToyPuzzle.Tests.PlayMode
{
    public sealed class GameplayControllerPlayModeTests
    {
        [Test]
        public void LoadLevel_BuildsBoardAndRelaysMoveAndUndoWithoutWaitingForAFrame()
        {
            TestRig rig = CreateRig(CreateTwoPieceLevel());
            try
            {
                PuzzleSession startedSession = null;
                rig.Controller.SessionStarted += session => startedSession = session;

                rig.Controller.LoadLevel(rig.RuntimeLevel);

                Assert.That(rig.Controller.Session, Is.SameAs(startedSession));
                Assert.That(rig.Board.CellSize, Is.GreaterThan(0f));
                Assert.That(rig.Board.FindPiece("a"), Is.Not.Null);
                Assert.That(rig.Board.FindPiece("b"), Is.Not.Null);

                PuzzlePieceView movedView = rig.Board.FindPiece("a");
                Transform originalVisualCell = movedView.transform.GetChild(0);
                PuzzleActionResult move = rig.Controller.Session.TryMove("a", new GridCoordinate(1, 0));

                Assert.That(move.Succeeded, Is.True);
                Assert.That(movedView.RectTransform.anchoredPosition,
                    Is.EqualTo(new Vector2(rig.Board.CellSize, 0f)));
                Assert.That(movedView.IsLocked, Is.True);
                Assert.That(movedView.transform.childCount, Is.EqualTo(1));
                Assert.That(movedView.transform.GetChild(0), Is.SameAs(originalVisualCell),
                    "Translation-only moves should retain their visual geometry.");

                rig.Controller.Undo();

                Assert.That(rig.Controller.Session.TryGetPiece("a", out PieceState restored), Is.True);
                Assert.That(restored.Pose.position, Is.EqualTo(GridCoordinate.Zero));
                Assert.That(restored.IsLocked, Is.False);
                Assert.That(movedView.RectTransform.anchoredPosition, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void RotateSelected_UpdatesSessionAndRotatedViewFootprintSynchronously()
        {
            TestRig rig = CreateRig(CreateRotationLevel());
            try
            {
                rig.Controller.LoadLevel(rig.RuntimeLevel);
                PuzzlePieceView lineView = rig.Board.FindPiece("line");
                rig.Controller.SelectPiece(lineView);

                rig.Controller.RotateSelected();

                Assert.That(rig.Controller.SelectedPieceId, Is.EqualTo("line"));
                Assert.That(rig.Controller.Session.TryGetPiece("line", out PieceState line), Is.True);
                Assert.That(line.Pose, Is.EqualTo(new PiecePose(new GridCoordinate(2, 0), 90)));
                Assert.That(line.IsCorrect, Is.True);
                Assert.That(lineView.RectTransform.sizeDelta,
                    Is.EqualTo(new Vector2(rig.Board.CellSize, rig.Board.CellSize * 3f)));
            }
            finally
            {
                rig.Dispose();
            }
        }

        private static TestRig CreateRig(LevelDefinition level)
        {
            var root = new GameObject("GameplayControllerPlayModeTest", typeof(RectTransform));
            root.SetActive(false);
            var controller = root.AddComponent<PuzzleGameController>();

            var boardObject = new GameObject("Board", typeof(RectTransform), typeof(PuzzleBoardView));
            boardObject.transform.SetParent(root.transform, false);
            var boardRect = (RectTransform)boardObject.transform;
            boardRect.sizeDelta = new Vector2(600f, 600f);
            var board = boardObject.GetComponent<PuzzleBoardView>();

            RuntimeLevelData runtimeLevel = ScriptableObject.CreateInstance<RuntimeLevelData>();
            runtimeLevel.SetLevel(level);
            root.SetActive(true);
            return new TestRig(root, runtimeLevel, controller, board);
        }

        private static LevelDefinition CreateTwoPieceLevel()
        {
            return new LevelDefinition
            {
                levelId = "playmode_move",
                levelNumber = 1,
                boardWidth = 5,
                boardHeight = 5,
                scrambleSeed = 1204,
                pieces = new[]
                {
                    CreateSingleCellPiece("a", new GridCoordinate(0, 0), new GridCoordinate(1, 0), true),
                    CreateSingleCellPiece("b", new GridCoordinate(2, 0), new GridCoordinate(3, 0), false)
                }
            };
        }

        private static LevelDefinition CreateRotationLevel()
        {
            var line = new PieceDefinition
            {
                pieceId = "line",
                displayName = "Line",
                shapeType = PieceShapeType.Rectangle,
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

            return new LevelDefinition
            {
                levelId = "playmode_rotation",
                levelNumber = 1,
                boardWidth = 5,
                boardHeight = 5,
                pieces = new[]
                {
                    line,
                    CreateSingleCellPiece("other", new GridCoordinate(4, 4), new GridCoordinate(3, 4), false)
                }
            };
        }

        private static PieceDefinition CreateSingleCellPiece(
            string id,
            GridCoordinate start,
            GridCoordinate target,
            bool locksWhenCorrect)
        {
            return new PieceDefinition
            {
                pieceId = id,
                displayName = id,
                shapeType = PieceShapeType.Square,
                colorId = "cyan",
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

        private sealed class TestRig : IDisposable
        {
            public TestRig(
                GameObject root,
                RuntimeLevelData runtimeLevel,
                PuzzleGameController controller,
                PuzzleBoardView board)
            {
                Root = root;
                RuntimeLevel = runtimeLevel;
                Controller = controller;
                Board = board;
            }

            public GameObject Root { get; }
            public RuntimeLevelData RuntimeLevel { get; }
            public PuzzleGameController Controller { get; }
            public PuzzleBoardView Board { get; }

            public void Dispose()
            {
                if (Root != null) UnityEngine.Object.DestroyImmediate(Root);
                if (RuntimeLevel != null) UnityEngine.Object.DestroyImmediate(RuntimeLevel);
            }
        }
    }
}
