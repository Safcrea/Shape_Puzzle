# Toy Puzzle — Level Distribution and Board Sizing Guide

## Purpose

This document defines the recommended board dimensions, inner tile sizes, object scaling rules, bottom-control layout, and level distribution for a portrait mobile puzzle game targeting low-end Android and iOS devices.

The board should remain visually consistent across all levels. Difficulty should increase through grid density, piece count, shape complexity, rotation requirements, color repetition, and starting-layout complexity—not by manually changing the visual board size for individual objects.

---

## 1. Core Board Strategy

Use a **fixed physical board size** across all levels.

Change only:

- Grid count
- Piece count
- Shape complexity
- Target composition
- Starting arrangement
- Allowed rotations
- Correct-piece locking behavior

Do not manually resize the board or assign different tile sizes per object.

### Recommended Rule

```text
Tile Size = Inner Board Size / Grid Count
```

This provides:

- Consistent dragging
- Predictable snapping
- Stable touch behavior
- Reusable art
- Easier level validation
- Cleaner JSON data
- Better responsive layout
- More reliable low-end-device performance

---

## 2. Recommended 1080×1920 Reference Layout

Use the following reference layout for portrait devices:

| Area | Recommended Size |
|---|---:|
| Horizontal safe margin | 48–64 px |
| Top reference area | 230–260 px |
| Gap above board | 24–32 px |
| Board outer size | 920×920 px |
| Board frame thickness | 40 px |
| Board inner playable area | 840×840 px |
| Gap below board | 28–40 px |
| Bottom controls area | 190–230 px |
| Bottom safe padding | Device safe area + 24 px |

### Recommended Default

```text
Reference Resolution: 1080×1920
Outer Board: 920×920
Inner Board: 840×840
Frame Thickness: 40 px
Bottom Controls Zone: approximately 210 px
```

The final runtime layout must scale from the current device safe area rather than using fixed pixels directly.

---

## 3. Tile Size by Grid

With an 840×840 inner board:

| Grid Size | Tile Size | Recommended Use |
|---|---:|---|
| 5×5 | 168 px | Tutorials and very simple objects |
| 6×6 | 140 px | Most early and medium levels |
| 7×7 | 120 px | Medium-complexity silhouettes |
| 8×8 | 105 px | Advanced levels with more pieces |

### Formula

```text
5×5: 840 / 5 = 168 px
6×6: 840 / 6 = 140 px
7×7: 840 / 7 = 120 px
8×8: 840 / 8 = 105 px
```

Do not assign object-specific tile sizes such as a truck using 150 px and a rocket using 130 px.

The object should be adapted through its grid layout, not through an inconsistent tile scale.

---

## 4. Responsive Runtime Calculation

Calculate the board from available safe-area space.

```csharp
float availableWidth = safeAreaWidth - horizontalMargins;

float availableHeight =
    safeAreaHeight
    - topAreaHeight
    - bottomAreaHeight
    - verticalSpacing;

float outerBoardSize = Mathf.Min(availableWidth, availableHeight);
float innerBoardSize = outerBoardSize - frameThickness * 2f;
float cellSize = innerBoardSize / level.GridSize;
```

Important rules:

- Use one uniform board scale.
- Keep width and height equal.
- Never stretch cells differently on each axis.
- Keep the board square.
- Scale the entire board as one unit.
- Recalculate when the safe area changes.
- Preserve practical touch sizes on narrow devices.

---

## 5. Object Size Inside the Grid

The completed target object should not touch the board edges.

### Recommended Target Margin

```text
Minimum margin: 0.5 to 1 full grid cell
```

### Recommended Board Usage

| Dimension | Recommended Usage |
|---|---:|
| Target width | 65–85% of board width |
| Target height | 55–80% of board height |
| Maximum recommended use | 85% in either direction |

### Example: Wide Truck

For a 6×6 board:

```text
Target width: approximately 5 cells
Target height: approximately 3 cells
```

### Example: Vertical Rocket

For a 6×6 board:

```text
Target width: approximately 3–4 cells
Target height: approximately 5 cells
```

Do not enlarge tiles just to remove empty space.

Empty cells are required for manipulation, temporary placement, and rotation.

---

## 6. Free-Space Requirements

The solved object should not occupy the entire board.

### Recommended Solved Occupancy

| Difficulty | Maximum Occupied Cells |
|---|---:|
| Easy | 40–50% |
| Medium | 50–60% |
| Hard | 55–68% |

### Recommended Minimum Free Space

```text
Keep at least 30% of the board free where practical.
```

Suggested occupied-cell ranges:

