# Level Data Pipeline

JSON under `Assets/Game/Data/Levels/Source` is the source-safe authoring format. Each valid source generates an editable prefab under `Assets/Game/Prefabs/Levels`; `LevelPrefabCatalog.asset` passes all prefabs to the game in level-number order. The player never parses source JSON.

## Editing and import

Use `Tools > Toy Puzzle > Open Level Editor`. Its left sidebar lists all 50 levels; select one, edit its properties, pieces, starting grid, and target grid, then choose **Save + Prefab**. **Rebuild All Prefabs** validates and regenerates the complete `LevelPrefabCatalog`. Invalid sources never enter the catalog.

The source defines board dimensions, target metadata, deterministic seed, palette, piece footprints/pivots, start and target poses, allowed rotations, locking, hints, tutorials, reward metadata, tags, notes, and thumbnail configuration.

## Stable content

Prefab filenames follow stable level numbers (`PF_Level_001.prefab` through `PF_Level_050.prefab`). You may inspect or temporarily tune a prefab directly; use the Level Editor for persistent source-safe changes because rebuilding prefabs reapplies JSON. Add Level 51 by creating a unique source, saving it through the editor, and verifying catalog ordering.

## Preview parity

The runtime target card is always assembled from target poses in `RuntimeLevelData`. Thumbnails should use that same generated asset and the same neutral piece sprite/palette resolution. Separately painted approximations are not accepted.
