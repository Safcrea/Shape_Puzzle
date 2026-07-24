using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

        [UnityTest]
        public IEnumerator LoadLevel_AfterCompletionPop_RestoresVisibleInteractivePieceLayer()
        {
            TestRig rig = CreateRig(CreateTwoPieceLevel());
            try
            {
                rig.Controller.LoadLevel(rig.RuntimeLevel);
                rig.Board.PlayCompletionPop();
                yield return new WaitForSecondsRealtime(0.20f);

                CanvasGroup fadedGroup = rig.Board.PieceLayer.GetComponent<CanvasGroup>();
                Assert.That(fadedGroup, Is.Not.Null);
                Assert.That(fadedGroup.alpha, Is.LessThan(0.05f));

                rig.Controller.LoadLevel(rig.RuntimeLevel);

                CanvasGroup restoredGroup = rig.Board.PieceLayer.GetComponent<CanvasGroup>();
                Assert.That(restoredGroup.alpha, Is.EqualTo(1f).Within(0.001f));
                Assert.That(restoredGroup.blocksRaycasts, Is.True);
                Assert.That(rig.Board.PieceLayer.localScale, Is.EqualTo(Vector3.one));
                Assert.That(rig.Board.FindPiece("a"), Is.Not.Null);
                Assert.That(rig.Board.FindPiece("a").gameObject.activeInHierarchy, Is.True);
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
