using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToyPuzzle
{
    [Serializable]
    public sealed class PuzzlePieceSpriteSet
    {
        public Sprite rounded;
        public Sprite capsule;
        public Sprite circle;
        public Sprite ring;
        public Sprite triangle;
        public Sprite trapezoid;
        public Sprite wedge;
        public Sprite semicircle;
        public Sprite quarterCircle;
        public Sprite stud;
        public Sprite recessedHole;
        public Sprite insetPanel;

        public Sprite Resolve(PieceShapeType shapeType)
        {
            switch (shapeType)
            {
                case PieceShapeType.Capsule:
                    return capsule != null ? capsule : rounded;
                case PieceShapeType.Circle:
                    return circle != null ? circle : rounded;
                case PieceShapeType.Ring:
                    return ring != null ? ring : (circle != null ? circle : rounded);
                case PieceShapeType.Triangle:
                    return triangle != null ? triangle : rounded;
                case PieceShapeType.Trapezoid:
                    return trapezoid != null ? trapezoid : rounded;
                case PieceShapeType.Wedge:
                    return wedge != null ? wedge : (triangle != null ? triangle : rounded);
                case PieceShapeType.Semicircle:
                    return semicircle != null ? semicircle : (circle != null ? circle : rounded);
                case PieceShapeType.QuarterCircle:
                    return quarterCircle != null ? quarterCircle : (circle != null ? circle : rounded);
                default:
                    return rounded;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class PuzzlePieceView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private readonly List<TintBinding> _tintBindings = new List<TintBinding>();
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private PuzzleGameController _controller;
        private PieceDefinition _definition;
        private PuzzlePieceSpriteSet _sprites;
        private Color _baseColor;
        private PuzzlePieceArtwork _artwork;
        private float _cellSize;
        private Vector2 _boardSize;
        private bool _interactive;
        private bool _locked;
        private bool _referenceAnchor;
        private bool _dragPresentationActive;
        private Coroutine _presentationRoutine;
        private Quaternion _restRotation = Quaternion.identity;
        private int _visualRotation = int.MinValue;
        private GridCoordinate[] _visualCoverageCells = Array.Empty<GridCoordinate>();
        private Color32[] _artworkPixels = Array.Empty<Color32>();
        private Rect _artworkTextureRect;
        private int _artworkTextureWidth;
        private int _artworkTextureHeight;

        public string PieceId => _definition == null ? string.Empty : _definition.pieceId;
        public RectTransform RectTransform => _rectTransform;
        public PieceDefinition Definition => _definition;
        public bool IsLocked => _locked;
        public bool IsReferenceAnchor => _referenceAnchor;
        public bool UsesFreeformArtwork => _artwork != null && _artwork.freeformColorBlock;
        public PuzzlePieceArtwork Artwork => _artwork;
        public float LargestVisualDimension => _rectTransform == null
            ? 0f
            : Mathf.Max(_rectTransform.sizeDelta.x, _rectTransform.sizeDelta.y);
        public Vector2 VisualCenterPosition => _rectTransform == null
            ? Vector2.zero
            : (UsesFreeformArtwork
                ? _rectTransform.anchoredPosition
                : _rectTransform.anchoredPosition + _rectTransform.sizeDelta * 0.5f);

        public void Initialize(
            PieceDefinition definition,
            PiecePose pose,
            float cellSize,
            PuzzlePieceSpriteSet sprites,
            Color color,
            bool interactive,
            PuzzleGameController controller,
            PuzzlePieceArtwork artwork = null,
            Vector2 boardSize = default)
        {
            _definition = definition;
            _cellSize = cellSize;
            _boardSize = boardSize;
            _sprites = sprites ?? new PuzzlePieceSpriteSet();
            _baseColor = color;
            _artwork = artwork != null && artwork.IsValid ? artwork : null;
            CacheArtworkAlpha();
            _interactive = interactive;
            _controller = controller;
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) _rectTransform = gameObject.AddComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.zero;
            _rectTransform.pivot = Vector2.zero;
            SetPose(pose);
            _restRotation = _rectTransform.localRotation;
        }

        public void Rebuild(PiecePose pose)
        {
            if (_definition == null) return;
            _tintBindings.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject oldCell = transform.GetChild(i).gameObject;
                oldCell.SetActive(false);
                Destroy(oldCell);
            }

            RotatedFootprint rotated = GridMath.GetRotatedFootprint(_definition, pose.rotation);
            _visualCoverageCells = rotated.Cells ?? Array.Empty<GridCoordinate>();
            _visualRotation = GridMath.NormalizeRotation(pose.rotation);
            _rectTransform.sizeDelta = new Vector2(rotated.Width * _cellSize, rotated.Height * _cellSize);
            float gap = Mathf.Max(1f, _cellSize * 0.035f);

            if (_artwork != null)
            {
                if (_artwork.freeformColorBlock)
                {
                    BuildFreeformArtwork();
                    return;
                }
                BuildThumbnailArtwork(pose, rotated);
                BuildHitAreas(rotated);
                return;
            }

            if (UsesCellComposition(_definition.shapeType))
            {
                BuildCellComposition(rotated, gap);
                BuildDecorationRoot(pose, rotated, false, gap);
                return;
            }

            BuildDecorationRoot(pose, rotated, true, gap);
        }

        private void BuildFreeformArtwork()
        {
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = new Vector2(
                _artwork.sizeNormalized.x * _boardSize.x,
                _artwork.sizeNormalized.y * _boardSize.y);
            Image art = CreateImage("ColorBlock", _rectTransform, _artwork.sprite, Color.white, _interactive);
            Stretch(art.rectTransform);
            art.type = Image.Type.Simple;
            art.preserveAspect = false;
            if (_interactive && art.sprite != null && art.sprite.texture != null && art.sprite.texture.isReadable)
            {
                art.alphaHitTestMinimumThreshold = 0.08f;
            }
        }

        private void BuildThumbnailArtwork(PiecePose pose, RotatedFootprint rotated)
        {
            var pivotObject = new GameObject("ArtworkPivot", typeof(RectTransform));
            pivotObject.transform.SetParent(transform, false);
            RectTransform pivotRoot = pivotObject.GetComponent<RectTransform>();
            pivotRoot.anchorMin = Vector2.zero;
            pivotRoot.anchorMax = Vector2.zero;
            pivotRoot.pivot = new Vector2(0.5f, 0.5f);
            pivotRoot.anchoredPosition = new Vector2(rotated.Pivot.x * _cellSize, rotated.Pivot.y * _cellSize);
            pivotRoot.sizeDelta = Vector2.zero;
            int rotationDelta = GridMath.NormalizeRotation(pose.rotation - _artwork.bakedTargetRotation);
            pivotRoot.localEulerAngles = new Vector3(0f, 0f, -rotationDelta);

            Image art = CreateImage("ThumbnailFragment", pivotRoot, _artwork.sprite, Color.white, false);
            RectTransform rect = art.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = _artwork.offsetFromTargetPivotInCells * _cellSize;
            rect.sizeDelta = _artwork.sizeInCells * _cellSize;
            art.type = Image.Type.Simple;
            art.preserveAspect = false;
        }

        private void BuildHitAreas(RotatedFootprint rotated)
        {
            if (!_interactive) return;
            for (int i = 0; i < rotated.Cells.Length; i++)
            {
                GridCoordinate cell = rotated.Cells[i];
                Image hit = CreateImage("Hit_" + cell.x + "_" + cell.y, _rectTransform, null, Color.clear, true);
                RectTransform rect = hit.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(cell.x * _cellSize, cell.y * _cellSize);
                rect.sizeDelta = Vector2.one * _cellSize;
            }
        }

        private void BuildCellComposition(RotatedFootprint rotated, float gap)
        {
            for (int i = 0; i < rotated.Cells.Length; i++)
            {
                GridCoordinate cell = rotated.Cells[i];
                var child = new GameObject("Cell_" + cell.x + "_" + cell.y, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child.transform.SetParent(transform, false);
                RectTransform rect = child.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(cell.x * _cellSize + gap * 0.5f, cell.y * _cellSize + gap * 0.5f);
                rect.sizeDelta = Vector2.one * (_cellSize - gap);
                Image image = child.GetComponent<Image>();
                image.sprite = _sprites.rounded;
                image.type = GetImageType(image.sprite);
                image.color = _baseColor;
                image.raycastTarget = _interactive;
                AddMoldedShadow(image, _cellSize * 0.065f, 0.48f);
                RegisterTint(image, _baseColor);

                if (_definition.artGeneration != null && _definition.artGeneration.insetPanel && _sprites.insetPanel != null)
                {
                    Image inset = CreateImage("Inset", rect, _sprites.insetPanel, Color.Lerp(_baseColor, Color.black, 0.18f), false);
                    Stretch(inset.rectTransform, Mathf.Max(3f, _cellSize * 0.12f));
                }
            }
        }

        private void BuildDecorationRoot(PiecePose pose, RotatedFootprint rotated, bool includeBody, float gap)
        {
            bool hasStuds = _definition.decorativeStuds != null && _definition.decorativeStuds.Length > 0;
            bool hasHoles = _definition.recessedHoles != null && _definition.recessedHoles.Length > 0;
            if (!includeBody && !hasStuds && !hasHoles) return;

            RotatedFootprint canonical = GridMath.GetRotatedFootprint(_definition, 0);
            int canonicalWidth = Mathf.Max(1, canonical.Width > 0 ? canonical.Width : _definition.width);
            int canonicalHeight = Mathf.Max(1, canonical.Height > 0 ? canonical.Height : _definition.height);
            float overhangX = Mathf.Max(0f, _definition.visualOverhang.x) * _cellSize;
            float overhangY = Mathf.Max(0f, _definition.visualOverhang.y) * _cellSize;

            var rootObject = new GameObject("VisualRoot", typeof(RectTransform));
            rootObject.transform.SetParent(transform, false);
            RectTransform visualRoot = rootObject.GetComponent<RectTransform>();
            visualRoot.anchorMin = Vector2.zero;
            visualRoot.anchorMax = Vector2.zero;
            visualRoot.pivot = new Vector2(0.5f, 0.5f);
            visualRoot.anchoredPosition = new Vector2(rotated.Width * _cellSize * 0.5f, rotated.Height * _cellSize * 0.5f);
            visualRoot.sizeDelta = new Vector2(canonicalWidth * _cellSize - gap + overhangX * 2f, canonicalHeight * _cellSize - gap + overhangY * 2f);
            visualRoot.localEulerAngles = new Vector3(0f, 0f, -GridMath.NormalizeRotation(pose.rotation));

            if (includeBody)
            {
                Sprite bodySprite = _sprites.Resolve(_definition.shapeType);
                Image body = CreateImage("Body", visualRoot, bodySprite, _baseColor, _interactive);
                Stretch(body.rectTransform);
                AddMoldedShadow(body, _cellSize * 0.075f, 0.56f);
            }

            if (includeBody && _definition.artGeneration != null && _definition.artGeneration.insetPanel && _sprites.insetPanel != null)
            {
                Color insetColor = Color.Lerp(_baseColor, Color.black, 0.22f);
                Image inset = CreateImage("InsetPanel", visualRoot, _sprites.insetPanel, insetColor, false);
                inset.rectTransform.anchorMin = new Vector2(0.16f, 0.18f);
                inset.rectTransform.anchorMax = new Vector2(0.84f, 0.82f);
                inset.rectTransform.offsetMin = Vector2.zero;
                inset.rectTransform.offsetMax = Vector2.zero;
            }

            BuildStuds(visualRoot);
            BuildHoles(visualRoot);
        }

        private void BuildStuds(RectTransform visualRoot)
        {
            if (_definition.decorativeStuds == null || _sprites.stud == null) return;
            for (int i = 0; i < _definition.decorativeStuds.Length; i++)
            {
                DecorativeStudData data = _definition.decorativeStuds[i];
                if (data == null) continue;
                Color color = Color.Lerp(_baseColor, Color.white, 0.12f);
                Image stud = CreateImage("Stud_" + i, visualRoot, _sprites.stud, color, false);
                PlaceDecoration(stud.rectTransform, visualRoot, data.position, data.radius);
            }
        }

        private void BuildHoles(RectTransform visualRoot)
        {
            if (_definition.recessedHoles == null || _sprites.recessedHole == null) return;
            for (int i = 0; i < _definition.recessedHoles.Length; i++)
            {
                RecessedHoleData data = _definition.recessedHoles[i];
                if (data == null) continue;
                Color color = Color.Lerp(_baseColor, Color.black, 0.34f);
                Image hole = CreateImage("RecessedHole_" + i, visualRoot, _sprites.recessedHole, color, false);
                PlaceDecoration(hole.rectTransform, visualRoot, data.position, data.radius);
            }
        }

        private void PlaceDecoration(RectTransform rect, RectTransform visualRoot, FloatCoordinate position, float radius)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((position.x - 0.5f) * visualRoot.sizeDelta.x, (position.y - 0.5f) * visualRoot.sizeDelta.y);
            float diameter = Mathf.Max(_cellSize * 0.08f, radius * _cellSize * 2f);
            rect.sizeDelta = Vector2.one * diameter;
        }

        private Image CreateImage(string objectName, RectTransform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = GetImageType(sprite);
            image.color = color;
            image.raycastTarget = raycastTarget;
            RegisterTint(image, color);
            return image;
        }

        private void RegisterTint(Image image, Color color)
        {
            _tintBindings.Add(new TintBinding(image, color));
        }

        public void SetPose(PiecePose pose)
        {
            if (_visualRotation != GridMath.NormalizeRotation(pose.rotation)) Rebuild(pose);
            if (UsesFreeformArtwork)
            {
                _rectTransform.localEulerAngles = new Vector3(0f, 0f, -GridMath.NormalizeRotation(pose.rotation));
                _rectTransform.anchoredPosition = TargetPoseValidator.IsCorrect(_definition, pose)
                    ? GetFreeformTargetPosition()
                    : GetFreeformStartingPosition();
                if (!_dragPresentationActive) _restRotation = _rectTransform.localRotation;
                return;
            }
            _rectTransform.anchoredPosition = new Vector2(pose.position.x * _cellSize, pose.position.y * _cellSize);
            if (!_dragPresentationActive) _restRotation = _rectTransform.localRotation;
        }

        public void SetFreeformPosition(Vector2 position)
        {
            if (!UsesFreeformArtwork || _rectTransform == null) return;
            _rectTransform.anchoredPosition = ClampFreeformPosition(position);
        }

        public Vector2 ClampFreeformPosition(Vector2 position)
        {
            if (!UsesFreeformArtwork || _rectTransform == null) return position;
            Vector2 half = _rectTransform.sizeDelta * 0.5f;
            return new Vector2(
                Mathf.Clamp(position.x, half.x, Mathf.Max(half.x, _boardSize.x - half.x)),
                Mathf.Clamp(position.y, half.y, Mathf.Max(half.y, _boardSize.y - half.y)));
        }

        public bool ContainsVisualWorldPoint(Vector3 worldPoint, float alphaThreshold = 0.08f)
        {
            if (_rectTransform == null) return false;
            Vector2 local = _rectTransform.InverseTransformPoint(worldPoint);
            Rect bounds = _rectTransform.rect;
            if (!bounds.Contains(local)) return false;

            if (UsesFreeformArtwork)
            {
                if (_artworkPixels.Length == 0 ||
                    _artworkTextureWidth <= 0 ||
                    _artworkTextureHeight <= 0)
                    return true;

                float u = Mathf.InverseLerp(bounds.xMin, bounds.xMax, local.x);
                float v = Mathf.InverseLerp(bounds.yMin, bounds.yMax, local.y);
                int textureRectMinX = Mathf.FloorToInt(_artworkTextureRect.xMin);
                int textureRectMinY = Mathf.FloorToInt(_artworkTextureRect.yMin);
                int textureRectMaxX = Mathf.CeilToInt(_artworkTextureRect.xMax) - 1;
                int textureRectMaxY = Mathf.CeilToInt(_artworkTextureRect.yMax) - 1;
                int x = Mathf.Clamp(
                    Mathf.FloorToInt(_artworkTextureRect.xMin + u * _artworkTextureRect.width),
                    Mathf.Max(0, textureRectMinX),
                    Mathf.Min(_artworkTextureWidth - 1, textureRectMaxX));
                int y = Mathf.Clamp(
                    Mathf.FloorToInt(_artworkTextureRect.yMin + v * _artworkTextureRect.height),
                    Mathf.Max(0, textureRectMinY),
                    Mathf.Min(_artworkTextureHeight - 1, textureRectMaxY));
                int pixelIndex = y * _artworkTextureWidth + x;
                if (pixelIndex < 0 || pixelIndex >= _artworkPixels.Length) return false;
                return _artworkPixels[pixelIndex].a >=
                       Mathf.CeilToInt(Mathf.Clamp01(alphaThreshold) * 255f);
            }

            if (_cellSize <= 0f || _visualCoverageCells.Length == 0) return true;
            int cellX = Mathf.FloorToInt(local.x / _cellSize);
            int cellY = Mathf.FloorToInt(local.y / _cellSize);
            for (int i = 0; i < _visualCoverageCells.Length; i++)
            {
                GridCoordinate cell = _visualCoverageCells[i];
                if (cell.x == cellX && cell.y == cellY) return true;
            }
            return false;
        }

        public bool IsNearFreeformTarget()
        {
            return IsNearFreeformTarget(GetFreeformTargetPosition());
        }

        public bool IsNearFreeformTarget(Vector2 targetPosition)
        {
            if (!UsesFreeformArtwork || _rectTransform == null) return false;
            float threshold = Mathf.Max(8f, Mathf.Min(_boardSize.x, _boardSize.y) * _artwork.snapDistanceNormalized);
            return Vector2.Distance(_rectTransform.anchoredPosition, targetPosition) <= threshold;
        }

        public void SnapToFreeformTarget()
        {
            SnapToFreeformTarget(GetFreeformTargetPosition());
        }

        public void SnapToFreeformTarget(Vector2 targetPosition)
        {
            if (UsesFreeformArtwork && _rectTransform != null) _rectTransform.anchoredPosition = targetPosition;
        }

        public GameObject CreateFreeformHint(RectTransform parent)
        {
            return CreateFreeformHint(parent, GetFreeformTargetPosition());
        }

        public GameObject CreateFreeformHint(RectTransform parent, Vector2 targetPosition)
        {
            if (!UsesFreeformArtwork || parent == null || _artwork.sprite == null) return null;
            var hintObject = new GameObject("ColorBlockHint_" + PieceId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = hintObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = targetPosition;
            rect.sizeDelta = new Vector2(_artwork.sizeNormalized.x * _boardSize.x, _artwork.sizeNormalized.y * _boardSize.y);
            Image image = hintObject.GetComponent<Image>();
            image.sprite = _artwork.sprite;
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.28f);
            image.raycastTarget = false;
            hintObject.transform.SetAsFirstSibling();
            return hintObject;
        }

        private Vector2 GetFreeformTargetPosition()
        {
            return new Vector2(_artwork.targetCenterNormalized.x * _boardSize.x, _artwork.targetCenterNormalized.y * _boardSize.y);
        }

        private Vector2 GetFreeformStartingPosition()
        {
            return new Vector2(_artwork.startingCenterNormalized.x * _boardSize.x, _artwork.startingCenterNormalized.y * _boardSize.y);
        }

        private void CacheArtworkAlpha()
        {
            _artworkPixels = Array.Empty<Color32>();
            _artworkTextureRect = default;
            _artworkTextureWidth = 0;
            _artworkTextureHeight = 0;
            if (_artwork == null || _artwork.sprite == null || _artwork.sprite.texture == null) return;

            Texture2D texture = _artwork.sprite.texture;
            if (!texture.isReadable) return;
            _artworkPixels = texture.GetPixels32();
            _artworkTextureRect = _artwork.sprite.textureRect;
            _artworkTextureWidth = texture.width;
            _artworkTextureHeight = texture.height;
        }

        public void SetLocked(bool locked, bool referenceAnchor = false)
        {
            _locked = locked;
            _referenceAnchor = referenceAnchor;
            if (_canvasGroup != null) _canvasGroup.alpha = locked && !referenceAnchor ? 0.9f : 1f;
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = !locked;
            if (_rectTransform != null)
            {
                if (locked) _rectTransform.SetAsFirstSibling();
                else _rectTransform.SetAsLastSibling();
            }
        }

        public void FlashWhite(float duration = 0.22f, float strength = 0.92f)
        {
            if (isActiveAndEnabled) StartCoroutine(FlashWhiteRoutine(duration, strength));
        }

        private IEnumerator FlashWhiteRoutine(float duration, float strength)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float amount = Mathf.Sin(t * Mathf.PI) * Mathf.Clamp01(strength);
                for (int i = 0; i < _tintBindings.Count; i++)
                {
                    TintBinding binding = _tintBindings[i];
                    if (binding.Image != null) binding.Image.color = Color.Lerp(binding.Color, Color.white, amount);
                }
                yield return null;
            }
            ClearPlacementTint();
        }

        public void SetSelected(bool selected)
        {
            if (_rectTransform == null) return;
            if (selected) _rectTransform.SetAsLastSibling();
        }

        public void BeginDragPresentation(float pickupScale, float duration = 0.09f)
        {
            if (_rectTransform == null) return;
            if (_presentationRoutine != null) StopCoroutine(_presentationRoutine);
            _presentationRoutine = null;
            _dragPresentationActive = true;
            _restRotation = _rectTransform.localRotation;
            _rectTransform.SetAsLastSibling();
            _presentationRoutine = StartCoroutine(
                AnimatePresentationRoutine(
                    Vector3.one * Mathf.Max(1f, pickupScale),
                    _restRotation,
                    Mathf.Max(0.01f, duration),
                    false));
        }

        public void UpdateDragPresentation(Vector2 frameMotion, float pickupScale, float maximumTilt)
        {
            if (!_dragPresentationActive || _rectTransform == null) return;
            if (_presentationRoutine != null)
            {
                StopCoroutine(_presentationRoutine);
                _presentationRoutine = null;
            }
            float normalizedVelocity = Mathf.Clamp(
                frameMotion.x / Mathf.Max(1f, LargestVisualDimension * 0.35f),
                -1f,
                1f);
            Quaternion targetRotation =
                _restRotation * Quaternion.Euler(0f, 0f, -normalizedVelocity * Mathf.Max(0f, maximumTilt));
            float blend = 1f - Mathf.Exp(-14f * Mathf.Max(0.001f, Time.unscaledDeltaTime));
            _rectTransform.localScale = Vector3.Lerp(
                _rectTransform.localScale,
                Vector3.one * Mathf.Max(1f, pickupScale),
                blend);
            _rectTransform.localRotation = Quaternion.Slerp(
                _rectTransform.localRotation,
                targetRotation,
                blend);
        }

        public void EndDragPresentation(float duration = 0.12f, bool immediate = false)
        {
            if (_rectTransform == null) return;
            _dragPresentationActive = false;
            if (_presentationRoutine != null) StopCoroutine(_presentationRoutine);
            _presentationRoutine = null;
            if (immediate)
            {
                _rectTransform.localScale = Vector3.one;
                _rectTransform.localRotation = _restRotation;
                return;
            }
            _presentationRoutine = StartCoroutine(
                AnimatePresentationRoutine(Vector3.one, _restRotation, Mathf.Max(0.01f, duration), true));
        }

        private IEnumerator AnimatePresentationRoutine(
            Vector3 targetScale,
            Quaternion targetRotation,
            float duration,
            bool clearHandle)
        {
            Vector3 startScale = _rectTransform.localScale;
            Quaternion startRotation = _rectTransform.localRotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_rectTransform == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - (1f - t) * (1f - t);
                _rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
                _rectTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }
            if (_rectTransform != null)
            {
                _rectTransform.localScale = targetScale;
                _rectTransform.localRotation = targetRotation;
            }
            if (clearHandle) _presentationRoutine = null;
        }

        private void OnDisable()
        {
            if (_presentationRoutine != null) StopCoroutine(_presentationRoutine);
            _presentationRoutine = null;
            _dragPresentationActive = false;
            if (_rectTransform == null) return;
            _rectTransform.localScale = Vector3.one;
            _rectTransform.localRotation = _restRotation;
        }

        public void SetPlacementTint(bool valid)
        {
            // Keep authored sprite colors intact while dragging. Invalid placement is
            // communicated by the return motion instead of recoloring the artwork.
            ClearPlacementTint();
        }

        public void ClearPlacementTint()
        {
            for (int i = 0; i < _tintBindings.Count; i++)
            {
                TintBinding binding = _tintBindings[i];
                if (binding.Image != null) binding.Image.color = binding.Color;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_interactive && !_locked && _controller != null) _controller.SelectPiece(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_interactive && !_locked && _controller != null) _controller.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_interactive && !_locked && _controller != null) _controller.Drag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_interactive && !_locked && _controller != null) _controller.EndDrag(this, eventData);
        }

        private static bool UsesCellComposition(PieceShapeType shapeType)
        {
            switch (shapeType)
            {
                case PieceShapeType.LShape:
                case PieceShapeType.TShape:
                case PieceShapeType.UShape:
                case PieceShapeType.ZShape:
                case PieceShapeType.CrossShape:
                case PieceShapeType.Polyomino:
                case PieceShapeType.CustomGridFootprint:
                case PieceShapeType.CustomPolygon:
                    return true;
                default:
                    return false;
            }
        }

        private static Image.Type GetImageType(Sprite sprite)
        {
            return sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static void AddMoldedShadow(Image image, float distance, float alpha)
        {
            if (image == null || image.GetComponent<Shadow>() != null) return;
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.025f, 0.055f, alpha);
            shadow.effectDistance = new Vector2(0f, -Mathf.Max(2f, distance));
            shadow.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private readonly struct TintBinding
        {
            public TintBinding(Image image, Color color)
            {
                Image = image;
                Color = color;
            }

            public Image Image { get; }
            public Color Color { get; }
        }
    }
}
