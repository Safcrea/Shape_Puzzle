using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    public sealed class PuzzleBoardView : MonoBehaviour
    {
        [SerializeField] private RectTransform boardFrame;
        [SerializeField] private RectTransform cellLayer;
        [SerializeField] private RectTransform pieceLayer;
        [SerializeField] private RectTransform referenceLayer;
        [SerializeField] private ToyPalette palette;
        [SerializeField] private Sprite roundedSprite;
        [SerializeField] private Sprite capsuleSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite ringSprite;
        [SerializeField] private Sprite triangleSprite;
        [SerializeField] private Sprite trapezoidSprite;
        [SerializeField] private Sprite wedgeSprite;
        [SerializeField] private Sprite semicircleSprite;
        [SerializeField] private Sprite quarterCircleSprite;
        [SerializeField] private Sprite studSprite;
        [SerializeField] private Sprite recessedHoleSprite;
        [SerializeField] private Sprite insetPanelSprite;
        [SerializeField] private float innerPadding = PuzzleLayoutConstants.ReferenceFrameThickness;

        private readonly Dictionary<string, PuzzlePieceView> _pieceViews = new Dictionary<string, PuzzlePieceView>(StringComparer.Ordinal);
        private readonly List<GameObject> _hintCells = new List<GameObject>();
        private float _cellSize;
        private Vector2 _gridSize;
        private PuzzleLevelPrefab _levelPrefab;

        public RectTransform PieceLayer => pieceLayer;
        public float CellSize => _cellSize;
        public Vector2 GridSize => _gridSize;

        public void Build(PuzzleSession session, PuzzleGameController controller)
        {
            Build(session, controller, null);
        }

        public void Build(PuzzleSession session, PuzzleGameController controller, PuzzleLevelPrefab levelPrefab)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            EnsureLayers();
            ClearLayer(cellLayer);
            ClearLayer(pieceLayer);
            ClearLayer(referenceLayer);
            _pieceViews.Clear();
            _hintCells.Clear();
            _levelPrefab = levelPrefab;

            LevelDefinition level = session.Level;
            ResponsiveGameLayout responsiveLayout = GetComponentInParent<ResponsiveGameLayout>();
            if (responsiveLayout != null) responsiveLayout.SetBoardGridDimensions(level.boardWidth, level.boardHeight);
            Vector2 available = boardFrame == null ? ((RectTransform)transform).rect.size : boardFrame.rect.size;
            available -= Vector2.one * innerPadding * 2f;
            _cellSize = Mathf.Floor(Mathf.Min(available.x / level.boardWidth, available.y / level.boardHeight));
            _cellSize = Mathf.Max(24f, _cellSize);
            _gridSize = new Vector2(level.boardWidth * _cellSize, level.boardHeight * _cellSize);
            ConfigureGridLayer(cellLayer, _gridSize);
            ConfigureGridLayer(pieceLayer, _gridSize);
            BuildCells(level);

            IReadOnlyList<PieceState> pieces = session.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                PuzzlePieceArtwork artwork = levelPrefab == null ? null : levelPrefab.FindPieceArtwork(state.PieceId);
                PuzzlePieceView view = CreatePiece(pieceLayer, state.Definition, state.Pose, _cellSize, true, controller, artwork);
                view.SetPose(state.Pose);
                view.SetLocked(state.IsLocked);
                _pieceViews.Add(state.PieceId, view);
            }

            BuildReference(level, levelPrefab);
        }

        public PuzzlePieceView FindPiece(string pieceId)
        {
            _pieceViews.TryGetValue(pieceId, out PuzzlePieceView view);
            return view;
        }

        public PuzzlePieceArtwork FindArtwork(string pieceId)
        {
            return _levelPrefab == null ? null : _levelPrefab.FindPieceArtwork(pieceId);
        }

        public Vector2 GetFreeformTarget(string pieceId, Vector2 assemblyOffset)
        {
            PuzzlePieceArtwork artwork = FindArtwork(pieceId);
            if (artwork == null || !artwork.IsValid) return assemblyOffset;
            return new Vector2(
                artwork.targetCenterNormalized.x * _gridSize.x,
                artwork.targetCenterNormalized.y * _gridSize.y) + assemblyOffset;
        }

        public Vector2 ClampAssemblyOffset(Vector2 requestedOffset)
        {
            if (_levelPrefab == null || _levelPrefab.PieceArtwork == null) return requestedOffset;
            float minimumX = float.NegativeInfinity;
            float minimumY = float.NegativeInfinity;
            float maximumX = float.PositiveInfinity;
            float maximumY = float.PositiveInfinity;
            PuzzlePieceArtwork[] artwork = _levelPrefab.PieceArtwork;
            for (int i = 0; i < artwork.Length; i++)
            {
                PuzzlePieceArtwork part = artwork[i];
                if (part == null || !part.IsValid || !part.freeformColorBlock) continue;
                Vector2 center = new Vector2(
                    part.targetCenterNormalized.x * _gridSize.x,
                    part.targetCenterNormalized.y * _gridSize.y);
                Vector2 half = new Vector2(
                    part.sizeNormalized.x * _gridSize.x,
                    part.sizeNormalized.y * _gridSize.y) * 0.5f;
                minimumX = Mathf.Max(minimumX, half.x - center.x);
                minimumY = Mathf.Max(minimumY, half.y - center.y);
                maximumX = Mathf.Min(maximumX, _gridSize.x - half.x - center.x);
                maximumY = Mathf.Min(maximumY, _gridSize.y - half.y - center.y);
            }

            if (float.IsInfinity(minimumX) || minimumX > maximumX || minimumY > maximumY) return requestedOffset;
            return new Vector2(
                Mathf.Clamp(requestedOffset.x, minimumX, maximumX),
                Mathf.Clamp(requestedOffset.y, minimumY, maximumY));
        }

        public void ApplyState(PieceState state)
        {
            if (state == null || !_pieceViews.TryGetValue(state.PieceId, out PuzzlePieceView view)) return;
            view.SetPose(state.Pose);
            view.SetLocked(state.IsLocked);
        }

        public void ApplyAll(PuzzleSession session)
        {
            IReadOnlyList<PieceState> pieces = session.Pieces;
            for (int i = 0; i < pieces.Count; i++) ApplyState(pieces[i]);
        }

        public GridCoordinate GetCandidatePosition(PuzzlePieceView view)
        {
            Vector2 position = view.RectTransform.anchoredPosition;
            return new GridCoordinate(Mathf.RoundToInt(position.x / _cellSize), Mathf.RoundToInt(position.y / _cellSize));
        }

        public bool IsWithinSnapThreshold(PuzzlePieceView view, GridCoordinate candidate, float thresholdInCells)
        {
            Vector2 snapped = new Vector2(candidate.x * _cellSize, candidate.y * _cellSize);
            return Vector2.Distance(view.RectTransform.anchoredPosition, snapped) <= Mathf.Max(0f, thresholdInCells) * _cellSize;
        }

        public void ShowHint(PieceDefinition piece)
        {
            ShowHint(piece, Vector2.zero);
        }

        public void ShowHint(PieceDefinition piece, Vector2 assemblyOffset)
        {
            ClearHint();
            if (piece == null) return;
            if (_pieceViews.TryGetValue(piece.pieceId, out PuzzlePieceView freeformView) && freeformView.UsesFreeformArtwork)
            {
                GameObject hintObject = freeformView.CreateFreeformHint(
                    pieceLayer,
                    GetFreeformTarget(piece.pieceId, assemblyOffset));
                if (hintObject != null) _hintCells.Add(hintObject);
                return;
            }
            GridCoordinate[] cells = GridMath.GetOccupiedCells(piece, piece.TargetPose);
            for (int i = 0; i < cells.Length; i++)
            {
                GameObject hint = CreateImageObject("Hint", pieceLayer, roundedSprite, palette == null ? new Color(1f, 0.92f, 0.35f, 0.45f) : palette.highlight);
                RectTransform rect = hint.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(cells[i].x * _cellSize + 3f, cells[i].y * _cellSize + 3f);
                rect.sizeDelta = Vector2.one * (_cellSize - 6f);
                hint.transform.SetAsFirstSibling();
                hint.GetComponent<Image>().raycastTarget = false;
                _hintCells.Add(hint);
            }
        }

        public void ClearHint()
        {
            for (int i = 0; i < _hintCells.Count; i++)
            {
                if (_hintCells[i] != null) Destroy(_hintCells[i]);
            }
            _hintCells.Clear();
        }

        private void BuildCells(LevelDefinition level)
        {
            Color first = palette == null ? new Color32(41, 46, 38, 255) : palette.boardCell;
            Color second = palette == null ? new Color32(48, 53, 45, 255) : palette.boardCellAlternate;
            float gap = Mathf.Max(2f, _cellSize * 0.035f);
            for (int y = 0; y < level.boardHeight; y++)
            {
                for (int x = 0; x < level.boardWidth; x++)
                {
                    GameObject cell = CreateImageObject("Cell_" + x + "_" + y, cellLayer, roundedSprite, ((x + y) & 1) == 0 ? first : second);
                    RectTransform rect = cell.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = Vector2.zero;
                    rect.anchoredPosition = new Vector2(x * _cellSize + gap * 0.5f, y * _cellSize + gap * 0.5f);
                    rect.sizeDelta = Vector2.one * (_cellSize - gap);
                    Image cellImage = cell.GetComponent<Image>();
                    cellImage.raycastTarget = false;
                    Shadow shadow = cell.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.36f);
                    shadow.effectDistance = new Vector2(0f, -Mathf.Max(2f, _cellSize * 0.025f));
                    shadow.useGraphicAlpha = true;
                }
            }
        }

        private void BuildReference(LevelDefinition level, PuzzleLevelPrefab levelPrefab)
        {
            if (referenceLayer == null || level.pieces == null || level.pieces.Length == 0) return;
            if (levelPrefab != null && levelPrefab.Thumbnail != null)
            {
                GameObject reference = CreateImageObject("ReferenceImage", referenceLayer, levelPrefab.Thumbnail, Color.white);
                RectTransform rect = reference.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(6f, 6f);
                rect.offsetMax = new Vector2(-6f, -6f);
                Image image = reference.GetComponent<Image>();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return;
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            for (int i = 0; i < level.pieces.Length; i++)
            {
                GridCoordinate[] cells = GridMath.GetOccupiedCells(level.pieces[i], level.pieces[i].TargetPose);
                for (int j = 0; j < cells.Length; j++)
                {
                    minX = Mathf.Min(minX, cells[j].x);
                    minY = Mathf.Min(minY, cells[j].y);
                    maxX = Mathf.Max(maxX, cells[j].x);
                    maxY = Mathf.Max(maxY, cells[j].y);
                }
            }

            float width = Mathf.Max(1, maxX - minX + 1);
            float height = Mathf.Max(1, maxY - minY + 1);
            Vector2 size = referenceLayer.rect.size;
            float previewCell = Mathf.Max(8f, Mathf.Min(size.x / width, size.y / height) * 0.96f);
            Vector2 contentSize = new Vector2(width * previewCell, height * previewCell);
            Vector2 offset = (size - contentSize) * 0.5f;
            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceDefinition definition = level.pieces[i];
                PiecePose target = definition.TargetPose;
                PiecePose localPose = new PiecePose(new GridCoordinate(target.position.x - minX, target.position.y - minY), target.rotation);
                PuzzlePieceView view = CreatePiece(referenceLayer, definition, localPose, previewCell, false, null, null);
                view.SetPose(localPose);
                view.RectTransform.anchoredPosition += offset;
            }
        }

        private PuzzlePieceView CreatePiece(RectTransform parent, PieceDefinition definition, PiecePose pose, float cellSize, bool interactive, PuzzleGameController controller, PuzzlePieceArtwork artwork)
        {
            var go = new GameObject("Piece_" + definition.pieceId, typeof(RectTransform), typeof(CanvasGroup), typeof(PuzzlePieceView));
            go.transform.SetParent(parent, false);
            PuzzlePieceView view = go.GetComponent<PuzzlePieceView>();
            Color color = palette == null ? Color.cyan : palette.ResolvePieceColor(definition.colorId);
            view.Initialize(definition, pose, cellSize, CreateSpriteSet(), color, interactive, controller, artwork, _gridSize);
            return view;
        }

        private PuzzlePieceSpriteSet CreateSpriteSet()
        {
            return new PuzzlePieceSpriteSet
            {
                rounded = roundedSprite,
                capsule = capsuleSprite,
                circle = circleSprite,
                ring = ringSprite,
                triangle = triangleSprite,
                trapezoid = trapezoidSprite,
                wedge = wedgeSprite,
                semicircle = semicircleSprite,
                quarterCircle = quarterCircleSprite,
                stud = studSprite,
                recessedHole = recessedHoleSprite,
                insetPanel = insetPanelSprite
            };
        }

        private void EnsureLayers()
        {
            if (boardFrame == null) boardFrame = transform as RectTransform;
            if (cellLayer == null) cellLayer = CreateLayer("Cells", boardFrame);
            if (pieceLayer == null) pieceLayer = CreateLayer("Pieces", boardFrame);
            if (referenceLayer == null)
            {
                Transform found = transform.root.Find("SafeArea/Gameplay/TopBar/ReferenceCard/ReferenceContent");
                if (found != null) referenceLayer = found as RectTransform;
            }
        }

        private static RectTransform CreateLayer(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void ConfigureGridLayer(RectTransform layer, Vector2 size)
        {
            layer.anchorMin = new Vector2(0.5f, 0.5f);
            layer.anchorMax = new Vector2(0.5f, 0.5f);
            layer.pivot = Vector2.zero;
            layer.sizeDelta = size;
            layer.anchoredPosition = size * -0.5f;
        }

        private static GameObject CreateImageObject(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return go;
        }

        private static void ClearLayer(RectTransform layer)
        {
            if (layer == null) return;
            for (int i = layer.childCount - 1; i >= 0; i--)
            {
                GameObject oldChild = layer.GetChild(i).gameObject;
                oldChild.SetActive(false);
                Destroy(oldChild);
            }
        }
    }
}
