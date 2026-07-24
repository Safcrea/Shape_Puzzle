using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Tests.ContentValidation
{
    public sealed class GeneratedPieceArtTests
    {
        private const string CatalogPath = "Assets/Game/Data/Levels/Generated/LevelPrefabCatalog.asset";
        private const string MaskRoot = "Assets/Game/Art/Generated/PieceMasks";

        [Test]
        public void CampaignPieceMasks_AreCompleteConnectedAndMatchGeneratedSprites()
        {
            LevelPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, "Generated level prefab catalog is missing.");
            var errors = new List<string>();

            for (int levelIndex = 0; levelIndex < catalog.Count; levelIndex++)
            {
                PuzzleLevelPrefab prefab = catalog.GetByIndex(levelIndex);
                if (prefab == null || prefab.Level == null)
                {
                    errors.Add("Catalog entry " + levelIndex + " is missing its prefab or level definition.");
                    continue;
                }

                int levelNumber = prefab.Level.levelNumber;
                string prefix = "Level " + levelNumber + ": ";
                string maskPath = MaskRoot + "/level_" + levelNumber.ToString("D3") + "_mask.png";
                Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
                if (mask == null)
                {
                    errors.Add(prefix + "piece mask is missing.");
                    continue;
                }

                Color32[] maskPixels = mask.GetPixels32();
                var countsByColor = new Dictionary<int, int>();
                int maskForeground = 0;
                for (int pixelIndex = 0; pixelIndex < maskPixels.Length; pixelIndex++)
                {
                    Color32 pixel = maskPixels[pixelIndex];
                    if (pixel.a == 0) continue;
                    int key = ToColorKey(pixel);
                    countsByColor.TryGetValue(key, out int count);
                    countsByColor[key] = count + 1;
                    maskForeground++;
                }

                int expectedPieces = prefab.Level.pieces == null ? 0 : prefab.Level.pieces.Length;
                if (countsByColor.Count != expectedPieces)
                    errors.Add(prefix + "mask has " + countsByColor.Count + " labels; expected " + expectedPieces + ".");
                if (prefab.PieceArtwork == null || prefab.PieceArtwork.Length != expectedPieces)
                    errors.Add(prefix + "has " + (prefab.PieceArtwork == null ? 0 : prefab.PieceArtwork.Length) +
                               " artwork entries; expected " + expectedPieces + ".");

                ValidateMaskConnectivity(maskPixels, mask.width, mask.height, countsByColor, prefix, errors);

                int spriteForeground = 0;
                if (prefab.PieceArtwork != null)
                {
                    for (int pieceIndex = 0; pieceIndex < prefab.PieceArtwork.Length; pieceIndex++)
                    {
                        PuzzlePieceArtwork artwork = prefab.PieceArtwork[pieceIndex];
                        if (artwork == null || artwork.sprite == null)
                        {
                            errors.Add(prefix + "piece " + pieceIndex + " has no generated sprite.");
                            continue;
                        }

                        Color32[] piecePixels = artwork.sprite.texture.GetPixels32();
                        for (int pixelIndex = 0; pixelIndex < piecePixels.Length; pixelIndex++)
                        {
                            Color32 pixel = piecePixels[pixelIndex];
                            if (pixel.a == 0) continue;
                            spriteForeground++;
                            Color.RGBToHSV((Color)pixel, out _, out float saturation, out float value);
                            if (saturation < 0.25f || value < 0.20f)
                            {
                                errors.Add(prefix + artwork.pieceId + " contains a neutral gray/white/black foreground pixel.");
                                pixelIndex = piecePixels.Length;
                            }
                        }
                    }
                }

                if (spriteForeground != maskForeground)
                    errors.Add(prefix + "mask coverage (" + maskForeground + ") does not match generated sprite coverage (" + spriteForeground + ").");
            }

            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
        }

        private static void ValidateMaskConnectivity(
            Color32[] pixels,
            int width,
            int height,
            Dictionary<int, int> expectedCounts,
            string prefix,
            List<string> errors)
        {
            var firstPixelByColor = new Dictionary<int, int>();
            for (int index = 0; index < pixels.Length; index++)
            {
                if (pixels[index].a == 0) continue;
                int key = ToColorKey(pixels[index]);
                if (!firstPixelByColor.ContainsKey(key)) firstPixelByColor.Add(key, index);
            }

            var visited = new bool[pixels.Length];
            var queue = new Queue<int>();
            foreach (KeyValuePair<int, int> pair in firstPixelByColor)
            {
                int connected = 0;
                queue.Enqueue(pair.Value);
                visited[pair.Value] = true;
                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    connected++;
                    int x = index % width;
                    int y = index / width;
                    EnqueueNeighbor(x - 1, y, pair.Key, pixels, visited, width, height, queue);
                    EnqueueNeighbor(x + 1, y, pair.Key, pixels, visited, width, height, queue);
                    EnqueueNeighbor(x, y - 1, pair.Key, pixels, visited, width, height, queue);
                    EnqueueNeighbor(x, y + 1, pair.Key, pixels, visited, width, height, queue);
                }

                if (connected != expectedCounts[pair.Key])
                    errors.Add(prefix + "mask label " + pair.Key + " contains detached islands.");
            }
        }

        private static void EnqueueNeighbor(
            int x,
            int y,
            int colorKey,
            Color32[] pixels,
            bool[] visited,
            int width,
            int height,
            Queue<int> queue)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (visited[index] || pixels[index].a == 0 || ToColorKey(pixels[index]) != colorKey) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        private static int ToColorKey(Color32 color)
        {
            return color.r | (color.g << 8) | (color.b << 16);
        }
    }
}
