# QA Checklist

## Generation and data

- Generate Complete Game completes twice without duplicates or lost source changes.
- All 50 JSON sources validate, import, and appear once in `LevelCatalog`.
- All target/start poses are in bounds, non-overlapping, and start unsolved.
- `Game` is the only enabled build scene and has no missing scripts/references.
- `LevelPrefabCatalog` contains all 50 ordered prefab levels.

## Gameplay

- Mouse and one-finger touch select, drag with preserved offset, snap, rotate, and cancel safely.
- Invalid, overlap, and out-of-bounds actions restore the previous pose and do not add moves.
- Move count, locking, undo, reset, deterministic hint, timer, pause, completion, next, and replay work.
- Every target reference uses the same pieces/poses as gameplay and remains visible.
- All 50 levels are solvable through normal interaction.

## UI and accessibility

- Home, level selection, gameplay, settings, pause, completion, and both confirmations work.
- Bottom controls are ordered Undo, Hint, Rotate, Pause and meet touch-size targets.
- Tall/short phones, notches, navigation areas, tablets, and orientation changes keep content inside the safe area with square cells.
- Sound, music, haptics, and reduced motion persist; no feedback relies on color alone.
- Text remains readable and target thumbnails remain recognizable at the smallest supported display.

## Performance and builds

- No recurring idle GC allocation; dragging remains near zero allocation.
- No physics authority, runtime procedural art, post-processing, or real-time shadows.
- Only one completion celebration is active and pooled effects return cleanly.
- Android and iOS development builds launch to Home, resume from background safely, save progress, and recover from corrupted save data.
- Test airplane/early, animal/mid, and Star Trophy/final levels on representative low-end hardware.

Platform-native subtle haptic amplitude is not available through Unity's dependency-free fallback on every device; unsupported platforms intentionally no-op and significant mobile cues use `Handheld.Vibrate`.
