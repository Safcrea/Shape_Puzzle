using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor.Levels
{
    [CustomEditor(typeof(PuzzleLevelPrefab))]
    public sealed class PuzzleLevelPrefabInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PuzzleLevelPrefab levelPrefab = (PuzzleLevelPrefab)target;
            LevelDefinition level = levelPrefab.Level;

            EditorGUILayout.LabelField(level == null ? "Empty Level Prefab" :
                "Level " + level.levelNumber + " · " + level.targetObjectName, EditorStyles.boldLabel);
            if (level != null)
            {
                EditorGUILayout.LabelField("Grid", level.boardWidth + " × " + level.boardHeight);
                EditorGUILayout.LabelField("Pieces", level.pieces == null ? "0" : level.pieces.Length.ToString());
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open in Level Editor"))
            {
                if (string.IsNullOrEmpty(levelPrefab.SourceJsonPath))
                    EditorUtility.DisplayDialog("No source path", "This prefab has no linked JSON source. Rebuild the level prefabs first.", "OK");
                else
                    LevelAuthoringWindow.OpenAsset(levelPrefab.SourceJsonPath);
            }
            if (GUILayout.Button("Validate Prefab")) Validate(level);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Prefab fields are editable and used by the game immediately. Use the Level Editor for source-safe changes; Rebuild All Prefabs reapplies the JSON sources.",
                MessageType.Info);

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "sourceJsonPath");
            serializedObject.ApplyModifiedProperties();
        }

        private static void Validate(LevelDefinition level)
        {
            ToyPuzzle.LevelValidationResult result = LevelDefinitionValidator.Validate(level);
            if (result.IsValid)
            {
                EditorUtility.DisplayDialog("Level Prefab", "This level prefab is valid.", "OK");
                return;
            }

            var message = new System.Text.StringBuilder("Fix these issues:\n");
            for (int i = 0; i < result.Issues.Length; i++)
                message.Append("\n• ").Append(result.Issues[i].Message);
            EditorUtility.DisplayDialog("Invalid Level Prefab", message.ToString(), "OK");
        }
    }
}
