using System;
using UnityEngine;

namespace ToyPuzzle
{
    [CreateAssetMenu(fileName = "RuntimeLevelData", menuName = "Toy Puzzle/Runtime Level Data")]
    public sealed class RuntimeLevelData : ScriptableObject
    {
        [SerializeField] private LevelDefinition level = new LevelDefinition();

        public LevelDefinition Level => level;

        public void SetLevel(LevelDefinition value)
        {
            level = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
