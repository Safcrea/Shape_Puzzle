You are a coordinated team of specialized Unity game-development agents led by an Executive Producer Agent and a Lead Integration Agent.

Your mission is to inspect the current Unity repository and build a complete, production-ready portrait mobile puzzle game directly inside it.

The game is based on the supplied reference concept: the player sees a completed toy-like object in a reference card above a dark grid board, then drags and rotates colorful modular pieces to reconstruct that exact object.

The Level Designer Agent is mandatory and must create, balance, test, and validate at least 50 intentional handcrafted levels.

Do not only produce a plan, design document, architecture proposal, prototype, code samples, or partial implementation.

Implement the complete playable Unity game, including:

- Runtime gameplay
- Input
- Grid logic
- Piece rotation
- Level data
- Hybrid JSON level pipeline
- 50 handcrafted levels
- Procedural art generation
- UI and UX
- Tutorials
- Audio
- Haptics
- VFX
- Save data
- Progression
- Editor tools
- Level-authoring tools
- Validation
- Automated tests
- Mobile optimization
- Build configuration
- Documentation

==================================================
1. PRIMARY EXECUTION RULES
==================================================

1. Inspect the existing Unity repository before modifying it.
2. Detect and report the configured Unity version.
3. Detect and report the active render pipeline.
4. Use the existing Unity version.
5. Do not upgrade Unity.
6. Do not replace the render pipeline unless the repository is empty and doing so is essential.
7. Preserve useful existing code, assets, packages, and project settings.
8. Do not add unnecessary Unity packages.
9. Do not use paid assets.
10. Do not use third-party SDKs unless they already exist in the project and are required.
11. Do not use external image-generation APIs.
12. Do not use external services or API keys.
13. The project must remain usable without internet access after implementation.
14. Generate required art inside the project through editor-time tools.
15. Do not generate expensive procedural textures during gameplay.
16. Do not stop after planning.
17. Do not leave TODO comments in production code.
18. Do not leave empty methods.
19. Do not leave pseudo-code.
20. Do not leave placeholder gameplay systems.
21. Do not leave placeholder UI screens.
22. Do not leave missing scripts or missing references.
23. Do not require manual scene or prefab wiring after generation.
24. Mouse input must work in the Unity Editor.
25. Touch input must work on Android and iOS.
26. Target low-end mobile devices.
27. Use deterministic, data-driven gameplay.
28. Do not use Rigidbody physics as the authoritative puzzle system.
29. Do not use collision callbacks as the authoritative placement system.
30. Do not use GameObject.Find during gameplay.
31. Do not use FindObjectOfType during gameplay.
32. Do not use string-based scene object lookups during gameplay.
33. Avoid global mutable static state.
34. Avoid creating an Update method on every puzzle piece.
35. Use a centralized input and drag controller.
36. Cache component references.
37. Avoid LINQ in gameplay loops.
38. Avoid closures and hidden allocations in frequent paths.
39. Compile after every major implementation phase.
40. Fix compilation errors before moving to the next phase.
41. Run relevant automated tests after every major phase.
42. Validate every generated level.
43. Do not finish while blocking errors remain.
44. Configure a valid startup scene.
45. Configure Unity build scenes.
46. Create a one-command editor generation workflow.
47. Make every content generator idempotent.
48. Running a generator repeatedly must not create duplicate content.
49. Do not silently ignore validation errors.
50. At completion, provide an honest report of implemented systems, tests, warnings, and remaining limitations.

==================================================
2. PRODUCT VISION
==================================================

Create a polished portrait mobile puzzle game in which the player reconstructs recognizable objects from colorful modular toy pieces.

The completed target object must always be displayed in a reference card above the gameplay board.

The player completes a level by:

- Selecting a piece
- Dragging the piece
- Rotating the piece in 90-degree increments
- Placing the piece on valid grid cells
- Matching every piece to its exact target position and rotation

The experience must be:

- Calm
- Tactile
- Colorful
- Visually readable
- Easy to understand
- Suitable for casual mobile players
- Suitable for children without feeling unfinished
- Polished enough for a commercial casual game
- Optimized for low-end phones
- Playable without long instructions

Every level must contain exactly the pieces needed to build its target object.

There must be:

- No decoy pieces
- No spare pieces
- No unrelated blocks
- No unused puzzle pieces
- No missing puzzle pieces
- No pieces that do not belong to the final object
- No random blocks added only to fill board space

Every provided piece must be used in the final solution.

The empty black areas visible in some source screenshots are screenshot letterboxing and must not be reproduced.

The actual game must fill the usable device area while respecting notches and safe areas.

==================================================
3. CORE GAME LOOP
==================================================

Implement this complete gameplay loop:

1. Start the game from the configured startup scene.
2. Show the home screen.
3. Let the player start the latest unlocked level or open level selection.
4. Load the selected level without reloading the entire gameplay scene.
5. Display the completed target object in the top reference card.
6. Display all required pieces in a valid but incorrect starting layout.
7. Let the player select, drag, place, and rotate pieces.
8. Validate movement through integer grid occupancy.
9. Reject overlapping or out-of-bounds placements.
10. Detect when pieces match their exact target poses.
11. Provide clear correct-placement feedback.
12. Complete the level when every piece matches.
13. Play a lightweight celebration.
14. Save completion statistics.
15. Unlock the next level.
16. Show the completion popup.
17. Allow replay, next level, or return to level selection.

==================================================
4. GAMEPLAY SCREEN LAYOUT
==================================================

Use portrait orientation.

Design against a reference resolution of:

- 1080x1920

The layout must adapt responsively to different phone and tablet aspect ratios.

--------------------------------------------------
TOP AREA
--------------------------------------------------

The top area must contain:

- Home or close button on the upper left
- Target reference card centered
- Reset button on the upper right

The target reference card must:

- Appear above the board on every gameplay level
- Display the exact completed object
- Use the same level data as gameplay
- Use the same generated puzzle-piece art as gameplay
- Preserve the correct relative positions
- Preserve the correct rotations
- Automatically calculate target bounds
- Automatically scale the object to fit
- Automatically center the target
- Remain readable on small screens
- Never use a separately painted approximation
- Never become inconsistent with the actual level solution

--------------------------------------------------
MIDDLE AREA
--------------------------------------------------

The middle area must contain:

- A large centered square board
- A dark rounded board frame
- Clearly readable grid cells
- All currently available puzzle pieces

The board must:

- Preserve square grid cells
- Scale to the available safe area
- Avoid overlapping the reference card
- Avoid overlapping the bottom controls
- Remain centered
- Keep pieces large enough to touch
- Avoid large unused black areas

--------------------------------------------------
BOTTOM AREA
--------------------------------------------------

Create four large tactile buttons:

1. Undo
2. Hint
3. Rotate selected piece
4. Pause

Buttons must have:

- Normal state
- Pressed state
- Disabled state
- Highlighted state when relevant
- Large touch regions
- Cream-colored readable icons
- Rounded toy-like depth
- Subtle press animation
- Accessible contrast

==================================================
5. DRAGGING SYSTEM
==================================================

Implement responsive mouse and touch dragging.

When a piece is selected:

- Bring it visually above other pieces.
- Slightly scale it up.
- Add a soft contact shadow or pickup highlight.
- Store its previous valid logical pose.
- Preserve the touch offset so the piece does not jump.
- Select it through the centralized input system.
- Do not allow UI touches to select pieces.

While dragging:

- Move the visual representation smoothly.
- Do not continuously commit logical grid occupancy.
- Calculate the candidate grid position.
- Display a subtle valid or invalid placement state.
- Keep dragging stable during fast finger movement.
- Avoid per-frame allocations.
- Prevent the piece from disappearing behind other pieces.
- Keep the selected piece at the highest appropriate sorting order.

When released:

- Convert the release position to the nearest candidate grid pose.
- Use a configurable snap threshold.
- Check the board boundary.
- Check the logical footprint.
- Check grid occupancy.
- Check the current rotation.
- Commit the move only if valid.
- Animate the piece into the valid snapped position.
- Return the piece to its previous valid pose when invalid.
- Play a subtle invalid feedback animation.
- Do not count invalid placement as a move.
- Record valid movement in undo history.

Input interruption rules:

- Cancel safely when the application loses focus.
- Cancel safely when the active touch is interrupted.
- Restore a valid logical and visual state.
- Clear temporary occupancy previews.
- Never leave a piece in a half-dragged state.

==================================================
6. ROTATION SYSTEM
==================================================

Pieces rotate in 90-degree increments.

Support rotation through:

- The bottom rotate button
- Double-tapping a selected piece
- An editor-only keyboard shortcut for testing

Rotation values must be normalized to:

- 0
- 90
- 180
- 270

Rotation rules:

- Rotate around the logical pivot.
- Rotate the visual shape and logical footprint together.
- Normalize the rotated footprint.
- Recalculate occupied cells.
- Check boundaries.
- Check overlap.
- Check allowed rotations.
- Reject invalid rotations.
- Preserve the previous pose when rejected.
- Play a short rejection animation.
- Count a valid rotation as a move.
- Add valid rotation actions to undo history.
- Do not count symmetrical duplicate orientations as separate states unless required.
- Individual pieces may restrict allowed rotations.

