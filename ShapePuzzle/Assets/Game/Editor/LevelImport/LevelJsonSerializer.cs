using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ToyPuzzle.Editor.Levels
{
    public static class LevelJsonSerializer
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public static LevelJsonDocument Deserialize(string json, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException(sourceName + ": source is empty.");
            }

            try
            {
                LevelJsonDocument document = JsonUtility.FromJson<LevelJsonDocument>(json);
                if (document == null)
                {
                    throw new InvalidDataException(sourceName + ": JSON produced no level document.");
                }

                NormalizeNullCollections(document);
                return document;
            }
            catch (Exception exception) when (!(exception is InvalidDataException))
            {
                throw new InvalidDataException(sourceName + ": malformed level JSON. " + exception.Message, exception);
            }
        }

        public static string Serialize(LevelJsonDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            NormalizeNullCollections(document);
            return JsonUtility.ToJson(document, true).Replace("\r\n", "\n") + "\n";
        }

        public static LevelJsonDocument Load(string assetPath)
        {
            return Deserialize(File.ReadAllText(ToAbsolutePath(assetPath), Encoding.UTF8), assetPath);
        }

        public static void SaveAtomic(string assetPath, LevelJsonDocument document)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("Cannot resolve destination folder for " + assetPath);
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = absolutePath + ".tmp";
            File.WriteAllText(temporaryPath, Serialize(document), Utf8WithoutBom);
            if (File.Exists(absolutePath))
            {
                File.Replace(temporaryPath, absolutePath, null);
            }
            else
            {
                File.Move(temporaryPath, absolutePath);
            }
        }

        public static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("Unity project root is unavailable.");
            }

            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static void NormalizeNullCollections(LevelJsonDocument document)
        {
            document.pieces = document.pieces ?? Array.Empty<PieceJson>();
            document.levelTags = document.levelTags ?? Array.Empty<string>();
            document.solutionCertificate = document.solutionCertificate ?? Array.Empty<SolutionStepJson>();
            document.hintMetadata = document.hintMetadata ?? new HintJson();
            document.tutorialMetadata = document.tutorialMetadata ?? new TutorialJson();
            document.completionRewardData = document.completionRewardData ?? new CompletionRewardJson();
            document.thumbnailConfiguration = document.thumbnailConfiguration ?? new ThumbnailJson();

            for (int i = 0; i < document.pieces.Length; i++)
            {
                PieceJson piece = document.pieces[i];
                if (piece == null)
                {
                    continue;
                }

                piece.footprint = piece.footprint ?? Array.Empty<Int2Json>();
                piece.customPolygonPoints = piece.customPolygonPoints ?? Array.Empty<Float2Json>();
                piece.allowedRotations = piece.allowedRotations ?? Array.Empty<int>();
                piece.decorativeStuds = piece.decorativeStuds ?? Array.Empty<DecorativeStudJson>();
                piece.recessedHoles = piece.recessedHoles ?? Array.Empty<RecessedHoleJson>();
                piece.logicalPivot = piece.logicalPivot ?? new Int2Json();
                piece.visualPivot = piece.visualPivot ?? new Float2Json(0.5f, 0.5f);
                piece.targetPosition = piece.targetPosition ?? new Int2Json();
                piece.startingPosition = piece.startingPosition ?? new Int2Json();
                piece.visualOverhang = piece.visualOverhang ?? new Float2Json();
                piece.artGenerationParameters = piece.artGenerationParameters ?? new ArtGenerationJson();
            }
        }
    }
}
