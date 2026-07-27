using System;
using System.Collections;
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
        [SerializeField, Range(1, 4)] private int visualGridSubdivision = 3;

        private readonly Dictionary<string, PuzzlePieceView> _pieceViews = new Dictionary<string, PuzzlePieceView>(StringComparer.Ordinal);
        private readonly List<GameObject> _hintCells = new List<GameObject>();
        private readonly Dictionary<GridCoordinate, CellGlowBinding> _cellVisuals =
            new Dictionary<GridCoordinate, CellGlowBinding>();
        private readonly Dictionary<GridCoordinate, Coroutine> _cellGlowRoutines =
            new Dictionary<GridCoordinate, Coroutine>();
        private readonly HashSet<GridCoordinate> _hoveredCells = new HashSet<GridCoordinate>();
        private readonly HashSet<GridCoordinate> _coverageBuffer = new HashSet<GridCoordinate>();
        private readonly HashSet<GridCoordinate> _sweepBuffer = new HashSet<GridCoordinate>();
        private readonly List<GridCoordinate> _releaseBuffer = new List<GridCoordinate>();
        private readonly Vector3[] _pieceWorldCorners = new Vector3[4];
        private bool _hasLastHoverCenter;
        private Vector2 _lastHoverCenter;
        private float _cellSize;
        private float _visualCellSize;
        private int _visualColumns;
        private int _visualRows;
        private Vector2 _gridSize;
        private PuzzleLevelPrefab _levelPrefab;

        public RectTransform PieceLayer => pieceLayer;
        public float CellSize => _cellSize;
        public int VisualGridSubdivision => Mathf.Clamp(visualGridSubdivision, 1, 4);
        public float VisualCellSize => _visualCellSize;
        public Vector2 GridSize => _gridSize;

        public void Build(PuzzleSession session, PuzzleGameController controller)
        {
            Build(session, controller, null);
        }

        private void OnDisable()
        {
            ClearHoverTrail();
        }

        public void Build(PuzzleSession session, PuzzleGameController controller, PuzzleLevelPrefab levelPrefab)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            ClearHoverTrail();
            StopAllCoroutines();
            EnsureLayers();
            ResetPieceLayerVisualState();
            ClearLayer(cellLayer);
            ClearLayer(pieceLayer);
            ClearLayer(referenceLayer);
            _pieceViews.Clear();
            _hintCells.Clear();
            _cellVisuals.Clear();
            _cellGlowRoutines.Clear();
            _hoveredCells.Clear();
            _coverageBuffer.Clear();
            _sweepBuffer.Clear();
            _releaseBuffer.Clear();
            _hasLastHoverCenter = false;
            _levelPrefab = levelPrefab;

            LevelDefinition level = session.Level;
            ResponsiveGameLayout responsiveLayout = GetComponentInParent<ResponsiveGameLayout>();
            if (responsiveLayout != null) responsiveLayout.SetBoardGridDimensions(level.boardWidth, level.boardHeight);
            Vector2 available = boardFrame == null ? ((RectTransform)transform).rect.size : boardFrame.rect.size;
            available -= Vector2.one * innerPadding * 2f;
            _cellSize = Mathf.Floor(Mathf.Min(available.x / level.boardWidth, available.y / level.boardHeight));
            _cellSize = Mathf.Max(24f, _cellSize);
            _gridSize = new Vector2(level.boardWidth * _cellSize, level.boardHeight * _cellSize);
            _visualCellSize = _cellSize / VisualGridSubdivision;
            _visualColumns = level.boardWidth * VisualGridSubdivision;
            _visualRows = level.boardHeight * VisualGridSubdivision;
            ConfigureGridLayer(cellLayer, _gridSize);
            ConfigureGridLayer(pieceLayer, _gridSize);
            BuildCells();

            IReadOnlyList<PieceState> pieces = session.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                PuzzlePieceArtwork artwork = levelPrefab == null ? null : levelPrefab.FindPieceArtwork(state.PieceId);
                PuzzlePieceView view = CreatePiece(pieceLayer, state.Definition, state.Pose, _cellSize, true, controller, artwork);
                view.SetPose(state.Pose);
                view.SetLocked(state.IsLocked, state.IsReferenceAnchor);
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

        public Vector2 GetCenteredAssemblyOffset(string anchorPieceId)
        {
            if (string.IsNullOrEmpty(anchorPieceId)) return Vector2.zero;
            Vector2 target = GetFreeformTarget(anchorPieceId, Vector2.zero);
            return ClampAssemblyOffset(_gridSize * 0.5f - target);
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
            view.SetLocked(state.IsLocked, state.IsReferenceAnchor);
        }

        public void ApplyAll(PuzzleSession session)
        {
            IReadOnlyList<PieceState> pieces = session.Pieces;
            for (int i = 0; i < pieces.Count; i++) ApplyState(pieces[i]);
        }

        public void UpdateHoverTrail(PuzzlePieceView view, float duration, float strength)
        {
            if (view == null ||
                view.RectTransform == null ||
                cellLayer == null ||
                _visualCellSize <= 0f ||
                _cellVisuals.Count == 0)
                return;

            Vector2 currentCenter = cellLayer.InverseTransformPoint(
                view.RectTransform.TransformPoint(view.RectTransform.rect.center));
            _coverageBuffer.Clear();
            CollectCoveredCells(view, Vector2.zero, _coverageBuffer);

            _sweepBuffer.Clear();
            if (_hasLastHoverCenter)
            {
                float distance = Vector2.Distance(_lastHoverCenter, currentCenter);
                int steps = Mathf.CeilToInt(distance / Mathf.Max(1f, _visualCellSize * 0.5f));
                for (int step = 1; step < steps; step++)
                {
                    float t = step / (float)steps;
                    Vector2 sampleCenter = Vector2.Lerp(_lastHoverCenter, currentCenter, t);
                    CollectCoveredCells(view, sampleCenter - currentCenter, _sweepBuffer);
                }
            }

            _releaseBuffer.Clear();
            foreach (GridCoordinate coordinate in _hoveredCells)
            {
                if (!_coverageBuffer.Contains(coordinate)) _releaseBuffer.Add(coordinate);
            }
            for (int i = 0; i < _releaseBuffer.Count; i++) _hoveredCells.Remove(_releaseBuffer[i]);

            foreach (GridCoordinate coordinate in _coverageBuffer)
            {
                if (_hoveredCells.Add(coordinate)) TriggerCellGlow(coordinate, duration, strength);
            }
            foreach (GridCoordinate coordinate in _sweepBuffer)
            {
                if (_coverageBuffer.Contains(coordinate) || _cellGlowRoutines.ContainsKey(coordinate)) continue;
                TriggerCellGlow(coordinate, duration, strength);
            }

            _lastHoverCenter = currentCenter;
            _hasLastHoverCenter = true;
        }

        public void ReleaseHoverTrail()
        {
            _hoveredCells.Clear();
            _coverageBuffer.Clear();
            _sweepBuffer.Clear();
            _releaseBuffer.Clear();
            _hasLastHoverCenter = false;
        }

        public void ClearHoverTrail()
        {
            foreach (KeyValuePair<GridCoordinate, Coroutine> pair in _cellGlowRoutines)
            {
                if (pair.Value != null) StopCoroutine(pair.Value);
            }
            _cellGlowRoutines.Clear();
            _hoveredCells.Clear();
            _coverageBuffer.Clear();
            _sweepBuffer.Clear();
            _releaseBuffer.Clear();
            foreach (KeyValuePair<GridCoordinate, CellGlowBinding> pair in _cellVisuals)
            {
                if (pair.Value.Image != null) pair.Value.Image.color = pair.Value.BaseColor;
            }
            _hasLastHoverCenter = false;
        }

        private void CollectCoveredCells(
            PuzzlePieceView view,
            Vector2 simulatedBoardOffset,
            HashSet<GridCoordinate> destination)
        {
            view.RectTransform.GetWorldCorners(_pieceWorldCorners);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < _pieceWorldCorners.Length; i++)
            {
                Vector2 boardCorner = (Vector2)cellLayer.InverseTransformPoint(_pieceWorldCorners[i]) +
                                      simulatedBoardOffset;
                minX = Mathf.Min(minX, boardCorner.x);
                minY = Mathf.Min(minY, boardCorner.y);
                maxX = Mathf.Max(maxX, boardCorner.x);
                maxY = Mathf.Max(maxY, boardCorner.y);
            }

            int firstX = Mathf.Clamp(Mathf.FloorToInt(minX / _visualCellSize), 0, _visualColumns - 1);
            int firstY = Mathf.Clamp(Mathf.FloorToInt(minY / _visualCellSize), 0, _visualRows - 1);
            int lastX = Mathf.Clamp(Mathf.FloorToInt(maxX / _visualCellSize), 0, _visualColumns - 1);
            int lastY = Mathf.Clamp(Mathf.FloorToInt(maxY / _visualCellSize), 0, _visualRows - 1);
            if (firstX > lastX || firstY > lastY) return;

            for (int y = firstY; y <= lastY; y++)
            {
                for (int x = firstX; x <= lastX; x++)
                {
                    if (!IsVisualCellCovered(view, x, y, simulatedBoardOffset)) continue;
                    destination.Add(new GridCoordinate(x, y));
                }
            }
        }

        private bool IsVisualCellCovered(
            PuzzlePieceView view,
            int x,
            int y,
            Vector2 simulatedBoardOffset)
        {
            Vector2 cellOrigin = new Vector2(x * _visualCellSize, y * _visualCellSize);
            if (IsPieceVisibleAtSample(view, cellOrigin, new Vector2(0.5f, 0.5f), simulatedBoardOffset)) return true;
            if (IsPieceVisibleAtSample(view, cellOrigin, new Vector2(0.25f, 0.25f), simulatedBoardOffset)) return true;
            if (IsPieceVisibleAtSample(view, cellOrigin, new Vector2(0.75f, 0.25f), simulatedBoardOffset)) return true;
            if (IsPieceVisibleAtSample(view, cellOrigin, new Vector2(0.25f, 0.75f), simulatedBoardOffset)) return true;
            return IsPieceVisibleAtSample(view, cellOrigin, new Vector2(0.75f, 0.75f), simulatedBoardOffset);
        }

        private bool IsPieceVisibleAtSample(
            PuzzlePieceView view,
            Vector2 cellOrigin,
            Vector2 normalizedSample,
            Vector2 simulatedBoardOffset)
        {
            Vector2 boardPoint = cellOrigin + normalizedSample * _visualCellSize - simulatedBoardOffset;
            return view.ContainsVisualWorldPoint(cellLayer.TransformPoint(boardPoint));
        }

        private void TriggerCellGlow(GridCoordinate coordinate, float duration, float strength)
        {
            if (!_cellVisuals.TryGetValue(coordinate, out CellGlowBinding binding) ||
                binding.Image == null)
                return;
            if (_cellGlowRoutines.TryGetValue(coordinate, out Coroutine running) && running != null)
                StopCoroutine(running);
            _cellGlowRoutines[coordinate] = StartCoroutine(
                CellGlowRoutine(coordinate, binding, Mathf.Max(0.08f, duration), Mathf.Clamp01(strength)));
        }

        private IEnumerator CellGlowRoutine(
            GridCoordinate coordinate,
            CellGlowBinding binding,
            float duration,
            float strength)
        {
            Color accent = palette == null ? new Color(0.45f, 0.9f, 1f, 1f) : palette.highlight;
            accent.a = 1f;
            Color peak = Color.Lerp(binding.BaseColor, accent, strength);
            Color start = binding.Image == null ? binding.BaseColor : binding.Image.color;
            float riseDuration = Mathf.Max(0.04f, duration * 0.35f);
            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                if (binding.Image == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / riseDuration));
                binding.Image.color = Color.Lerp(start, peak, t);
                yield return null;
            }

            while (_hoveredCells.Contains(coordinate))
            {
                if (binding.Image == null) yield break;
                binding.Image.color = peak;
                yield return null;
            }

            Color fadeStart = binding.Image == null ? peak : binding.Image.color;
            float fadeDuration = Mathf.Max(0.05f, duration * 0.65f);
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (binding.Image == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                binding.Image.color = Color.Lerp(fadeStart, binding.BaseColor, t);
                yield return null;
            }
            if (binding.Image != null) binding.Image.color = binding.BaseColor;
            _cellGlowRoutines.Remove(coordinate);
        }

        public void PlayPlacementRipple(PuzzleSession session, string originPieceId, bool completion)
        {
            if (session == null || string.IsNullOrEmpty(originPieceId)) return;
            StartCoroutine(PlacementRippleRoutine(session, originPieceId, completion));
        }

        public void PlayWholeObjectBounce()
        {
            if (pieceLayer != null) StartCoroutine(ScaleLayerRoutine(pieceLayer, 1.09f, 0.28f, false));
        }

        public void PlayObjectAction(string action)
        {
            if (pieceLayer != null) StartCoroutine(ObjectActionRoutine(pieceLayer, action));
        }

        public void PlayCompletionPop()
        {
            if (pieceLayer != null) StartCoroutine(ScaleLayerRoutine(pieceLayer, 1.14f, 0.15f, true));
        }

        private IEnumerator PlacementRippleRoutine(PuzzleSession session, string originPieceId, bool completion)
        {
            var depths = new Dictionary<string, int>(StringComparer.Ordinal) { [originPieceId] = 0 };
            var queue = new Queue<string>();
            queue.Enqueue(originPieceId);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                int nextDepth = depths[current] + 1;
                foreach (KeyValuePair<string, PuzzlePieceView> pair in _pieceViews)
                {
                    if (depths.ContainsKey(pair.Key) || !IsCorrect(session, pair.Key)) continue;
                    if (!AreArtworkNeighbors(current, pair.Key)) continue;
                    depths.Add(pair.Key, nextDepth);
                    queue.Enqueue(pair.Key);
                }
            }

            if (completion)
            {
                // Disconnected decorative islands still participate in the final celebration.
                foreach (KeyValuePair<string, PuzzlePieceView> pair in _pieceViews)
                {
                    if (depths.ContainsKey(pair.Key) || !IsCorrect(session, pair.Key)) continue;
                    depths.Add(pair.Key, 2);
                }
            }

            int maxDepth = 0;
            foreach (int depth in depths.Values) maxDepth = Mathf.Max(maxDepth, depth);
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                foreach (KeyValuePair<string, int> entry in depths)
                {
                    if (entry.Value != depth || !_pieceViews.TryGetValue(entry.Key, out PuzzlePieceView view)) continue;
                    view.FlashWhite(completion ? 0.26f : 0.20f, completion ? 1f : (depth == 0 ? 0.94f : 0.68f));
                }
                if (depth < maxDepth) yield return new WaitForSecondsRealtime(0.06f);
            }
        }

        private bool AreArtworkNeighbors(string firstId, string secondId)
        {
            PuzzlePieceArtwork first = FindArtwork(firstId);
            PuzzlePieceArtwork second = FindArtwork(secondId);
            if (first == null || second == null) return false;
            Rect a = new Rect(first.targetCenterNormalized - first.sizeNormalized * 0.5f, first.sizeNormalized);
            Rect b = new Rect(second.targetCenterNormalized - second.sizeNormalized * 0.5f, second.sizeNormalized);
            float dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
            float dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
            return dx <= 0.035f && dy <= 0.035f;
        }

        private static bool IsCorrect(PuzzleSession session, string pieceId)
        {
            return session.TryGetPiece(pieceId, out PieceState state) && state.IsCorrect;
        }

        private static IEnumerator ScaleLayerRoutine(RectTransform target, float peakScale, float duration, bool fadeOut)
        {
            Vector3 startScale = target.localScale;
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
            float startAlpha = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = fadeOut ? t : Mathf.Sin(t * Mathf.PI);
                target.localScale = startScale * Mathf.Lerp(1f, peakScale, wave);
                if (fadeOut) group.alpha = Mathf.Lerp(startAlpha, 0f, t * t);
                yield return null;
            }
            if (target != null && !fadeOut) target.localScale = startScale;
        }

        private static IEnumerator ObjectActionRoutine(RectTransform target, string action)
        {
            string normalized = (action ?? string.Empty).ToLowerInvariant();
            Vector2 startPosition = target.anchoredPosition;
            Quaternion startRotation = target.localRotation;
            float elapsed = 0f;
            const float duration = 0.55f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                if (normalized.Contains("lift") || normalized.Contains("rocket") || normalized.Contains("balloon"))
                    target.anchoredPosition = startPosition + Vector2.up * (26f * wave);
                else
                    target.localRotation = startRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 4.5f * wave);
                yield return null;
            }
            if (target != null)
            {
                target.anchoredPosition = startPosition;
                target.localRotation = startRotation;
            }
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

        private void BuildCells()
        {
            Color first = palette == null ? new Color32(32, 36, 31, 255) : palette.boardCell;
            Color second = palette == null ? new Color32(37, 41, 36, 255) : palette.boardCellAlternate;
            float gap = Mathf.Max(1f, _visualCellSize * 0.035f);
            for (int y = 0; y < _visualRows; y++)
            {
                for (int x = 0; x < _visualColumns; x++)
                {
                    GameObject cell = CreateImageObject("Cell_" + x + "_" + y, cellLayer, roundedSprite, ((x + y) & 1) == 0 ? first : second);
                    RectTransform rect = cell.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                    rect.pivot = Vector2.zero;
                    rect.anchoredPosition = new Vector2(
                        x * _visualCellSize + gap * 0.5f,
                        y * _visualCellSize + gap * 0.5f);
                    rect.sizeDelta = Vector2.one * (_visualCellSize - gap);
                    Image cellImage = cell.GetComponent<Image>();
                    cellImage.raycastTarget = false;
                    _cellVisuals[new GridCoordinate(x, y)] =
                        new CellGlowBinding(cellImage, cellImage.color);
                    Shadow shadow = cell.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.36f);
                    shadow.effectDistance = new Vector2(0f, -Mathf.Max(1f, _visualCellSize * 0.025f));
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

        private void ResetPieceLayerVisualState()
        {
            if (pieceLayer == null) return;
            pieceLayer.localScale = Vector3.one;
            pieceLayer.localRotation = Quaternion.identity;
            CanvasGroup group = pieceLayer.GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
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

        private readonly struct CellGlowBinding
        {
            public CellGlowBinding(Image image, Color baseColor)
            {
                Image = image;
                BaseColor = baseColor;
            }

            public Image Image { get; }
            public Color BaseColor { get; }
        }
    }
}