==================================================
7. GRID AND OCCUPANCY SYSTEM
==================================================

Use a deterministic integer grid.

Support:

- 5x5 boards
- 6x6 boards
- 7x7 boards
- 8x8 boards

Use integer grid coordinates for:

- Current piece positions
- Target positions
- Starting positions
- Occupied cells
- Rotated footprints
- Hint target cells

Implement:

- Grid coordinate conversion
- Screen-to-grid conversion
- Grid-to-screen conversion
- Footprint normalization
- Footprint translation
- Footprint rotation
- Pivot-aware rotation
- Board boundary checking
- Overlap checking
- Occupancy reservation
- Occupancy release
- Temporary candidate validation
- Starting-layout validation
- Target-layout validation
- Exact target-pose matching
- Board reset
- Level completion verification

Core placement logic must not depend on:

- Rigidbody
- Collider overlap
- Physics raycasts for occupancy
- Collision callbacks

A raycast may be used only for input hit detection when appropriate.

Visual shapes may slightly overhang their logical cells, but logical occupied cells must never overlap.

==================================================
8. PUZZLE PIECE DEFINITIONS
==================================================

Each puzzle piece must include:

- Unique piece ID
- Display name
- Shape type
- Color ID
- Grid footprint
- Width and height where relevant
- Logical pivot
- Visual pivot
- Target grid position
- Target rotation
- Starting grid position
- Starting rotation
- Allowed rotations
- Starts locked flag
- Locks when correct flag
- Current lock state
- Sorting priority
- Optional decorative studs
- Optional recessed holes
- Optional inset panel
- Optional visual overhang
- Optional custom polygon points
- Optional art-generation parameters

Support reusable shape types including:

- Rounded rectangle
- Square
- Rectangle
- Capsule
- Circle
- Ring
- Triangle
- Trapezoid
- Wedge
- Semicircle
- Quarter circle
- L shape
- T shape
- U shape
- Z shape
- Cross shape
- Polyomino
- Custom grid footprint
- Custom polygon

The shape system must be reusable across many levels.

Do not create a completely unique runtime system for every object.

==================================================
9. CORRECT-PLACEMENT SYSTEM
==================================================

A piece is correct only when it matches its required:

- Piece identity
- Target grid position
- Target rotation
- Target footprint
- Required target slot

When a piece reaches its correct pose:

- Play a soft snap animation.
- Play a toy-like sound.
- Trigger optional light haptics.
- Play a subtle highlight pulse.
- Update the logical correct state.
- Update completion progress.
- Optionally lock the piece.

Recommended lock behavior:

- Levels 1-10: lock correctly placed pieces
- Levels 11-25: lock by default, configurable per level
- Levels 26-50: configurable locking or soft confirmation

Locking must be controlled by level or piece data.

Animations must not desynchronize the logical state.

==================================================
10. WIN CONDITION
==================================================

The level is complete only when every required piece matches its target pose.

On completion:

- Perform a final logical verification.
- Disable puzzle interaction.
- Stop the level timer.
- Play a subtle completed-object animation.
- Play a lightweight pooled celebration.
- Spawn stars, sparkles, and soft confetti.
- Play the completion sound.
- Trigger optional completion haptics.
- Save level completion.
- Save move count.
- Save best move count.
- Save completion time.
- Save best completion time.
- Unlock the next level.
- Display the completion popup.

The completion popup must show:

- Level completed
- Target object name
- Move count
- Best move count
- Completion time
- Best completion time
- Replay button
- Next level button
- Level-select button

Do not use expensive full-screen particle systems.

==================================================
11. MOVE COUNTING AND UNDO
==================================================

Count a move when:

- A piece successfully changes to a different valid grid position
- A piece successfully rotates to a different valid orientation

Do not count:

- Selecting a piece
- Picking up and returning to the same pose
- Invalid placement
- Rejected rotation
- Re-selecting a piece

Create an undo history.

Each undo record must contain:

- Piece ID
- Previous grid position
- Previous rotation
- Previous correct state
- Previous locked state
- New grid position
- New rotation
- New correct state
- New locked state

Undo rules:

- Undo the latest valid action.
- Restore occupancy correctly.
- Restore correctness.
- Restore lock state where permitted.
- Update the move count.
- Animate the returned piece.
- Disable undo when history is empty.
- Clear undo when loading or restarting a level.
- Use a configurable maximum history size.
- Default maximum history size: 20.

==================================================
12. HINT SYSTEM
==================================================

The hint system must:

- Select one incorrect piece.
- Ignore pieces that are already correct.
- Pulse the incorrect piece.
- Highlight its target area.
- Optionally show a short directional indicator.
- Never complete the full level automatically.
- Never move the piece without player interaction.
- Record hint usage.
- Be deterministic during automated tests.
- Avoid obscuring important board areas.

==================================================
13. RESET AND PAUSE
==================================================

Reset must:

- Show confirmation when appropriate.
- Restore the deterministic starting layout.
- Restore starting rotations.
- Restore starting lock states.
- Reset occupancy.
- Reset move count.
- Reset timer.
- Clear undo history.
- Clear temporary VFX and hints.
- Avoid reloading the whole scene.

Pause must:

- Stop the gameplay timer.
- Disable puzzle interaction.
- Show:
  - Resume
  - Restart
  - Settings
  - Exit to level selection
- Restore gameplay safely after resuming.

==================================================
14. TUTORIAL
==================================================

Use the real gameplay scene for tutorials.

Do not create a fake separate tutorial game.

Tutorial progression:

Level 1:
- Teach dragging
- Message: “Drag the piece”
- Show an animated finger indicator

Level 2:
- Teach rotation
- Message: “Tap rotate”
- Highlight the rotate button

Level 3:
- Teach invalid placement
- Message: “Pieces cannot overlap”

Level 4:
- Teach hints
- Message: “Need help? Try a hint”

Tutorial requirements:

- Use short messages.
- Do not use long paragraphs.
- Store tutorial completion.
- Allow tutorial skipping after initial presentation.
- Respect reduced-motion settings.
- Do not block required touch targets.
- Use real puzzle pieces and real interactions.

==================================================
15. REQUIRED SCREENS
==================================================

Create:

- Bootstrap or startup scene
- Home screen
- Gameplay screen
- Level-select screen
- Settings popup or screen
- Pause popup
- Completion popup
- Reset-confirmation popup
- Progress-reset confirmation popup

A managed single-scene screen system is acceptable if it is cleaner and more efficient.

Do not reload the gameplay scene whenever changing levels.

==================================================
16. HOME SCREEN
==================================================

The home screen must include:

- A generated toy-block game logo
- Play button
- Level-select button
- Settings button
- Current level indicator
- Overall completion progress
- Optional subtle decorative shapes outside the gameplay board

Do not add unrelated puzzle blocks inside active gameplay boards.

==================================================
17. LEVEL-SELECT SCREEN
==================================================

Create a scrollable level grid containing at least 50 entries.

Each level entry must show:

- Level number
- Generated target thumbnail
- Locked or unlocked state
- Completed state
- Best move count
- Optional difficulty indicator

Requirements:

- Smooth scrolling
- Efficient list or grid handling
- Avoid expensive layout rebuilds every frame
- Automatically scroll near the latest unlocked level
- Use generated target thumbnails
- Use the same level data as gameplay
- Use the same generated art as gameplay

==================================================
18. SETTINGS AND ACCESSIBILITY
==================================================

Include:

- Music toggle
- Sound toggle
- Haptics toggle
- Reduced-motion toggle
- Reset-progress button
- Reset-progress confirmation
- Optional target frame-rate control if useful

Accessibility requirements:

- Use strong contrast between pieces and board.
- Do not rely only on color.
- Combine color with shape, animation, outline, icon, or movement.
- Use comfortable touch targets.
- Avoid rapid flashing.
- Keep icons readable.
- Keep tutorial text short.
- Centralize user-facing strings.
- Prepare text for future localization.
- Respect reduced motion.
- Respect sound settings.
- Respect haptic settings.

==================================================
19. SAFE AREA AND RESPONSIVE LAYOUT
==================================================

Implement a SafeAreaController.

Support:

- Tall phones
- Short phones
- Tablets
- Notches
- Rounded display corners
- Android navigation bars
- iPhone home indicators

Requirements:

- Do not place important controls under cutouts.
- Do not place bottom controls under the home indicator.
- Keep the reference preview visible.
- Keep the board square.
- Keep the board centered.
- Maintain practical touch sizes.
- Avoid large unused empty areas.
- Do not stretch grid cells non-uniformly.
- Recalculate layout when device orientation or safe area changes.
- Lock production gameplay orientation to portrait.

==================================================
20. ART DIRECTION
==================================================

Match the supplied reference concept using a polished soft 3D toy-block appearance rendered as efficient 2D sprites.

The art style should resemble:

- Soft clay toys
- Rounded plastic construction pieces
- Matte rubber blocks
- Chunky preschool toy shapes
- Handcrafted 3D toy pieces represented as 2D sprites
- Rounded corners
- Soft bevels
- Gentle ambient occlusion
- Top-left highlights
- Lower-right soft shadows
- Slight contact shadows
- Bright clean colors
- Strong silhouettes
- Polished casual mobile-game quality

