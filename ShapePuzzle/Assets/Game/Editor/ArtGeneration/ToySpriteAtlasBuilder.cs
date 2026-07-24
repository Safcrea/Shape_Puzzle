using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor
{
    public static class ToySpriteAtlasBuilder
    {
        private const string AtlasRoot = "Assets/Game/Art/Atlases";

        [MenuItem("Tools/Toy Puzzle/Rebuild Atlases", priority = 105)]
        public static void RebuildAtlases()
        {
            EnsureFolder("Assets/Game/Art");
            EnsureFolder(AtlasRoot);
            Type atlasType = FindType("UnityEngine.U2D.SpriteAtlas");
            Type extensionType = FindType("UnityEditor.U2D.SpriteAtlasExtensions");
            if (atlasType == null || extensionType == null)
            {
                Debug.LogWarning("Sprite Atlas API is unavailable. Generated sprites remain directly referenceable.");
                return;
            }
            MethodInfo add = extensionType.GetMethod("Add", BindingFlags.Public | BindingFlags.Static, null, new[] { atlasType, typeof(UnityEngine.Object[]) }, null);
            if (add == null)
            {
                Debug.LogWarning("Sprite Atlas Add API was not found for this Unity editor version.");
                return;
            }
            CreateAtlas(atlasType, add, "UIAtlas", ToyArtGenerator.UiRoot);
            CreateAtlas(atlasType, add, "PieceAtlas", ToyArtGenerator.PieceRoot);
            CreateAtlas(atlasType, add, "EffectsAtlas", ToyArtGenerator.EffectRoot);
            AssetDatabase.SaveAssets();
        }

        private static void CreateAtlas(Type atlasType, MethodInfo add, string name, string folderPath)
        {
            string legacyPath = AtlasRoot + "/" + name + ".spriteatlasv2";
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(legacyPath))) AssetDatabase.DeleteAsset(legacyPath);

            string path = AtlasRoot + "/" + name + ".asset";
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))) AssetDatabase.DeleteAsset(path);
            UnityEngine.Object atlas = Activator.CreateInstance(atlasType) as UnityEngine.Object;
            if (atlas == null) throw new InvalidOperationException("Unable to construct " + atlasType.FullName + ".");
            atlas.name = name;
            AssetDatabase.CreateAsset(atlas, path);
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
            if (folder != null) add.Invoke(null, new object[] { atlas, new[] { folder } });
            EditorUtility.SetDirty(atlas);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }

        private static Type FindType(string fullName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }
    }
}
