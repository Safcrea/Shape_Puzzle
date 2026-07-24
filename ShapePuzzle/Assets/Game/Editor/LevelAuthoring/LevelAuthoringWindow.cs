using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToyPuzzle.Editor.Levels
{
    public sealed class LevelAuthoringWorkingState : ScriptableObject
    {
        public LevelJsonDocument document;
    }

    public sealed class LevelAuthoringWindow : EditorWindow
    {
        private enum BoardEditMode { Target, Starting }

        private LevelAuthoringWorkingState state;
        private Vector2 scroll;
        private Vector2 pieceScroll;
        private Vector2 levelListScroll;
        private int selectedPiece;
        private BoardEditMode editMode;
        private string currentPath;
        private string status;
        private string[] cachedLevelPaths = Array.Empty<string>();
        private GUIStyle cellStyle;

        [MenuItem("Tools/Toy Puzzle/Levels/Open Level Editor")]
        public static void Open()
        {
            GetWindow<LevelAuthoringWindow>("Toy Puzzle Levels").minSize = new Vector2(920f, 640f);
        }

        public static void OpenAsset(string assetPath)
        {
            LevelAuthoringWindow window = GetWindow<LevelAuthoringWindow>("Toy Puzzle Levels");
            window.minSize = new Vector2(920f, 640f);
            window.LoadLevelPath(assetPath);
            window.Focus();
        }

        private void OnEnable()
        {
            state = CreateInstance<LevelAuthoringWorkingState>();
            state.hideFlags = HideFlags.HideAndDontSave;
            state.document = CreateNewDocument(1);
            RefreshLevelList();
            if (cachedLevelPaths.Length > 0)
            {
                state.document = LevelJsonSerializer.Load(cachedLevelPaths[0]);
                LevelSchemaMigrator.MigrateToCurrent(state.document);
                currentPath = cachedLevelPaths[0];
            }
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            if (state != null) DestroyImmediate(state);
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
            if (state == null || state.document == null) return;

            EditorGUILayout.BeginHorizontal();
            DrawAllLevels();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawLevelProperties(state.document);
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            DrawPieceList(state.document);
            EditorGUILayout.Space(8f);
            DrawSelectedPiece(state.document);
            EditorGUILayout.Space(8f);
            DrawBoardArea(state.document);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("New", EditorStyles.toolbarButton)) NewLevel();
            if (GUILayout.Button("Open", EditorStyles.toolbarButton)) OpenLevel();
            if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton)) DuplicateLevel();
            if (GUILayout.Button("Save + Prefab", EditorStyles.toolbarButton)) SaveLevel();
            if (GUILayout.Button("Rebuild All Prefabs", EditorStyles.toolbarButton)) ImportLevels();
            if (GUILayout.Button("Open Prefab", EditorStyles.toolbarButton)) OpenPrefab();
            if (GUILayout.Button("Thumbnail", EditorStyles.toolbarButton)) GenerateThumbnail();
            if (GUILayout.Button("Test", EditorStyles.toolbarButton)) TestLevel();
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton)) DeleteLevel();
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.IsNullOrEmpty(currentPath) ? "Unsaved" : currentPath, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAllLevels()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(225f), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("All Level Prefabs", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(58f))) RefreshLevelList();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(cachedLevelPaths.Length + " source levels", EditorStyles.miniLabel);
            levelListScroll = EditorGUILayout.BeginScrollView(levelListScroll);
            for (int i = 0; i < cachedLevelPaths.Length; i++)
            {
                string path = cachedLevelPaths[i];
                string label = Path.GetFileNameWithoutExtension(path).Replace('_', ' ');
                GUIStyle style = string.Equals(path, currentPath, StringComparison.Ordinal) ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (GUILayout.Button(label, style, GUILayout.Height(24f))) LoadLevelPath(path);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("Select any level, edit its grids and pieces, then Save + Prefab. Runtime loads the generated prefab catalog in this order.", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawLevelProperties(LevelJsonDocument level)
        {
            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int levelNumber = EditorGUILayout.IntField("Level Number", level.levelNumber);
            string levelId = EditorGUILayout.TextField("Stable ID", level.levelId);
            string displayName = EditorGUILayout.TextField("Display Name", level.displayName);
            string targetName = EditorGUILayout.TextField("Target Object", level.targetObjectName);
            int width = EditorGUILayout.IntSlider("Board Width", level.boardWidth, 5, 8);
            int height = EditorGUILayout.IntSlider("Board Height", level.boardHeight, 5, 8);
            int difficulty = EditorGUILayout.IntSlider("Difficulty Tier", level.difficultyTier, 1, 5);
            string palette = EditorGUILayout.TextField("Palette", level.paletteId);
            int seed = EditorGUILayout.IntField("Scramble Seed", level.scrambleSeed);
            int moves = EditorGUILayout.IntField("Recommended Moves", level.recommendedMoves);
            bool lockDefault = EditorGUILayout.Toggle("Lock Correct By Default", level.lockCorrectPiecesByDefault);
            string hint = EditorGUILayout.TextField("Hint", level.hintMetadata.message);
            string tutorialId = EditorGUILayout.TextField("Tutorial ID", level.tutorialMetadata.tutorialId);
            string tutorialMessage = EditorGUILayout.TextField("Tutorial Message", level.tutorialMetadata.message);
            string tags = EditorGUILayout.TextField("Tags (comma separated)", string.Join(",", level.levelTags ?? Array.Empty<string>()));
            string notes = EditorGUILayout.TextArea(level.designerNotes ?? string.Empty, GUILayout.MinHeight(36f));
            if (EditorGUI.EndChangeCheck())
            {
                Record("Edit level settings");
                level.levelNumber = Mathf.Max(1, levelNumber);
                level.levelId = levelId;
                level.displayName = displayName;
                level.targetObjectName = targetName;
                level.boardWidth = width;
                level.boardHeight = height;
                level.difficultyTier = difficulty;
                level.paletteId = palette;
                level.scrambleSeed = seed;
                level.recommendedMoves = Mathf.Max(1, moves);
                level.lockCorrectPiecesByDefault = lockDefault;
                level.hintMetadata.message = hint;
                level.tutorialMetadata.tutorialId = tutorialId;
                level.tutorialMetadata.message = tutorialMessage;
                level.levelTags = SplitTags(tags);
                level.designerNotes = notes;
            }
        }

        private void DrawPieceList(LevelJsonDocument level)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(185f));
            EditorGUILayout.LabelField("Required Pieces", EditorStyles.boldLabel);
            pieceScroll = EditorGUILayout.BeginScrollView(pieceScroll, GUILayout.Height(360f));
            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceJson piece = level.pieces[i];
                GUIStyle style = i == selectedPiece ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (GUILayout.Button((i + 1) + ". " + (piece?.displayName ?? "<null>"), style)) selectedPiece = i;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+")) AddPiece(level);
            EditorGUI.BeginDisabledGroup(level.pieces.Length == 0);
            if (GUILayout.Button("−")) RemovePiece(level);
            if (GUILayout.Button("↑")) MovePiece(level, -1);
            if (GUILayout.Button("↓")) MovePiece(level, 1);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedPiece(LevelJsonDocument level)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(310f));
            EditorGUILayout.LabelField("Piece", EditorStyles.boldLabel);
            if (level.pieces.Length == 0)
            {
                EditorGUILayout.HelpBox("Add a required piece to begin authoring.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            selectedPiece = Mathf.Clamp(selectedPiece, 0, level.pieces.Length - 1);
            PieceJson piece = level.pieces[selectedPiece];
            EditorGUI.BeginChangeCheck();
            string id = EditorGUILayout.TextField("Piece ID", piece.pieceId);
            string name = EditorGUILayout.TextField("Display Name", piece.displayName);
            PieceShapeType shape = ParseShape(piece.shapeType);
            shape = (PieceShapeType)EditorGUILayout.EnumPopup("Shape", shape);
            string color = EditorGUILayout.TextField("Color", piece.colorId);
            int width = EditorGUILayout.IntField("Art Width", piece.width);
            int height = EditorGUILayout.IntField("Art Height", piece.height);
            Vector2Int pivot = EditorGUILayout.Vector2IntField("Logical Pivot", ToVector(piece.logicalPivot));
            Vector2 visualPivot = EditorGUILayout.Vector2Field("Visual Pivot", ToVector(piece.visualPivot));
            Vector2Int target = EditorGUILayout.Vector2IntField("Target Position", ToVector(piece.targetPosition));
            int targetRotation = EditorGUILayout.IntPopup("Target Rotation", piece.targetRotation, RotationLabels, Rotations);
            Vector2Int start = EditorGUILayout.Vector2IntField("Starting Position", ToVector(piece.startingPosition));
            int startRotation = EditorGUILayout.IntPopup("Starting Rotation", piece.startingRotation, RotationLabels, Rotations);
            bool strict = EditorGUILayout.Toggle("Strict Visual Rotation", piece.strictTargetRotation);
            bool startsLocked = EditorGUILayout.Toggle("Starts Locked", piece.startsLocked);
            bool locks = EditorGUILayout.Toggle("Locks When Correct", piece.locksWhenCorrect);
            int sort = EditorGUILayout.IntField("Sorting Priority", piece.sortingPriority);
            Vector2 overhang = EditorGUILayout.Vector2Field("Visual Overhang", ToVector(piece.visualOverhang));
            if (EditorGUI.EndChangeCheck())
            {
                Record("Edit puzzle piece");
                piece.pieceId = id;
                piece.displayName = name;
                piece.shapeType = shape.ToString();
                piece.colorId = color;
                piece.width = Mathf.Max(1, width);
                piece.height = Mathf.Max(1, height);
                piece.logicalPivot = FromVector(pivot);
                piece.visualPivot = FromVector(visualPivot);
                piece.targetPosition = FromVector(target);
                piece.targetRotation = targetRotation;
                piece.startingPosition = FromVector(start);
                piece.startingRotation = startRotation;
                piece.strictTargetRotation = strict;
                piece.startsLocked = startsLocked;
                piece.locksWhenCorrect = locks;
                piece.sortingPriority = sort;
                piece.visualOverhang = FromVector(overhang);
            }

            DrawAllowedRotations(piece);
            DrawFootprint(piece);
            DrawPolygon(piece);
            DrawDecorations(piece);
            EditorGUILayout.EndVertical();
        }

        private void DrawAllowedRotations(PieceJson piece)
        {
            EditorGUILayout.LabelField("Allowed Rotations", EditorStyles.miniBoldLabel);
            HashSet<int> active = new HashSet<int>(piece.allowedRotations ?? Array.Empty<int>());
            EditorGUILayout.BeginHorizontal();
            bool changed = false;
            for (int i = 0; i < Rotations.Length; i++)
            {
                bool before = active.Contains(Rotations[i]);
                bool after = GUILayout.Toggle(before, RotationLabels[i], EditorStyles.miniButton);
                if (before != after)
                {
                    changed = true;
                    if (after) active.Add(Rotations[i]); else active.Remove(Rotations[i]);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (changed)
            {
                Record("Edit allowed rotations");
                List<int> rotations = new List<int>(active);
                rotations.Sort();
                piece.allowedRotations = rotations.ToArray();
            }
        }

        private void DrawFootprint(PieceJson piece)
        {
            EditorGUILayout.LabelField("Footprint (local cells)", EditorStyles.miniBoldLabel);
            HashSet<long> occupied = ToCellSet(piece.footprint);
            for (int y = 3; y >= -1; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = -1; x <= 3; x++)
                {
                    long key = Key(x, y);
                    Color before = GUI.backgroundColor;
                    GUI.backgroundColor = occupied.Contains(key) ? new Color(0.25f, 0.75f, 1f) : Color.white;
                    if (GUILayout.Button(x + "," + y, cellStyle, GUILayout.Width(48f), GUILayout.Height(24f)))
                    {
                        Record("Edit footprint");
                        if (!occupied.Add(key)) occupied.Remove(key);
                        piece.footprint = FromCellSet(occupied);
                    }
                    GUI.backgroundColor = before;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPolygon(PieceJson piece)
        {
            if (piece.shapeType != PieceShapeType.CustomPolygon.ToString()) return;
            EditorGUILayout.LabelField("Custom Polygon", EditorStyles.miniBoldLabel);
            for (int i = 0; i < piece.customPolygonPoints.Length; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector2 point = EditorGUILayout.Vector2Field("Point " + i, ToVector(piece.customPolygonPoints[i]));
                if (EditorGUI.EndChangeCheck())
                {
                    Record("Edit polygon point");
                    piece.customPolygonPoints[i] = FromVector(point);
                }
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Point"))
            {
                Record("Add polygon point");
                Array.Resize(ref piece.customPolygonPoints, piece.customPolygonPoints.Length + 1);
                piece.customPolygonPoints[piece.customPolygonPoints.Length - 1] = new Float2Json();
            }
            if (GUILayout.Button("Remove Point") && piece.customPolygonPoints.Length > 0)
            {
                Record("Remove polygon point");
                Array.Resize(ref piece.customPolygonPoints, piece.customPolygonPoints.Length - 1);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDecorations(PieceJson piece)
        {
            for (int i = 0; i < piece.decorativeStuds.Length; i++)
            {
                DecorativeStudJson stud = piece.decorativeStuds[i];
                EditorGUI.BeginChangeCheck();
                Vector2 position = EditorGUILayout.Vector2Field("Stud " + (i + 1), ToVector(stud.position));
                float radius = EditorGUILayout.FloatField("Stud Radius", stud.radius);
                if (EditorGUI.EndChangeCheck())
                {
                    Record("Edit decorative stud");
                    stud.position = FromVector(position);
                    stud.radius = Mathf.Max(0.01f, radius);
                }
            }
            for (int i = 0; i < piece.recessedHoles.Length; i++)
            {
                RecessedHoleJson hole = piece.recessedHoles[i];
                EditorGUI.BeginChangeCheck();
                Vector2 position = EditorGUILayout.Vector2Field("Hole " + (i + 1), ToVector(hole.position));
                float radius = EditorGUILayout.FloatField("Hole Radius", hole.radius);
                if (EditorGUI.EndChangeCheck())
                {
                    Record("Edit recessed hole");
                    hole.position = FromVector(position);
                    hole.radius = Mathf.Max(0.01f, radius);
                }
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Stud"))
            {
                Record("Add decorative stud");
                Array.Resize(ref piece.decorativeStuds, piece.decorativeStuds.Length + 1);
                piece.decorativeStuds[piece.decorativeStuds.Length - 1] = new DecorativeStudJson();
            }
            if (GUILayout.Button("Add Hole"))
            {
                Record("Add recessed hole");
                Array.Resize(ref piece.recessedHoles, piece.recessedHoles.Length + 1);
                piece.recessedHoles[piece.recessedHoles.Length - 1] = new RecessedHoleJson();
            }
            if (GUILayout.Button("Remove Last") && (piece.decorativeStuds.Length > 0 || piece.recessedHoles.Length > 0))
            {
                Record("Remove decoration");
                if (piece.recessedHoles.Length > 0) Array.Resize(ref piece.recessedHoles, piece.recessedHoles.Length - 1);
                else Array.Resize(ref piece.decorativeStuds, piece.decorativeStuds.Length - 1);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Studs: " + piece.decorativeStuds.Length + "   Holes: " + piece.recessedHoles.Length, EditorStyles.miniLabel);
        }

        private void DrawBoardArea(LevelJsonDocument level)
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(330f));
            EditorGUILayout.BeginHorizontal();
            editMode = (BoardEditMode)GUILayout.Toolbar((int)editMode, new[] { "Target", "Starting" });
            if (GUILayout.Button("Scramble", GUILayout.Width(80f))) GenerateScramble(level);
            if (GUILayout.Button("Validate", GUILayout.Width(80f))) ValidateCurrent(level);
            EditorGUILayout.EndHorizontal();
            DrawBoard(level, editMode == BoardEditMode.Target);
            EditorGUILayout.HelpBox("Select a piece, then click a board cell to move its target or starting origin. Red cells indicate an overlap or out-of-bounds pose.", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawBoard(LevelJsonDocument level, bool target)
        {
            Dictionary<long, int> owners = new Dictionary<long, int>();
            HashSet<long> conflicts = new HashSet<long>();
            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceJson piece = level.pieces[i];
                Int2Json position = target ? piece.targetPosition : piece.startingPosition;
                int rotation = target ? piece.targetRotation : piece.startingRotation;
                List<Int2Json> cells = LevelContentValidator.GetOccupiedCells(piece, position, rotation);
                for (int j = 0; j < cells.Count; j++)
                {
                    long key = Key(cells[j].x, cells[j].y);
                    if (owners.ContainsKey(key)) conflicts.Add(key); else owners.Add(key, i);
                    if (cells[j].x < 0 || cells[j].x >= level.boardWidth || cells[j].y < 0 || cells[j].y >= level.boardHeight) conflicts.Add(key);
                }
            }

            float size = Mathf.Min(48f, 330f / Mathf.Max(level.boardWidth, level.boardHeight));
            for (int y = level.boardHeight - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < level.boardWidth; x++)
                {
                    long key = Key(x, y);
                    Color original = GUI.backgroundColor;
                    int owner;
                    if (conflicts.Contains(key)) GUI.backgroundColor = new Color(1f, 0.25f, 0.25f);
                    else if (owners.TryGetValue(key, out owner)) GUI.backgroundColor = PieceColor(level.pieces[owner].colorId, owner == selectedPiece);
                    else GUI.backgroundColor = new Color(0.18f, 0.23f, 0.31f);
                    string label = owners.TryGetValue(key, out owner) ? (owner + 1).ToString() : string.Empty;
                    if (GUILayout.Button(label, cellStyle, GUILayout.Width(size), GUILayout.Height(size)) && level.pieces.Length > 0)
                    {
                        Record("Move piece on board");
                        if (target) level.pieces[selectedPiece].targetPosition = new Int2Json(x, y);
                        else level.pieces[selectedPiece].startingPosition = new Int2Json(x, y);
                    }
                    GUI.backgroundColor = original;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void NewLevel()
        {
            int number = FindFirstAvailableNumber();
            Record("New level");
            state.document = CreateNewDocument(number);
            currentPath = null;
            selectedPiece = 0;
            status = "Created a new unsaved level.";
        }

        private void OpenLevel()
        {
            string absoluteFolder = LevelJsonSerializer.ToAbsolutePath(LevelJsonSchema.SourceFolder);
            string selected = EditorUtility.OpenFilePanel("Open Toy Puzzle level", absoluteFolder, "json");
            if (string.IsNullOrEmpty(selected)) return;
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(root) || !Path.GetFullPath(selected).StartsWith(Path.GetFullPath(absoluteFolder), StringComparison.OrdinalIgnoreCase))
            {
                status = "Choose a JSON file inside " + LevelJsonSchema.SourceFolder + ".";
                return;
            }
            string assetPath = selected.Substring(root.Length + 1).Replace('\\', '/');
            LoadLevelPath(assetPath);
        }

        private void LoadLevelPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            Record("Open level");
            state.document = LevelJsonSerializer.Load(assetPath);
            LevelSchemaMigrator.MigrateToCurrent(state.document);
            currentPath = assetPath;
            selectedPiece = 0;
            status = "Opened " + assetPath;
        }

        private void DuplicateLevel()
        {
            LevelJsonDocument clone = LevelJsonSerializer.Deserialize(LevelJsonSerializer.Serialize(state.document), "duplicate");
            int number = FindFirstAvailableNumber();
            clone.levelNumber = number;
            clone.levelId = "level_" + number.ToString("D3");
            clone.displayName += " Copy";
            clone.scrambleSeed += 7919;
            Record("Duplicate level");
            state.document = clone;
            currentPath = null;
            status = "Duplicated as " + clone.levelId + "; save to create the source file.";
        }

        private void SaveLevel()
        {
            LevelValidationResult validation = LevelContentValidator.Validate(state.document);
            if (!validation.IsValid)
            {
                status = Summarize(validation);
                return;
            }
            string path = LevelJsonSchema.SourceFolder + "/level_" + state.document.levelNumber.ToString("D3") + "_" + Slug(state.document.targetObjectName) + ".json";
            if (!string.IsNullOrEmpty(currentPath) && !string.Equals(currentPath, path, StringComparison.Ordinal) && File.Exists(LevelJsonSerializer.ToAbsolutePath(path)))
            {
                status = "Cannot save: " + path + " already exists.";
                return;
            }
            LevelJsonSerializer.SaveAtomic(path, state.document);
            currentPath = path;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            LevelImportReport report = LevelImportPipeline.Run(true);
            RefreshLevelList();
            status = report.RejectedCount == 0
                ? "Saved JSON and rebuilt all editable level prefabs. " + report.ImportedCount + " levels are in the prefab catalog."
                : report.ToSummary();
        }

        private void ImportLevels()
        {
            LevelImportReport report = LevelImportPipeline.Run(true);
            RefreshLevelList();
            status = report.ToSummary();
        }

        private void OpenPrefab()
        {
            if (state == null || state.document == null) return;
            LevelDefinition definition = RuntimeLevelAssetBuilder.Convert(state.document);
            string prefabPath = LevelPrefabAssetBuilder.GetPrefabPath(definition);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                status = "Prefab not found. Use Save + Prefab or Rebuild All Prefabs first.";
                return;
            }
            AssetDatabase.OpenAsset(prefab);
            Selection.activeObject = prefab;
            status = "Opened " + prefabPath;
        }

        private void GenerateThumbnail()
        {
            SaveLevel();
            if (!EditorApplication.ExecuteMenuItem("Tools/Toy Puzzle/Generate Thumbnails"))
                status = "Thumbnail generator menu is not currently available.";
        }

        private void TestLevel()
        {
            SaveLevel();
            LevelValidationResult validation = LevelContentValidator.Validate(state.document);
            if (!validation.IsValid)
            {
                status = Summarize(validation);
                return;
            }
            ImportLevels();
            EditorPrefs.SetInt("ToyPuzzle.TestLevelNumber", state.document.levelNumber);
            EditorApplication.isPlaying = true;
        }

        private void DeleteLevel()
        {
            if (string.IsNullOrEmpty(currentPath)) { status = "This level has not been saved."; return; }
            if (!EditorUtility.DisplayDialog("Delete level source?", "Delete " + currentPath + "? This removes the authoritative JSON source.", "Delete", "Cancel")) return;
            AssetDatabase.DeleteAsset(currentPath);
            AssetDatabase.DeleteAsset(LevelPrefabAssetBuilder.GetPrefabPath(RuntimeLevelAssetBuilder.Convert(state.document)));
            currentPath = null;
            state.document = CreateNewDocument(FindFirstAvailableNumber());
            LevelImportPipeline.Run(true);
            RefreshLevelList();
            status = "Deleted the selected source. Version control may be used to recover it.";
        }

        private void RefreshLevelList()
        {
            cachedLevelPaths = LevelImportPipeline.DiscoverSourcePaths();
            Repaint();
        }

        private void GenerateScramble(LevelJsonDocument level)
        {
            Record("Generate deterministic scramble");
            System.Random random = new System.Random(level.scrambleSeed);
            HashSet<long> occupied = new HashSet<long>();
            for (int i = 0; i < level.pieces.Length; i++)
            {
                PieceJson piece = level.pieces[i];
                piece.startsLocked = false;
                bool placed = false;
                for (int attempt = 0; attempt < 512 && !placed; attempt++)
                {
                    int rotation = piece.allowedRotations[random.Next(piece.allowedRotations.Length)];
                    Int2Json position = new Int2Json(random.Next(level.boardWidth), random.Next(level.boardHeight));
                    List<Int2Json> cells = LevelContentValidator.GetOccupiedCells(piece, position, rotation);
                    if (!CanUse(cells, occupied, level.boardWidth, level.boardHeight)) continue;
                    piece.startingPosition = position;
                    piece.startingRotation = rotation;
                    for (int cell = 0; cell < cells.Count; cell++) occupied.Add(Key(cells[cell].x, cells[cell].y));
                    placed = true;
                }
                if (!placed)
                {
                    status = "Could not generate a valid scramble for piece " + piece.pieceId + ". Reduce occupancy or simplify footprints.";
                    return;
                }
            }
            if (LevelContentValidator.Validate(level).IsValid) status = "Generated a deterministic valid scramble.";
            else status = "Scramble generated, but level validation still reports issues.";
        }

        private void ValidateCurrent(LevelJsonDocument level)
        {
            status = Summarize(LevelContentValidator.Validate(level));
        }

        private void AddPiece(LevelJsonDocument level)
        {
            Record("Add puzzle piece");
            int index = level.pieces.Length;
            Array.Resize(ref level.pieces, index + 1);
            level.pieces[index] = CreatePiece(index + 1);
            selectedPiece = index;
        }

        private void RemovePiece(LevelJsonDocument level)
        {
            if (level.pieces.Length == 0) return;
            Record("Remove puzzle piece");
            for (int i = selectedPiece; i < level.pieces.Length - 1; i++) level.pieces[i] = level.pieces[i + 1];
            Array.Resize(ref level.pieces, level.pieces.Length - 1);
            selectedPiece = Mathf.Clamp(selectedPiece, 0, Mathf.Max(0, level.pieces.Length - 1));
        }

        private void MovePiece(LevelJsonDocument level, int direction)
        {
            int next = selectedPiece + direction;
            if (next < 0 || next >= level.pieces.Length) return;
            Record("Reorder puzzle piece");
            PieceJson temporary = level.pieces[selectedPiece];
            level.pieces[selectedPiece] = level.pieces[next];
            level.pieces[next] = temporary;
            selectedPiece = next;
        }

        private void Record(string label)
        {
            Undo.RecordObject(state, label);
            EditorUtility.SetDirty(state);
        }

        private void EnsureStyles()
        {
            if (cellStyle == null) cellStyle = new GUIStyle(EditorStyles.miniButton) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
        }

        private static readonly int[] Rotations = { 0, 90, 180, 270 };
        private static readonly string[] RotationLabels = { "0°", "90°", "180°", "270°" };

        private static LevelJsonDocument CreateNewDocument(int number)
        {
            return new LevelJsonDocument
            {
                levelId = "level_" + number.ToString("D3"),
                levelNumber = number,
                displayName = "New Level",
                targetObjectName = "New Object",
                boardWidth = 6,
                boardHeight = 6,
                difficultyTier = Mathf.Clamp((number - 1) / 10 + 1, 1, 5),
                scrambleSeed = 1000 + number,
                recommendedMoves = 4,
                pieces = new[] { CreatePiece(1) },
                designerNotes = "Authored with the Toy Puzzle Level Editor."
            };
        }

        private static PieceJson CreatePiece(int number)
        {
            return new PieceJson
            {
                pieceId = "piece_" + number.ToString("D2"),
                displayName = "Piece " + number,
                footprint = new[] { new Int2Json(0, 0) },
                targetPosition = new Int2Json(1, 1),
                startingPosition = new Int2Json(0, 0)
            };
        }

        private static string[] SplitTags(string value)
        {
            string[] raw = (value ?? string.Empty).Split(',');
            List<string> tags = new List<string>();
            for (int i = 0; i < raw.Length; i++)
            {
                string tag = raw[i].Trim();
                if (!string.IsNullOrEmpty(tag)) tags.Add(tag);
            }
            return tags.ToArray();
        }

        private static string Summarize(LevelValidationResult validation)
        {
            if (validation.IsValid && validation.Issues.Count == 0) return "Level is valid.";
            System.Text.StringBuilder builder = new System.Text.StringBuilder(validation.IsValid ? "Level is valid with warnings:" : "Level is invalid:");
            for (int i = 0; i < validation.Issues.Count; i++) builder.AppendLine().Append(validation.Issues[i]);
            return builder.ToString();
        }

        private static int FindFirstAvailableNumber()
        {
            HashSet<int> used = new HashSet<int>();
            string[] paths = LevelImportPipeline.DiscoverSourcePaths();
            for (int i = 0; i < paths.Length; i++)
            {
                try { used.Add(LevelJsonSerializer.Load(paths[i]).levelNumber); }
                catch (Exception) { }
            }
            int number = 1;
            while (used.Contains(number)) number++;
            return number;
        }

        private static bool CanUse(List<Int2Json> cells, HashSet<long> occupied, int width, int height)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                Int2Json cell = cells[i];
                if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height || occupied.Contains(Key(cell.x, cell.y))) return false;
            }
            return true;
        }

        private static string Slug(string value)
        {
            string source = (value ?? "level").ToLowerInvariant();
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            bool underscore = false;
            for (int i = 0; i < source.Length; i++)
            {
                char character = source[i];
                if (character >= 'a' && character <= 'z' || character >= '0' && character <= '9')
                {
                    builder.Append(character);
                    underscore = false;
                }
                else if (!underscore && builder.Length > 0)
                {
                    builder.Append('_');
                    underscore = true;
                }
            }
            return builder.ToString().TrimEnd('_');
        }

        private static PieceShapeType ParseShape(string value)
        {
            return Enum.TryParse(value, out PieceShapeType parsed) ? parsed : PieceShapeType.RoundedRectangle;
        }

        private static Vector2Int ToVector(Int2Json value) => value == null ? Vector2Int.zero : new Vector2Int(value.x, value.y);
        private static Vector2 ToVector(Float2Json value) => value == null ? Vector2.zero : new Vector2(value.x, value.y);
        private static Int2Json FromVector(Vector2Int value) => new Int2Json(value.x, value.y);
        private static Float2Json FromVector(Vector2 value) => new Float2Json(value.x, value.y);
        private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;

        private static HashSet<long> ToCellSet(Int2Json[] values)
        {
            HashSet<long> cells = new HashSet<long>();
            if (values != null) for (int i = 0; i < values.Length; i++) if (values[i] != null) cells.Add(Key(values[i].x, values[i].y));
            return cells;
        }

        private static Int2Json[] FromCellSet(HashSet<long> cells)
        {
            List<Int2Json> result = new List<Int2Json>(cells.Count);
            foreach (long value in cells) result.Add(new Int2Json((int)(value >> 32), (int)value));
            result.Sort((left, right) => left.y != right.y ? left.y.CompareTo(right.y) : left.x.CompareTo(right.x));
            return result.ToArray();
        }

        private static Color PieceColor(string colorId, bool selected)
        {
            int hash = (colorId ?? string.Empty).GetHashCode();
            float hue = Mathf.Abs(hash % 997) / 997f;
            Color color = Color.HSVToRGB(hue, 0.58f, selected ? 1f : 0.8f);
            return color;
        }
    }
}
