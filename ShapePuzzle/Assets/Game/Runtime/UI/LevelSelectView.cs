using System;
using UnityEngine;
using UnityEngine.UI;

namespace ToyPuzzle
{
    [DisallowMultipleComponent]
    public sealed class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private ToyPalette palette;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite[] thumbnails = Array.Empty<Sprite>();
        [SerializeField] private int columns = 4;

        private Font _font;

        public void Rebuild(LevelCatalog catalog, PlayerSaveData saveData, Action<int> selected)
        {
            if (catalog == null) return;
            Rebuild(catalog.Count, index =>
            {
                RuntimeLevelData runtimeLevel = catalog.GetByIndex(index);
                return runtimeLevel == null ? null : runtimeLevel.Level;
            }, index => index >= 0 && index < thumbnails.Length ? thumbnails[index] : null, saveData, selected);
        }

        public void Rebuild(LevelPrefabCatalog catalog, PlayerSaveData saveData, Action<int> selected)
        {
            if (catalog == null) return;
            Rebuild(catalog.Count, index =>
            {
                PuzzleLevelPrefab levelPrefab = catalog.GetByIndex(index);
                return levelPrefab == null ? null : levelPrefab.Level;
            }, index =>
            {
                PuzzleLevelPrefab levelPrefab = catalog.GetByIndex(index);
                if (levelPrefab != null && levelPrefab.Thumbnail != null) return levelPrefab.Thumbnail;
                return index >= 0 && index < thumbnails.Length ? thumbnails[index] : null;
            }, saveData, selected);
        }

        private void Rebuild(int levelCount, Func<int, LevelDefinition> levelAt, Func<int, Sprite> thumbnailAt, PlayerSaveData saveData, Action<int> selected)
        {
            if (saveData == null || content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject oldEntry = content.GetChild(i).gameObject;
                oldEntry.SetActive(false);
                Destroy(oldEntry);
            }
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(2, columns);
            bool largeCards = grid.constraintCount == 2;
            grid.cellSize = largeCards ? new Vector2(420f, 280f) : new Vector2(280f, 250f);
            grid.spacing = largeCards ? new Vector2(32f, 34f) : new Vector2(24f, 28f);
            grid.padding = new RectOffset(24, 24, 24, 24);
            grid.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < levelCount; i++)
            {
                LevelDefinition level = levelAt(i);
                if (level == null) continue;
                LevelProgressData progress = FindProgress(saveData, level.levelNumber);
                bool unlocked = level.levelNumber <= saveData.highestUnlockedLevel;
                CreateEntry(level, thumbnailAt(i), progress, unlocked, i, selected);
            }

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null && levelCount > 1)
            {
                float progress = Mathf.Clamp01((saveData.highestUnlockedLevel - 1f) / (levelCount - 1f));
                scrollRect.verticalNormalizedPosition = 1f - progress;
            }
        }

        private void CreateEntry(LevelDefinition level, Sprite thumbnailSprite, LevelProgressData progress, bool unlocked, int catalogIndex, Action<int> selected)
        {
            var entry = new GameObject("Level_" + level.levelNumber, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            entry.transform.SetParent(content, false);
            Image image = entry.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            Color unlockedColor = palette == null ? new Color32(32, 168, 220, 255) : palette.cyan;
            Color lockedColor = palette == null ? new Color32(72, 79, 76, 255) : palette.disabled;
            image.color = unlocked ? unlockedColor : lockedColor;
            Shadow shadow = entry.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.025f, 0.055f, 0.52f);
            shadow.effectDistance = new Vector2(0f, -9f);
            shadow.useGraphicAlpha = true;
            Button button = entry.GetComponent<Button>();
            button.targetGraphic = image;
            button.interactable = unlocked;
            button.onClick.AddListener(() => selected?.Invoke(catalogIndex));

            if (thumbnailSprite != null)
            {
                var thumbnailObject = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                thumbnailObject.transform.SetParent(entry.transform, false);
                Image thumbnail = thumbnailObject.GetComponent<Image>();
                thumbnail.sprite = thumbnailSprite;
                thumbnail.preserveAspect = true;
                thumbnail.raycastTarget = false;
                RectTransform thumbnailRect = thumbnail.rectTransform;
                thumbnailRect.anchorMin = new Vector2(0.10f, 0.24f);
                thumbnailRect.anchorMax = new Vector2(0.90f, 0.91f);
                thumbnailRect.offsetMin = Vector2.zero;
                thumbnailRect.offsetMax = Vector2.zero;
            }

            Text number = CreateText(entry.transform, "LevelNumber", level.levelNumber.ToString(), 48, FontStyle.Bold);
            RectTransform numberRect = number.rectTransform;
            numberRect.anchorMin = new Vector2(0f, 0.73f);
            numberRect.anchorMax = new Vector2(0.27f, 1f);
            numberRect.offsetMin = new Vector2(8f, 0f);
            numberRect.offsetMax = new Vector2(-8f, -8f);
            Text name = CreateText(entry.transform, "ObjectName", unlocked ? level.targetObjectName : "LOCKED", 25, FontStyle.Bold);
            RectTransform nameRect = name.rectTransform;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = new Vector2(1f, 0.26f);
            nameRect.offsetMin = new Vector2(8f, 12f);
            nameRect.offsetMax = new Vector2(-8f, 0f);
            if (progress != null && progress.completed)
            {
                name.text = "COMPLETED\nBEST " + progress.bestMoveCount;
            }
        }

        private Text CreateText(Transform parent, string name, string value, int size, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = palette == null ? new Color32(244, 240, 223, 255) : palette.cream;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static LevelProgressData FindProgress(PlayerSaveData data, int levelNumber)
        {
            if (data.levelProgress == null) return null;
            for (int i = 0; i < data.levelProgress.Length; i++)
            {
                if (data.levelProgress[i] != null && data.levelProgress[i].levelNumber == levelNumber) return data.levelProgress[i];
            }
            return null;
        }
    }
}
