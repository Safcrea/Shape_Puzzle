using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor.Levels
{
    public static class LevelImportPipeline
    {
        private sealed class Candidate
        {
            public string Path;
            public LevelJsonDocument Document;
            public LevelValidationResult Validation;
            public bool Migrated;
            public string Error;
        }

        public static LevelImportReport Run(bool buildRuntimeAssets)
        {
            return Run(buildRuntimeAssets, new RuntimeLevelAssetBuilder(), new LevelCatalogBuilder());
        }

        public static LevelImportReport Run(bool buildRuntimeAssets, IRuntimeLevelAssetBuilder assetBuilder, ILevelCatalogBuilder catalogBuilder)
        {
            if (buildRuntimeAssets && assetBuilder == null) throw new ArgumentNullException(nameof(assetBuilder));
            if (buildRuntimeAssets && catalogBuilder == null) throw new ArgumentNullException(nameof(catalogBuilder));

            List<Candidate> candidates = LoadCandidates();
            RejectCrossFileDuplicates(candidates);
            LevelImportReport report = new LevelImportReport();
            List<RuntimeLevelData> runtimeLevels = new List<RuntimeLevelData>(candidates.Count);

            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                bool valid = candidate.Document != null && candidate.Validation != null && candidate.Validation.IsValid && string.IsNullOrEmpty(candidate.Error);
                LevelImportEntry entry = new LevelImportEntry
                {
                    SourcePath = candidate.Path,
                    LevelId = candidate.Document?.levelId,
                    LevelNumber = candidate.Document?.levelNumber ?? 0,
                    PieceCount = candidate.Document?.pieces?.Length ?? 0,
                    Migrated = candidate.Migrated,
                    Validation = candidate.Validation,
                    Error = candidate.Error,
                    Imported = valid
                };

                if (valid && buildRuntimeAssets)
                {
                    try
                    {
                        RuntimeLevelData asset = assetBuilder.Build(candidate.Document, candidate.Path);
                        runtimeLevels.Add(asset);
                        entry.RuntimeAssetPath = AssetDatabase.GetAssetPath(asset);
                    }
                    catch (Exception exception)
                    {
                        entry.Imported = false;
                        entry.Error = "Runtime asset build failed: " + exception.Message;
                    }
                }

                report.Add(entry);
            }

            if (buildRuntimeAssets)
            {
                catalogBuilder.Build(runtimeLevels);
                AssetDatabase.SaveAssets();
            }

            return report;
        }

        public static string[] DiscoverSourcePaths()
        {
            string absoluteFolder = LevelJsonSerializer.ToAbsolutePath(LevelJsonSchema.SourceFolder);
            if (!Directory.Exists(absoluteFolder)) return Array.Empty<string>();
            string[] absolutePaths = Directory.GetFiles(absoluteFolder, "*.json", SearchOption.TopDirectoryOnly);
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            for (int i = 0; i < absolutePaths.Length; i++)
            {
                absolutePaths[i] = absolutePaths[i].Substring(projectRoot.Length + 1).Replace('\\', '/');
            }
            Array.Sort(absolutePaths, StringComparer.Ordinal);
            return absolutePaths;
        }

        public static LevelValidationResult ValidateRuntimeCatalog(LevelCatalog catalog)
        {
            LevelValidationResult result = new LevelValidationResult();
            if (catalog == null)
            {
                result.Add(LevelValidationSeverity.Error, "CATALOG_MISSING", "Generated LevelCatalog is missing.");
                return result;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<int> numbers = new HashSet<int>();
            RuntimeLevelData[] levels = catalog.Levels;
            for (int i = 0; i < levels.Length; i++)
            {
                RuntimeLevelData asset = levels[i];
                if (asset == null || asset.Level == null)
                {
                    result.Add(LevelValidationSeverity.Error, "RUNTIME_LEVEL_NULL", "Catalog entry " + i + " has no runtime level.");
                    continue;
                }
                LevelDefinition level = asset.Level;
                if (!ids.Add(level.levelId)) result.Add(LevelValidationSeverity.Error, "RUNTIME_ID_DUPLICATE", "Duplicate runtime level ID " + level.levelId + ".");
                if (!numbers.Add(level.levelNumber)) result.Add(LevelValidationSeverity.Error, "RUNTIME_NUMBER_DUPLICATE", "Duplicate runtime level number " + level.levelNumber + ".");
                if (level.pieces == null || level.pieces.Length == 0) result.Add(LevelValidationSeverity.Error, "RUNTIME_PIECES_EMPTY", level.levelId + " has no pieces.");
            }
            return result;
        }

        private static List<Candidate> LoadCandidates()
        {
            string[] paths = DiscoverSourcePaths();
            List<Candidate> candidates = new List<Candidate>(paths.Length);
            for (int i = 0; i < paths.Length; i++)
            {
                Candidate candidate = new Candidate { Path = paths[i] };
                try
                {
                    candidate.Document = LevelJsonSerializer.Load(paths[i]);
                    if (candidate.Document.schemaVersion > LevelJsonSchema.CurrentVersion)
                        throw new InvalidDataException("Unsupported future schema version " + candidate.Document.schemaVersion + ".");
                    if (candidate.Document.schemaVersion < LevelJsonSchema.CurrentVersion)
                    {
                        if (!LevelSchemaMigrator.CanMigrate(candidate.Document.schemaVersion))
                            throw new InvalidDataException("Unsupported old schema version " + candidate.Document.schemaVersion + ".");
                        candidate.Migrated = LevelSchemaMigrator.MigrateToCurrent(candidate.Document);
                    }
                    candidate.Validation = LevelContentValidator.Validate(candidate.Document);
                }
                catch (Exception exception)
                {
                    candidate.Error = exception.Message;
                }
                candidates.Add(candidate);
            }
            return candidates;
        }

        private static void RejectCrossFileDuplicates(List<Candidate> candidates)
        {
            Dictionary<string, Candidate> ids = new Dictionary<string, Candidate>(StringComparer.Ordinal);
            Dictionary<int, Candidate> numbers = new Dictionary<int, Candidate>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                if (candidate.Document == null || candidate.Validation == null) continue;
                if (ids.TryGetValue(candidate.Document.levelId ?? string.Empty, out Candidate idOwner))
                {
                    candidate.Validation.Add(LevelValidationSeverity.Error, "LEVEL_ID_DUPLICATE", "Level ID also appears in " + idOwner.Path + ".");
                    idOwner.Validation.Add(LevelValidationSeverity.Error, "LEVEL_ID_DUPLICATE", "Level ID also appears in " + candidate.Path + ".");
                }
                else ids.Add(candidate.Document.levelId ?? string.Empty, candidate);

                if (numbers.TryGetValue(candidate.Document.levelNumber, out Candidate numberOwner))
                {
                    candidate.Validation.Add(LevelValidationSeverity.Error, "LEVEL_NUMBER_DUPLICATE", "Level number also appears in " + numberOwner.Path + ".");
                    numberOwner.Validation.Add(LevelValidationSeverity.Error, "LEVEL_NUMBER_DUPLICATE", "Level number also appears in " + candidate.Path + ".");
                }
                else numbers.Add(candidate.Document.levelNumber, candidate);
            }
        }
    }

    public static class LevelImportMenu
    {
        [MenuItem("Tools/Toy Puzzle/Levels/Import JSON Levels")]
        public static void ImportJsonLevels() => Log(LevelImportPipeline.Run(true));

        [MenuItem("Tools/Toy Puzzle/Levels/Rebuild Runtime Level Assets")]
        public static void RebuildRuntimeLevelAssets() => Log(LevelImportPipeline.Run(true));

        [MenuItem("Tools/Toy Puzzle/Levels/Rebuild Level Prefabs")]
        public static void RebuildLevelPrefabs() => Log(LevelImportPipeline.Run(true));

        [MenuItem("Tools/Toy Puzzle/Levels/Validate JSON Sources")]
        public static void ValidateJsonSources() => Log(LevelImportPipeline.Run(false));

        [MenuItem("Tools/Toy Puzzle/Levels/Validate Runtime Assets")]
        public static void ValidateRuntimeAssets()
        {
            LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelJsonSchema.CatalogPath);
            LevelValidationResult result = LevelImportPipeline.ValidateRuntimeCatalog(catalog);
            if (result.IsValid) Debug.Log("Toy Puzzle runtime level catalog is valid (" + catalog.Count + " levels).");
            else
            {
                for (int i = 0; i < result.Issues.Count; i++) Debug.LogError(result.Issues[i]);
            }
        }

        [MenuItem("Tools/Toy Puzzle/Levels/Validate Level Prefabs")]
        public static void ValidateLevelPrefabs()
        {
            LevelPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(LevelJsonSchema.PrefabCatalogPath);
            ToyPuzzle.LevelValidationResult result = LevelPrefabAssetBuilder.ValidateCatalog(catalog);
            if (result.IsValid) Debug.Log("Toy Puzzle prefab level catalog is valid (" + catalog.Count + " levels).");
            else
            {
                for (int i = 0; i < result.Issues.Length; i++) Debug.LogError(result.Issues[i].Code + ": " + result.Issues[i].Message);
            }
        }

        [MenuItem("Tools/Toy Puzzle/Levels/Migrate Level Schema")]
        public static void MigrateLevelSchema()
        {
            string[] paths = LevelImportPipeline.DiscoverSourcePaths();
            int migrated = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                LevelJsonDocument document = LevelJsonSerializer.Load(paths[i]);
                if (LevelSchemaMigrator.MigrateToCurrent(document))
                {
                    LevelJsonSerializer.SaveAtomic(paths[i], document);
                    migrated++;
                }
            }
            AssetDatabase.Refresh();
            Debug.Log("Toy Puzzle schema migration complete. Migrated " + migrated + " of " + paths.Length + " sources.");
        }

        [MenuItem("Tools/Toy Puzzle/Levels/Open Source Folder")]
        public static void OpenSourceFolder()
        {
            Directory.CreateDirectory(LevelJsonSerializer.ToAbsolutePath(LevelJsonSchema.SourceFolder));
            EditorUtility.RevealInFinder(LevelJsonSerializer.ToAbsolutePath(LevelJsonSchema.SourceFolder));
        }

        private static void Log(LevelImportReport report)
        {
            if (report.RejectedCount == 0) Debug.Log(report.ToSummary());
            else Debug.LogError(report.ToSummary());
        }
    }
}
