# Performance Budget

Target: 60 FPS on common low-end phones with a stable 30 FPS fallback. Normal gameplay should stay near 30 draw calls and about 32 MB active texture memory where practical.

## Runtime rules

- Screen Space Overlay uGUI, unlit sprites, no real-time lights/shadows or post-processing.
- No runtime texture generation, physics occupancy, per-piece `Update`, or scene reload between levels.
- Fixed AudioSource and effect pools; one active completion celebration.
- Separate stable screen roots from moving piece content to limit Canvas rebuilds.
- Recalculate safe area/layout only when dimensions change.
- Keep drag work integer-grid based and allocation-free in the steady loop.

## Assets

Generated UI sprites disable mipmaps/readability and cap maximum size at 512. Short mono sounds use 22.05 kHz ADPCM and decompress on load. UI, piece, and effect atlases keep batching predictable. Avoid large transparent full-screen art beyond the opaque background.

## Profiling

Profile a development build after level load, using an early 5×5, a mid 7×7, and Level 50. Capture idle, sustained dragging, repeated rotate/undo, level transition, level-select scrolling, pause/settings, and completion. Check CPU frame time, GC Alloc, batches, texture memory, audio voices, and UI layout rebuilds. Device validation remains mandatory because Editor profiling is not representative of mobile GPUs.