| Grid | Suggested Occupied Cells |
|---|---:|
| 5×5 | 10–14 cells |
| 6×6 | 16–22 cells |
| 7×7 | 23–31 cells |
| 8×8 | 31–43 cells |

The starting layout may use more scattered cells than the solved layout, but all pieces must remain visible and movable.

---

## 7. Bottom Controls UX

Use four permanent bottom controls:

1. Undo
2. Hint
3. Rotate
4. Pause

### Recommended Button Dimensions

```text
Visible button: 144–156 px
Recommended default: 148×148 px
Touch area: approximately 168×168 px
Gap between buttons: 20–28 px
Bottom control zone: approximately 200–220 px
```

### Recommended Order

```text
[ Undo ] [ Hint ] [ Rotate ] [ Pause ]
```

### Label Rules

For tutorial levels:

- Show icon and short label.
- Use labels such as Undo, Hint, Rotate, and Pause.

After tutorial completion:

- Prefer icon-only controls.
- Keep accessible names internally.
- Show labels only when required for accessibility or onboarding.

The visible sprite may be smaller than the touch target.

Do not reduce the touch target to match a narrow visual icon.

---

## 8. Grid Cell Visual Spacing

Grid cells should be clearly separated without wasting board space.

### Cell Gap

```text
Cell gap = approximately 3–4% of cell size
```

| Grid | Suggested Gap |
|---|---:|
| 5×5 | 6 px |
| 6×6 | 5 px |
| 7×7 | 4–5 px |
| 8×8 | 4 px |

### Cell Corner Radius

```text
Cell radius = approximately 7–10% of cell size
```

### Piece Corner Radius

```text
Piece radius = approximately 10–16% of cell size
```

### Visual Overhang

Puzzle-piece art may extend beyond logical occupied cells by:

```text
Approximately 4–7% of cell size
```

Visual overhang must not affect logical occupancy or overlap validation.

---

## 9. Touch Handling

The logical grid cell may become smaller on 8×8 boards, but the touch target must remain practical.

### Minimum Touch Target

```text
44–48 dp minimum
```

Use an expanded invisible hit region for:

- Small triangles
- Narrow bars
- Thin connectors
- Small wheels
- Small decorative pieces

Do not enlarge the logical footprint only to make a piece easier to select.

Logical occupancy and touch detection should remain separate systems.

---

## 10. Recommended 50-Level Grid Distribution

Use this distribution as the default progression:

| Level Range | Recommended Grid |
|---|---|
| Levels 1–5 | 5×5 |
| Levels 6–15 | 6×6 |
| Levels 16–30 | 6×6 or 7×7 |
| Levels 31–42 | 7×7 |
| Levels 43–50 | 8×8 |

Do not increase the grid automatically only because the level number increases.

Choose the grid using:

- Target silhouette
- Piece count
- Required free space
- Rotation complexity
- Similarity between pieces
- Shape repetition
- Target aspect ratio
- Touch accessibility

---

## 11. Detailed Level Distribution

## Tier 1 — Levels 1–10

### Goals

- Teach dragging
- Teach rotation
- Teach invalid placement
- Teach hints
- Build player confidence
- Use large, readable pieces

### General Setup

| Property | Recommendation |
|---|---|
| Grid | 5×5 and 6×6 |
| Pieces | 4–6 |
| Occupancy | 40–50% |
| Rotation | Limited |
| Locking | Correct pieces lock |
| Colors | Highly distinct |
| Starting assistance | Allowed |

| Level | Object | Grid | Suggested Pieces | Notes |
|---:|---|---:|---:|---|
| 1 | Airplane | 5×5 | 4 | Drag tutorial |
| 2 | Truck | 5×5 | 5 | Rotation tutorial |
| 3 | Car | 5×5 | 5 | Overlap tutorial |
| 4 | Rocket | 5×5 | 5 | Hint tutorial |
| 5 | Boat | 5×5 | 5 | Simple horizontal silhouette |
| 6 | Bicycle | 6×6 | 6 | First thin connectors |
| 7 | Train | 6×6 | 6 | Repeated wheel shapes |
| 8 | Bus | 6×6 | 6 | Wider object |
| 9 | Sailboat | 6×6 | 6 | Triangle rotation |
| 10 | Scooter | 6×6 | 6 | Mixed small and long pieces |

---

## Tier 2 — Levels 11–20

### Goals

- Increase rotation usage
- Introduce irregular footprints
- Add moderate shape repetition
- Require more planning

### General Setup

| Property | Recommendation |
|---|---|
| Grid | Mostly 6×6 |
| Pieces | 6–9 |
| Occupancy | 45–58% |
| Rotation | Moderate |
| Locking | Usually enabled |
| Colors | Limited repetition |
| Starting assistance | Minimal |

