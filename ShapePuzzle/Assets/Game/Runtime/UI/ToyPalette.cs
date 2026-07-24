using UnityEngine;

namespace ToyPuzzle
{
    [CreateAssetMenu(fileName = "ToyPalette", menuName = "Toy Puzzle/Toy Palette")]
    public sealed class ToyPalette : ScriptableObject
    {
        [Header("Environment")]
        public Color background = new Color32(0, 91, 164, 255);
        public Color backgroundSecondary = new Color32(0, 122, 205, 255);
        public Color boardFrame = new Color32(28, 30, 25, 255);
        public Color boardCell = new Color32(38, 41, 35, 255);
        public Color boardCellAlternate = new Color32(43, 46, 40, 255);

        [Header("Pieces and controls")]
        public Color red = new Color32(242, 55, 19, 255);
        public Color yellow = new Color32(255, 186, 11, 255);
        public Color cyan = new Color32(0, 157, 221, 255);
        public Color green = new Color32(82, 190, 18, 255);
        public Color orange = new Color32(255, 110, 12, 255);
        public Color purple = new Color32(153, 92, 205, 255);
        public Color teal = new Color32(31, 177, 158, 255);
        public Color cream = new Color32(247, 242, 226, 255);

        [Header("Feedback")]
        public Color disabled = new Color32(101, 112, 110, 180);
        public Color valid = new Color32(113, 220, 101, 255);
        public Color invalid = new Color32(239, 74, 55, 255);
        public Color shadow = new Color32(0, 20, 43, 150);
        public Color highlight = new Color32(255, 250, 224, 75);

        public Color ResolvePieceColor(string colorId)
        {
            if (string.IsNullOrEmpty(colorId)) return cyan;
            switch (colorId.ToLowerInvariant())
            {
                case "red": return red;
                case "yellow": return yellow;
                case "blue":
                case "cyan": return cyan;
                case "green": return green;
                case "orange": return orange;
                case "purple": return purple;
                case "teal": return teal;
                case "cream":
                case "white": return cream;
                default: return cyan;
            }
        }
    }
}
