using System;
using UnityEngine;

namespace ToyPuzzle
{
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Toy Puzzle/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private RuntimeLevelData[] levels = Array.Empty<RuntimeLevelData>();

        public int Count => levels == null ? 0 : levels.Length;
        public RuntimeLevelData[] Levels => levels;

        public RuntimeLevelData GetByIndex(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length)
            {
                return null;
            }

            return levels[index];
        }

        public RuntimeLevelData FindByNumber(int levelNumber)
        {
            if (levels == null)
            {
                return null;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                RuntimeLevelData candidate = levels[i];
                if (candidate != null && candidate.Level != null && candidate.Level.levelNumber == levelNumber)
                {
                    return candidate;
                }
            }

            return null;
        }

        public RuntimeLevelData FindById(string levelId)
        {
            if (levels == null || string.IsNullOrEmpty(levelId))
            {
                return null;
            }

            for (int i = 0; i < levels.Length; i++)
            {
                RuntimeLevelData candidate = levels[i];
                if (candidate != null && candidate.Level != null &&
                    string.Equals(candidate.Level.levelId, levelId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        public void SetLevels(RuntimeLevelData[] value)
        {
            levels = value ?? Array.Empty<RuntimeLevelData>();
        }
    }
}