| Level | Object | Grid | Suggested Pieces | Notes |
|---:|---|---:|---:|---|
| 11 | Helicopter | 6×6 | 7 | Long rotor and tail |
| 12 | Submarine | 6×6 | 7 | Curved body pieces |
| 13 | Taxi | 6×6 | 7 | Repeated rectangular forms |
| 14 | Fire Truck | 6×6 | 8 | Long body and ladder |
| 15 | Tractor | 6×6 | 8 | Different wheel sizes |
| 16 | Excavator | 6×6 | 8 | Arm orientation challenge |
| 17 | Hot-Air Balloon | 6×6 | 7 | Vertical composition |
| 18 | Spaceship | 6×6 | 8 | Symmetrical target |
| 19 | Robot | 6×6 | 9 | Repeated limb pieces |
| 20 | House | 6×6 | 8 | Roof and wall alignment |

---

## Tier 3 — Levels 21–30

### Goals

- Increase silhouette complexity
- Use repeated colors
- Introduce 7×7 boards
- Use more polyomino and custom pieces

### General Setup

| Property | Recommendation |
|---|---|
| Grid | 6×6 and 7×7 |
| Pieces | 8–11 |
| Occupancy | 50–60% |
| Rotation | Moderate to high |
| Locking | Configurable |
| Colors | Repeated |
| Shape repetition | Moderate |

| Level | Object | Grid | Suggested Pieces | Notes |
|---:|---|---:|---:|---|
| 21 | Castle | 7×7 | 10 | Repeated towers |
| 22 | Windmill | 7×7 | 9 | Blade rotations |
| 23 | Lighthouse | 6×6 | 9 | Tall narrow silhouette |
| 24 | Bridge | 7×7 | 10 | Repeated structural pieces |
| 25 | Tree | 6×6 | 9 | Organic-looking block arrangement |
| 26 | Flower | 6×6 | 9 | Radial petal placement |
| 27 | Cactus | 6×6 | 8 | Branch rotation |
| 28 | Mushroom | 6×6 | 8 | Curved cap construction |
| 29 | Fish | 7×7 | 10 | Tail and fin orientation |
| 30 | Whale | 7×7 | 11 | Wide curved silhouette |

---

## Tier 4 — Levels 31–40

### Goals

- Increase piece count
- Introduce duplicate shape types
- Increase symmetry
- Reduce reliance on automatic locking

### General Setup

| Property | Recommendation |
|---|---|
| Grid | Mostly 7×7 |
| Pieces | 10–14 |
| Occupancy | 55–65% |
| Rotation | High |
| Locking | Limited or configurable |
| Colors | Similar color groups |
| Shape repetition | High |

| Level | Object | Grid | Suggested Pieces | Notes |
|---:|---|---:|---:|---|
| 31 | Crab | 7×7 | 11 | Mirrored claws and legs |
| 32 | Turtle | 7×7 | 11 | Symmetrical limbs |
| 33 | Butterfly | 7×7 | 12 | Strong bilateral symmetry |
| 34 | Owl | 7×7 | 12 | Repeated eye and wing shapes |
| 35 | Cat | 7×7 | 12 | Ear and leg orientation |
| 36 | Dog | 7×7 | 12 | Similar body pieces |
| 37 | Duck | 7×7 | 11 | Curved silhouette |
| 38 | Elephant | 7×7 | 13 | Trunk and ear complexity |
| 39 | Giraffe | 7×7 | 13 | Tall narrow composition |
| 40 | Penguin | 7×7 | 12 | Symmetrical body layout |

---

## Tier 5 — Levels 41–50

### Goals

- Use advanced layouts
- Increase similar-looking pieces
- Use 8×8 boards
- Require full rotation knowledge
- Keep pieces touchable despite higher density

### General Setup

| Property | Recommendation |
|---|---|
| Grid | 7×7 and 8×8 |
| Pieces | 12–18 |
| Occupancy | 58–68% |
| Rotation | Full |
| Locking | Reduced or disabled |
| Colors | Repeated |
| Shape repetition | High |
| Free space | Never below practical manipulation needs |

| Level | Object | Grid | Suggested Pieces | Notes |
|---:|---|---:|---:|---|
| 41 | Ice Cream | 7×7 | 12 | Cone and scoop composition |
| 42 | Cupcake | 7×7 | 12 | Layered horizontal pieces |
| 43 | Camera | 8×8 | 14 | Repeated controls and lens parts |
| 44 | Guitar | 8×8 | 15 | Long neck and curved body |
| 45 | Umbrella | 8×8 | 14 | Repeated canopy segments |
| 46 | Crown | 8×8 | 15 | Repeated points and jewels |
| 47 | Key | 8×8 | 13 | Long narrow silhouette |
| 48 | Gift Box | 8×8 | 16 | Ribbon symmetry |
| 49 | Clock | 8×8 | 16 | Radial and repeated pieces |
| 50 | Star Trophy | 8×8 | 18 | Final advanced composition |