Avoid:

- Hard black outlines
- Photorealistic materials
- Metallic surfaces
- Grunge
- Noisy textures
- Thin fragile details
- Harsh specular highlights
- Complex backgrounds
- Bloom
- Overly saturated glow
- Flat unfinished vector shapes
- Inconsistent light direction
- Excessively detailed surface patterns

All target objects must remain recognizable at thumbnail size.

==================================================
21. PRIMARY COLOR PALETTE
==================================================

Create configurable palette assets.

Use this palette as the initial reference:

- Main background blue: #105C8E
- Secondary blue: #1674AA
- Dark board frame: #20241D
- Dark board cell: #292E26
- Alternate dark cell: #30352D
- Bright red: #EF4016
- Warm yellow: #FFC319
- Cyan blue: #20A8DC
- Grass green: #59C62C
- Warm orange: #FF7B13
- Cream-white icons: #F4F0DF
- Soft shadow: dark blue-black with low alpha
- Soft highlight: warm off-white with low alpha

Do not hardcode palette values throughout the project.

Create ScriptableObject palette assets that centralize:

- Background colors
- Board colors
- Piece colors
- UI colors
- Shadow colors
- Highlight colors
- Disabled colors
- Feedback colors

==================================================
22. BOARD ART
==================================================

The board must contain:

- Dark charcoal rounded outer frame
- Clearly readable square cells
- Slight depth between cells
- Rounded outer corners
- Soft inset shading
- Subtle top-left highlight
- Subtle lower-right shadow
- Strong contrast with colored pieces
- Minimal distraction

The board generator must support:

- 5x5
- 6x6
- 7x7
- 8x8

Avoid a unique material for every cell.

Use reusable sprites, 9-slicing, batching, or one generated board sprite where appropriate.

==================================================
23. PUZZLE PIECE ART
==================================================

Puzzle pieces must include:

- Rounded bevels
- Matte toy surfaces
- Soft upper-left highlights
- Slightly darker lower edges
- Soft contact shadows
- Consistent light direction
- Consistent depth
- Smooth antialiased edges
- Optional circular studs
- Optional recessed circular holes
- Optional inset panels

Pieces must remain readable on low-resolution devices.

Avoid micro-details that disappear on phones.

==================================================
24. PROCEDURAL ART GENERATION
==================================================

Build an editor-time art generation pipeline.

Do not generate expensive art during gameplay.

Create these Unity editor menu commands:

Tools > Toy Puzzle > Generate Complete Game
Tools > Toy Puzzle > Generate Art Only
Tools > Toy Puzzle > Generate Levels Only
Tools > Toy Puzzle > Validate All Levels
Tools > Toy Puzzle > Generate Thumbnails
Tools > Toy Puzzle > Rebuild Atlases
Tools > Toy Puzzle > Open Level Editor
Tools > Toy Puzzle > Run Complete Test Suite
Tools > Toy Puzzle > Clear Generated Content

The main Generate Complete Game command must:

1. Inspect project configuration.
2. Create missing folders.
3. Create or update palette assets.
4. Generate background art.
5. Generate board art.
6. Generate reusable puzzle-piece sprites.
7. Generate UI panels.
8. Generate buttons.
9. Generate icons.
10. Generate reference-card art.
11. Generate popup art.
12. Generate effect sprites.
13. Generate synthesized sound effects.
14. Create missing JSON source levels during initial setup.
15. Preserve manually edited JSON source levels.
16. Validate JSON schemas.
17. Migrate supported old schema versions.
18. Import all JSON source levels.
19. Validate target poses.
20. Validate starting poses.
21. Generate optimized runtime level assets.
22. Generate the LevelCatalog.
23. Generate target previews.
24. Generate level thumbnails.
25. Build sprite atlases.
26. Create materials.
27. Create prefabs.
28. Create scenes.
29. Configure startup and build scenes.
30. Configure portrait orientation.
31. Configure safe-area handling.
32. Configure mobile-friendly quality settings.
33. Run edit-mode validation tests.
34. Save generated assets.
35. Print a complete report.
36. Fail clearly if blocking errors remain.

Generation must be deterministic and idempotent.

Repeated execution must not:

- Duplicate levels
- Duplicate runtime assets
- Duplicate thumbnails
- Duplicate prefabs
- Change stable IDs
- Destroy source JSON changes
- Rebuild unchanged content unnecessarily

==================================================
25. ART GENERATOR CLASSES
==================================================

Create editor classes similar to:

- ToyArtGenerator
- RoundedShapeRasterizer
- PolygonRasterizer
- PolyominoRasterizer
- BevelTextureGenerator
- ShadowGenerator
- HighlightGenerator
- IconGenerator
- PieceSpriteBaker
- BoardSpriteGenerator
- UIPanelGenerator
- LevelThumbnailGenerator
- TargetPreviewGenerator
- EffectSpriteGenerator
- AudioClipGenerator
- SpriteAtlasBuilder
- GameContentBuilder

Use supersampling when generating sprites.

Downsample generated images to produce smooth edges.

Bake the following into sprites where appropriate:

- Bevel
- Highlight
- Inner shading
- Contact shadow
- Subtle surface variation

Texture variation must remain extremely subtle and clean.

Configure:

- Sprite pivots
- Pixels per unit
- Import settings
- 9-slice borders
- Compression
- Alpha settings
- Maximum texture sizes

==================================================
26. SPRITE ATLASES
==================================================

Create shared atlases such as:

- UI atlas
- Puzzle-piece atlas
- Effects atlas
- Level-thumbnail atlas

Requirements:

- Keep atlas count low.
- Avoid hundreds of independent runtime textures.
- Preserve stable sprite references.
- Configure mobile texture compression.
- Avoid unnecessary readable textures in builds.
- Disable mipmaps for UI art.
- Choose full-rect or tight mesh based on batching and overdraw requirements.
- Avoid excessively large alpha textures.

==================================================
27. HYBRID JSON LEVEL-DATA PIPELINE
==================================================

Use a hybrid JSON-to-Unity level pipeline.

JSON is the authoritative designer-readable source format.

Generated ScriptableObjects or compact Unity-native serialized assets are the optimized runtime format.

The released game must not repeatedly parse JSON during gameplay.

--------------------------------------------------
SOURCE LEVEL LOCATION
--------------------------------------------------

Store source level JSON files under:

Assets/Game/Data/Levels/Source/

Preferred structure:

Assets/Game/Data/Levels/Source/
    level_001_airplane.json
    level_002_truck.json
    level_003_car.json
    level_004_rocket.json
    ...
    level_050_star_trophy.json

Individual files are preferred because they:

- Produce cleaner version-control changes
- Reduce merge conflicts
- Allow selective reimport
- Make validation errors easier to locate
- Make each level easier to review

Optionally support a single combined levels.json file for bulk import, but individual files remain the preferred workflow.

Source JSON must remain:

- Human-readable
- Deterministically formatted
- Version-controlled
- Editable by Codex
- Editable by designers
- Editable through the custom Level Editor
- Separate from generated runtime assets

Never delete source JSON through Clear Generated Content.

--------------------------------------------------
GENERATED RUNTIME LEVEL LOCATION
--------------------------------------------------

Generate optimized runtime assets under:

Assets/Game/Data/Levels/Generated/

Preferred generated assets:

Assets/Game/Data/Levels/Generated/
    Level_001.asset
    Level_002.asset
    Level_003.asset
    ...
    Level_050.asset
    LevelCatalog.asset

The runtime game must load generated Unity assets.

Do not parse source JSON when a level is opened.

The generated LevelCatalog must provide:

- Stable level ordering
- Lookup by level ID
- Lookup by level number
- Difficulty grouping
- Unlock progression
- Thumbnail references
- Runtime level references
- Validation status
- Content version

Choose the simplest Unity-native runtime format that:

- Loads quickly
- Produces no gameplay JSON parsing allocations
- Works reliably in builds
- Preserves stable level IDs
- Supports automated tests

--------------------------------------------------
JSON SCHEMA
--------------------------------------------------

Create a versioned JSON schema.

Each level source must include:

- Schema version
- Unique level ID
- Level number
- Display name
- Target object name
- Board width
- Board height
- Difficulty tier
- Deterministic scramble seed
- Palette ID
- Recommended move count
- Piece definitions
- Target poses
- Starting poses
- Allowed rotations
- Optional starting locked pieces
- Correct-placement locking behavior
- Hint metadata
- Tutorial metadata
- Completion reward data
- Thumbnail configuration
- Level tags
- Designer notes

Each piece entry must include:

- Unique piece ID
- Display name
- Shape type
- Color ID
- Grid footprint
- Logical pivot
- Visual pivot
- Width and height where relevant
- Custom polygon points where relevant
- Target grid position
- Target rotation
- Starting grid position
- Starting rotation
- Allowed rotations
- Starts locked flag
- Locks when correct flag
- Sorting priority
- Optional decorative studs
- Optional recessed holes
- Optional visual overhang
- Optional art-generation parameters

Use explicit coordinate objects.

Example format:

