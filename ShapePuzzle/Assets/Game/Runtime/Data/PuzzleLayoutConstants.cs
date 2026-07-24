using UnityEngine;

namespace ToyPuzzle
{
    public static class PuzzleLayoutConstants
    {
        // Runtime code uses the catalog count; this value describes the shipped batch.
        public const int TotalPlayableLevels = 50;
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const float ReferenceHorizontalMargin = 64f;
        public const float ReferenceTopAreaHeight = 320f;
        public const float ReferenceOuterBoardSize = 920f;
        public const float ReferenceInnerBoardSize = 840f;
        public const float ReferenceFrameThickness = 40f;
        public const float ReferenceBottomControlsHeight = 210f;
        public const float ReferenceVisibleButtonSize = 148f;
        public const float ReferenceButtonTouchSize = 168f;
        public const float MinimumSolvedFreeSpaceRatio = 0.30f;

        public static float CalculateReferenceCellSize(int gridSize)
        {
            return ReferenceInnerBoardSize / Mathf.Max(1, gridSize);
        }
    }
}
