using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor.Levels
{
    public static class LevelPrefabAssetBuilder
    {
        private const string ThumbnailFolder = "Assets/Game/Art/Generated/Thumbnails";

        public static LevelPrefabCatalog BuildAll(IReadOnlyList<RuntimeLevelData> runtimeLevels)
        {
            RuntimeLevelAssetBuilder.EnsureFolder(LevelJsonSchema.PrefabFolder);
            var prefabs = new List<GameObject>(runtimeLevels == null ? 0 : runtimeLevels.Count);
            Dictionary<int, string> sourcePaths = BuildSourcePathLookup();

            if (runtimeLevels != null)
            {
                for (int i = 0; i < runtimeLevels.Count; i++)
                {
                    RuntimeLevelData runtimeLevel = runtimeLevels[i];
                    if (runtimeLevel == null || runtimeLevel.Level == null) continue;
                    sourcePaths.TryGetValue(runtimeLevel.Level.levelNumber, out string sourcePath);
                    GameObject prefab = Build(runtimeLevel.Level, sourcePath);
                    if (prefab != null) prefabs.Add(prefab);
                }
            }

            prefabs.Sort(ComparePrefabs);
            LevelPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(LevelJsonSchema.PrefabCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, LevelJsonSchema.PrefabCatalogPath);
            }
            catalog.SetLevelPrefabs(prefabs.ToArray());
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        public static GameObject Build(LevelDefinition level, string sourceJsonPath)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            RuntimeLevelAssetBuilder.EnsureFolder(LevelJsonSchema.PrefabFolder);
            string path = GetPrefabPath(level);
            PuzzlePieceArtwork[] existingArtwork = CopyExistingArtwork(path);
            string rootName = "Level_" + level.levelNumber.ToString("D3") + "_" + SanitizeName(level.targetObjectName);
            var root = new GameObject(rootName);
            try
            {
                PuzzleLevelPrefab component = root.AddComponent<PuzzleLevelPrefab>();
                component.SetLevel(Clone(level));
                component.SetSourceJsonPath(sourceJsonPath);
                component.SetThumbnail(AssetDatabase.LoadAssetAtPath<Sprite>(ThumbnailFolder + "/level_" + level.levelNumber.ToString("D3") + ".png"));
                component.SetPieceArtwork(existingArtwork);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (saved == null) throw new InvalidOperationException("Unity could not save level prefab " + path + ".");
                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        public static string GetPrefabPath(LevelDefinition level)
        {
            if (level == null) return string.Empty;
            return LevelJsonSchema.PrefabFolder + "/PF_Level_" + level.levelNumber.ToString("D3") + ".prefab";
        }

        public static ToyPuzzle.LevelValidationResult ValidateCatalog(LevelPrefabCatalog catalog)
        {
            var issues = new List<ToyPuzzle.LevelValidationIssue>();
            if (catalog == null)
            {
                issues.Add(new ToyPuzzle.LevelValidationIssue("prefab.catalog.missing", "Generated LevelPrefabCatalog is missing.", ToyPuzzle.ValidationSeverity.Error));
                return new ToyPuzzle.LevelValidationResult(issues.ToArray());
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var numbers = new HashSet<int>();
            for (int i = 0; i < catalog.Count; i++)
            {
                GameObject prefab = catalog.GetPrefabByIndex(i);
                PuzzleLevelPrefab levelPrefab = catalog.GetByIndex(i);
                if (prefab == null || levelPrefab == null || levelPrefab.Level == null)
                {
                    issues.Add(new ToyPuzzle.LevelValidationIssue("prefab.entry.missing", "Prefab catalog entry " + i + " is missing PuzzleLevelPrefab data.", ToyPuzzle.ValidationSeverity.Error));
                    continue;
                }
                LevelDefinition level = levelPrefab.Level;
                if (!ids.Add(level.levelId)) issues.Add(new ToyPuzzle.LevelValidationIssue("prefab.id.duplicate", "Duplicate prefab level ID " + level.levelId + ".", ToyPuzzle.ValidationSeverity.Error));
                if (!numbers.Add(level.levelNumber)) issues.Add(new ToyPuzzle.LevelValidationIssue("prefab.number.duplicate", "Duplicate prefab level number " + level.levelNumber + ".", ToyPuzzle.ValidationSeverity.Error));
                ToyPuzzle.LevelValidationResult validation = LevelDefinitionValidator.Validate(level);
                for (int issueIndex = 0; issueIndex < validation.Issues.Length; issueIndex++) issues.Add(validation.Issues[issueIndex]);
            }
            return new ToyPuzzle.LevelValidationResult(issues.ToArray());
        }

        private static Dictionary<int, string> BuildSourcePathLookup()
        {
            var result = new Dictionary<int, string>();
            string[] paths = LevelImportPipeline.DiscoverSourcePaths();
            for (int i = 0; i < paths.Length; i++)
            {
                try
                {
                    LevelJsonDocument document = LevelJsonSerializer.Load(paths[i]);
                    result[document.levelNumber] = paths[i];
                }
                catch (Exception)
                {
                }
            }
            return result;
        }

        private static LevelDefinition Clone(LevelDefinition level)
        {
            return JsonUtility.FromJson<LevelDefinition>(JsonUtility.ToJson(level));
        }

        private static PuzzlePieceArtwork[] CopyExistingArtwork(string prefabPath)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            PuzzleLevelPrefab component = existing == null ? null : existing.GetComponent<PuzzleLevelPrefab>();
            PuzzlePieceArtwork[] source = component == null ? null : component.PieceArtwork;
            if (source == null || source.Length == 0) return Array.Empty<PuzzlePieceArtwork>();

            var copy = new PuzzlePieceArtwork[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                PuzzlePieceArtwork item = source[i];
                if (item == null) continue;
                copy[i] = new PuzzlePieceArtwork
                {
                    pieceId = item.pieceId,
                    sprite = item.sprite,
                    sizeInCells = item.sizeInCells,
                    offsetFromTargetPivotInCells = item.offsetFromTargetPivotInCells,
                    bakedTargetRotation = item.bakedTargetRotation,
                    freeformColorBlock = item.freeformColorBlock,
                    targetCenterNormalized = item.targetCenterNormalized,
                    startingCenterNormalized = item.startingCenterNormalized,
                    sizeNormalized = item.sizeNormalized,
                    snapDistanceNormalized = item.snapDistanceNormalized
                };
            }
            return copy;
        }

        private static int ComparePrefabs(GameObject left, GameObject right)
        {
            PuzzleLevelPrefab leftLevel = left == null ? null : left.GetComponent<PuzzleLevelPrefab>();
            PuzzleLevelPrefab rightLevel = right == null ? null : right.GetComponent<PuzzleLevelPrefab>();
            int leftNumber = leftLevel == null || leftLevel.Level == null ? int.MaxValue : leftLevel.Level.levelNumber;
            int rightNumber = rightLevel == null || rightLevel.Level == null ? int.MaxValue : rightLevel.Level.levelNumber;
            return leftNumber.CompareTo(rightNumber);
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Puzzle";
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            string result = value.Trim().Replace(' ', '_').Replace('-', '_');
            for (int i = 0; i < invalid.Length; i++) result = result.Replace(invalid[i], '_');
            return result;
        }
    }
}