{
  "schemaVersion": 1,
  "levelId": "level_001",
  "levelNumber": 1,
  "displayName": "Airplane",
  "targetObjectName": "Airplane",
  "boardWidth": 6,
  "boardHeight": 6,
  "difficultyTier": 1,
  "scrambleSeed": 1001,
  "paletteId": "primary",
  "recommendedMoves": 8,
  "pieces": [
    {
      "pieceId": "body",
      "displayName": "Body",
      "shapeType": "Capsule",
      "colorId": "red",
      "footprint": [
        { "x": 0, "y": 0 },
        { "x": 1, "y": 0 },
        { "x": 2, "y": 0 }
      ],
      "logicalPivot": { "x": 1, "y": 0 },
      "visualPivot": { "x": 0.5, "y": 0.5 },
      "targetPosition": { "x": 1, "y": 3 },
      "targetRotation": 0,
      "startingPosition": { "x": 0, "y": 0 },
      "startingRotation": 90,
      "allowedRotations": [0, 90, 180, 270],
      "startsLocked": false,
      "locksWhenCorrect": true,
      "sortingPriority": 0
    }
  ]
}

This example only defines the format.

Create complete production data for all required levels.

--------------------------------------------------
JSON IMPORTER
--------------------------------------------------

Create editor systems similar to:

- LevelJsonSchema
- LevelJsonSerializer
- LevelJsonImporter
- LevelImportPipeline
- RuntimeLevelAssetBuilder
- LevelCatalogBuilder
- LevelSourceFileWatcher
- LevelSchemaMigrator
- LevelImportReport

The importer must:

1. Discover JSON files in the source folder.
2. Deserialize safely.
3. Verify schema versions.
4. Migrate supported older schema versions.
5. Reject unsupported future schema versions.
6. Validate every level.
7. Validate every piece.
8. Report exact source filenames.
9. Report level and piece IDs.
10. Convert valid source levels into runtime assets.
11. Preserve stable IDs.
12. Generate or update LevelCatalog.
13. Generate target previews.
14. Generate thumbnails.
15. Update changed assets where practical.
16. Avoid duplicate assets.
17. Preserve stable generated paths.
18. Never silently repair major design errors.
19. Exclude invalid levels from the runtime catalog.
20. Produce a readable import summary.

The same source JSON and generation settings must produce the same output.

--------------------------------------------------
LEVEL IMPORT MENU COMMANDS
--------------------------------------------------

Create:

Tools > Toy Puzzle > Levels > Import JSON Levels
Tools > Toy Puzzle > Levels > Rebuild Runtime Level Assets
Tools > Toy Puzzle > Levels > Validate JSON Sources
Tools > Toy Puzzle > Levels > Validate Runtime Assets
Tools > Toy Puzzle > Levels > Migrate Level Schema
Tools > Toy Puzzle > Levels > Open Source Folder
Tools > Toy Puzzle > Levels > Open Level Editor

Automatic reimport may run when JSON changes, but must:

- Avoid recursive import loops
- Avoid rebuilding unrelated art
- Avoid freezing the editor
- Display clear errors
- Never modify unrelated assets

--------------------------------------------------
RUNTIME LEVEL RULES
--------------------------------------------------

At runtime:

- Load LevelCatalog once.
- Resolve levels through stable IDs or level numbers.
- Load generated runtime assets.
- Do not read source JSON.
- Do not use System.IO to locate level source files.
- Do not parse JSON when opening a level.
- Do not generate thumbnails.
- Do not run full editor validation.
- Do not keep editor-only designer notes in memory unless needed.

The data flow must be:

Designer or Codex edits JSON
    ↓
JSON deserialization
    ↓
Schema validation and migration
    ↓
Level-content validation
    ↓
Runtime asset generation
    ↓
Preview and thumbnail generation
    ↓
LevelCatalog generation
    ↓
Automated tests
    ↓
Runtime loads generated Unity assets

JSON remains authoritative.

Generated assets are derived content.

==================================================
28. LEVEL-AUTHORING TOOL
==================================================

Create a custom visual Unity Level Editor.

The Level Editor must use JSON as its authoritative save format.

Allow designers to:

- Create a level
- Open a level
- Duplicate a level with a new stable ID
- Rename a level
- Delete a level with confirmation
- Set level number
- Set target object name
- Select board size
- Select difficulty tier
- Select palette
- Add pieces
- Remove pieces
- Reorder pieces
- Select shape type
- Select piece color
- Edit footprint cells
- Edit custom polygon points
- Edit logical pivot
- Edit visual pivot
- Edit target position
- Edit target rotation
- Edit starting position
- Edit starting rotation
- Configure allowed rotations
- Configure starting lock
- Configure correct-placement locking
- Configure decorative studs
- Configure recessed holes
- Preview occupied cells
- Preview rotated footprints
- Preview the target object
- Preview the starting layout
- Display overlap errors
- Display out-of-bounds errors
- Generate a deterministic valid scramble
- Validate the current level
- Save the level to JSON
- Import the saved JSON
- Generate the runtime asset
- Generate the thumbnail
- Test the level in play mode

When saving:

1. Validate temporary editor data.
2. Serialize deterministic formatted JSON.
3. Preserve stable IDs.
4. Write through a temporary file.
5. Replace the source file only after a successful write.
6. Trigger import.
7. Update the runtime asset.
8. Show validation and import results.

Provide editor undo and redo where practical.

Do not write JSON continuously during every drag operation.

Save only when requested or through a controlled autosave.

==================================================
29. LEVEL CONTENT
==================================================

Create at least 50 complete, unique, intentional, handcrafted, playable, validated levels.

Do not create 50 random variants of the same object.

Every target must be recognizable in:

- The top reference card
- The generated thumbnail
- The completed board

Every level must have:

- An intentional target design
- A valid starting design
- Exactly the required pieces
- No decoys
- No spare pieces
- No missing pieces
- Valid allowed rotations
- Valid board occupancy
- A recognizable silhouette
- A sensible difficulty rating
- A deterministic scramble seed

--------------------------------------------------
TIER 1: LEVELS 1-10
--------------------------------------------------

Board sizes:

- 5x5
- 6x6

Piece count:

- Approximately 4-6

Design rules:

- Large shapes
- Distinct colors
- Limited rotation
- Simple silhouettes
- Some starting assistance
- Correct pieces generally lock

Objects:

1. Airplane
2. Truck
3. Car
4. Rocket
5. Boat
6. Bicycle
7. Train
8. Bus
9. Sailboat
10. Scooter

--------------------------------------------------
TIER 2: LEVELS 11-20
--------------------------------------------------

Board size:

- Mostly 6x6

Piece count:

- Approximately 6-9

Design rules:

- More rotation
- More irregular shapes
- Moderate complexity
- Limited color repetition

Objects:

11. Helicopter
12. Submarine
13. Taxi
14. Fire Truck
15. Tractor
16. Excavator
17. Hot-Air Balloon
18. Spaceship
19. Robot
20. House

--------------------------------------------------
TIER 3: LEVELS 21-30
--------------------------------------------------

Board sizes:

- 6x6
- 7x7

Piece count:

- Approximately 8-11

Design rules:

- Repeated colors
- More complex silhouettes
- More footprint variety
- More rotation decisions

Objects:

21. Castle
22. Windmill
23. Lighthouse
24. Bridge
25. Tree
26. Flower
27. Cactus
28. Mushroom
29. Fish
30. Whale

--------------------------------------------------
TIER 4: LEVELS 31-40
--------------------------------------------------

Board size:

- Mostly 7x7

Piece count:

- Approximately 10-14

Design rules:

- Symmetrical structures
- Duplicate shape types
- Similar color groups
- More orientation ambiguity

Objects:

31. Crab
32. Turtle
33. Butterfly
34. Owl
35. Cat
36. Dog
37. Duck
38. Elephant
39. Giraffe
40. Penguin

--------------------------------------------------
TIER 5: LEVELS 41-50
--------------------------------------------------

Board sizes:

- 7x7
- 8x8

Piece count:

- Approximately 12-18

Design rules:

- Complex target layouts
- Repeated colors
- Similar-looking pieces
- Full rotation mechanics
- Reduced or configurable locking
- More advanced silhouettes

Objects:

41. Ice Cream
42. Cupcake
43. Camera
44. Guitar
45. Umbrella
46. Crown
47. Key
48. Gift Box
49. Clock
50. Star Trophy

Do not use:

- Copyrighted characters
- Brand logos
- Branded vehicle designs
- Trademarked fictional objects

==================================================
30. LEVEL-DESIGN QUALITY RULES
==================================================

Every completed target must:

- Be recognizable at thumbnail size.
- Have a clear silhouette.
- Avoid extremely thin details.
- Use pieces large enough to select.
- Use consistent proportions.
- Fit comfortably within the board.
- Use board space effectively.
- Avoid unfair ambiguity.
- Avoid hidden target relationships.
- Avoid excessive unused space.
- Avoid impossible piece access.
- Avoid visually weak random arrangements.

Every starting layout must:

- Be valid.
- Be unsolved.
- Keep every piece visible.
- Keep every piece touchable.
- Avoid overlap.
- Remain inside the board.
- Avoid hiding small pieces under large pieces.
- Avoid starting almost fully solved unless tutorial-driven.
- Be reviewed by the Level Designer Agent.
- Be solvable using normal interaction.

