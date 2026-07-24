using System;
using UnityEditor;

namespace ToyPuzzle.Editor.Levels
{
    public sealed class LevelSourceFileWatcher : AssetPostprocessor
    {
        private static bool importQueued;
        private static bool importing;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importing || importQueued || !ContainsLevelJson(importedAssets) && !ContainsLevelJson(movedAssets)) return;
            importQueued = true;
            EditorApplication.delayCall += ImportChangedSources;
        }

        private static void ImportChangedSources()
        {
            importQueued = false;
            if (importing || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            importing = true;
            try
            {
                LevelImportReport report = LevelImportPipeline.Run(true);
                if (report.RejectedCount == 0) UnityEngine.Debug.Log(report.ToSummary());
                else UnityEngine.Debug.LogError(report.ToSummary());
            }
            finally
            {
                importing = false;
            }
        }

        private static bool ContainsLevelJson(string[] paths)
        {
            if (paths == null) return false;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (path.StartsWith(LevelJsonSchema.SourceFolder + "/", StringComparison.Ordinal) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
