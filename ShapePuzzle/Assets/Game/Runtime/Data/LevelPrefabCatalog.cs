using System;
using UnityEngine;

namespace ToyPuzzle
{
    [CreateAssetMenu(fileName = "LevelPrefabCatalog", menuName = "Toy Puzzle/Level Prefab Catalog")]
    public sealed class LevelPrefabCatalog : ScriptableObject
    {
        [SerializeField] private GameObject[] levelPrefabs = Array.Empty<GameObject>();

        public int Count => levelPrefabs == null ? 0 : levelPrefabs.Length;
        public GameObject[] LevelPrefabs => levelPrefabs;

        public GameObject GetPrefabByIndex(int index)
        {
            return levelPrefabs != null && index >= 0 && index < levelPrefabs.Length ? levelPrefabs[index] : null;
        }

        public PuzzleLevelPrefab GetByIndex(int index)
        {
            GameObject prefab = GetPrefabByIndex(index);
            return prefab == null ? null : prefab.GetComponent<PuzzleLevelPrefab>();
        }

        public PuzzleLevelPrefab FindByNumber(int levelNumber)
        {
            for (int i = 0; i < Count; i++)
            {
                PuzzleLevelPrefab candidate = GetByIndex(i);
                if (candidate != null && candidate.Level != null && candidate.Level.levelNumber == levelNumber) return candidate;
            }
            return null;
        }

        public PuzzleLevelPrefab FindById(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            for (int i = 0; i < Count; i++)
            {
                PuzzleLevelPrefab candidate = GetByIndex(i);
                if (candidate != null && candidate.Level != null &&
                    string.Equals(candidate.Level.levelId, levelId, StringComparison.Ordinal)) return candidate;
            }
            return null;
        }

        public void SetLevelPrefabs(GameObject[] value)
        {
            levelPrefabs = value ?? Array.Empty<GameObject>();
        }
    }
}
