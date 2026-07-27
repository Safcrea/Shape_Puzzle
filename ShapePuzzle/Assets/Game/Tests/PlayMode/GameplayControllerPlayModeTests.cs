using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
                Assert.That(rig.Board.VisualGridSubdivision, Is.EqualTo(3));
                Assert.That(rig.Board.VisualCellSize, Is.EqualTo(rig.Board.CellSize / 3f).Within(0.001f));
                Transform visualCells = rig.Board.transform.Find("Cells");
                Assert.That(visualCells, Is.Not.Null);
                Assert.That(visualCells.childCount, Is.EqualTo(5 * 5 * 9));
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

        [Test]
        public void LoadFreeformLevel_CentersAndLocksGeneratedReferenceAnchor()
        {
            TestRig rig = CreateRig(CreateTwoPieceLevel());
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Sprite sprite = null;
            var prefabObject = new GameObject("FreeformLevel");
            try
            {
                Color[] pixels = new Color[64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);

                PuzzleLevelPrefab prefab = prefabObject.AddComponent<PuzzleLevelPrefab>();
                prefab.SetLevel(rig.RuntimeLevel.Level);
                prefab.SetPieceArtwork(new[]
                {
                    new PuzzlePieceArtwork
                    {
                        pieceId = "a",
                        sprite = sprite,
                        freeformColorBlock = true,
                        targetCenterNormalized = new Vector2(0.42f, 0.48f),
                        startingCenterNormalized = new Vector2(0.1f, 0.1f),
                        sizeNormalized = new Vector2(0.12f, 0.12f)
                    },
                    new PuzzlePieceArtwork
                    {
                        pieceId = "b",
                        sprite = sprite,
                        freeformColorBlock = true,
                        targetCenterNormalized = new Vector2(0.72f, 0.55f),
                        startingCenterNormalized = new Vector2(0.9f, 0.1f),
                        sizeNormalized = new Vector2(0.12f, 0.12f)
                    }
                });
                prefab.SetReferenceAnchorPieceId("a");

                rig.Controller.LoadLevel(prefab);

                PuzzlePieceView anchor = rig.Board.FindPiece("a");
                Assert.That(anchor, Is.Not.Null);
                Assert.That(anchor.IsReferenceAnchor, Is.True);
                Assert.That(anchor.IsLocked, Is.True);
                Assert.That(
                    Vector2.Distance(anchor.RectTransform.anchoredPosition, rig.Board.GridSize * 0.5f),
                    Is.LessThan(0.01f));
                Assert.That(rig.Controller.AssemblyOffset, Is.Not.EqualTo(Vector2.zero));
                Assert.That(rig.Controller.Session.MoveCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabObject);
                if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                rig.Dispose();
            }
        }

        [Test]
        public void TryPlaceFreeformPiece_RejectsAnotherSameColorPiecesTarget()
        {
            TestRig rig = CreateRig(CreateThreePieceLevel());
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Sprite sprite = null;
            var prefabObject = new GameObject("SameColorFreeformLevel");
            try
            {
                Color[] pixels = new Color[64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.cyan;
                texture.SetPixels(pixels);
                texture.Apply();
                sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);

                PuzzleLevelPrefab prefab = prefabObject.AddComponent<PuzzleLevelPrefab>();
                prefab.SetLevel(rig.RuntimeLevel.Level);
                prefab.SetPieceArtwork(new[]
                {
                    CreateFreeformArtwork("a", sprite, new Vector2(0.50f, 0.50f), new Vector2(0.10f, 0.10f)),
                    CreateFreeformArtwork("b", sprite, new Vector2(0.30f, 0.70f), new Vector2(0.10f, 0.90f)),
                    CreateFreeformArtwork("c", sprite, new Vector2(0.70f, 0.70f), new Vector2(0.90f, 0.90f))
                });
                prefab.SetReferenceAnchorPieceId("a");
                rig.Controller.LoadLevel(prefab);

                Vector2 wrongTarget = rig.Board.GetFreeformTarget("c", rig.Controller.AssemblyOffset);
                bool wrongPlacement = rig.Controller.TryPlaceFreeformPiece("b", wrongTarget);

                Assert.That(wrongPlacement, Is.False,
                    "A same-color piece must not be accepted by another piece's target.");
                Assert.That(rig.Controller.Session.TryGetPiece("b", out PieceState rejectedPiece), Is.True);
                Assert.That(rejectedPiece.IsCorrect, Is.False);
                Assert.That(rejectedPiece.IsLocked, Is.False);

                Vector2 exactTarget = rig.Board.GetFreeformTarget("b", rig.Controller.AssemblyOffset);
                bool exactPlacement = rig.Controller.TryPlaceFreeformPiece("b", exactTarget);

                Assert.That(exactPlacement, Is.True);
                Assert.That(rig.Controller.Session.TryGetPiece("b", out PieceState placedPiece), Is.True);
                Assert.That(placedPiece.IsCorrect, Is.True);
                Assert.That(placedPiece.IsLocked, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabObject);
                if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DenseGridGlow_UsesSpriteSilhouette_HoldsAndSweepsWithoutGaps()
        {
            TestRig rig = CreateRig(CreateThreePieceLevel());
            var silhouetteTexture = new Texture2D(12, 12, TextureFormat.RGBA32, false);
            var solidTexture = new Texture2D(12, 12, TextureFormat.RGBA32, false);
            Sprite silhouetteSprite = null;
            Sprite solidSprite = null;
            var prefabObject = new GameObject("DenseGridGlowLevel");
            try
            {
                Color[] silhouettePixels = new Color[144];
                Color[] solidPixels = new Color[144];
                for (int y = 0; y < 12; y++)
                {
                    for (int x = 0; x < 12; x++)
                    {
                        silhouettePixels[y * 12 + x] = x < 6 ? Color.cyan : Color.clear;
                        solidPixels[y * 12 + x] = Color.yellow;
                    }
                }
                silhouetteTexture.SetPixels(silhouettePixels);
                silhouetteTexture.Apply();
                solidTexture.SetPixels(solidPixels);
                solidTexture.Apply();
                silhouetteSprite = Sprite.Create(
                    silhouetteTexture,
                    new Rect(0f, 0f, 12f, 12f),
                    new Vector2(0.5f, 0.5f),
                    12f);
                solidSprite = Sprite.Create(
                    solidTexture,
                    new Rect(0f, 0f, 12f, 12f),
                    new Vector2(0.5f, 0.5f),
                    12f);

                PuzzleLevelPrefab prefab = prefabObject.AddComponent<PuzzleLevelPrefab>();
                prefab.SetLevel(rig.RuntimeLevel.Level);
                prefab.SetPieceArtwork(new[]
                {
                    CreateFreeformArtwork(
                        "a",
                        solidSprite,
                        new Vector2(0.50f, 0.50f),
                        new Vector2(0.50f, 0.50f)),
                    CreateFreeformArtwork(
                        "b",
                        silhouetteSprite,
                        new Vector2(0.30f, 0.70f),
                        new Vector2(0.30f, 0.28f),
                        new Vector2(0.30f, 0.22f)),
                    CreateFreeformArtwork(
                        "c",
                        solidSprite,
                        new Vector2(0.70f, 0.70f),
                        new Vector2(0.18f, 0.74f),
                        new Vector2(0.18f, 0.18f))
                });
                prefab.SetReferenceAnchorPieceId("a");
                rig.Controller.LoadLevel(prefab);

                Transform cellLayer = rig.Board.transform.Find("Cells");
                Assert.That(cellLayer, Is.Not.Null);
                var baseColors = CaptureCellColors(cellLayer);
                PuzzlePieceView silhouettePiece = rig.Board.FindPiece("b");
                Assert.That(silhouettePiece, Is.Not.Null);

                rig.Board.UpdateHoverTrail(silhouettePiece, 0.24f, 1f);
                yield return new WaitForSecondsRealtime(0.12f);

                int opaqueChanged = 0;
                int transparentChanged = 0;
                Rect silhouetteBounds = silhouettePiece.RectTransform.rect;
                for (int i = 0; i < cellLayer.childCount; i++)
                {
                    Transform cell = cellLayer.GetChild(i);
                    Image image = cell.GetComponent<Image>();
                    RectTransform rect = (RectTransform)cell;
                    Vector2 pieceLocal = silhouettePiece.RectTransform.InverseTransformPoint(
                        rect.TransformPoint(rect.rect.center));
                    if (Mathf.Abs(pieceLocal.y) > silhouetteBounds.height * 0.25f) continue;
                    bool changed = ColorDistance(image.color, baseColors[cell.name]) > 0.01f;
                    if (pieceLocal.x < -silhouetteBounds.width * 0.18f &&
                        pieceLocal.x > silhouetteBounds.xMin)
                    {
                        if (changed) opaqueChanged++;
                    }
                    else if (pieceLocal.x > silhouetteBounds.width * 0.18f &&
                             pieceLocal.x < silhouetteBounds.xMax)
                    {
                        if (changed) transparentChanged++;
                    }
                }
                Assert.That(opaqueChanged, Is.GreaterThan(0),
                    "Opaque sprite pixels should illuminate the mini-grid.");
                Assert.That(transparentChanged, Is.Zero,
                    "Transparent sprite pixels must not illuminate mini-grid tiles.");

                yield return new WaitForSecondsRealtime(0.24f);
                Assert.That(CountChangedCells(cellLayer, baseColors), Is.GreaterThan(0),
                    "Covered cells should remain illuminated until the piece leaves.");

                rig.Board.ReleaseHoverTrail();
                yield return new WaitForSecondsRealtime(0.24f);
                Assert.That(CountChangedCells(cellLayer, baseColors), Is.Zero,
                    "Released cells should fade completely back to their palette colors.");

                rig.Board.ClearHoverTrail();
                PuzzlePieceView sweepPiece = rig.Board.FindPiece("c");
                float yPosition = rig.Board.GridSize.y * 0.55f;
                Vector2 sweepStart = new Vector2(rig.Board.GridSize.x * 0.16f, yPosition);
                Vector2 sweepEnd = new Vector2(rig.Board.GridSize.x * 0.84f, yPosition);
                sweepPiece.SetFreeformPosition(sweepStart);
                rig.Board.UpdateHoverTrail(sweepPiece, 0.60f, 1f);
                yield return new WaitForSecondsRealtime(0.08f);
                sweepPiece.SetFreeformPosition(sweepEnd);
                rig.Board.UpdateHoverTrail(sweepPiece, 0.60f, 1f);
                yield return new WaitForSecondsRealtime(0.08f);

                int row = Mathf.Clamp(
                    Mathf.FloorToInt(yPosition / rig.Board.VisualCellSize),
                    0,
                    Mathf.RoundToInt(rig.Board.GridSize.y / rig.Board.VisualCellSize) - 1);
                int firstColumn = Mathf.FloorToInt(sweepStart.x / rig.Board.VisualCellSize);
                int lastColumn = Mathf.FloorToInt(sweepEnd.x / rig.Board.VisualCellSize);
                for (int x = firstColumn; x <= lastColumn; x++)
                {
                    Transform cell = cellLayer.Find("Cell_" + x + "_" + row);
                    Assert.That(cell, Is.Not.Null);
                    Image image = cell.GetComponent<Image>();
                    Assert.That(
                        ColorDistance(image.color, baseColors[cell.name]),
                        Is.GreaterThan(0.01f),
                        "Fast sweep left a gap at visual cell " + x + "," + row + ".");
                }

                rig.Board.ReleaseHoverTrail();
                yield return new WaitForSecondsRealtime(0.50f);
                Assert.That(CountChangedCells(cellLayer, baseColors), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabObject);
                if (silhouetteSprite != null) UnityEngine.Object.DestroyImmediate(silhouetteSprite);
                if (solidSprite != null) UnityEngine.Object.DestroyImmediate(solidSprite);
                UnityEngine.Object.DestroyImmediate(silhouetteTexture);
                UnityEngine.Object.DestroyImmediate(solidTexture);
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

        private static LevelDefinition CreateThreePieceLevel()
        {
            return new LevelDefinition
            {
                levelId = "playmode_same_color_identity",
                levelNumber = 1,
                boardWidth = 5,
                boardHeight = 5,
                scrambleSeed = 1205,
                pieces = new[]
                {
                    CreateSingleCellPiece("a", new GridCoordinate(0, 0), new GridCoordinate(1, 1), true),
                    CreateSingleCellPiece("b", new GridCoordinate(2, 0), new GridCoordinate(2, 2), true),
                    CreateSingleCellPiece("c", new GridCoordinate(4, 0), new GridCoordinate(3, 3), true)
                }
            };
        }

        private static PuzzlePieceArtwork CreateFreeformArtwork(
            string pieceId,
            Sprite sprite,
            Vector2 target,
            Vector2 start,
            Vector2? size = null)
        {
            return new PuzzlePieceArtwork
            {
                pieceId = pieceId,
                sprite = sprite,
                freeformColorBlock = true,
                targetCenterNormalized = target,
                startingCenterNormalized = start,
                sizeNormalized = size ?? new Vector2(0.12f, 0.12f)
            };
        }

        private static System.Collections.Generic.Dictionary<string, Color> CaptureCellColors(Transform cellLayer)
        {
            var colors = new System.Collections.Generic.Dictionary<string, Color>(StringComparer.Ordinal);
            for (int i = 0; i < cellLayer.childCount; i++)
            {
                Transform cell = cellLayer.GetChild(i);
                colors[cell.name] = cell.GetComponent<Image>().color;
            }
            return colors;
        }

        private static int CountChangedCells(
            Transform cellLayer,
            System.Collections.Generic.IReadOnlyDictionary<string, Color> baseColors)
        {
            int changed = 0;
            for (int i = 0; i < cellLayer.childCount; i++)
            {
                Transform cell = cellLayer.GetChild(i);
                if (ColorDistance(cell.GetComponent<Image>().color, baseColors[cell.name]) > 0.01f) changed++;
            }
            return changed;
        }

        private static float ColorDistance(Color first, Color second)
        {
            return Mathf.Max(
                Mathf.Abs(first.r - second.r),
                Mathf.Abs(first.g - second.g),
                Mathf.Abs(first.b - second.b));
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