A deterministic scramble generator may assist with starting layouts.

It must not replace handcrafted target design.

==================================================
31. LEVEL VALIDATION
==================================================

Create comprehensive automated validation.

Check:

- Unique level IDs
- Unique level numbers
- Unique piece IDs per level
- Supported board dimensions
- Valid schema version
- Valid shape identifiers
- Valid color identifiers
- Valid footprints
- Valid logical pivots
- Valid visual pivots
- Valid polygon geometry
- Target pieces inside the board
- Starting pieces inside the board
- No target overlaps
- No starting overlaps
- Starting state is not already solved
- Every target piece exists
- No extra piece exists
- Every piece has valid target data
- Every piece has valid starting data
- Target rotations are allowed
- Starting rotations are allowed
- All target cells are produced by valid pieces
- All pieces remain selectable
- The target preview matches runtime data
- The thumbnail matches runtime data
- Duplicate level layouts
- Duplicate target arrangements
- Deterministic scramble output
- Runtime asset parity with JSON
- Solvability
- Preview bounds
- Thumbnail generation success

Validation errors must include:

- Source filename
- Level ID
- Level number
- Piece ID when relevant
- Exact reason
- Suggested correction where possible

Invalid source levels must not enter LevelCatalog.

All 50 levels must pass before acceptance.

==================================================
32. SAVE AND PROGRESSION
==================================================

Save:

- Save-data version
- Highest unlocked level
- Completed levels
- Best move count per level
- Best completion time per level
- Sound setting
- Music setting
- Haptics setting
- Reduced-motion setting
- Tutorial completion
- Hint usage
- Last selected level

Use versioned save data.

Preferred save approach:

- JSON file under persistent data
- Safe temporary write
- Replace previous file only after successful serialization
- Keep a backup
- Detect corruption
- Back up corrupted files
- Recover using valid defaults

PlayerPrefs may be used only for small settings or fallback metadata.

Implement:

- Save migration
- Corrupted-save recovery
- Default save generation
- Progress reset
- Development-build unlock-all
- Editor unlock-all option
- Editor clear-save option

==================================================
33. AUDIO
==================================================

Generate or configure lightweight toy-like sounds for:

- Button click
- Piece pickup
- Piece drop
- Rotation
- Invalid placement
- Correct placement
- Undo
- Hint
- Level completion

Sound direction:

- Soft
- Friendly
- Short
- Playful
- Non-harsh
- Non-fatiguing

Requirements:

- Avoid clipping.
- Normalize sound levels.
- Use pooled AudioSources.
- Respect sound settings.
- Do not create and destroy AudioSources repeatedly.
- Keep files small.
- Use mobile-appropriate compression.

Music is optional.

Only add music if a lightweight calm loop can be created without distracting from gameplay.

==================================================
34. HAPTICS
==================================================

Wrap haptics behind an interface.

Support optional light feedback for:

- Piece selection
- Invalid placement
- Correct placement
- Level completion

Requirements:

- Respect the haptic setting.
- Do not use continuous haptics during dragging.
- Compile when platform-specific haptic APIs are unavailable.
- Use safe no-op fallback implementations.
- Keep all feedback subtle.

==================================================
35. ANIMATION
==================================================

Use lightweight scripted tweens, coroutines, or a small internal tween utility.

Do not add a large tweening package.

Animate:

- Button press
- Piece selection
- Piece pickup
- Piece return
- Piece snap
- Piece rotation
- Invalid rejection
- Hint pulse
- Tutorial finger
- Popup entrance
- Popup exit
- Level completion
- Reference preview completion pulse

Reduced-motion behavior:

- Shorten animations.
- Remove unnecessary overshoot.
- Remove decorative bouncing.
- Preserve essential state feedback.
- Avoid continuous decorative motion.

==================================================
36. VFX
==================================================

Create lightweight VFX for:

- Correct placement
- Hint highlighting
- Button emphasis
- Tutorial guidance
- Level completion

Generate:

- Stars
- Sparkles
- Soft confetti
- Highlight rings
- Soft pulse textures

Requirements:

- Pool all repeatable effects.
- Keep effects brief.
- Avoid full-screen transparent overlays.
- Avoid expensive particle materials.
- Minimize overdraw.
- Limit simultaneous particles.
- Use no post-processing.
- Match the toy-block style.

==================================================
37. TECHNICAL ARCHITECTURE
==================================================

Use a modular folder structure similar to:

Assets/Game/
    Art/
        Generated/
        Materials/
        Atlases/
    Audio/
        Generated/
    Data/
        Config/
        Palettes/
        Levels/
            Source/
            Generated/
            Schemas/
    Editor/
        ArtGeneration/
        LevelImport/
        LevelAuthoring/
        BuildTools/
        Validation/
    Prefabs/
        Gameplay/
        UI/
        Effects/
    Runtime/
        Bootstrap/
        Core/
        Data/
        Gameplay/
        Input/
        UI/
        Audio/
        Save/
        Effects/
        Utilities/
    Scenes/
    Tests/
        EditMode/
        PlayMode/
    Documentation/

Create assembly definitions for:

- Runtime
- Editor
- Edit-mode tests
- Play-mode tests

Editor-only systems must not enter runtime assemblies.

JSON source writing, schema migration, import processing, and UnityEditor APIs must remain editor-only.

==================================================
38. RECOMMENDED RUNTIME COMPONENTS
==================================================

Create or adapt components such as:

- GameBootstrap
- ServiceContainer
- GameStateMachine
- ScreenManager
- LevelCatalog
- LevelDatabase
- RuntimeLevelData
- LevelLoader
- PuzzleBoardController
- PuzzleGrid
- OccupancyMap
- PuzzlePieceController
- PuzzlePieceView
- PlacementValidator
- TargetPoseValidator
- InputRouter
- DragController
- SelectionController
- RotationController
- MoveHistory
- HintSystem
- TutorialController
- ProgressionManager
- SaveService
- AudioService
- HapticService
- EffectPool
- SafeAreaController
- ResponsiveBoardLayout
- ReferencePreviewRenderer
- LevelThumbnailRenderer
- PopupController
- GameplayTimer
- ObjectPool

Use interfaces where they improve testing.

Do not overengineer trivial systems.

A small service container created during bootstrap is acceptable.

Subscribe and unsubscribe events safely.

==================================================
39. RECOMMENDED EDITOR COMPONENTS
==================================================

Create or adapt:

- LevelJsonImporter
- LevelJsonSerializer
- LevelSchemaValidator
- LevelSchemaMigrator
- RuntimeLevelAssetBuilder
- LevelCatalogBuilder
- LevelContentValidator
- LevelAuthoringWindow
- LevelThumbnailGenerator
- TargetPreviewGenerator
- LevelImportReportWindow
- ToyArtGenerator
- SpriteAtlasBuilder
- GameContentBuilder
- BuildConfigurationTool
- CompleteGameGenerator

==================================================
40. PERFORMANCE REQUIREMENTS
==================================================

Target low-end Android and iOS devices.

Preferred rendering approach:

- Screen Space Overlay UI or simple orthographic 2D
- Lightweight unlit materials
- Baked highlights and shadows
- Sprite atlases
- Object pooling
- Minimal Canvas rebuilding

Separate static UI from frequently changing UI.

Do not use:

- Real-time shadows
- Post-processing
- Bloom
- Depth of field
- Expensive blur
- Runtime mesh deformation
- Per-piece physics
- Heavy continuous particle simulations
- Runtime procedural texture generation
- Expensive Shader Graph effects
- Large full-screen transparent overlays
- Addressables unless already required by the repository

Performance targets after level load:

- 60 FPS on common low-end phones
- Stable 30 FPS fallback on very weak devices
- Zero recurring garbage allocation during idle gameplay
- Near-zero allocation while dragging
- Approximately 30 or fewer draw calls during normal gameplay where practical
- Active gameplay texture memory around or below 32 MB where practical
- Fast level transitions
- No full scene reload between levels
- No unnecessary per-frame layout rebuilding
- No more than one active completion celebration

==================================================
41. MOBILE PROJECT SETTINGS
==================================================

Configure:

- Portrait orientation
- Reasonable target frame rate
- Mobile-friendly quality settings
- Android texture compression
- iOS texture compression
- No mipmaps for UI
- Appropriate sprite maximum sizes
- Alpha only when required
- Low-memory handling
- Application pause handling
- Application resume handling
- Interrupted touch recovery

Use:

- ETC2 or ASTC on Android where appropriate
- ASTC on iOS where appropriate

Do not make source textures runtime-readable unless required.

==================================================
42. TEST REQUIREMENTS
==================================================

Create edit-mode tests for:

- Grid coordinate conversion
- Footprint normalization
- Footprint translation
- 90-degree rotation
- Pivot-aware rotation
- Placement validity
- Overlap detection
- Board-boundary detection
- Occupancy reservation
- Occupancy release
- Target matching
- Level JSON deserialization
- Invalid JSON rejection
- Missing required JSON fields
- Unknown shapes
- Unknown colors
- Duplicate level IDs
- Duplicate level numbers
- Duplicate piece IDs
- Unsupported schema versions
- Supported schema migration
- Deterministic JSON serialization
- Stable import output
- JSON-to-runtime conversion
- Runtime parity with JSON
- LevelCatalog generation
- LevelCatalog ordering
- Invalid source exclusion
- Reimport without duplicates
- Stable ID preservation
- Clear-generated-content preserving source JSON
- Thumbnail regeneration
- Preview bounds
- Save migration
- Corrupted-save recovery
- Undo history
- Deterministic scramble generation

