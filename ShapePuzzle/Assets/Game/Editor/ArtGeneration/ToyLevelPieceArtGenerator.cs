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
        public const string MaskRoot = "Assets/Game/Art/Generated/PieceMasks";
        private const string PrefabCatalogPath = "Assets/Game/Data/Levels/Generated/LevelPrefabCatalog.asset";
        private const int PixelsPerCell = 128;
        private const int FirstGeneratedLevel = 1;
        private const float ArtworkFill = 0.70f;
        private const float StartMargin = 0.012f;
        private const int CandidateGridSize = 25;

        private static readonly string[] ColorNames = { "red", "blue", "green", "yellow", "orange", "purple", "teal", "cream" };
        private static readonly Color[] VibrantPalette =
        {
            new Color(0.95f, 0.12f, 0.03f),
            new Color(0.02f, 0.48f, 0.78f),
            new Color(0.35f, 0.68f, 0.04f),
            new Color(1f, 0.62f, 0.02f),
            new Color(1f, 0.43f, 0.05f),
            new Color(0.60f, 0.36f, 0.80f),
            new Color(0.12f, 0.69f, 0.62f)
        };

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

        [MenuItem("Tools/Toy Puzzle/Generate Campaign Separate Color Parts")]
        private static void GenerateFirstTenMenu()
        {
            GenerateFirstTen();
        }

        [MenuItem("Tools/Toy Puzzle/Regenerate Campaign Masks And Art")]
        private static void RegenerateCampaignMenu()
        {
            RegenerateCampaignMasksAndArt();
        }

        public static int GenerateFirstTen()
        {
            return GenerateCampaign(false);
        }

        public static int RegenerateCampaignMasksAndArt()
        {
            return GenerateCampaign(true);
        }

        private static int GenerateCampaign(bool forceRegenerateMasks)
        {
            EnsureFolder(OutputRoot);
            EnsureFolder(MaskRoot);
            LevelPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(PrefabCatalogPath);
            if (catalog == null) throw new InvalidOperationException("Level prefab catalog is missing at " + PrefabCatalogPath + ".");
            int lastGeneratedLevel = catalog.Count;

            var artworkByLevel = new Dictionary<int, PuzzlePieceArtwork[]>();
            var pieceIdsByLevel = new Dictionary<int, string[]>();
            int pieceCount = 0;
            for (int levelNumber = FirstGeneratedLevel; levelNumber <= lastGeneratedLevel; levelNumber++)
            {
                PuzzleLevelPrefab prefabData = catalog.FindByNumber(levelNumber);
                if (prefabData == null || prefabData.Level == null)
                    throw new InvalidOperationException("Level " + levelNumber + " is missing from the prefab catalog.");
                if (prefabData.Thumbnail == null)
                    throw new InvalidOperationException("Level " + levelNumber + " does not have a thumbnail sprite.");

                string thumbnailPath = AssetDatabase.GetAssetPath(prefabData.Thumbnail);
                PuzzlePieceArtwork[] artwork;
                try
                {
                    artwork = GenerateLevel(prefabData.Level, thumbnailPath, forceRegenerateMasks);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        "Level " + levelNumber + " (" + prefabData.Level.targetObjectName + ") art generation failed: " + exception.Message,
                        exception);
                }
                artworkByLevel.Add(levelNumber, artwork);
                var ids = new string[artwork.Length];
                for (int i = 0; i < artwork.Length; i++) ids[i] = artwork[i].pieceId;
                pieceIdsByLevel.Add(levelNumber, ids);
                pieceCount += artwork.Length;
            }

            FourColorBlockLevelConverter.ConvertAndRebuild(pieceIdsByLevel);
            catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(PrefabCatalogPath);
            if (catalog == null) throw new InvalidOperationException("Level prefab catalog disappeared after rebuilding separated-part definitions.");

            for (int levelNumber = FirstGeneratedLevel; levelNumber <= lastGeneratedLevel; levelNumber++)
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
            Debug.Log("Generated and assigned " + pieceCount + " separate molded-color gameplay parts across " + lastGeneratedLevel + " levels.");
            return pieceCount;
        }

        private static PuzzlePieceArtwork[] GenerateLevel(LevelDefinition level, string thumbnailPath, bool forceRegenerateMask)
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
                RecoverForegroundEdges(sourcePixels, externalBackground, source.width, source.height);
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

                int desiredPieces = level.pieces == null ? GetPieceCap(level.levelNumber) : level.pieces.Length;
                string maskPath = MaskRoot + "/level_" + level.levelNumber.ToString("D3") + "_mask.png";
                int[] labels;
                List<PartRegion> regions;
                if (!forceRegenerateMask && TryLoadPieceMask(maskPath, composite, canvasWidth, canvasHeight, out labels))
                {
                    regions = RebuildRegions(composite, seedOwners, labels, canvasWidth, canvasHeight);
                    SortRegionsAndRelabel(regions, labels);
                }
                else
                {
                    int[] separatedSeeds = ErodePaletteSeeds(seedOwners, canvasWidth, canvasHeight, 4);
                    regions = FindSeedRegions(separatedSeeds, canvasWidth, canvasHeight, foregroundPixels);
                    if (regions.Count == 0) throw new InvalidOperationException("No colored part regions were detected in " + thumbnailPath + ".");
                    labels = GrowRegionsAcrossArtwork(composite, seedOwners, regions, canvasWidth, canvasHeight);
                    labels = NormalizeRegionCount(composite, seedOwners, labels, ref regions, canvasWidth, canvasHeight, desiredPieces);
                    SavePieceMask(maskPath, labels, canvasWidth, canvasHeight);
                }

                ValidateLabels(composite, labels, regions, canvasWidth, canvasHeight, desiredPieces, level.levelNumber);
                if (regions.Count != desiredPieces)
                {
                    throw new InvalidOperationException("Level " + level.levelNumber + " needs " + desiredPieces +
                                                        " visible parts, but the mask produced " + regions.Count + ".");
                }

                float[] regionHues = ApplyVibrantNeutralColors(composite, labels, regions, level.levelNumber);
                WriteRecoloredThumbnail(
                    thumbnailPath,
                    source.width,
                    source.height,
                    sourcePixels,
                    externalBackground,
                    sourceBounds,
                    contentX,
                    contentY,
                    contentWidth,
                    contentHeight,
                    labels,
                    canvasWidth,
                    canvasHeight,
                    regionHues);
                AssignStableIds(regions);
                if (regions.Count > 16 || regions.Count * 2 > level.boardWidth * level.boardHeight)
                    throw new InvalidOperationException("Level " + level.levelNumber + " produced " + regions.Count + " playable regions, exceeding its gameplay capacity.");
                string levelFolder = OutputRoot + "/Level_" + level.levelNumber.ToString("D3");
                EnsureFolder(levelFolder);
                var expectedSpritePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
                {
                    PartRegion region = regions[regionIndex];
                    region.Bounds = FindLabelBounds(labels, canvasWidth, canvasHeight, regionIndex);
                    if (region.Bounds.width <= 0 || region.Bounds.height <= 0)
                        throw new InvalidOperationException("Generated region " + region.PieceId + " has no pixels.");
                    Color32[] pixels = BuildFragmentPixels(composite, labels, canvasWidth, region.Bounds, regionIndex);
                    string spritePath = levelFolder + "/level_" + level.levelNumber.ToString("D3") + "_" + region.PieceId + ".png";
                    expectedSpritePaths.Add(spritePath);
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
                DeleteObsoleteLevelSprites(levelFolder, expectedSpritePaths);
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

        private static int[] NormalizeRegionCount(
            Color32[] composite,
            int[] seedOwners,
            int[] labels,
            ref List<PartRegion> regions,
            int width,
            int height,
            int desiredPieces)
        {
            FillUnlabeledForeground(composite, labels, width, height);
            regions = RebuildRegions(composite, seedOwners, labels, width, height);
            if (regions.Count > desiredPieces)
            {
                MergeAdjacentRegions(labels, regions, width, height, desiredPieces);
                regions = RebuildRegions(composite, seedOwners, labels, width, height);
            }

            while (regions.Count < desiredPieces)
            {
                if (!TrySplitLargestRegion(labels, regions, width, height))
                    throw new InvalidOperationException("Could not create " + desiredPieces + " connected puzzle pieces from the reference artwork.");
                regions = RebuildRegions(composite, seedOwners, labels, width, height);
            }

            SortRegionsAndRelabel(regions, labels);
            return labels;
        }

        private static void FillUnlabeledForeground(Color32[] composite, int[] labels, int width, int height)
        {
            int nextLabel = 0;
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] >= nextLabel) nextLabel = labels[i] + 1;

            var visited = new bool[labels.Length];
            var queue = new Queue<int>();
            var component = new List<int>();
            var contacts = new Dictionary<int, int>();
            for (int start = 0; start < labels.Length; start++)
            {
                if (composite[start].a == 0 || labels[start] >= 0 || visited[start]) continue;
                component.Clear();
                contacts.Clear();
                visited[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    component.Add(index);
                    int x = index % width;
                    int y = index / width;
                    InspectUnlabeledNeighbor(x - 1, y, composite, labels, visited, width, height, queue, contacts);
                    InspectUnlabeledNeighbor(x + 1, y, composite, labels, visited, width, height, queue, contacts);
                    InspectUnlabeledNeighbor(x, y - 1, composite, labels, visited, width, height, queue, contacts);
                    InspectUnlabeledNeighbor(x, y + 1, composite, labels, visited, width, height, queue, contacts);
                }

                int destination = -1;
                int strongestContact = -1;
                foreach (KeyValuePair<int, int> pair in contacts)
                {
                    if (pair.Value <= strongestContact) continue;
                    destination = pair.Key;
                    strongestContact = pair.Value;
                }
                if (destination < 0) destination = nextLabel++;
                for (int i = 0; i < component.Count; i++) labels[component[i]] = destination;
            }
        }

        private static void InspectUnlabeledNeighbor(
            int x,
            int y,
            Color32[] composite,
            int[] labels,
            bool[] visited,
            int width,
            int height,
            Queue<int> queue,
            Dictionary<int, int> contacts)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (composite[index].a == 0) return;
            if (labels[index] >= 0)
            {
                contacts.TryGetValue(labels[index], out int count);
                contacts[labels[index]] = count + 1;
                return;
            }
            if (visited[index]) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        private static void MergeAdjacentRegions(int[] labels, List<PartRegion> regions, int width, int height, int desiredPieces)
        {
            int count = regions.Count;
            var areas = new int[count];
            var sumX = new long[count];
            var sumY = new long[count];
            var shared = new int[count, count];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int label = labels[index];
                    if (label < 0) continue;
                    areas[label]++;
                    sumX[label] += x;
                    sumY[label] += y;
                    if (x + 1 < width) AddSharedBoundary(label, labels[index + 1], shared);
                    if (y + 1 < height) AddSharedBoundary(label, labels[index + width], shared);
                }
            }

            var active = new bool[count];
            var parent = new int[count];
            for (int i = 0; i < count; i++)
            {
                active[i] = true;
                parent[i] = i;
            }

            int activeCount = count;
            while (activeCount > desiredPieces)
            {
                int bestA = -1;
                int bestB = -1;
                float bestScore = float.MaxValue;
                for (int a = 0; a < count; a++)
                {
                    if (!active[a]) continue;
                    for (int b = a + 1; b < count; b++)
                    {
                        if (!active[b] || shared[a, b] <= 0) continue;
                        float ax = areas[a] == 0 ? 0f : (float)sumX[a] / areas[a];
                        float ay = areas[a] == 0 ? 0f : (float)sumY[a] / areas[a];
                        float bx = areas[b] == 0 ? 0f : (float)sumX[b] / areas[b];
                        float by = areas[b] == 0 ? 0f : (float)sumY[b] / areas[b];
                        float distance = (ax - bx) * (ax - bx) + (ay - by) * (ay - by);
                        float colorFactor = regions[a].ColorIndex == regions[b].ColorIndex ? 0.58f : 1f;
                        float score = Mathf.Min(areas[a], areas[b]) * colorFactor / (shared[a, b] + 1f) + distance * 0.0005f;
                        if (score >= bestScore) continue;
                        bestScore = score;
                        bestA = a;
                        bestB = b;
                    }
                }

                if (bestA < 0)
                    throw new InvalidOperationException("The reference contains more disconnected artwork islands than the configured puzzle piece count.");

                int keep = areas[bestA] >= areas[bestB] ? bestA : bestB;
                int remove = keep == bestA ? bestB : bestA;
                parent[remove] = keep;
                active[remove] = false;
                areas[keep] += areas[remove];
                sumX[keep] += sumX[remove];
                sumY[keep] += sumY[remove];
                for (int other = 0; other < count; other++)
                {
                    if (!active[other] || other == keep) continue;
                    shared[keep, other] += shared[remove, other];
                    shared[other, keep] = shared[keep, other];
                }
                activeCount--;
            }

            var compact = new Dictionary<int, int>();
            for (int i = 0; i < labels.Length; i++)
            {
                int label = labels[i];
                if (label < 0) continue;
                while (parent[label] != label) label = parent[label];
                if (!compact.TryGetValue(label, out int compactLabel))
                {
                    compactLabel = compact.Count;
                    compact.Add(label, compactLabel);
                }
                labels[i] = compactLabel;
            }
        }

        private static void AddSharedBoundary(int left, int right, int[,] shared)
        {
            if (left < 0 || right < 0 || left == right) return;
            shared[left, right]++;
            shared[right, left]++;
        }

        private static bool TrySplitLargestRegion(int[] labels, List<PartRegion> regions, int width, int height)
        {
            var candidates = new List<int>(regions.Count);
            var areas = new int[regions.Count];
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] >= 0) areas[labels[i]]++;
            for (int i = 0; i < regions.Count; i++) candidates.Add(i);
            candidates.Sort((left, right) => areas[right].CompareTo(areas[left]));
            for (int i = 0; i < candidates.Count; i++)
            {
                int label = candidates[i];
                if (areas[label] < 256) continue;
                if (SplitRegionGeodesically(labels, label, regions.Count, width, height, areas[label])) return true;
            }
            return false;
        }

        private static bool SplitRegionGeodesically(int[] labels, int label, int newLabel, int width, int height, int area)
        {
            int start = -1;
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != label) continue;
                start = i;
                break;
            }
            if (start < 0) return false;

            int first = FindFarthestPixel(labels, label, start, width, height);
            int second = FindFarthestPixel(labels, label, first, width, height);
            if (first == second) return false;

            var owner = new sbyte[labels.Length];
            for (int i = 0; i < owner.Length; i++) owner[i] = -1;
            var queue = new Queue<int>();
            owner[first] = 0;
            owner[second] = 1;
            queue.Enqueue(first);
            queue.Enqueue(second);
            int firstCount = 1;
            int secondCount = 1;
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                GrowSplitOwner(x - 1, y, label, owner[index], labels, owner, width, height, queue, ref firstCount, ref secondCount);
                GrowSplitOwner(x + 1, y, label, owner[index], labels, owner, width, height, queue, ref firstCount, ref secondCount);
                GrowSplitOwner(x, y - 1, label, owner[index], labels, owner, width, height, queue, ref firstCount, ref secondCount);
                GrowSplitOwner(x, y + 1, label, owner[index], labels, owner, width, height, queue, ref firstCount, ref secondCount);
            }

            int minimumArea = Mathf.Max(64, area / 20);
            if (firstCount < minimumArea || secondCount < minimumArea) return false;
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] == label && owner[i] == 1) labels[i] = newLabel;
            return true;
        }

        private static int FindFarthestPixel(int[] labels, int label, int start, int width, int height)
        {
            var distances = new int[labels.Length];
            for (int i = 0; i < distances.Length; i++) distances[i] = -1;
            var queue = new Queue<int>();
            distances[start] = 0;
            queue.Enqueue(start);
            int farthest = start;
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                if (distances[index] > distances[farthest]) farthest = index;
                int x = index % width;
                int y = index / width;
                EnqueueDistanceNeighbor(x - 1, y, label, labels, distances, width, height, queue, distances[index] + 1);
                EnqueueDistanceNeighbor(x + 1, y, label, labels, distances, width, height, queue, distances[index] + 1);
                EnqueueDistanceNeighbor(x, y - 1, label, labels, distances, width, height, queue, distances[index] + 1);
                EnqueueDistanceNeighbor(x, y + 1, label, labels, distances, width, height, queue, distances[index] + 1);
            }
            return farthest;
        }

        private static void EnqueueDistanceNeighbor(
            int x,
            int y,
            int label,
            int[] labels,
            int[] distances,
            int width,
            int height,
            Queue<int> queue,
            int distance)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (labels[index] != label || distances[index] >= 0) return;
            distances[index] = distance;
            queue.Enqueue(index);
        }

        private static void GrowSplitOwner(
            int x,
            int y,
            int label,
            int source,
            int[] labels,
            sbyte[] owner,
            int width,
            int height,
            Queue<int> queue,
            ref int firstCount,
            ref int secondCount)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (labels[index] != label || owner[index] >= 0) return;
            owner[index] = (sbyte)source;
            if (source == 0) firstCount++;
            else secondCount++;
            queue.Enqueue(index);
        }

        private static List<PartRegion> RebuildRegions(Color32[] composite, int[] seedOwners, int[] labels, int width, int height)
        {
            int count = 0;
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] >= count) count = labels[i] + 1;
            var bounds = new RectInt[count];
            var minX = new int[count];
            var minY = new int[count];
            var maxX = new int[count];
            var maxY = new int[count];
            var paletteCounts = new int[count, ColorNames.Length];
            for (int i = 0; i < count; i++)
            {
                minX[i] = width;
                minY[i] = height;
                maxX[i] = -1;
                maxY[i] = -1;
            }

            for (int index = 0; index < labels.Length; index++)
            {
                int label = labels[index];
                if (label < 0) continue;
                int x = index % width;
                int y = index / width;
                minX[label] = Mathf.Min(minX[label], x);
                minY[label] = Mathf.Min(minY[label], y);
                maxX[label] = Mathf.Max(maxX[label], x);
                maxY[label] = Mathf.Max(maxY[label], y);
                int palette = seedOwners[index];
                if (palette >= 0 && palette < ColorNames.Length) paletteCounts[label, palette]++;
            }

            var result = new List<PartRegion>(count);
            for (int label = 0; label < count; label++)
            {
                if (maxX[label] < minX[label]) throw new InvalidOperationException("Piece mask contains an empty label " + label + ".");
                bounds[label] = new RectInt(minX[label], minY[label], maxX[label] - minX[label] + 1, maxY[label] - minY[label] + 1);
                int dominant = 0;
                int strongest = -1;
                for (int palette = 0; palette < ColorNames.Length; palette++)
                {
                    if (paletteCounts[label, palette] <= strongest) continue;
                    strongest = paletteCounts[label, palette];
                    dominant = palette;
                }
                if (strongest <= 0 || dominant == ColorNames.Length - 1) dominant = label % VibrantPalette.Length;
                result.Add(new PartRegion
                {
                    ColorIndex = dominant,
                    SeedBounds = bounds[label],
                    Bounds = bounds[label]
                });
            }
            return result;
        }

        private static void SortRegionsAndRelabel(List<PartRegion> regions, int[] labels)
        {
            var order = new List<int>(regions.Count);
            for (int i = 0; i < regions.Count; i++) order.Add(i);
            order.Sort((left, right) =>
            {
                int color = regions[left].ColorIndex.CompareTo(regions[right].ColorIndex);
                if (color != 0) return color;
                int horizontal = regions[left].Bounds.center.x.CompareTo(regions[right].Bounds.center.x);
                return horizontal != 0 ? horizontal : regions[left].Bounds.center.y.CompareTo(regions[right].Bounds.center.y);
            });
            var remap = new int[regions.Count];
            var sorted = new List<PartRegion>(regions.Count);
            for (int newIndex = 0; newIndex < order.Count; newIndex++)
            {
                remap[order[newIndex]] = newIndex;
                sorted.Add(regions[order[newIndex]]);
            }
            for (int i = 0; i < labels.Length; i++)
                if (labels[i] >= 0) labels[i] = remap[labels[i]];
            regions.Clear();
            regions.AddRange(sorted);
        }

        private static bool TryLoadPieceMask(string maskPath, Color32[] composite, int width, int height, out int[] labels)
        {
            labels = null;
            string fullPath = Path.GetFullPath(maskPath);
            if (!File.Exists(fullPath)) return false;

            var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!mask.LoadImage(File.ReadAllBytes(fullPath), false))
                    throw new InvalidOperationException("Could not decode piece mask " + maskPath + ".");
                if (mask.width != width || mask.height != height)
                    throw new InvalidOperationException("Piece mask dimensions do not match the level canvas: " + maskPath + ".");

                Color32[] pixels = mask.GetPixels32();
                var colorKeys = new List<int>();
                var unique = new HashSet<int>();
                for (int i = 0; i < pixels.Length; i++)
                {
                    bool artwork = composite[i].a > 0;
                    bool masked = pixels[i].a > 0;
                    if (artwork != masked)
                        throw new InvalidOperationException("Piece mask coverage does not match the reference artwork at pixel " + i + ": " + maskPath + ".");
                    if (!masked) continue;
                    int key = pixels[i].r | (pixels[i].g << 8) | (pixels[i].b << 16);
                    if (unique.Add(key)) colorKeys.Add(key);
                }

                colorKeys.Sort();
                var keyToLabel = new Dictionary<int, int>(colorKeys.Count);
                for (int i = 0; i < colorKeys.Count; i++) keyToLabel.Add(colorKeys[i], i);
                labels = new int[pixels.Length];
                for (int i = 0; i < labels.Length; i++)
                {
                    if (pixels[i].a == 0)
                    {
                        labels[i] = -1;
                        continue;
                    }
                    int key = pixels[i].r | (pixels[i].g << 8) | (pixels[i].b << 16);
                    labels[i] = keyToLabel[key];
                }
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mask);
            }
        }

        private static void SavePieceMask(string maskPath, int[] labels, int width, int height)
        {
            var pixels = new Color32[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int label = labels[i];
                if (label < 0) continue;
                int encoded = label + 1;
                pixels[i] = new Color32(
                    (byte)(encoded & 255),
                    (byte)((encoded >> 8) & 255),
                    (byte)((encoded >> 16) & 255),
                    255);
            }
            bool changed = WriteTextureBytesIfChanged(maskPath, width, height, pixels);
            if (changed || AssetImporter.GetAtPath(maskPath) == null) ConfigureMaskImporter(maskPath);
        }

        private static void ValidateLabels(
            Color32[] composite,
            int[] labels,
            List<PartRegion> regions,
            int width,
            int height,
            int expectedPieces,
            int levelNumber)
        {
            if (regions.Count != expectedPieces)
                throw new InvalidOperationException("Level " + levelNumber + " mask has " + regions.Count + " pieces; expected " + expectedPieces + ".");

            var counts = new int[regions.Count];
            var first = new int[regions.Count];
            for (int i = 0; i < first.Length; i++) first[i] = -1;
            int foreground = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                bool artwork = composite[i].a > 0;
                int label = labels[i];
                if (!artwork && label >= 0)
                    throw new InvalidOperationException("Level " + levelNumber + " mask assigns a background pixel.");
                if (artwork && (label < 0 || label >= regions.Count))
                    throw new InvalidOperationException("Level " + levelNumber + " mask leaves foreground artwork unassigned.");
                if (!artwork) continue;
                foreground++;
                counts[label]++;
                if (first[label] < 0) first[label] = i;
            }

            int minimumArea = Mathf.Max(64, foreground / 12000);
            var visited = new bool[labels.Length];
            var queue = new Queue<int>();
            for (int label = 0; label < regions.Count; label++)
            {
                if (counts[label] < minimumArea)
                    throw new InvalidOperationException("Level " + levelNumber + " piece " + label + " is too small (" + counts[label] + " pixels).");
                int connected = 0;
                queue.Enqueue(first[label]);
                visited[first[label]] = true;
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    connected++;
                    int x = index % width;
                    int y = index / width;
                    EnqueueLabelNeighbor(x - 1, y, label, labels, visited, width, height, queue);
                    EnqueueLabelNeighbor(x + 1, y, label, labels, visited, width, height, queue);
                    EnqueueLabelNeighbor(x, y - 1, label, labels, visited, width, height, queue);
                    EnqueueLabelNeighbor(x, y + 1, label, labels, visited, width, height, queue);
                }
                if (connected != counts[label])
                    throw new InvalidOperationException("Level " + levelNumber + " piece " + label + " contains detached artwork islands.");
            }
        }

        private static void EnqueueLabelNeighbor(
            int x,
            int y,
            int label,
            int[] labels,
            bool[] visited,
            int width,
            int height,
            Queue<int> queue)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (visited[index] || labels[index] != label) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        private static float[] ApplyVibrantNeutralColors(Color32[] composite, int[] labels, List<PartRegion> regions, int levelNumber)
        {
            var cosine = new double[regions.Count];
            var sine = new double[regions.Count];
            var weights = new double[regions.Count];
            for (int i = 0; i < composite.Length; i++)
            {
                int label = labels[i];
                if (label < 0) continue;
                Color.RGBToHSV((Color)composite[i], out float hue, out float saturation, out float value);
                if (saturation < 0.28f || value < 0.22f) continue;
                double weight = saturation * value;
                double angle = hue * Math.PI * 2.0;
                cosine[label] += Math.Cos(angle) * weight;
                sine[label] += Math.Sin(angle) * weight;
                weights[label] += weight;
            }

            var regionHues = new float[regions.Count];
            for (int label = 0; label < regions.Count; label++)
            {
                if (weights[label] > 0.001)
                {
                    double angle = Math.Atan2(sine[label], cosine[label]);
                    if (angle < 0.0) angle += Math.PI * 2.0;
                    regionHues[label] = (float)(angle / (Math.PI * 2.0));
                }
                else
                {
                    Color.RGBToHSV(VibrantPalette[(levelNumber + label) % VibrantPalette.Length], out regionHues[label], out _, out _);
                }
            }

            for (int i = 0; i < composite.Length; i++)
            {
                int label = labels[i];
                if (label >= 0) composite[i] = RecolorNeutralPixel(composite[i], regionHues[label]);
            }
            return regionHues;
        }

        private static Color32 RecolorNeutralPixel(Color32 source, float replacementHue)
        {
            Color.RGBToHSV((Color)source, out float hue, out float saturation, out float value);
            if (saturation >= 0.28f && value >= 0.22f) return source;
            float recoloredSaturation = Mathf.Max(saturation, value > 0.82f ? 0.46f : 0.62f);
            float recoloredValue = Mathf.Clamp(value, 0.30f, 1f);
            Color result = Color.HSVToRGB(replacementHue, recoloredSaturation, recoloredValue);
            result.a = source.a / 255f;
            return (Color32)result;
        }

        private static void WriteRecoloredThumbnail(
            string thumbnailPath,
            int sourceWidth,
            int sourceHeight,
            Color32[] sourcePixels,
            bool[] externalBackground,
            RectInt sourceBounds,
            float contentX,
            float contentY,
            float contentWidth,
            float contentHeight,
            int[] labels,
            int canvasWidth,
            int canvasHeight,
            float[] regionHues)
        {
            var recolored = new Color32[sourcePixels.Length];
            Array.Copy(sourcePixels, recolored, sourcePixels.Length);
            for (int y = sourceBounds.yMin; y < sourceBounds.yMax; y++)
            {
                for (int x = sourceBounds.xMin; x < sourceBounds.xMax; x++)
                {
                    int sourceIndex = y * sourceWidth + x;
                    if (externalBackground[sourceIndex]) continue;
                    float normalizedX = (x + 0.5f - sourceBounds.xMin) / sourceBounds.width;
                    float normalizedY = (y + 0.5f - sourceBounds.yMin) / sourceBounds.height;
                    int canvasX = Mathf.Clamp(Mathf.FloorToInt(contentX + normalizedX * contentWidth), 0, canvasWidth - 1);
                    int canvasY = Mathf.Clamp(Mathf.FloorToInt(contentY + normalizedY * contentHeight), 0, canvasHeight - 1);
                    int label = FindNearestLabel(labels, canvasX, canvasY, canvasWidth, canvasHeight);
                    if (label < 0 || label >= regionHues.Length) continue;
                    recolored[sourceIndex] = RecolorNeutralPixel(recolored[sourceIndex], regionHues[label]);
                }
            }
            if (WriteTextureBytesIfChanged(thumbnailPath, sourceWidth, sourceHeight, recolored))
                AssetDatabase.ImportAsset(thumbnailPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static int FindNearestLabel(int[] labels, int x, int y, int width, int height)
        {
            int direct = labels[y * width + x];
            if (direct >= 0) return direct;
            for (int radius = 1; radius <= 5; radius++)
            {
                int minX = Mathf.Max(0, x - radius);
                int maxX = Mathf.Min(width - 1, x + radius);
                int minY = Mathf.Max(0, y - radius);
                int maxY = Mathf.Min(height - 1, y + radius);
                for (int sampleX = minX; sampleX <= maxX; sampleX++)
                {
                    int bottom = labels[minY * width + sampleX];
                    if (bottom >= 0) return bottom;
                    int top = labels[maxY * width + sampleX];
                    if (top >= 0) return top;
                }
                for (int sampleY = minY + 1; sampleY < maxY; sampleY++)
                {
                    int left = labels[sampleY * width + minX];
                    if (left >= 0) return left;
                    int right = labels[sampleY * width + maxX];
                    if (right >= 0) return right;
                }
            }
            return -1;
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

        private static void RecoverForegroundEdges(Color32[] pixels, bool[] externalBackground, int width, int height)
        {
            int radius = Mathf.Clamp(Mathf.Min(width, height) / 80, 6, 16);
            var recovered = new bool[externalBackground.Length];
            for (int y = radius; y < height - radius; y++)
            {
                for (int x = radius; x < width - radius; x++)
                {
                    int index = y * width + x;
                    if (!externalBackground[index] || pixels[index].a == 0) continue;
                    bool nearArtwork = false;
                    for (int dy = -radius; dy <= radius && !nearArtwork; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            if (dx * dx + dy * dy > radius * radius) continue;
                            int sample = (y + dy) * width + x + dx;
                            if (!externalBackground[sample])
                            {
                                nearArtwork = true;
                                break;
                            }
                        }
                    }
                    recovered[index] = nearArtwork;
                }
            }

            for (int i = 0; i < externalBackground.Length; i++)
                if (recovered[i]) externalBackground[i] = false;
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

        private static void DeleteObsoleteLevelSprites(string levelFolder, HashSet<string> expectedPaths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { levelFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !expectedPaths.Contains(path))
                    AssetDatabase.DeleteAsset(path);
            }
        }

        private static void WriteTexture(string assetPath, int width, int height, Color32[] pixels)
        {
            bool changed = WriteTextureBytesIfChanged(assetPath, width, height, pixels);
            if (changed || AssetImporter.GetAtPath(assetPath) == null)
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static bool WriteTextureBytesIfChanged(string assetPath, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            byte[] encoded;
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                encoded = texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            string fullPath = Path.GetFullPath(assetPath);
            if (File.Exists(fullPath))
            {
                byte[] existing = File.ReadAllBytes(fullPath);
                if (ByteArraysEqual(existing, encoded)) return false;
            }
            File.WriteAllBytes(fullPath, encoded);
            return true;
        }

        private static bool ByteArraysEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer is missing for " + assetPath + ".");
            var currentSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(currentSettings);
            bool needsUpdate =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerCell) ||
                currentSettings.spriteMeshType != SpriteMeshType.FullRect ||
                importer.mipmapEnabled ||
                !importer.isReadable ||
                !importer.alphaIsTransparency ||
                importer.wrapMode != TextureWrapMode.Clamp ||
                importer.filterMode != FilterMode.Bilinear ||
                importer.npotScale != TextureImporterNPOTScale.None ||
                importer.maxTextureSize != 1024 ||
                importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (!needsUpdate) return;
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

        private static void ConfigureMaskImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer is missing for " + assetPath + ".");
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
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
