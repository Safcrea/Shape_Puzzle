using UnityEngine;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    public sealed class ResponsiveGameLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform layoutRoot;
        [SerializeField] private RectTransform topZone;
        [SerializeField] private RectTransform board;
        [SerializeField] private RectTransform bottomControls;
        [SerializeField] private float horizontalMargin = PuzzleLayoutConstants.ReferenceHorizontalMargin;
        [SerializeField] private float verticalGap = 22f;
        [SerializeField] private float minimumControlHeight = PuzzleLayoutConstants.ReferenceBottomControlsHeight;
        [SerializeField] private float maximumBoardSize = PuzzleLayoutConstants.ReferenceOuterBoardSize;

        private Vector2 _lastSize;
        private int _boardColumns = 1;
        private int _boardRows = 1;

        private void OnEnable()
        {
            Recalculate();
        }

        private void OnRectTransformDimensionsChange()
        {
            Recalculate();
        }

        public void Recalculate()
        {
            if (layoutRoot == null || topZone == null || board == null || bottomControls == null) return;
            Vector2 size = layoutRoot.rect.size;
            if (size.x <= 0f || size.y <= 0f || size == _lastSize) return;
            _lastSize = size;

            float referenceScale = Mathf.Min(size.x / PuzzleLayoutConstants.ReferenceWidth, size.y / PuzzleLayoutConstants.ReferenceHeight);
            float topHeight = Mathf.Clamp(PuzzleLayoutConstants.ReferenceTopAreaHeight * referenceScale, 180f, 360f);
            float controlsHeight = Mathf.Clamp(PuzzleLayoutConstants.ReferenceBottomControlsHeight * referenceScale, minimumControlHeight, 260f);
            float availableWidth = size.x - horizontalMargin * 2f;
            float availableHeight = size.y - topHeight - controlsHeight - verticalGap * 2f;
            float cellSize = Mathf.Min(
                availableWidth / Mathf.Max(1, _boardColumns),
                availableHeight / Mathf.Max(1, _boardRows),
                maximumBoardSize / Mathf.Max(_boardColumns, _boardRows));
            Vector2 boardSize = new Vector2(_boardColumns * cellSize, _boardRows * cellSize);

            SetTop(topZone, topHeight);
            SetBottom(bottomControls, controlsHeight);
            board.anchorMin = board.anchorMax = new Vector2(0.5f, 0.5f);
            board.pivot = new Vector2(0.5f, 0.5f);
            board.sizeDelta = boardSize;
            float centerY = (controlsHeight - topHeight) * 0.5f;
            board.anchoredPosition = new Vector2(0f, centerY);
        }

        public void SetBoardGridDimensions(int columns, int rows)
        {
            _boardColumns = Mathf.Max(1, columns);
            _boardRows = Mathf.Max(1, rows);
            _lastSize = Vector2.zero;
            Recalculate();
        }

        private static void SetTop(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -height);
            rect.offsetMax = Vector2.zero;
        }

        private static void SetBottom(RectTransform rect, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, height);
        }
    }
}