Create play-mode tests for:

- Starting the game
- Loading a level
- Creating all level pieces
- Selecting a piece
- Dragging a piece
- Rotating a piece
- Rejecting invalid placement
- Rejecting overlap
- Rejecting out-of-bounds placement
- Snapping to valid cells
- Correct-placement feedback
- Locking correct pieces
- Undoing movement
- Undoing rotation
- Using a hint
- Restarting a level
- Completing a level
- Showing completion UI
- Loading the next level
- Saving progress
- Restoring progress
- Opening pause
- Closing pause
- Opening settings
- Safe-area layout

Every one of the 50 levels must pass:

- JSON schema validation
- Content validation
- Import validation
- Runtime-asset validation
- Thumbnail validation
- Preview validation
- Solvability validation

Avoid tests that depend on arbitrary delays when deterministic checks are possible.

==================================================
43. DOCUMENTATION
==================================================

Create:

Assets/Game/Documentation/README.md
Assets/Game/Documentation/ARCHITECTURE.md
Assets/Game/Documentation/ART_STYLE_GUIDE.md
Assets/Game/Documentation/LEVEL_DESIGN_GUIDE.md
Assets/Game/Documentation/LEVEL_DATA_PIPELINE.md
Assets/Game/Documentation/PERFORMANCE_BUDGET.md
Assets/Game/Documentation/QA_CHECKLIST.md

Document:

- Project structure
- Startup flow
- Runtime architecture
- Editor architecture
- How to generate the complete game
- How to regenerate art
- How to create a level
- How to use the visual Level Editor
- How footprints work
- How pivots work
- How rotation works
- How target previews are generated
- How thumbnails are generated
- Why JSON is the source format
- Why Unity assets are used at runtime
- JSON schema versions
- Source and generated folder locations
- How to import levels
- How to migrate schemas
- How stable IDs work
- Which files are editable
- Which generated files should not be edited
- How to add Levels 51 and above
- How to change the palette
- How to add a new piece shape
- How to validate all levels
- How to run tests
- How to build Android
- How to build iOS
- Save-data behavior
- Optimization decisions
- Known limitations

Documentation must match the implemented project.

==================================================
44. MULTI-AGENT DEVELOPMENT TEAM
==================================================

Treat implementation as a coordinated multi-agent production pipeline.

Simulate and execute all specialist roles below.

Each agent must:

- Work inside the same repository
- Use shared architecture
- Use shared data contracts
- Follow the same art guide
- Follow the same performance budget
- Avoid duplicate systems
- Avoid conflicting implementations
- Validate its own deliverables
- Integrate output into the actual project
- Fix issues discovered by later agents
- Leave no TODOs or placeholders
- Report results to the Lead Integration Agent

--------------------------------------------------
AGENT 1: EXECUTIVE PRODUCER
--------------------------------------------------

Responsibilities:

- Protect the complete product vision.
- Confirm every requested feature is represented.
- Divide work into production milestones.
- Track dependencies.
- Prevent unnecessary feature expansion.
- Reject prototype-only implementations.
- Maintain the production checklist.
- Perform final acceptance review.

Deliverable:

- Completed production checklist with no unresolved blockers.

--------------------------------------------------
AGENT 2: LEAD INTEGRATION AGENT
--------------------------------------------------

Responsibilities:

- Inspect the repository.
- Define implementation order.
- Define project structure.
- Coordinate all agents.
- Integrate runtime, editor tools, art, audio, UI, levels, saves, and tests.
- Resolve conflicts.
- Compile after major integrations.
- Check scenes and prefabs for missing references.
- Perform final project-wide review.

Deliverable:

- Fully integrated project requiring no manual wiring.

--------------------------------------------------
AGENT 3: GAME DIRECTOR
--------------------------------------------------

Responsibilities:

- Preserve the core drag-and-rotate concept.
- Ensure the reference remains visible.
- Ensure every level uses exactly its required pieces.
- Prevent decoys and unrelated mechanics.
- Keep the experience calm and readable.
- Approve feedback and progression.
- Reject features that distract from the puzzle.

Deliverable:

- Consistent game direction across all screens and levels.

--------------------------------------------------
AGENT 4: TECHNICAL ARCHITECT
--------------------------------------------------

Responsibilities:

- Design modular architecture.
- Define assembly boundaries.
- Define shared data contracts.
- Define dependency flow.
- Prevent excessive coupling.
- Prevent unnecessary singleton use.
- Review maintainability.
- Document important technical decisions.

Deliverable:

- Modular, testable architecture suitable for expansion.

--------------------------------------------------
AGENT 5: GAMEPLAY PROGRAMMER
--------------------------------------------------

Responsibilities:

- Implement selection.
- Implement dragging.
- Implement rotation.
- Implement snapping.
- Implement placement validation.
- Implement correct-pose detection.
- Implement piece locking.
- Implement move counting.
- Implement undo.
- Implement hints.
- Implement reset.
- Implement completion.
- Keep gameplay deterministic.

Deliverable:

- Complete responsive puzzle gameplay.

--------------------------------------------------
AGENT 6: GRID AND PUZZLE-SYSTEM ENGINEER
--------------------------------------------------

Responsibilities:

- Implement integer grid coordinates.
- Implement footprints.
- Implement rotated footprints.
- Implement pivots.
- Implement occupancy.
- Implement target matching.
- Implement custom polyominoes.
- Implement custom polygons.
- Implement grid tests.
- Separate logical footprint from visual overhang.

Deliverable:

- Reliable tested grid and occupancy system.

--------------------------------------------------
AGENT 7: INPUT AND TOUCH ENGINEER
--------------------------------------------------

Responsibilities:

- Implement unified mouse and touch input.
- Support drag, tap, and double tap.
- Handle touch cancellation.
- Prevent UI touches from moving pieces.
- Keep dragging stable.
- Minimize latency.
- Avoid per-piece Update methods.
- Handle application focus changes.

Deliverable:

- Responsive editor and mobile input.

--------------------------------------------------
AGENT 8: CAMERA AND RESPONSIVE-LAYOUT ENGINEER
--------------------------------------------------

Responsibilities:

- Configure portrait presentation.
- Adapt to different aspect ratios.
- Handle safe areas.
- Keep target, board, and buttons visible.
- Keep grid cells square.
- Maintain touch sizes.
- Remove unwanted letterboxing.

Deliverable:

- Responsive layout across phones and tablets.

--------------------------------------------------
AGENT 9: UI/UX DESIGNER
--------------------------------------------------

Responsibilities:

- Design the home screen.
- Design gameplay HUD.
- Design level selection.
- Design settings.
- Design pause.
- Design completion.
- Design confirmation dialogs.
- Design tutorial guidance.
- Establish interaction hierarchy.
- Match the toy-block visual style.

Deliverable:

- Complete polished mobile UI and UX.

--------------------------------------------------
AGENT 10: ACCESSIBILITY DESIGNER
--------------------------------------------------

Responsibilities:

- Review contrast.
- Avoid color-only feedback.
- Support reduced motion.
- Support sound and haptic settings.
- Review touch sizes.
- Review icon clarity.
- Review tutorial text.
- Prepare strings for localization.
- Prevent rapid flashing.

Deliverable:

- Accessible readable gameplay and settings.

--------------------------------------------------
AGENT 11: ART DIRECTOR
--------------------------------------------------

Responsibilities:

- Define the visual style.
- Match the supplied concept.
- Maintain consistent lighting.
- Maintain consistent bevels.
- Maintain consistent shadows.
- Approve palette usage.
- Reject noisy, metallic, flat, realistic, or inconsistent art.
- Review generated assets.

Deliverable:

- Unified approved art direction.

--------------------------------------------------
AGENT 12: PROCEDURAL ART GENERATION ENGINEER
--------------------------------------------------

Responsibilities:

- Build sprite-generation tools.
- Generate geometric shapes.
- Generate bevels.
- Generate shadows.
- Generate board art.
- Generate UI art.
- Generate icons.
- Generate effect textures.
- Generate thumbnails.
- Use supersampling.
- Ensure deterministic output.
- Prevent duplicate assets.

Deliverable:

- Complete procedural art-generation pipeline.

--------------------------------------------------
AGENT 13: TECHNICAL ARTIST
--------------------------------------------------

Responsibilities:

- Configure sprite import settings.
- Configure pivots.
- Configure 9-slicing.
- Configure compression.
- Configure atlases.
- Minimize overdraw.
- Minimize texture memory.
- Create lightweight materials.
- Check small-screen readability.
- Balance quality and performance.

Deliverable:

- Mobile-ready art and rendering setup.

--------------------------------------------------
AGENT 14: PUZZLE DESIGNER
--------------------------------------------------

Responsibilities:

