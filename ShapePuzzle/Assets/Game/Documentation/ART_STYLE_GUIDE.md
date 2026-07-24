# Art Style Guide

The visual target is `Reference_01.png`: chunky molded preschool toys on a deep blue field, with charcoal padded surfaces and cream icons.

## Palette

The authoritative runtime palette is `ToyPalette`. Its initial colors are background `#105C8E`, secondary blue `#1674AA`, frame `#20241D`, cells `#292E26` / `#30352D`, red `#EF4016`, yellow `#FFC319`, cyan `#20A8DC`, green `#59C62C`, orange `#FF7B13`, and cream `#F4F0DF`.

Use color through the palette; do not scatter literals through gameplay. Pair color with silhouette, motion, icons, or state changes.

## Shape and lighting

- Use broad rounded corners and simple readable silhouettes.
- Bake a soft top-left highlight and darker lower-right edge.
- Keep surfaces matte with extremely subtle texture variation.
- Keep contact shadows short and low-alpha.
- Avoid outlines, metallic highlights, bloom, grunge, thin details, and noisy patterns.

Neutral rounded/circle sprites are tinted by `PuzzleBoardView`, keeping one consistent bevel across every piece color. Reference previews use the exact same piece creation path as gameplay.

## Adding art

Add new deterministic raster operations to `ToyArtGenerator`, keep supersampling enabled, import as sprites without mipmaps, and cap UI art at 512 px. If a new shape cannot be represented by composed cells, add a neutral reusable sprite and extend `PuzzlePieceView` shape selection. Run Generate Art Only, Rebuild Atlases, then verify gameplay and thumbnail readability at small size.
