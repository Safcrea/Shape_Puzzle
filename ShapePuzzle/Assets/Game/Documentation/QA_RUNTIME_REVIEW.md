# Runtime QA Review

Reviewed against `Toy_Puzzle_Master_Prompt.md`, `Toy_Puzzle_Level_Distribution.md`, and `Reference_01.png` on 2026-07-22. This is a static integration review; Unity compilation and PlayMode execution remain pending until the parallel implementation work is merged.

## Blocking finding

1. **Runtime pieces do not implement the required visual shape system.** `PuzzlePieceView` renders every footprint as independent square cells (or circular cells for Circle/Ring). Triangle, trapezoid, wedge, semicircle, quarter-circle, L/T/U/Z/cross, custom polygon, overhang, studs, holes, inset panels, and art-generation metadata are not represented. This cannot match the supplied soft 3D toy reference and also makes the target preview diverge from the authored shape semantics.

## Resolved during integration

- `AudioService.SetSoundEnabled(false)` now safely accepts configuration before `Awake`, removing the undefined cross-component startup-order failure.
- `ScreenManager.Show` now closes all other bindings when selecting a base screen, preventing stale popups from intercepting the new flow.
- `ResponsiveGameLayout` now accepts grid dimensions and sizes rectangular boards with square cells inside the shared responsive envelope.
- Drag-time placement validation now uses a footprint cached at drag start, eliminating coordinate-array creation per pointer event.
- Translation-only `PuzzlePieceView.SetPose` calls now retain visual children; rotation rebuilds deactivate old children before deferred destruction.

## Major findings

- `PuzzleBoardView.ClearHint` still uses deferred `Destroy` without first deactivating hints, so requesting a replacement hint in the same frame can briefly retain both highlight sets.
- Successful move commits and the occasional rotation/undo still allocate coordinate arrays; verify their measured impact, although the continuous drag path has been corrected.
- A valid drop is committed directly to the snapped model/view position. The required soft snap animation is not used; `ToyTween.Move` exists but is not wired into placement or undo.
- `sortingPriority`, `visualPivot`, `visualOverhang`, `width`, `height`, custom polygon points, and decorative metadata are unused by runtime presentation and hit testing.
- `PuzzleBoardView.Build` enforces a minimum 24-pixel cell after fitting. On a board rect smaller than the required grid, this can overflow the available frame rather than fitting safely.
- `PuzzleGameController.LoadLevel` assumes a `PuzzleBoardView` will be found and dereferences it without a clear configuration exception. Generated content should guarantee the reference, but a guard would convert missing wiring into an actionable failure.

## Automated coverage added

- `GameplayControllerPlayModeTests`
  - level load creates a session and board views synchronously;
  - model move events update the view;
  - translation retains existing visual geometry;
  - controller undo restores model, lock state, occupancy-facing pose, and view;
  - selected-piece rotation updates the logical pose and rotated view bounds.
- `RuntimeLifecyclePlayModeTests`
  - configuring disabled audio before `Awake` is safe;
  - selecting a new base screen closes a previously visible popup;
  - rectangular board layout preserves square cells.

The lifecycle tests now encode regressions for the startup, popup cleanup, and rectangular-layout fixes.

## Pending verification

- Recompile after integration, then run both EditMode and PlayMode suites.
- Inspect the generated scene for missing serialized references and console errors.
- Exercise mouse drag, touch drag/cancel, focus loss, double-tap rotation, every popup exit route, and safe-area changes.
- Profile idle and continuous drag on the target mobile device matrix; confirm allocations, frame time, batches, draw calls, and texture memory against the documented budgets.
- Compare gameplay pieces and reference-card pieces visually against the same authored data and `Reference_01.png`.
