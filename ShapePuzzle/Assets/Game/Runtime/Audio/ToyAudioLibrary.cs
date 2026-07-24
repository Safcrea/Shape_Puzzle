using System;
using UnityEngine;

namespace ToyPuzzle
{
    public enum ToyAudioCue
    {
        ButtonClick,
        PiecePickup,
        PieceDrop,
        Rotate,
        InvalidPlacement,
        CorrectPlacement,
        Undo,
        Hint,
        LevelComplete
    }

    [Serializable]
    public sealed class ToyAudioEntry
    {
        public ToyAudioCue cue;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.75f;
        [Range(0.5f, 1.5f)] public float pitch = 1f;
    }

    [CreateAssetMenu(fileName = "ToyAudioLibrary", menuName = "Toy Puzzle/Audio Library")]
    public sealed class ToyAudioLibrary : ScriptableObject
    {
        [SerializeField] private ToyAudioEntry[] entries = Array.Empty<ToyAudioEntry>();
        [SerializeField] private AudioClip musicLoop;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.25f;

        public AudioClip MusicLoop => musicLoop;
        public float MusicVolume => musicVolume;

        public ToyAudioEntry Find(ToyAudioCue cue)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                ToyAudioEntry entry = entries[i];
                if (entry != null && entry.cue == cue) return entry;
            }
            return null;
        }
    }
}
