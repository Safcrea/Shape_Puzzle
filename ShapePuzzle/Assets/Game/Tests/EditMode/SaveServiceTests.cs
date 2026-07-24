using NUnit.Framework;
using UnityEngine;

namespace ToyPuzzle.Tests
{
    public sealed class SaveServiceTests
    {
        [Test]
        public void MissingSave_CreatesProductionDefaults()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);

            SaveLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.CreatedDefault));
            Assert.That(result.Data.version, Is.EqualTo(PlayerSaveData.CurrentVersion));
            Assert.That(result.Data.highestUnlockedLevel, Is.EqualTo(1));
            Assert.That(result.Data.soundEnabled, Is.True);
            Assert.That(storage.Primary, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Completion_UnlocksNextAndKeepsBestMetrics()
        {
            var service = new SaveService(new MemorySaveStorage());
            service.Load();

            service.CompleteLevel(1, 12, 45f, 2);
            service.CompleteLevel(1, 15, 40f, 1);
            LevelProgressData progress = service.GetLevelProgress(1);

            Assert.That(service.Data.highestUnlockedLevel, Is.EqualTo(2));
            Assert.That(progress.completionCount, Is.EqualTo(2));
            Assert.That(progress.bestMoveCount, Is.EqualTo(12));
            Assert.That(progress.bestCompletionSeconds, Is.EqualTo(40f));
            Assert.That(progress.totalHintsUsed, Is.EqualTo(3));
        }

        [Test]
        public void CorruptPrimary_UsesValidBackupAndPreservesCorruptData()
        {
            PlayerSaveData backup = PlayerSaveData.CreateDefault();
            backup.highestUnlockedLevel = 7;
            var storage = new MemorySaveStorage
            {
                Primary = "{ definitely not json",
                Backup = JsonUtility.ToJson(backup)
            };
            var service = new SaveService(storage);

            SaveLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup));
            Assert.That(result.Data.highestUnlockedLevel, Is.EqualTo(7));
            Assert.That(storage.CorruptPrimary, Is.EqualTo("{ definitely not json"));
            Assert.That(storage.Primary, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void VersionOneSave_IsMigratedAndRewritten()
        {
            PlayerSaveData legacy = PlayerSaveData.CreateDefault();
            legacy.version = 1;
            legacy.reducedMotion = true;
            var storage = new MemorySaveStorage { Primary = JsonUtility.ToJson(legacy) };
            var service = new SaveService(storage);

            SaveLoadResult result = service.Load();

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.LoadedAndMigrated));
            Assert.That(result.Data.version, Is.EqualTo(PlayerSaveData.CurrentVersion));
            Assert.That(result.Data.reducedMotion, Is.False);
            Assert.That(storage.WriteCount, Is.EqualTo(1));
        }

        [Test]
        public void InProgressFreeformPieces_RoundTripThroughSave()
        {
            var storage = new MemorySaveStorage();
            var service = new SaveService(storage);
            service.Load();
            service.SetPuzzleProgress(
                1,
                new[]
                {
                    new PieceProgressData { pieceId = "red_part_01", normalizedX = 0.23f, normalizedY = 0.71f, snapped = false },
                    new PieceProgressData { pieceId = "blue_part_01", normalizedX = 0.54f, normalizedY = 0.42f, snapped = true }
                },
                3,
                18.5f,
                1);
            service.Save();

            var restored = new SaveService(storage);
            SaveLoadResult result = restored.Load();
            LevelProgressData progress = restored.GetLevelProgress(1);

            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.LoadedPrimary));
            Assert.That(progress, Is.Not.Null);
            Assert.That(progress.pieceProgress, Has.Length.EqualTo(2));
            Assert.That(progress.pieceProgress[0].normalizedX, Is.EqualTo(0.23f).Within(0.0001f));
            Assert.That(progress.pieceProgress[1].snapped, Is.True);
            Assert.That(progress.inProgressMoveCount, Is.EqualTo(3));
            Assert.That(progress.inProgressElapsedSeconds, Is.EqualTo(18.5f).Within(0.0001f));
            Assert.That(progress.inProgressHintsUsed, Is.EqualTo(1));
        }

        private sealed class MemorySaveStorage : ISaveStorage
        {
            public string Primary;
            public string Backup;
            public string CorruptPrimary;
            public int WriteCount;

            public bool TryReadPrimary(out string json)
            {
                json = Primary;
                return json != null;
            }

            public bool TryReadBackup(out string json)
            {
                json = Backup;
                return json != null;
            }

            public void WriteAtomically(string json)
            {
                if (Primary != null)
                {
                    Backup = Primary;
                }

                Primary = json;
                WriteCount++;
            }

            public void PreserveCorruptPrimary()
            {
                CorruptPrimary = Primary;
                Primary = null;
            }
        }
    }
}