- Define how each object is constructed.
- Maintain recognizable silhouettes.
- Select reusable modular shapes.
- Avoid ambiguous target relationships.
- Ensure pieces fit cleanly.
- Ensure pieces remain selectable.
- Define complexity rules by tier.
- Review all target concepts.

Deliverable:

- Construction plans for all 50 target objects.

--------------------------------------------------
AGENT 15: LEVEL DESIGNER
--------------------------------------------------

This agent is mandatory.

Do not skip this role.

Do not replace handcrafted target design with random generation.

Responsibilities:

- Create all 50 target layouts.
- Create recognizable object silhouettes.
- Select board dimensions.
- Select piece shapes.
- Select piece colors.
- Define target positions.
- Define target rotations.
- Define starting positions.
- Define starting rotations.
- Configure allowed rotations.
- Configure starting locked pieces.
- Configure correct-placement locking.
- Balance piece counts.
- Balance color repetition.
- Balance shape repetition.
- Balance rotation complexity.
- Maintain a smooth difficulty curve.
- Ensure starting states are valid.
- Ensure starting states are unsolved.
- Ensure every piece is required.
- Ensure no extra piece exists.
- Ensure target pieces do not overlap.
- Ensure starting pieces do not overlap.
- Ensure pieces remain inside the board.
- Ensure targets are recognizable.
- Ensure puzzles are solvable.
- Review deterministic scrambles.
- Hand-test representative levels from every tier.
- Run validation on every level.
- Revise unclear, repetitive, weak, or unbalanced levels.

Required deliverables:

- At least 50 handcrafted target layouts
- 50 valid starting layouts
- Designer-readable source JSON
- Difficulty labels
- Recommended move counts
- Generated thumbnails
- Level-design validation report
- Confirmation that all levels pass

--------------------------------------------------
AGENT 16: LEVEL-PROGRESSION DESIGNER
--------------------------------------------------

Responsibilities:

- Organize the five difficulty tiers.
- Define unlock progression.
- Introduce mechanics gradually.
- Avoid sudden difficulty spikes.
- Balance piece counts.
- Balance color and shape ambiguity.
- Define recommended move counts.
- Review the entire Level 1-50 sequence.

Deliverable:

- Smooth progression from beginner to advanced.

--------------------------------------------------
AGENT 17: LEVEL-TOOLS ENGINEER
--------------------------------------------------

Responsibilities:

- Build the visual Level Editor.
- Support visual piece editing.
- Support footprint editing.
- Support pivot editing.
- Support pose editing.
- Display occupancy.
- Preview rotations.
- Preview targets.
- Generate valid scrambles.
- Validate levels.
- Save JSON.
- Generate thumbnails.
- Support editor undo.

Deliverable:

- Practical level-authoring workflow.

--------------------------------------------------
AGENT 18: CONTENT VALIDATION ENGINEER
--------------------------------------------------

Responsibilities:

- Validate level IDs.
- Validate piece IDs.
- Detect duplicate layouts.
- Detect overlaps.
- Detect out-of-bounds pieces.
- Detect solved starting states.
- Detect unsupported rotations.
- Detect missing pieces.
- Detect extra pieces.
- Confirm JSON/runtime parity.
- Confirm preview/gameplay parity.
- Confirm solvability.
- Produce exact error reports.

Deliverable:

- Zero unresolved level-validation errors.

--------------------------------------------------
AGENT 19: ANIMATION DESIGNER
--------------------------------------------------

Responsibilities:

- Animate selection.
- Animate pickup.
- Animate return.
- Animate rotation.
- Animate snapping.
- Animate rejection.
- Animate buttons.
- Animate hints.
- Animate popups.
- Animate completion.
- Respect reduced motion.
- Avoid heavy packages.

Deliverable:

- Lightweight polished animation set.

--------------------------------------------------
AGENT 20: VFX ARTIST
--------------------------------------------------

Responsibilities:

- Create star bursts.
- Create sparkles.
- Create soft confetti.
- Create hint pulses.
- Create correct-placement highlights.
- Pool effects.
- Minimize overdraw.
- Match the toy style.

Deliverable:

- Optimized reusable VFX.

--------------------------------------------------
AGENT 21: AUDIO DESIGNER
--------------------------------------------------

Responsibilities:

- Generate or configure toy-like sounds.
- Balance levels.
- Avoid clipping.
- Configure pooling.
- Respect settings.
- Review feedback for all important actions.

Deliverable:

- Complete lightweight audio-feedback set.

--------------------------------------------------
AGENT 22: HAPTICS ENGINEER
--------------------------------------------------

Responsibilities:

- Implement optional haptics.
- Wrap platform behavior.
- Respect settings.
- Keep feedback light.
- Ensure compilation without platform APIs.
- Provide safe fallbacks.

Deliverable:

- Safe platform-independent haptic layer.

--------------------------------------------------
AGENT 23: SAVE AND PROGRESSION ENGINEER
--------------------------------------------------

Responsibilities:

- Implement versioned saves.
- Save completion.
- Save best results.
- Save settings.
- Save tutorials.
- Handle corruption.
- Handle migration.
- Add development unlock.
- Test recovery.

Deliverable:

- Reliable local persistence.

--------------------------------------------------
AGENT 24: PERFORMANCE ENGINEER
--------------------------------------------------

Responsibilities:

- Profile allocations.
- Remove idle allocations.
- Minimize drag allocations.
- Reduce draw calls.
- Reduce Canvas rebuilding.
- Reduce texture memory.
- Configure pooling.
- Detect unnecessary Update methods.
- Review loading and unloading.
- Create a performance report.

Deliverable:

- Game matching the mobile performance budget.

--------------------------------------------------
AGENT 25: MOBILE OPTIMIZATION ENGINEER
--------------------------------------------------

Responsibilities:

- Configure Android and iOS settings.
- Configure portrait orientation.
- Configure frame rate.
- Configure texture compression.
- Handle pause and resume.
- Handle interrupted touches.
- Review low-memory behavior.
- Review effects on weak hardware.

Deliverable:

- Mobile-ready project configuration.

--------------------------------------------------
AGENT 26: EDITOR TOOLS ENGINEER
--------------------------------------------------

Responsibilities:

- Build one-click complete generation.
- Build separate generation commands.
- Make tools idempotent.
- Add progress reporting.
- Add clear errors.
- Preserve source data.
- Maintain stable asset paths.
- Configure scenes and build settings.

Deliverable:

- Reliable editor automation.

--------------------------------------------------
AGENT 27: JSON DATA PIPELINE ENGINEER
--------------------------------------------------

Responsibilities:

- Define the versioned level schema.
- Implement JSON serialization.
- Implement JSON import.
- Implement schema migration.
- Implement runtime asset conversion.
- Implement LevelCatalog generation.
- Preserve stable IDs.
- Prevent invalid content from entering runtime.
- Verify JSON/runtime parity.
- Keep editor code out of builds.

Deliverable:

- Reliable authoritative JSON-to-runtime pipeline.

--------------------------------------------------
AGENT 28: QA ENGINEER
--------------------------------------------------

Responsibilities:

- Create the QA plan.
- Test gameplay actions.
- Test screen navigation.
- Test popups.
- Test settings.
- Test save recovery.
- Test level transitions.
- Test safe areas.
- Test interrupted input.
- Test reset and undo.
- Test every difficulty tier.
- Test all 50 levels.
- Record reproducible issues.
- Confirm fixes.

Deliverable:

- Completed QA checklist with no blocking issues.

--------------------------------------------------
AGENT 29: AUTOMATED TEST ENGINEER
--------------------------------------------------

Responsibilities:

- Create edit-mode tests.
- Create play-mode tests.
- Test grid math.
- Test occupancy.
- Test placement.
- Test rotation.
- Test JSON import.
- Test schema migration.
- Test runtime data parity.
- Test completion.
- Test undo.
- Test saving.
- Validate all 50 levels.

Deliverable:

- Passing automated test suite.

--------------------------------------------------
AGENT 30: CODE REVIEW ENGINEER
--------------------------------------------------

Responsibilities:

- Review production scripts.
- Detect duplicated logic.
- Detect unsafe event handling.
- Detect hidden allocations.
- Detect excessive coupling.
- Detect null-safety issues.
- Detect editor APIs in runtime.
- Detect unused code.
- Detect placeholders.
- Review naming and folder conventions.

Deliverable:

- Resolved code-review report.

--------------------------------------------------
AGENT 31: BUILD AND RELEASE ENGINEER
--------------------------------------------------

Responsibilities:

- Configure build scenes.
- Configure startup scene.
- Configure orientation.
- Configure graphics settings.
- Validate platform compilation where available.
- Exclude editor assets from runtime.
- Detect missing scripts.
- Detect missing references.
- Review development and release configurations.

Deliverable:

- Android- and iOS-ready project configuration.

--------------------------------------------------
AGENT 32: DOCUMENTATION ENGINEER
--------------------------------------------------

Responsibilities:

- Document setup.
- Document architecture.
- Document generation tools.
- Document JSON pipeline.
- Document level creation.
- Document art generation.
- Document optimization.
- Document testing.
- Document builds.
- Document limitations.
- Keep documentation synchronized.

Deliverable:

- Complete production documentation.

==================================================
45. AGENT EXECUTION ORDER
==================================================

Execute agents in this dependency-aware order.

