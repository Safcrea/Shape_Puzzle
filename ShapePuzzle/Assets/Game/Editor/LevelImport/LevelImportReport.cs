using System.Collections.Generic;
using System.Text;

namespace ToyPuzzle.Editor.Levels
{
    public sealed class LevelImportEntry
    {
        public string SourcePath;
        public string LevelId;
        public int LevelNumber;
        public int PieceCount;
        public bool Imported;
        public bool Migrated;
        public string RuntimeAssetPath;
        public LevelValidationResult Validation;
        public string Error;
    }

    public sealed class LevelImportReport
    {
        private readonly List<LevelImportEntry> entries = new List<LevelImportEntry>();
        public IReadOnlyList<LevelImportEntry> Entries => entries;
        public int SourceCount => entries.Count;
        public int ImportedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public int MigratedCount { get; private set; }

        public void Add(LevelImportEntry entry)
        {
            entries.Add(entry);
            if (entry.Imported) ImportedCount++;
            else RejectedCount++;
            if (entry.Migrated) MigratedCount++;
        }

        public string ToSummary()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Toy Puzzle level import: sources=").Append(SourceCount)
                .Append(", imported=").Append(ImportedCount)
                .Append(", rejected=").Append(RejectedCount)
                .Append(", migrated=").Append(MigratedCount);
            for (int i = 0; i < entries.Count; i++)
            {
                LevelImportEntry entry = entries[i];
                builder.AppendLine().Append(entry.Imported ? "[OK] " : "[REJECTED] ")
                    .Append(entry.SourcePath).Append(" level=").Append(entry.LevelId ?? "<unknown>")
                    .Append(" pieces=").Append(entry.PieceCount);
                if (!string.IsNullOrEmpty(entry.Error)) builder.Append(" error=").Append(entry.Error);
                if (entry.Validation != null)
                {
                    for (int issueIndex = 0; issueIndex < entry.Validation.Issues.Count; issueIndex++)
                        builder.AppendLine().Append("  ").Append(entry.Validation.Issues[issueIndex]);
                }
            }
            return builder.ToString();
        }
    }
}