---

## 12. Grid Selection Rules by Object Type

Use these guidelines when assigning future objects:

### Use 5×5 When

- The object uses 4–6 pieces.
- The silhouette is simple.
- Most pieces occupy large rectangular footprints.
- The level is instructional.
- Rotation is minimal.

### Use 6×6 When

- The object uses 6–9 pieces.
- The object is wider or taller than a 5×5 board supports.
- The target needs moderate free space.
- Irregular shapes are introduced.
- The object remains readable with 140 px reference cells.

### Use 7×7 When

- The object uses 9–14 pieces.
- The target has repeated or symmetrical parts.
- More rotation decisions are required.
- The silhouette contains limbs, fins, wings, or connectors.
- More empty manipulation space is required around the target.

### Use 8×8 When

- The object uses 12–18 pieces.
- The object contains similar-looking parts.
- Full rotation mechanics are active.
- The silhouette is complex.
- The touch targets can still meet accessibility requirements.
- The starting layout remains readable and movable.

---

## 13. Starting Layout Rules

The starting arrangement must:

- Include every required piece.
- Contain no extra pieces.
- Remain entirely inside the board.
- Avoid logical overlap.
- Keep every piece visible.
- Keep every piece selectable.
- Avoid covering smaller pieces.
- Avoid starting already solved.
- Preserve enough empty cells for movement.
- Avoid placing all pieces against the same board edge.
- Avoid excessive random scattering.
- Be intentionally reviewed by the Level Designer.

### Early Levels

- One or two pieces may start correct.
- Correct pieces may start locked.
- Pieces should be near their target area.

### Mid Levels

- Most pieces should start incorrect.
- Rotation differences should be moderate.
- Avoid overwhelming visual disorder.

### Late Levels

- Use more distant starting positions.
- Use repeated shapes and colors.
- Use varied starting rotations.
- Keep the arrangement fair and readable.

---

## 14. Locking Distribution

Recommended correct-placement locking:

| Level Range | Locking Recommendation |
|---|---|
| 1–10 | Always lock correct pieces |
| 11–20 | Lock by default |
| 21–30 | Configurable by level |
| 31–40 | Limited locking |
| 41–50 | Usually no permanent locking |

Locking should reduce frustration early and increase planning difficulty later.

---

## 15. Rotation Distribution

| Level Range | Rotation Rules |
|---|---|
| 1 | No rotation required |
| 2–5 | One or two pieces require 90° rotation |
| 6–10 | Limited 90° and 180° rotation |
| 11–20 | Most pieces may rotate |
| 21–30 | Multiple valid-looking orientations |
| 31–40 | Repeated shapes create orientation ambiguity |
| 41–50 | Full 0°, 90°, 180°, and 270° use |

Symmetrical pieces should not require visually identical rotations to be distinguished unless the logical design specifically needs it.

---

## 16. Recommended Unity Constants

```csharp
public static class PuzzleLayoutConstants
{
    public const float ReferenceWidth = 1080f;
    public const float ReferenceHeight = 1920f;

    public const float ReferenceOuterBoardSize = 920f;
    public const float ReferenceInnerBoardSize = 840f;
    public const float ReferenceFrameThickness = 40f;

    public const float ReferenceBottomControlsHeight = 210f;
    public const float ReferenceVisibleButtonSize = 148f;
    public const float ReferenceButtonTouchSize = 168f;

    public const float MinimumSolvedFreeSpaceRatio = 0.30f;

    public static float CalculateReferenceCellSize(int gridSize)
    {
        return ReferenceInnerBoardSize / gridSize;
    }
}
```

Use runtime-calculated values for actual devices.

The constants above should act only as the design reference.

---

## 17. Final Recommended Configuration

```text
Reference Resolution: 1080×1920
Outer Board: 920×920
Inner Board: 840×840
Frame: 40 px
Supported Grids: 5×5, 6×6, 7×7, 8×8
Tile Size: Inner Board / Grid Count
Bottom Controls Zone: approximately 210 px
Visible Button Size: approximately 148 px
Touch Area: approximately 168 px
Target Margin: 0.5–1 cell
Minimum Free Space: approximately 30%
Maximum Target Use: approximately 85% of board width or height
```

The visual board size should remain consistent between levels.

Change the grid count and puzzle composition—not the physical board size or manually assigned tile size.