--------------------------------------------------
PHASE 1: DISCOVERY AND DIRECTION
--------------------------------------------------

1. Executive Producer
2. Lead Integration Agent
3. Game Director
4. Technical Architect
5. Art Director
6. Puzzle Designer

Phase 1 deliverables:

- Repository inspection
- Unity version report
- Render-pipeline report
- Existing-system report
- Architecture plan
- Production milestones
- Gameplay rules
- Art direction
- Level-complexity rules

--------------------------------------------------
PHASE 2: FOUNDATIONAL RUNTIME
--------------------------------------------------

7. Grid and Puzzle-System Engineer
8. Gameplay Programmer
9. Input and Touch Engineer
10. Camera and Responsive-Layout Engineer
11. Save and Progression Engineer

Phase 2 validation:

- Compile the project.
- Run grid tests.
- Run placement tests.
- Verify mouse input.
- Verify touch input or touch simulation.
- Verify one temporary test level.
- Verify save creation.

--------------------------------------------------
PHASE 3: ART AND CONTENT PIPELINE
--------------------------------------------------

12. Procedural Art Generation Engineer
13. Technical Artist
14. JSON Data Pipeline Engineer
15. Editor Tools Engineer
16. Level-Tools Engineer
17. Content Validation Engineer

Phase 3 validation:

- Generate test art.
- Generate a test board.
- Generate a test piece set.
- Import one test JSON level.
- Generate a runtime asset.
- Generate a target preview.
- Generate a thumbnail.
- Verify idempotent regeneration.
- Verify source JSON preservation.

--------------------------------------------------
PHASE 4: LEVEL PRODUCTION
--------------------------------------------------

18. Level Designer
19. Level-Progression Designer
20. Content Validation Engineer second pass

The Level Designer must not begin full production until:

- Grid footprints work.
- Rotation tests pass.
- Target previews work.
- JSON serialization works.
- JSON import works.
- Runtime conversion works.
- Level validation works.
- The visual Level Editor works.

Phase 4 deliverables:

- 50 handcrafted targets
- 50 valid starting layouts
- 50 source JSON files
- 50 runtime assets
- 50 thumbnails
- LevelCatalog
- Difficulty progression
- Full validation report

--------------------------------------------------
PHASE 5: PRESENTATION
--------------------------------------------------

21. UI/UX Designer
22. Accessibility Designer
23. Animation Designer
24. VFX Artist
25. Audio Designer
26. Haptics Engineer

Phase 5 validation:

- Compile the project.
- Review every screen.
- Review every popup.
- Review tutorial flow.
- Review reduced motion.
- Review sound settings.
- Review haptic fallbacks.
- Review gameplay clarity.

--------------------------------------------------
PHASE 6: OPTIMIZATION AND RELEASE
--------------------------------------------------

27. Performance Engineer
28. Mobile Optimization Engineer
29. Automated Test Engineer
30. QA Engineer
31. Code Review Engineer
32. Build and Release Engineer
33. Documentation Engineer
34. Lead Integration Agent final review
35. Executive Producer final acceptance

==================================================
46. PHASE HANDOFF RULES
==================================================

At the end of every phase:

- Compile the project.
- Fix compilation errors.
- Validate newly created assets.
- Run relevant automated tests.
- Record completed deliverables.
- Record unresolved warnings.
- Do not continue while blockers remain.

The QA Engineer must reject the project when:

- Any level is invalid.
- Any target is unrecognizable.
- Any level contains an extra piece.
- Any required piece is missing.
- Pieces can overlap.
- Pieces can leave the board.
- The target preview differs from the solution.
- Touch controls fail.
- Mouse controls fail.
- Progress does not save.
- Corrupted-save recovery fails.
- JSON and runtime assets differ.
- Missing references exist.
- The startup scene is not configured.
- The complete-game generator fails.
- Critical recurring runtime allocations remain.

==================================================
47. SCENES AND PREFABS
==================================================

Create and configure required scenes automatically.

Possible scenes:

- Bootstrap
- MainMenu
- Gameplay

A single managed scene with screen panels is acceptable when more efficient.

Create prefabs for:

- Puzzle board
- Puzzle piece
- Target reference preview
- Gameplay HUD
- Home screen
- Level-select entry
- Settings popup
- Pause popup
- Completion popup
- Confirmation popup
- Tutorial finger
- Star effect
- Sparkle effect
- Confetti effect

No prefab may contain:

- Missing scripts
- Missing references
- Broken serialized data
- Unnecessary components

==================================================
48. FINAL ACCEPTANCE CRITERIA
==================================================

The project is complete only when all of the following are true:

- The Unity project compiles without errors.
- A valid startup scene is configured.
- The game opens to the home screen.
- The home screen works.
- Level selection works.
- Gameplay works.
- Settings work.
- Pause works.
- Completion UI works.
- Mouse input works in the Editor.
- Touch input works on mobile.
- The target reference remains visible above the board.
- The target reference uses the same data as gameplay.
- Pieces can be selected.
- Pieces can be dragged.
- Pieces can rotate.
- Valid placements snap correctly.
- Invalid placements are rejected.
- Pieces cannot overlap.
- Pieces cannot leave the board.
- Correct positions provide clear feedback.
- Move counting works.
- Undo works.
- Hints work.
- Reset works.
- Level completion works.
- Progress saves.
- Progress restores.
- Corrupted saves recover safely.
- The next level unlocks.
- At least 50 unique levels exist.
- All 50 target layouts are handcrafted.
- All 50 starting layouts are valid.
- Every level contains exactly the required pieces.
- No level contains decoys.
- No level contains spare pieces.
- Every target is recognizable.
- Every thumbnail is generated.
- All 50 JSON sources exist.
- All 50 JSON sources pass schema validation.
- All 50 JSON sources import successfully.
- All 50 runtime assets are generated.
- JSON is the authoritative source.
- Runtime gameplay does not parse source JSON.
- Runtime gameplay uses generated Unity assets.
- Invalid source levels cannot enter LevelCatalog.
- Reimporting does not create duplicates.
- Reimporting preserves stable IDs.
- Clearing generated content preserves source JSON.
- JSON and runtime data remain synchronized.
- The visual Level Editor saves back to JSON.
- All art uses one consistent toy-block style.
- UI adapts to portrait phones and tablets.
- Safe areas are respected.
- Reduced motion works.
- Sound settings work.
- Haptic settings work.
- Generated art requires no external service.
- Generated content can be rebuilt through one command.
- Generation is idempotent.
- There are no missing references.
- There are no production TODOs.
- There are no placeholder implementations.
- Idle gameplay has no recurring garbage allocations.
- Dragging has near-zero garbage allocations.
- No unnecessary physics is used.
- No real-time shadows are used.
- No post-processing is used.
- Tests compile and pass.
- Android settings are configured.
- iOS settings are configured.
- Documentation is complete.
- The Level Designer Agent confirms all 50 levels.
- The Lead Integration Agent approves the project.
- The Executive Producer accepts the project.

==================================================
49. FINAL IMPLEMENTATION REPORT
==================================================

After implementation, provide a concise but complete final report containing:

1. Detected Unity version
2. Detected render pipeline
3. Major systems created
4. Results from every agent role
5. Important files created or modified
6. Final project folder structure
7. Number of source JSON levels discovered
8. Number of source JSON levels imported
9. Number of runtime level assets generated
10. Number of handcrafted levels created
11. Number of levels passing validation
12. Difficulty distribution
13. JSON schema version
14. Number of schema migrations performed
15. Number of rejected invalid source files
16. JSON source folder path
17. Runtime-level folder path
18. LevelCatalog path
19. Art assets generated
20. Atlases generated
21. Editor tools created
22. Scenes created
23. Prefabs created
24. Tests executed
25. Test results
26. Performance findings
27. Build-readiness status
28. Remaining non-blocking warnings
29. Exact editor command used to regenerate the game
30. Confirmation that runtime gameplay does not parse source JSON
31. Confirmation that all 50 JSON levels match runtime assets
32. Confirmation that the Level Designer completed all 50 levels
33. Confirmation that the Lead Integration Agent approved the project
34. Confirmation that the Executive Producer accepted the project

==================================================
50. BEGIN EXECUTION
==================================================

Begin by inspecting the current Unity repository.

Then:

1. Report the detected Unity version.
2. Report the detected render pipeline.
3. Report the existing project structure.
4. Identify reusable systems and assets.
5. Identify conflicts or missing requirements.
6. Establish architecture and production milestones.
7. Implement the project phase by phase.
8. Compile after every major phase.
9. Fix errors immediately.
10. Generate the complete art system.
11. Build the hybrid JSON level pipeline.
12. Build the visual Level Editor.
13. Create all 50 handcrafted levels.
14. Generate all runtime level assets.
15. Generate all previews and thumbnails.
16. Validate all levels.
17. Build the complete UI.
18. Implement audio, haptics, animation, and VFX.
19. Run the full test suite.
20. Perform optimization and QA.
21. Configure Android and iOS project settings.
22. Create all documentation.
23. Produce the final implementation report.

Do not stop after creating a plan.

Do not merely describe how the game could be built.

Do not provide only code snippets.

Implement the complete playable game directly inside the repository.