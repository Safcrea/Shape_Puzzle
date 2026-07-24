using System;

namespace ToyPuzzle.Editor.Levels
{
    public static class LevelSchemaMigrator
    {
        public static bool CanMigrate(int version)
        {
            return version >= LevelJsonSchema.MinimumSupportedVersion && version < LevelJsonSchema.CurrentVersion;
        }

        public static bool MigrateToCurrent(LevelJsonDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (document.schemaVersion > LevelJsonSchema.CurrentVersion)
                throw new InvalidOperationException("Future schema version " + document.schemaVersion + " is not supported.");
            if (document.schemaVersion < LevelJsonSchema.MinimumSupportedVersion)
                throw new InvalidOperationException("Schema version " + document.schemaVersion + " is too old to migrate.");

            bool changed = false;
            if (document.schemaVersion == 0)
            {
                document.schemaVersion = 1;
                document.paletteId = string.IsNullOrWhiteSpace(document.paletteId) ? "primary" : document.paletteId;
                document.hintMetadata = document.hintMetadata ?? new HintJson();
                document.tutorialMetadata = document.tutorialMetadata ?? new TutorialJson();
                document.completionRewardData = document.completionRewardData ?? new CompletionRewardJson();
                document.thumbnailConfiguration = document.thumbnailConfiguration ?? new ThumbnailJson();
                document.levelTags = document.levelTags ?? Array.Empty<string>();
                document.solutionCertificate = document.solutionCertificate ?? Array.Empty<SolutionStepJson>();
                changed = true;
            }

            return changed;
        }
    }
}
