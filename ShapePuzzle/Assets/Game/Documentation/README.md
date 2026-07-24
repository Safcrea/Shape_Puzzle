# Toy Puzzle

Toy Puzzle is a portrait mobile reconstruction game built for Unity 6000.3 and URP. Levels are authored as JSON, imported to `RuntimeLevelData`, and played through a deterministic integer-grid session. Presentation uses responsive Screen Space Overlay uGUI and editor-baked soft-toy sprites, so runtime play never generates textures or parses source JSON.

## Generate the game

Open the project, allow scripts to compile, then run:

`Tools > Toy Puzzle > Generate Complete Game`

The command generates art and audio, validates all levels, rebuilds the 50 level prefabs and their catalog, rebuilds atlases and `PF_AppUI`, creates `Assets/Game/Scenes/Game.unity`, configures it as the only enabled build scene, and applies portrait Android/iOS settings. The workflow is deterministic and can be run repeatedly.

Focused commands are available for art, audio, atlases, levels, thumbnails, validation, and presentation rebuilding. `Clear Generated Presentation` deletes presentation outputs only; it never deletes level JSON under `Assets/Game/Data/Levels/Source`.

## Runtime flow

`PuzzleAppController` owns save/progression coordination and instantiates the selected `PuzzleLevelPrefab`. `ToyUIController` publishes navigation and action events. `PuzzleGameController` owns the active session and drives `PuzzleBoardView`. The same target poses and neutral piece sprites render both the board pieces and the target reference. The home, level-select, gameplay, settings, pause, completion, and confirmation screens live in one `Game` scene.

Mouse input works through the EventSystem in the Editor; mobile builds use the Input System UI module when available. The generated scene is safe-area aware and contains no real-time lights, shadows, post-processing, or runtime art generation.

## Generated content

- Art: `Assets/Game/Art/Generated`
- Audio: `Assets/Game/Audio/Generated`
- Atlases: `Assets/Game/Art/Atlases`
- UI/effect prefabs: `Assets/Game/Prefabs`
- Startup scene: `Assets/Game/Scenes/Game.unity`
- Level prefabs: `Assets/Game/Prefabs/Levels/PF_Level_001.prefab` through `PF_Level_050.prefab`
- Prefab catalog: `Assets/Game/Data/Levels/Generated/LevelPrefabCatalog.asset`

Do not hand-edit generated assets. Change source JSON, palette defaults, generators, or builder code and regenerate.
