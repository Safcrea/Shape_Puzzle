# Level Design Guide

Each level contains exactly the pieces used by one recognizable target. There are no decoys, spare parts, or filler pieces.

## Board and composition

Boards support independent widths and heights from 5–8 cells; square boards are the default and rectangular boards are allowed when the silhouette benefits. The rendered board envelope stays constant while cells stay square. Keep about 30% manipulation space, target occupancy below roughly 70%, and a half-to-one-cell target margin.

Targets must be recognizable at thumbnail size. Use large parts, strong color separation, and avoid single-cell details that become hard to touch. Starting layouts must be non-overlapping, fully in bounds, unsolved, visible, touchable, and intentionally rearrangeable.

## Progression

- Levels 1–10: 4–6 pieces, 5×5/6×6, limited rotation, locking enabled.
- Levels 11–20: 6–9 pieces, mostly 6×6, irregular shapes and moderate rotation.
- Levels 21–30: 8–11 pieces, 6×6/7×7, repeated colors and more orientation choices.
- Levels 31–40: 10–14 pieces, mostly 7×7, symmetry and similar shapes.
- Levels 41–50: 12–18 pieces, 7×7/8×8, full rotations and limited locking.

## Coordinates and rotation

Positions are integer grid coordinates. A piece footprint is a list of occupied cells relative to its authored logical origin. Rotations are clockwise quarter turns normalized to 0, 90, 180, or 270 around the logical pivot; the rotated footprint is normalized before placement. Visual pivot/overhang affects presentation only. Target and starting rotations must appear in `allowedRotations`.

After editing, run Validate All Levels and verify target/start occupancy, distinct layouts, silhouette readability, target preview parity, thumbnail parity, and the authored solution path.
