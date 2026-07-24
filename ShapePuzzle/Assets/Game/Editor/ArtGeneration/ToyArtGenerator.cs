using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor
{
    public static class ToyArtGenerator
    {
        public const string GeneratedRoot = "Assets/Game/Art/Generated";
        public const string UiRoot = GeneratedRoot + "/UI";
        public const string PieceRoot = GeneratedRoot + "/Pieces";
        public const string EffectRoot = GeneratedRoot + "/Effects";
        public const string PalettePath = "Assets/Game/Data/Palettes/ToyPalette.asset";

        private const int Supersampling = 3;

        [MenuItem("Tools/Toy Puzzle/Generate Art Only", priority = 101)]
        public static void GenerateArtOnly()
        {
            EnsureFolders();
            ToyPalette palette = CreateOrUpdatePalette();
            GenerateEnvironment(palette);
            GenerateButtons(palette);
            GeneratePieceSprites(palette);
            GenerateIcons(palette);
            GenerateEffects(palette);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Toy Puzzle art generated deterministically under " + GeneratedRoot);
        }

        public static ToyPalette CreateOrUpdatePalette()
        {
            EnsureFolder("Assets/Game/Data");
            EnsureFolder("Assets/Game/Data/Palettes");
            ToyPalette palette = AssetDatabase.LoadAssetAtPath<ToyPalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<ToyPalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }
            palette.background = new Color32(0, 91, 164, 255);
            palette.backgroundSecondary = new Color32(0, 122, 205, 255);
            palette.boardFrame = new Color32(28, 30, 25, 255);
            palette.boardCell = new Color32(38, 41, 35, 255);
            palette.boardCellAlternate = new Color32(43, 46, 40, 255);
            palette.red = new Color32(242, 55, 19, 255);
            palette.yellow = new Color32(255, 186, 11, 255);
            palette.cyan = new Color32(0, 157, 221, 255);
            palette.green = new Color32(82, 190, 18, 255);
            palette.orange = new Color32(255, 110, 12, 255);
            palette.cream = new Color32(247, 242, 226, 255);
            palette.shadow = new Color32(0, 20, 43, 150);
            EditorUtility.SetDirty(palette);
            return palette;
        }

        public static void EnsureFolders()
        {
            EnsureFolder("Assets/Game/Art");
            EnsureFolder(GeneratedRoot);
            EnsureFolder(UiRoot);
            EnsureFolder(PieceRoot);
            EnsureFolder(EffectRoot);
        }

        private static void GenerateEnvironment(ToyPalette palette)
        {
            BakeBackground("background", UiRoot, 512, 512, palette.background, palette.backgroundSecondary);
            BakeRounded("panel_dark", UiRoot, 256, 192, 0.20f, palette.boardFrame, true, 38);
            BakeRounded("popup_cream", UiRoot, 256, 256, 0.18f, palette.cream, true, 36);
            BakeRounded("board_frame", UiRoot, 256, 256, 0.17f, palette.boardFrame, true, 46);
            BakeRounded("board_cell", UiRoot, 160, 160, 0.18f, palette.boardCell, true, 24);
            BakeRounded("reference_card", UiRoot, 256, 160, 0.18f, palette.boardFrame, true, 34);
        }

        private static void BakeBackground(string name, string folder, int width, int height, Color bottom, Color top)
        {
            WriteSprite(folder + "/" + name + ".png", width, height, (x, y) =>
            {
                Color baseColor = Color.Lerp(bottom, top, 0.18f + y * 0.62f);
                float radial = Vector2.Distance(new Vector2(x, y), new Vector2(0.50f, 0.58f));
                float vignette = Mathf.Lerp(1.04f, 0.82f, Mathf.SmoothStep(0.18f, 0.78f, radial));
                float moldedGrain = MoldedNoise(x, y);
                Color result = baseColor * (vignette + moldedGrain);
                result.a = 1f;
                return result;
            }, Vector4.zero);
        }

        private static void GenerateButtons(ToyPalette palette)
        {
            BakeRounded("button_blue", UiRoot, 192, 192, 0.25f, palette.cyan, true, 34);
            BakeRounded("button_red", UiRoot, 192, 192, 0.25f, palette.red, true, 34);
            BakeRounded("button_yellow", UiRoot, 192, 192, 0.25f, palette.yellow, true, 34);
            BakeRounded("button_green", UiRoot, 192, 192, 0.25f, palette.green, true, 34);
            BakeRounded("button_dark", UiRoot, 192, 192, 0.25f, palette.boardFrame, true, 34);
        }

        private static void GeneratePieceSprites(ToyPalette palette)
        {
            Color[] colors = { palette.red, palette.yellow, palette.cyan, palette.green, palette.orange, palette.cream };
            string[] names = { "red", "yellow", "cyan", "green", "orange", "cream" };
            for (int i = 0; i < colors.Length; i++)
            {
                BakeRounded("piece_rect_" + names[i], PieceRoot, 256, 160, 0.18f, colors[i], true, 28);
                BakeCircle("piece_circle_" + names[i], PieceRoot, 192, colors[i]);
            }
            BakeRing("piece_ring_green", PieceRoot, 256, palette.green);
            BakeTriangle("piece_triangle_yellow", PieceRoot, 256, palette.yellow);
            BakeRounded("piece_rounded_neutral", PieceRoot, 256, 160, 0.18f, Color.white, true, 28);
            BakeCapsule("piece_capsule_neutral", PieceRoot, 256, 160, Color.white);
            BakeCircle("piece_circle_neutral", PieceRoot, 192, Color.white);
            BakeRing("piece_ring_neutral", PieceRoot, 256, Color.white);
            BakeTriangle("piece_triangle_neutral", PieceRoot, 256, Color.white);
            BakePolygon("piece_trapezoid_neutral", PieceRoot, 256, 256, Color.white, new[]
            {
                new Vector2(0.25f, 0.86f), new Vector2(0.75f, 0.86f),
                new Vector2(0.91f, 0.14f), new Vector2(0.09f, 0.14f)
            });
            BakePolygon("piece_wedge_neutral", PieceRoot, 256, 256, Color.white, new[]
            {
                new Vector2(0.10f, 0.14f), new Vector2(0.90f, 0.14f), new Vector2(0.90f, 0.86f)
            });
            BakeSemicircle("piece_semicircle_neutral", PieceRoot, 256, Color.white);
            BakeQuarterCircle("piece_quarter_circle_neutral", PieceRoot, 256, Color.white);
            BakeStud("piece_stud_neutral", PieceRoot, 128);
            BakeRecessedHole("piece_hole_neutral", PieceRoot, 128);
            BakeInsetPanel("piece_inset_neutral", PieceRoot, 192, 128);
        }

        private static void GenerateIcons(ToyPalette palette)
        {
            IconKind[] kinds = (IconKind[])Enum.GetValues(typeof(IconKind));
            for (int i = 0; i < kinds.Length; i++) BakeIcon(kinds[i], palette.cream);
        }

        private static void GenerateEffects(ToyPalette palette)
        {
            BakeStar("star", EffectRoot, 128, palette.yellow, 5);
            BakeStar("sparkle", EffectRoot, 128, palette.cream, 4);
            BakeRing("highlight_ring", EffectRoot, 160, palette.yellow);
            BakeRounded("confetti", EffectRoot, 64, 32, 0.22f, palette.red, false, 0);
        }

        private static void BakeRounded(string name, string folder, int width, int height, float radius, Color color, bool sliced, int border)
        {
            WriteSprite(folder + "/" + name + ".png", width, height, (x, y) =>
            {
                Vector2 p = new Vector2(x * 2f - 1f, y * 2f - 1f);
                float aspect = width / (float)height;
                p.x *= aspect;
                Vector2 half = new Vector2(aspect, 1f);
                float roundedRadius = Mathf.Max(0.001f, radius * 2f);
                float distance = RoundedBoxDistance(p, half - Vector2.one * roundedRadius, roundedRadius);
                float alpha = Mathf.Clamp01(0.5f - distance * Mathf.Min(width, height) * 0.5f);
                if (alpha <= 0f) return Color.clear;
                float edge = Mathf.Clamp01(-distance / 0.22f);
                float light = 0.82f + 0.14f * edge + 0.055f * (y - x);
                float texture = MoldedNoise(x, y) * 0.72f;
                Color result = color * (light + texture);
                result.a = color.a * alpha;
                return result;
            }, sliced ? new Vector4(border, border, border, border) : Vector4.zero);
        }

        private static void BakeCircle(string name, string folder, int size, Color color)
        {
            WriteSprite(folder + "/" + name + ".png", size, size, (x, y) =>
            {
                Vector2 p = new Vector2(x - 0.5f, y - 0.5f);
                float distance = p.magnitude - 0.445f;
                float alpha = Mathf.Clamp01(0.5f - distance * size);
                if (alpha <= 0f) return Color.clear;
                float edge = Mathf.Clamp01(-distance / 0.11f);
                float light = 0.82f + edge * 0.14f + (y - x) * 0.06f;
                Color result = color * light;
                result.a = alpha;
                return result;
            }, Vector4.zero);
        }

        private static void BakeCapsule(string name, string folder, int width, int height, Color color)
        {
            WriteSprite(folder + "/" + name + ".png", width, height, (x, y) =>
            {
                float aspect = width / (float)height;
                Vector2 p = new Vector2(x * aspect, y);
                float radius = 0.40f;
                float distance = DistanceToSegment(p, new Vector2(0.43f, 0.5f), new Vector2(aspect - 0.43f, 0.5f)) - radius;
                return MoldedColor(color, x, y, distance, Mathf.Min(width, height));
            }, Vector4.zero);
        }

        private static void BakeRing(string name, string folder, int size, Color color)
        {
            WriteSprite(folder + "/" + name + ".png", size, size, (x, y) =>
            {
                float radius = new Vector2(x - 0.5f, y - 0.5f).magnitude;
                float distance = Mathf.Abs(radius - 0.34f) - 0.11f;
                float alpha = Mathf.Clamp01(0.5f - distance * size);
                if (alpha <= 0f) return Color.clear;
                Color result = color * (0.88f + Mathf.Clamp01(-distance / 0.08f) * 0.11f + (y - x) * 0.04f);
                result.a = alpha;
                return result;
            }, Vector4.zero);
        }

        private static void BakeTriangle(string name, string folder, int size, Color color)
        {
            Vector2 a = new Vector2(0.5f, 0.88f);
            Vector2 b = new Vector2(0.10f, 0.14f);
            Vector2 c = new Vector2(0.90f, 0.14f);
            WriteSprite(folder + "/" + name + ".png", size, size, (x, y) =>
            {
                Vector2 p = new Vector2(x, y);
                float distance = SignedTriangleDistance(p, a, b, c);
                float alpha = Mathf.Clamp01(0.5f - distance * size);
                if (alpha <= 0f) return Color.clear;
                Color result = color * (0.88f + Mathf.Clamp01(-distance / 0.08f) * 0.11f + (y - x) * 0.04f);
                result.a = alpha;
                return result;
            }, Vector4.zero);
        }

        private static void BakePolygon(string name, string folder, int width, int height, Color color, Vector2[] points)
        {
            WriteSprite(folder + "/" + name + ".png", width, height, (x, y) =>
            {
                float distance = PolygonDistance(new Vector2(x, y), points);
                return MoldedColor(color, x, y, distance, Mathf.Min(width, height));
            }, Vector4.zero);
        }

        private static void BakeSemicircle(string name, string folder, int size, Color color)
        {
            WriteSprite(folder + "/" + name + ".png", size, size, (x, y) =>
            {
                Vector2 ellipse = new Vector2((x - 0.5f) / 0.42f, (y - 0.14f) / 0.72f);
                float curve = (ellipse.magnitude - 1f) * 0.42f;
                float flatEdge = 0.14f - y;
                return MoldedColor(color, x, y, Mathf.Max(curve, flatEdge), size);
            }, Vector4.zero);
        }

        private static void BakeQuarterCircle(string name, string folder, int size, Color color)
        {
            WriteSprite(folder + "/" + name + ".png", size, size, (x, y) =>
            {
                float arc = new Vector2(x - 0.13f, y - 0.13f).magnitude - 0.76f;
                float axes = Mathf.Max(0.13f - x, 0.13f - y);
                return MoldedColor(color, x, y, Mathf.Max(arc, axes), size);
            }, Vector4.zero);
        }

        private static void BakeStud(string name, string folder, int size)
        {
            WriteSprite(folder + "/" + name + ".png", size, size, (x, y) =>
            {
                Vector2 p = new Vector2(x - 0.5f, y - 0.5f);
                float distance = p.magnitude - 0.40f;
                float alpha = Mathf.Clamp01(0.5f - distance * size);
                if (alpha <= 0f) return Color.clear;
                Vector2 lightDirection = new Vector2(-0.36f, 0.42f);
                float dome = Mathf.Sqrt(Mathf.Clamp01(1f - p.sqrMagnitude / (0.40f * 0.40f)));
                float light = 0.74f + dome * 0.24f + Vector2.Dot(p, lightDirection) * 0.18f;
                return new Color(light, light, light, alpha);
            }, Vector4.zero);
        }

        private static void BakeRecessedHole(string name, string folder, int size)
        {
            WriteSprite(folder + "/" + name + ".png", size, size, (x, y) =>
            {
                Vector2 p = new Vector2(x - 0.5f, y - 0.5f);
                float radius = p.magnitude;
                float distance = radius - 0.43f;
                float alpha = Mathf.Clamp01(0.5f - distance * size);
                if (alpha <= 0f) return Color.clear;
                float rim = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.35f) / 0.085f);
                float directional = Mathf.Clamp01(0.5f + (p.x - p.y) * 2.2f);
                float light = Mathf.Lerp(0.38f, 0.68f, rim * directional);
                if (radius < 0.27f) light *= 0.72f;
                return new Color(light, light, light, alpha * 0.94f);
            }, Vector4.zero);
        }

        private static void BakeInsetPanel(string name, string folder, int width, int height)
        {
            WriteSprite(folder + "/" + name + ".png", width, height, (x, y) =>
            {
                Vector2 p = new Vector2(x * 2f - 1f, y * 2f - 1f);
                float aspect = width / (float)height;
                p.x *= aspect;
                float distance = RoundedBoxDistance(p, new Vector2(aspect - 0.22f, 0.78f), 0.20f);
                float alpha = Mathf.Clamp01(0.5f - distance * Mathf.Min(width, height) * 0.5f);
                if (alpha <= 0f) return Color.clear;
                float rim = Mathf.Clamp01(-distance / 0.16f);
                float light = 0.48f + rim * 0.16f + (x - y) * 0.08f;
                return new Color(light, light, light, alpha * 0.72f);
            }, new Vector4(24f, 24f, 24f, 24f));
        }

        private static Color MoldedColor(Color color, float x, float y, float distance, int edgeResolution)
        {
            float alpha = Mathf.Clamp01(0.5f - distance * edgeResolution);
            if (alpha <= 0f) return Color.clear;
            float edge = Mathf.Clamp01(-distance / 0.10f);
            float texture = MoldedNoise(x, y) * 0.72f;
            Color result = color * (0.82f + edge * 0.16f + (y - x) * 0.055f + texture);
            result.a = color.a * alpha;
            return result;
        }

        private static void BakeStar(string name, string folder, int size, Color color, int points)
        {
            Vector2[] polygon = new Vector2[points * 2];
            for (int i = 0; i < polygon.Length; i++)
            {
                float radius = (i & 1) == 0 ? 0.44f : 0.20f;
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / points;
                polygon[i] = new Vector2(0.5f + Mathf.Cos(angle) * radius, 0.5f + Mathf.Sin(angle) * radius);
            }
            WriteSprite(folder + "/" + name + ".png", size, size, (x, y) =>
            {
                float distance = PolygonDistance(new Vector2(x, y), polygon);
                float alpha = Mathf.Clamp01(0.5f - distance * size);
                Color result = color * (0.92f + (y - x) * 0.05f);
                result.a = alpha;
                return result;
            }, Vector4.zero);
        }

        private static void BakeIcon(IconKind kind, Color color)
        {
            const int size = 128;
            WriteSprite(UiRoot + "/icon_" + kind.ToString().ToLowerInvariant() + ".png", size, size, (x, y) =>
            {
                Vector2 p = new Vector2(x, y);
                float distance = IconDistance(kind, p);
                float alpha = Mathf.Clamp01(0.5f - distance * size);
                Color result = color * (0.94f + (y - x) * 0.04f);
                result.a = alpha;
                return result;
            }, Vector4.zero);
        }

        private static float IconDistance(IconKind kind, Vector2 p)
        {
            const float line = 0.055f;
            switch (kind)
            {
                case IconKind.Home:
                    return Mathf.Min(PolygonDistance(p, new[] { new Vector2(.18f,.48f), new Vector2(.5f,.80f), new Vector2(.82f,.48f) }), RoundedBoxDistance((p-new Vector2(.5f,.37f))*2f, new Vector2(.25f,.22f), .04f));
                case IconKind.Play:
                    return PolygonDistance(p, new[] { new Vector2(.32f,.20f), new Vector2(.78f,.50f), new Vector2(.32f,.80f) });
                case IconKind.Pause:
                    return Mathf.Min(RoundedBoxDistance((p-new Vector2(.36f,.5f))*2f, new Vector2(.07f,.30f), .045f), RoundedBoxDistance((p-new Vector2(.64f,.5f))*2f, new Vector2(.07f,.30f), .045f));
                case IconKind.Rotate:
                case IconKind.Undo:
                case IconKind.Reset:
                    float radius = (p - new Vector2(.5f,.5f)).magnitude;
                    float arc = Mathf.Abs(radius - .27f) - line;
                    float cut = kind == IconKind.Undo ? p.y - .64f : .36f - p.y;
                    float arrow = PolygonDistance(p, kind == IconKind.Undo ? new[] { new Vector2(.18f,.60f), new Vector2(.42f,.58f), new Vector2(.25f,.38f) } : new[] { new Vector2(.82f,.40f), new Vector2(.58f,.42f), new Vector2(.75f,.62f) });
                    return Mathf.Min(Mathf.Max(arc, cut), arrow);
                case IconKind.Hint:
                    float bulb = Mathf.Abs((p-new Vector2(.5f,.59f)).magnitude-.18f)-line;
                    float stem = RoundedBoxDistance((p-new Vector2(.5f,.30f))*2f, new Vector2(.08f,.11f), .04f);
                    return Mathf.Min(bulb, stem);
                case IconKind.Levels:
                    float d = 10f;
                    for (int ix=0;ix<2;ix++) for(int iy=0;iy<2;iy++) d=Mathf.Min(d, RoundedBoxDistance((p-new Vector2(.36f+ix*.28f,.36f+iy*.28f))*2f,new Vector2(.09f,.09f),.035f));
                    return d;
                case IconKind.Settings:
                    float gear = Mathf.Abs((p-new Vector2(.5f,.5f)).magnitude-.22f)-.07f;
                    float hole = .08f-(p-new Vector2(.5f,.5f)).magnitude;
                    return Mathf.Max(gear, hole);
                case IconKind.Close:
                    return Mathf.Min(DistanceToSegment(p,new Vector2(.28f,.28f),new Vector2(.72f,.72f))-line, DistanceToSegment(p,new Vector2(.28f,.72f),new Vector2(.72f,.28f))-line);
                case IconKind.Lock:
                    float body = RoundedBoxDistance((p-new Vector2(.5f,.38f))*2f,new Vector2(.23f,.20f),.05f);
                    float shackle = Mathf.Abs((p-new Vector2(.5f,.60f)).magnitude-.17f)-line;
                    return Mathf.Min(body, Mathf.Max(shackle,.50f-p.y));
                case IconKind.Check:
                    return Mathf.Min(DistanceToSegment(p,new Vector2(.22f,.48f),new Vector2(.42f,.28f))-line, DistanceToSegment(p,new Vector2(.42f,.28f),new Vector2(.80f,.72f))-line);
                default:
                    return (p-new Vector2(.5f,.5f)).magnitude-.2f;
            }
        }

        private static void WriteSprite(string assetPath, int width, int height, Func<float, float, Color> sampler, Vector4 border)
        {
            int highWidth = width * Supersampling;
            int highHeight = height * Supersampling;
            Color[] high = new Color[highWidth * highHeight];
            for (int y = 0; y < highHeight; y++)
            {
                float v = (y + 0.5f) / highHeight;
                for (int x = 0; x < highWidth; x++) high[y * highWidth + x] = sampler((x + 0.5f) / highWidth, v);
            }
            Color[] pixels = new Color[width * height];
            float inv = 1f / (Supersampling * Supersampling);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color sum = Color.clear;
                    for (int sy = 0; sy < Supersampling; sy++)
                    for (int sx = 0; sx < Supersampling; sx++) sum += high[(y * Supersampling + sy) * highWidth + x * Supersampling + sx];
                    pixels[y * width + x] = sum * inv;
                }
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            texture.name = Path.GetFileNameWithoutExtension(assetPath);
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            string fullPath = Path.GetFullPath(assetPath);
            if (File.Exists(fullPath) && BytesEqual(File.ReadAllBytes(fullPath), png)) return;
            File.WriteAllBytes(fullPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }

        private static bool BytesEqual(byte[] first, byte[] second)
        {
            if (first == null || second == null || first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++) if (first[i] != second[i]) return false;
            return true;
        }

        private static float RoundedBoxDistance(Vector2 p, Vector2 halfSize, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float MoldedNoise(float x, float y)
        {
            float fine = Mathf.PerlinNoise(x * 155f + 17.3f, y * 155f + 41.7f) - 0.5f;
            float broad = Mathf.PerlinNoise(x * 43f + 73.1f, y * 43f + 9.2f) - 0.5f;
            return fine * 0.020f + broad * 0.008f;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p-a;
            Vector2 ba = b-a;
            float h = Mathf.Clamp01(Vector2.Dot(pa,ba)/Vector2.Dot(ba,ba));
            return (pa-ba*h).magnitude;
        }

        private static float PolygonDistance(Vector2 p, IList<Vector2> vertices)
        {
            float distance = float.MaxValue;
            bool inside = false;
            for (int i=0,j=vertices.Count-1;i<vertices.Count;j=i++)
            {
                Vector2 a=vertices[j], b=vertices[i];
                distance=Mathf.Min(distance,DistanceToSegment(p,a,b));
                if (((a.y>p.y)!=(b.y>p.y)) && p.x < (b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
            }
            return inside ? -distance : distance;
        }

        private static float SignedTriangleDistance(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            return PolygonDistance(p, new[] { a, b, c });
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private enum IconKind { Home, Play, Pause, Rotate, Undo, Reset, Hint, Levels, Settings, Close, Lock, Check }
    }
}
