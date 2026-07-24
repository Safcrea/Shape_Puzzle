using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor
{
    public static class ToyAudioGenerator
    {
        public const string AudioRoot = "Assets/Game/Audio/Generated";
        public const string LibraryPath = AudioRoot + "/ToyAudioLibrary.asset";
        private const int SampleRate = 22050;

        [MenuItem("Tools/Toy Puzzle/Generate Audio Only", priority = 102)]
        public static void GenerateAudioOnly()
        {
            EnsureFolder("Assets/Game/Audio");
            EnsureFolder(AudioRoot);
            ToyAudioCue[] cues = (ToyAudioCue[])Enum.GetValues(typeof(ToyAudioCue));
            for (int i = 0; i < cues.Length; i++) GenerateCue(cues[i]);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporters(cues);
            CreateOrUpdateLibrary(cues);
            AssetDatabase.SaveAssets();
            Debug.Log("Toy Puzzle audio generated under " + AudioRoot);
        }

        private static void GenerateCue(ToyAudioCue cue)
        {
            CueRecipe recipe = RecipeFor(cue);
            int samples = Mathf.CeilToInt(recipe.duration * SampleRate);
            short[] data = new short[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                float normalized = t / recipe.duration;
                float attack = Mathf.Clamp01(normalized / 0.06f);
                float decay = Mathf.Pow(1f - normalized, recipe.decay);
                float envelope = attack * decay;
                float glideFrequency = Mathf.Lerp(recipe.frequency, recipe.endFrequency, normalized);
                float fundamental = Mathf.Sin(t * glideFrequency * Mathf.PI * 2f);
                float overtone = Mathf.Sin(t * glideFrequency * Mathf.PI * 4f + 0.25f) * 0.24f;
                float click = Mathf.Sin(t * 97f * Mathf.PI * 2f) * Mathf.Pow(1f - normalized, 9f) * 0.09f;
                float sample = (fundamental + overtone + click) * envelope * recipe.volume;
                data[i] = (short)Mathf.RoundToInt(Mathf.Clamp(sample, -0.92f, 0.92f) * short.MaxValue);
            }
            string fullPath = Path.GetFullPath(PathFor(cue));
            byte[] wave = EncodeWave(data, SampleRate);
            if (!File.Exists(fullPath) || !BytesEqual(File.ReadAllBytes(fullPath), wave)) File.WriteAllBytes(fullPath, wave);
        }

        private static void ConfigureImporters(ToyAudioCue[] cues)
        {
            for (int i = 0; i < cues.Length; i++)
            {
                AudioImporter importer = AssetImporter.GetAtPath(PathFor(cues[i])) as AudioImporter;
                if (importer == null) continue;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                settings.sampleRateOverride = SampleRate;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = false;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
        }

        private static void CreateOrUpdateLibrary(ToyAudioCue[] cues)
        {
            ToyAudioLibrary library = AssetDatabase.LoadAssetAtPath<ToyAudioLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<ToyAudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            SerializedObject serialized = new SerializedObject(library);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = cues.Length;
            for (int i = 0; i < cues.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("cue").enumValueIndex = (int)cues[i];
                entry.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(PathFor(cues[i]));
                entry.FindPropertyRelative("volume").floatValue = cues[i] == ToyAudioCue.LevelComplete ? 0.72f : 0.62f;
                entry.FindPropertyRelative("pitch").floatValue = 1f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }

        private static CueRecipe RecipeFor(ToyAudioCue cue)
        {
            switch (cue)
            {
                case ToyAudioCue.ButtonClick: return new CueRecipe(520f, 650f, 0.08f, 2.6f, 0.38f);
                case ToyAudioCue.PiecePickup: return new CueRecipe(360f, 520f, 0.11f, 2.0f, 0.44f);
                case ToyAudioCue.PieceDrop: return new CueRecipe(390f, 300f, 0.13f, 2.2f, 0.46f);
                case ToyAudioCue.Rotate: return new CueRecipe(480f, 620f, 0.12f, 1.8f, 0.42f);
                case ToyAudioCue.InvalidPlacement: return new CueRecipe(210f, 145f, 0.17f, 1.5f, 0.39f);
                case ToyAudioCue.CorrectPlacement: return new CueRecipe(520f, 820f, 0.20f, 1.4f, 0.49f);
                case ToyAudioCue.Undo: return new CueRecipe(500f, 330f, 0.13f, 1.8f, 0.40f);
                case ToyAudioCue.Hint: return new CueRecipe(690f, 870f, 0.22f, 1.3f, 0.40f);
                case ToyAudioCue.LevelComplete: return new CueRecipe(440f, 880f, 0.48f, 0.9f, 0.52f);
                default: return new CueRecipe(440f, 440f, 0.1f, 2f, 0.4f);
            }
        }

        private static string PathFor(ToyAudioCue cue)
        {
            return AudioRoot + "/" + cue.ToString().ToLowerInvariant() + ".wav";
        }

        private static byte[] EncodeWave(short[] samples, int sampleRate)
        {
            using (MemoryStream stream = new MemoryStream(44 + samples.Length * 2))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples.Length * 2);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(samples.Length * 2);
                for (int i = 0; i < samples.Length; i++) writer.Write(samples[i]);
                return stream.ToArray();
            }
        }

        private static bool BytesEqual(byte[] first, byte[] second)
        {
            if (first == null || second == null || first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++) if (first[i] != second[i]) return false;
            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private readonly struct CueRecipe
        {
            public readonly float frequency;
            public readonly float endFrequency;
            public readonly float duration;
            public readonly float decay;
            public readonly float volume;

            public CueRecipe(float frequency, float endFrequency, float duration, float decay, float volume)
            {
                this.frequency = frequency;
                this.endFrequency = endFrequency;
                this.duration = duration;
                this.decay = decay;
                this.volume = volume;
            }
        }
    }
}
