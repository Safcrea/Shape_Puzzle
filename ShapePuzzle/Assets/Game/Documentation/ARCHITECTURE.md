# Architecture

## Runtime

- Data: each `PF_Level_NNN` prefab owns a serialized `PuzzleLevelPrefab` definition. `LevelPrefabCatalog` keeps all playable prefabs ordered from 1 through 50. Runtime code consumes prefabs, not JSON.
- Grid/session: integer coordinates, rotated footprints, occupancy, placement validation, move history, hints, correctness, and completion remain independent of visuals.
- Gameplay: one `PuzzleGameController` centralizes selection, dragging, rotation, cancellation, sounds, haptics, and session events. Pieces have no `Update` loop.
- Presentation: `PuzzleBoardView` builds cells, pieces, and the target preview from one level definition. `ToyUIController` and `ScreenManager` manage the single-scene screen flow.
- App flow: `PuzzleAppController` coordinates saves, progression, prefab instantiation, settings, and UI directly in the `Game` scene. `AudioService` owns a fixed AudioSource pool; `HapticService` hides the platform fallback; `ToyEffectPool` owns a fixed visual-effect pool.

## Editor

`ToyArtGenerator` rasterizes supersampled toy sprites and assigns mobile UI import settings. `ToyAudioGenerator` synthesizes short normalized PCM cues and imports them with ADPCM. `ToySpriteAtlasBuilder` groups UI, piece, and effect folders when the Sprite Atlas API is present. `ToyPuzzlePresentationBuilder` creates UI prefabs and the `Game` scene entirely through Unity APIs and serializes all known runtime references.

Level import, authoring, schema migration, validation, thumbnail generation, and catalog building remain editor-only. The complete builder invokes their menu commands when installed.

## Data flow

`JSON source -> schema/content validation -> PF_Level_NNN -> LevelPrefabCatalog -> PuzzleAppController -> PuzzleSession -> board/reference UI`

Stable piece and level IDs originate in JSON. Regeneration updates generated assets without changing source IDs. No scene reload is required between levels.

## Assembly boundaries

Runtime source belongs to `ToyPuzzle.Runtime`. Art generation and build tools use separate editor-only assembly definitions so UnityEditor code cannot enter players.
