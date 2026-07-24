using System;
using System.Collections.Generic;
using System.IO;
using ToyPuzzle.Editor.Levels;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor
{
    public static class ToyLevelPieceArtGenerator
    {
        public const string OutputRoot = "Assets/Game/Art/Generated/LevelPieces";
        private const string PrefabCatalogPath = "Assets/Game/Data/Levels/Generated/LevelPrefabCatalog.asset";
        private const int PixelsPerCell = 128;
        private const int FirstGeneratedLevel = 1;
        private const int LastGeneratedLevel = PuzzleLayoutConstants.TotalPlayableLevels;
        private const float ArtworkFill = 0.70f;
        private const float StartMargin = 0.012f;
        private const int CandidateGridSize = 25;

        private static readonly string[] ColorNames = { "red", "blue", "green", "yellow", "orange", "purple", "teal", "cream" };

        private sealed class PartRegion
        {
            public int ColorIndex;
            public string PieceId;
            public readonly List<int> SeedPixels = new List<int>();
            public RectInt SeedBounds;
            public RectInt Bounds;
            public Vector2 TargetCenter;
            public Vector2 Size;
            public Vector2 StartingCenter;
            public Sprite Sprite;
        }

        [MenuItem("Tools/Toy Puzzle/Generate 35 Separate Color Parts")]
        private static void GenerateFirstTenMenu()
        {
            GenerateFirstTen();
        }

        public static int GenerateFirstTen()
        {
            EnsureFolder(OutputRoot);
            LevelPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(PrefabCatalogPath);
            if (catalog == null) throw new InvalidOperationException("Level prefab catalog is missing at " + PrefabCatalogPath + ".");

            var artworkByLevel = new Dictionary<int, PuzzlePieceArtwork[]>();
            var pieceIdsByLevel = new Dictionary<int, string[]>();
            int pieceCount = 0;
            for (int levelNumber = FirstGeneratedLevel; levelNumber <= LastGeneratedLevel; levelNumber++)
            {
                PuzzleLevelPrefab prefabData = catalog.FindByNumber(levelNumber);
                if (prefabData == null || prefabData.Level == null)
                    throw new InvalidOperationException("Level " + levelNumber + " is missing from the prefab catalog.");
                if (prefabData.Thumbnail == null)
                    throw new InvalidOperationException("Level " + levelNumber + " does not have a thumbnail sprite.");

                string thumbnailPath = AssetDatabase.GetAssetPath(prefabData.Thumbnail);
                PuzzlePieceArtwork[] artwork = GenerateLevel(prefabData.Level, thumbnailPath);
                artworkByLevel.Add(levelNumber, artwork);
                var ids = new string[artwork.Length];
                for (int i = 0; i < artwork.Length; i++) ids[i] = artwork[i].pieceId;
                pieceIdsByLevel.Add(levelNumber, ids);
                pieceCount += artwork.Length;
            }

            FourColorBlockLevelConverter.ConvertAndRebuild(pieceIdsByLevel);
            catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(PrefabCatalogPath);
            if (catalog == null) throw new InvalidOperationException("Level prefab catalog disappeared after rebuilding separated-part definitions.");

            for (int levelNumber = FirstGeneratedLevel; levelNumber <= LastGeneratedLevel; levelNumber++)
            {
                PuzzleLevelPrefab prefabData = catalog.FindByNumber(levelNumber);
                if (prefabData == null) throw new InvalidOperationException("Rebuilt level " + levelNumber + " is missing from the prefab catalog.");
                string prefabPath = AssetDatabase.GetAssetPath(prefabData.gameObject);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    PuzzleLevelPrefab editable = root.GetComponent<PuzzleLevelPrefab>();
                    if (editable == null) throw new InvalidOperationException("Prefab is missing PuzzleLevelPrefab: " + prefabPath);
                    editable.SetPieceArtwork(artworkByLevel[levelNumber]);
                    EditorUtility.SetDirty(editable);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Generated and assigned " + pieceCount + " separate molded-color gameplay parts across " + LastGeneratedLevel + " levels.");
            return pieceCount;
        }

        private static PuzzlePieceArtwork[] GenerateLevel(LevelDefinition level, string thumbnailPath)
        {
            string fullThumbnailPath = Path.GetFullPath(thumbnailPath);
            if (!File.Exists(fullThumbnailPath)) throw new FileNotFoundException("Thumbnail source file is missing.", fullThumbnailPath);

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(fullThumbnailPath), false))
                    throw new InvalidOperationException("Could not decode thumbnail " + thumbnailPath + ".");
                Color32[] sourcePixels = source.GetPixels32();
                bool[] externalBackground = FloodExternalBackground(sourcePixels, source.width, source.height);
                RectInt sourceBounds = FindForegroundBounds(externalBackground, source.width, source.height);
                if (sourceBounds.width <= 0 || sourceBounds.height <= 0)
                    throw new InvalidOperationException("No foreground artwork was detected in " + thumbnailPath + ".");

                int canvasWidth = level.boardWidth * PixelsPerCell;
                int canvasHeight = level.boardHeight * PixelsPerCell;
                var composite = new Color32[canvasWidth * canvasHeight];
                var seedOwners = new int[composite.Length];
                for (int i = 0; i < seedOwners.Length; i++) seedOwners[i] = -1;

                float scale = Mathf.Min(
                    canvasWidth * ArtworkFill / sourceBounds.width,
                    canvasHeight * ArtworkFill / sourceBounds.height);
                float contentWidth = sourceBounds.width * scale;
                float contentHeight = sourceBounds.height * scale;
                float contentX = (canvasWidth - contentWidth) * 0.5f;
                float contentY = (canvasHeight - contentHeight) * 0.5f;
                int startX = Mathf.Max(0, Mathf.FloorToInt(contentX));
                int startY = Mathf.Max(0, Mathf.FloorToInt(contentY));
                int endX = Mathf.Min(canvasWidth, Mathf.CeilToInt(contentX + contentWidth));
                int endY = Mathf.Min(canvasHeight, Mathf.CeilToInt(contentY + contentHeight));
                int foregroundPixels = 0;

                for (int y = startY; y < endY; y++)
                {
                    float sourceY = sourceBounds.yMin + ((y + 0.5f - contentY) / contentHeight) * sourceBounds.height;
                    int sampleY = Mathf.Clamp(Mathf.FloorToInt(sourceY), sourceBounds.yMin, sourceBounds.yMax - 1);
                    for (int x = startX; x < endX; x++)
                    {
                        float sourceX = sourceBounds.xMin + ((x + 0.5f - contentX) / contentWidth) * sourceBounds.width;
                        int sampleX = Mathf.Clamp(Mathf.FloorToInt(sourceX), sourceBounds.xMin, sourceBounds.xMax - 1);
                        int sourceIndex = sampleY * source.width + sampleX;
                        if (externalBackground[sourceIndex]) continue;

                        int index = y * canvasWidth + x;
                        Color32 color = sourcePixels[sourceIndex];
                        color.a = 255;
                        composite[index] = color;
                        seedOwners[index] = ClassifyPaletteColor(color, level.levelNumber > 10);
                        foregroundPixels++;
                    }
                }

                int[] separatedSeeds = ErodePaletteSeeds(seedOwners, canvasWidth, canvasHeight, 4);
                List<PartRegion> regions = FindSeedRegions(separatedSeeds, canvasWidth, canvasHeight, foregroundPixels);
                if (regions.Count == 0) throw new InvalidOperationException("No colored part regions were detected in " + thumbnailPath + ".");
                int[] labels = GrowRegionsAcrossArtwork(composite, seedOwners, regions, canvasWidth, canvasHeight);
                labels = SimplifyRegions(composite, labels, ref regions, canvasWidth, canvasHeight, foregroundPixels, level.levelNumber);
                AssignStableIds(regions);
                if (regions.Count > 10 || regions.Count * 2 > level.boardWidth * level.boardHeight)
                    throw new InvalidOperationException("Level " + level.levelNumber + " produced " + regions.Count + " playable regions, exceeding its gameplay capacity.");
                string levelFolder = OutputRoot + "/Level_" + level.levelNumber.ToString("D3");
                EnsureFolder(levelFolder);
                DeleteOldLevelSprites(levelFolder);

                for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
                {
                    PartRegion region = regions[regionIndex];
                    region.Bounds = FindLabelBounds(labels, canvasWidth, canvasHeight, regionIndex);
                    if (region.Bounds.width <= 0 || region.Bounds.height <= 0)
                        throw new InvalidOperationException("Generated region " + region.PieceId + " has no pixels.");
                    Color32[] pixels = BuildFragmentPixels(composite, labels, canvasWidth, region.Bounds, regionIndex);
                    string spritePath = levelFolder + "/level_" + level.levelNumber.ToString("D3") + "_" + region.PieceId + ".png";
                    WriteTexture(spritePath, region.Bounds.width, region.Bounds.height, pixels);
                    ConfigureSpriteImporter(spritePath);
                    region.Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (region.Sprite == null) throw new InvalidOperationException("Generated sprite could not be loaded: " + spritePath);
                    region.TargetCenter = new Vector2(
                        (region.Bounds.x + region.Bounds.width * 0.5f) / canvasWidth,
                        (region.Bounds.y + region.Bounds.height * 0.5f) / canvasHeight);
                    region.Size = new Vector2((float)region.Bounds.width / canvasWidth, (float)region.Bounds.height / canvasHeight);
                }

                PlaceStartingCenters(regions, level.levelNumber);
                var result = new PuzzlePieceArtwork[regions.Count];
                for (int i = 0; i < regions.Count; i++)
                {
                    PartRegion region = regions[i];
                    result[i] = new PuzzlePieceArtwork
                    {
                        pieceId = region.PieceId,
                        sprite = region.Sprite,
                        freeformColorBlock = true,
                        targetCenterNormalized = region.TargetCenter,
                        startingCenterNormalized = region.StartingCenter,
                        sizeNormalized = region.Size,
                        snapDistanceNormalized = 0.055f,
                        sizeInCells = new Vector2(region.Size.x * level.boardWidth, region.Size.y * level.boardHeight),
                        bakedTargetRotation = 0
                    };
                }
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static List<PartRegion> FindSeedRegions(int[] seedOwners, int width, int height, int foregroundPixels)
        {
            var visited = new bool[seedOwners.Length];
            var queue = new Queue<int>();
            var regions = new List<PartRegion>();
            int minimumSeedArea = Mathf.Max(16, foregroundPixels / 5000);
            for (int start = 0; start < seedOwners.Length; start++)
            {
                if (visited[start] || seedOwners[start] < 0) continue;
                int owner = seedOwners[start];
                var region = new PartRegion { ColorIndex = owner };
                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;
                visited[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    region.SeedPixels.Add(index);
                    int x = index % width;
                    int y = index / width;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            EnqueueSeedNeighbor(x + dx, y + dy, owner, seedOwners, visited, width, height, queue);
                        }
                    }
                }

                if (region.SeedPixels.Count < minimumSeedArea) continue;
                region.SeedBounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
                regions.Add(region);
            }

            regions.Sort((left, right) =>
            {
                int color = left.ColorIndex.CompareTo(right.ColorIndex);
                if (color != 0) return color;
                int horizontal = left.SeedBounds.center.x.CompareTo(right.SeedBounds.center.x);
                return horizontal != 0 ? horizontal : left.SeedBounds.center.y.CompareTo(right.SeedBounds.center.y);
            });
            return regions;
        }

        private static void AssignStableIds(List<PartRegion> regions)
        {
            var counters = new int[ColorNames.Length];
            for (int i = 0; i < regions.Count; i++)
            {
                PartRegion region = regions[i];
                counters[region.ColorIndex]++;
                region.PieceId = ColorNames[region.ColorIndex] + "_part_" + counters[region.ColorIndex].ToString("D2");
            }
        }

        private static int[] ErodePaletteSeeds(int[] source, int width, int height, int radius)
        {
            var result = new int[source.Length];
            for (int i = 0; i < result.Length; i++) result[i] = -1;
            for (int y = radius; y < height - radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {
                    int index = y * width + x;
                    int owner = source[index];
                    if (owner < 0) continue;
                    bool keep = true;
                    for (int dy = -radius; dy <= radius && keep; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            if (dx * dx + dy * dy > radius * radius) continue;
                            if (source[(y + dy) * width + x + dx] != owner)
                            {
                                keep = false;
                                break;
                            }
                        }
                    }
                    if (keep) result[index] = owner;
                }
            }
            return result;
        }

        private static void EnqueueSeedNeighbor(int x, int y, int owner, int[] seedOwners, bool[] visited, int width, int height, Queue<int> queue)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (visited[index] || seedOwners[index] != owner) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        private static int[] GrowRegionsAcrossArtwork(Color32[] composite, int[] seedOwners, List<PartRegion> regions, int width, int height)
        {
            var labels = new int[composite.Length];
            for (int i = 0; i < labels.Length; i++) labels[i] = -1;
            var queue = new Queue<int>();
            for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
            {
                List<int> seeds = regions[regionIndex].SeedPixels;
                for (int seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
                {
                    int pixel = seeds[seedIndex];
                    labels[pixel] = regionIndex;
                    queue.Enqueue(pixel);
                }
            }

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                GrowTo(x - 1, y, labels[index], composite, seedOwners, regions, labels, width, height, queue);
                GrowTo(x + 1, y, labels[index], composite, seedOwners, regions, labels, width, height, queue);
                GrowTo(x, y - 1, labels[index], composite, seedOwners, regions, labels, width, height, queue);
                GrowTo(x, y + 1, labels[index], composite, seedOwners, regions, labels, width, height, queue);
            }
            return labels;
        }

        private static void GrowTo(int x, int y, int label, Color32[] composite, int[] seedOwners, List<PartRegion> regions, int[] labels, int width, int height, Queue<int> queue)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (composite[index].a == 0 || labels[index] >= 0) return;
            int pixelColor = seedOwners[index];
            if (pixelColor >= 0 && pixelColor != regions[label].ColorIndex) return;
            labels[index] = label;
            queue.Enqueue(index);
        }

        private static int[] SimplifyRegions(
            Color32[] composite,
            int[] labels,
            ref List<PartRegion> regions,
            int width,
            int height,
            int foregroundPixels,
            int levelNumber)
        {
            int[] areas = new int[regions.Count];
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] >= 0) areas[labels[i]]++;

            var byArea = new List<int>(regions.Count);
            for (int i = 0; i < regions.Count; i++) byArea.Add(i);
            byArea.Sort((left, right) => areas[right].CompareTo(areas[left]));

            int cap = GetPieceCap(levelNumber);
            int minimumArea = Mathf.Max(180, foregroundPixels / 100);
            int minimumPieces = Mathf.Min(4, regions.Count);
            var kept = new List<int>(cap);
            for (int i = 0; i < byArea.Count && kept.Count < cap; i++)
            {
                int candidate = byArea[i];
                if (areas[candidate] >= minimumArea || kept.Count < minimumPieces) kept.Add(candidate);
            }
            if (kept.Count == 0 && byArea.Count > 0) kept.Add(byArea[0]);
            kept.Sort();

            var oldToNew = new int[regions.Count];
            var isKept = new bool[regions.Count];
            for (int i = 0; i < kept.Count; i++)
            {
                isKept[kept[i]] = true;
                oldToNew[kept[i]] = i;
            }

            for (int oldIndex = 0; oldIndex < regions.Count; oldIndex++)
            {
                if (isKept[oldIndex]) continue;
                Vector2 center = regions[oldIndex].SeedBounds.center;
                float bestDistance = float.MaxValue;
                int bestNewIndex = 0;
                for (int newIndex = 0; newIndex < kept.Count; newIndex++)
                {
                    PartRegion destination = regions[kept[newIndex]];
                    float distance = (destination.SeedBounds.center - center).sqrMagnitude;
                    if (destination.ColorIndex == regions[oldIndex].ColorIndex) distance *= 0.55f;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestNewIndex = newIndex;
                    }
                }
                oldToNew[oldIndex] = bestNewIndex;
            }

            for (int i = 0; i < labels.Length; i++)
            {
                int oldLabel = labels[i];
                if (oldLabel < 0) continue;
                labels[i] = oldToNew[oldLabel];
                if (!isKept[oldLabel])
                {
                    Color32 source = composite[i];
                    byte shade = (byte)Mathf.Clamp(118 + (source.r + source.g + source.b) / 12, 128, 184);
                    composite[i] = new Color32(shade, shade, (byte)Mathf.Max(118, shade - 4), source.a);
                }
            }

            var simplified = new List<PartRegion>(kept.Count);
            for (int i = 0; i < kept.Count; i++) simplified.Add(regions[kept[i]]);
            regions = simplified;
            return labels;
        }

        private static int GetPieceCap(int levelNumber)
        {
            int[] firstTenCaps = { 4, 6, 6, 6, 6, 6, 8, 8, 6, 7 };
            if (levelNumber >= 1 && levelNumber <= firstTenCaps.Length) return firstTenCaps[levelNumber - 1];
            if (levelNumber <= 20) return 8;
            return 10;
        }

        private static void PlaceStartingCenters(List<PartRegion> regions, int levelNumber)
        {
            var order = new List<int>(regions.Count);
            for (int i = 0; i < regions.Count; i++) order.Add(i);
            order.Sort((left, right) =>
            {
                float leftArea = regions[left].Size.x * regions[left].Size.y;
                float rightArea = regions[right].Size.x * regions[right].Size.y;
                int area = rightArea.CompareTo(leftArea);
                return area != 0 ? area : left.CompareTo(right);
            });

            var placed = new List<Rect>();
            for (int orderIndex = 0; orderIndex < order.Count; orderIndex++)
            {
                PartRegion region = regions[order[orderIndex]];
                Vector2 half = region.Size * 0.5f;
                float minX = Mathf.Min(0.5f, half.x + StartMargin);
                float maxX = Mathf.Max(0.5f, 1f - half.x - StartMargin);
                float minY = Mathf.Min(0.5f, half.y + StartMargin);
                float maxY = Mathf.Max(0.5f, 1f - half.y - StartMargin);
                Vector2 best = new Vector2(0.5f, 0.5f);
                float bestOverlap = float.MaxValue;
                float bestScore = float.MinValue;
                for (int gridY = 0; gridY < CandidateGridSize; gridY++)
                {
                    float ty = (gridY + 0.5f) / CandidateGridSize;
                    float y = Mathf.Lerp(minY, maxY, ty);
                    for (int gridX = 0; gridX < CandidateGridSize; gridX++)
                    {
                        float tx = (gridX + 0.5f) / CandidateGridSize;
                        float x = Mathf.Lerp(minX, maxX, tx);
                        Vector2 candidate = new Vector2(x, y);
                        Rect candidateRect = RectFromCenter(candidate, region.Size, StartMargin);
                        float overlap = 0f;
                        float nearest = 1f;
                        for (int placedIndex = 0; placedIndex < placed.Count; placedIndex++)
                        {
                            overlap += OverlapArea(candidateRect, placed[placedIndex]);
                            nearest = Mathf.Min(nearest, Vector2.Distance(candidate, placed[placedIndex].center));
                        }
                        float targetDistance = Vector2.Distance(candidate, region.TargetCenter);
                        float wobble = Mathf.Repeat((gridX + 1) * 0.173f + (gridY + 1) * 0.317f + levelNumber * 0.071f + orderIndex * 0.113f, 1f) * 0.001f;
                        float score = targetDistance * 3f + nearest * 0.35f + wobble;
                        if (overlap < bestOverlap - 0.000001f || (Mathf.Abs(overlap - bestOverlap) <= 0.000001f && score > bestScore))
                        {
                            best = candidate;
                            bestOverlap = overlap;
                            bestScore = score;
                        }
                    }
                }
                region.StartingCenter = best;
                placed.Add(RectFromCenter(best, region.Size, StartMargin));
            }
        }

        private static Rect RectFromCenter(Vector2 center, Vector2 size, float padding)
        {
            Vector2 padded = size + Vector2.one * padding * 2f;
            return new Rect(center - padded * 0.5f, padded);
        }

        private static float OverlapArea(Rect left, Rect right)
        {
            float width = Mathf.Max(0f, Mathf.Min(left.xMax, right.xMax) - Mathf.Max(left.xMin, right.xMin));
            float height = Mathf.Max(0f, Mathf.Min(left.yMax, right.yMax) - Mathf.Max(left.yMin, right.yMin));
            return width * height;
        }

        private static int ClassifyPaletteColor(Color32 color, bool extendedPalette)
        {
            Color.RGBToHSV((Color)color, out float hue, out float saturation, out float value);
            if (value < 0.42f) return -1;
            if (saturation < 0.16f) return extendedPalette && value > 0.72f ? 7 : -1;
            if (extendedPalette) return NearestPaletteByRgb(color, true);
            if (hue < 0.075f || hue >= 0.94f) return 0;
            if (hue >= 0.47f && hue < 0.72f) return 1;
            if (hue >= 0.18f && hue < 0.47f) return 2;
            if (hue >= 0.075f && hue < 0.18f) return 3;
            return NearestPaletteByRgb(color, false);
        }

        private static int NearestPaletteByRgb(Color32 color, bool extendedPalette)
        {
            Color[] palette =
            {
                new Color(0.95f, 0.12f, 0.03f),
                new Color(0.02f, 0.48f, 0.78f),
                new Color(0.35f, 0.68f, 0.04f),
                new Color(1f, 0.62f, 0.02f),
                new Color(1f, 0.43f, 0.05f),
                new Color(0.60f, 0.36f, 0.80f),
                new Color(0.12f, 0.69f, 0.62f),
                new Color(0.97f, 0.95f, 0.89f)
            };
            Color sample = (Color)color;
            float best = float.MaxValue;
            int result = 0;
            int count = extendedPalette ? palette.Length : 4;
            for (int i = 0; i < count; i++)
            {
                float red = sample.r - palette[i].r;
                float green = sample.g - palette[i].g;
                float blue = sample.b - palette[i].b;
                float distance = red * red + green * green + blue * blue;
                if (distance < best)
                {
                    best = distance;
                    result = i;
                }
            }
            return result;
        }

        private static bool[] FloodExternalBackground(Color32[] pixels, int width, int height)
        {
            var background = new bool[pixels.Length];
            var queue = new Queue<int>();
            for (int x = 0; x < width; x++)
            {
                EnqueueBackground(x, 0, pixels, width, height, background, queue);
                EnqueueBackground(x, height - 1, pixels, width, height, background, queue);
            }
            for (int y = 1; y < height - 1; y++)
            {
                EnqueueBackground(0, y, pixels, width, height, background, queue);
                EnqueueBackground(width - 1, y, pixels, width, height, background, queue);
            }
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                EnqueueBackground(x - 1, y, pixels, width, height, background, queue);
                EnqueueBackground(x + 1, y, pixels, width, height, background, queue);
                EnqueueBackground(x, y - 1, pixels, width, height, background, queue);
                EnqueueBackground(x, y + 1, pixels, width, height, background, queue);
            }
            return background;
        }

        private static void EnqueueBackground(int x, int y, Color32[] pixels, int width, int height, bool[] background, Queue<int> queue)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (background[index]) return;
            Color32 color = pixels[index];
            int maximum = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (maximum > 108) return;
            background[index] = true;
            queue.Enqueue(index);
        }

        private static RectInt FindForegroundBounds(bool[] externalBackground, int width, int height)
        {
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (externalBackground[y * width + x]) continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            return maxX < minX || maxY < minY ? new RectInt() : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static RectInt FindLabelBounds(int[] labels, int width, int height, int label)
        {
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (labels[y * width + x] != label) continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            return maxX < minX || maxY < minY ? new RectInt() : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Color32[] BuildFragmentPixels(Color32[] composite, int[] labels, int canvasWidth, RectInt bounds, int label)
        {
            var pixels = new Color32[bounds.width * bounds.height];
            for (int y = 0; y < bounds.height; y++)
            {
                int sourceY = bounds.y + y;
                for (int x = 0; x < bounds.width; x++)
                {
                    int sourceX = bounds.x + x;
                    int sourceIndex = sourceY * canvasWidth + sourceX;
                    if (labels[sourceIndex] == label) pixels[y * bounds.width + x] = composite[sourceIndex];
                }
            }
            return pixels;
        }

        private static void DeleteOldLevelSprites(string levelFolder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { levelFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) AssetDatabase.DeleteAsset(path);
            }
        }

        private static void WriteTexture(string assetPath, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer is missing for " + assetPath + ".");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerCell;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            if (split <= 0) throw new InvalidOperationException("Invalid generated-art folder: " + path);
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
