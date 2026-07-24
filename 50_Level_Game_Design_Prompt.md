# 50-Level Toy Assembly Puzzle — Master Production Prompt

You are the lead game designer, senior level designer, mobile UX designer, Unity systems designer, technical artist and casual-game retention designer for a portrait mobile puzzle game.

Use the attached reference images as the visual source of truth.

The game already has its core concept. Do not replace or redesign the core gameplay. Your task is to plan, document and implement a polished 50-level progression with slowly increasing difficulty.

---

## 1. Game Concept

The player sees a completed toy object in a reference card at the top of the screen.

The player receives separate colorful toy pieces in a bottom tray.

The player must:

- Drag pieces from the tray.
- Rotate pieces in 90-degree increments.
- Position pieces on a visible grid.
- Snap pieces into valid cells.
- Reconstruct the object shown in the reference card.
- Complete the level when every required piece is correctly positioned and rotated.

The core experience should feel:

- Relaxing.
- Satisfying.
- Easy to understand.
- Visually rewarding.
- Suitable for children and casual players.
- Comfortable for short mobile sessions.
- Progressively challenging without becoming frustrating.

Do not add physics-based construction, freeform scaling, complicated inventory mechanics or unnecessary systems that change the main puzzle experience.

---

## 2. Game Direction

The player is rebuilding a magical toy collection.

Every completed puzzle restores one colorful toy object. Completed objects are added to a larger collection world.

The collection world should contain several connected areas:

- Toy Town.
- Fantasy Kingdom.
- Adventure Harbor.
- Wonder Workshop.
- Nature Garden.
- Funfair.
- Space Corner.

The level order should mix object categories.

Do not place too many similar objects consecutively.

Bad progression example:

- Truck.
- Bus.
- Fire truck.
- Train.
- Car.

Better progression example:

- Truck.
- Crown.
- Rocket.
- Tree.
- Airplane.
- Gift box.
- Castle.

The visual category should change frequently even when the puzzle mechanic remains the same.

The chapters are difficulty bands, not strict visual themes. Vehicles, castles, fantasy objects, buildings, nature items and everyday objects can appear within the same chapter.

---

## 3. Session and Campaign Target

The first 10 levels should provide approximately 12–15 minutes of gameplay, including:

- Tutorial moments.
- Level transitions.
- Completion animations.
- The first collection-area reveal.

Do not make the entire 50-level campaign last only 15 minutes.

Target durations:

- Levels 1–5: 30–50 seconds each.
- Levels 6–10: 40–65 seconds each.
- Levels 11–20: 50–85 seconds each.
- Levels 21–30: 65–105 seconds each.
- Levels 31–40: 80–135 seconds each.
- Levels 41–50: 95–180 seconds each.

Target full campaign length:

- Approximately 75–100 minutes.
- Approximately 5–7 natural play sessions.
- Typical play session: 10–18 minutes.

---

## 4. Core Gameplay Loop

For every level:

1. Display the completed object in the top reference card.
2. Show the empty puzzle grid.
3. Place the required pieces in the bottom tray.
4. Apply the level’s starting rotations.
5. Allow dragging and 90-degree rotation.
6. Provide clear valid and invalid placement feedback.
7. Snap correctly positioned pieces into place.
8. Return invalid pieces gently to the tray.
9. Detect completion.
10. Play a short object-specific completion animation.
11. Add the completed object to the collection world.
12. Move quickly to the next level.

Recommended transition timing:

- Final piece snap: immediate.
- Completion animation: 1–1.5 seconds.
- Celebration: 1–1.5 seconds.
- Reward summary: 1–2 seconds.
- Next-level loading: less than 1 second.

The player should normally enter the next level within four seconds.

---

## 5. Difficulty Design Principles

Difficulty must not depend only on the number of pieces.

Use the following difficulty controls:

1. Piece count.
2. Piece shape complexity.
3. Starting rotation.
4. Similar-looking pieces.
5. Mirrored pieces.
6. Object symmetry.
7. Board dimensions.
8. Snap tolerance.
9. Placement guidance.
10. Multi-cell pieces.
11. Thin or angled pieces.
12. Distractor pieces.
13. Number of valid placement possibilities.
14. Complexity of the final silhouette.
15. Hint availability.

Difficulty should rise gradually.

Recommended piece-count progression:

- Levels 1–5: 4–6 pieces.
- Levels 6–10: 6–7 pieces.
- Levels 11–20: 7–9 pieces.
- Levels 21–30: 8–10 pieces.
- Levels 31–40: 10–13 pieces.
- Levels 41–50: 11–16 pieces.

Starting rotation progression:

- Levels 1–3: almost every piece begins correctly rotated.
- Levels 4–10: one or two pieces begin rotated.
- Levels 11–20: two to four pieces begin rotated.
- Levels 21–30: three to six pieces begin rotated.
- Levels 31–40: five to nine pieces begin rotated.
- Levels 41–50: most pieces begin rotated.

Distractor progression:

- Levels 1–30: no distractors.
- Levels 31–35: optional distractor only in selected levels.
- Levels 36–40: maximum one distractor.
- Levels 41–50: one or two distractors where appropriate.

Distractors must be fair.

They may be:

- A similar window shape.
- A wheel with the wrong size.
- A mirrored decorative block.
- A roof block with the wrong slope.

They must not be almost indistinguishable from required pieces.

---

## 6. The Rule of One

Every level should introduce or significantly increase only one main difficulty.

Examples:

- Increase the number of pieces, but keep rotations easy.
- Add mirrored pieces, but use a simple silhouette.
- Introduce repeated windows, but do not add distractors.
- Introduce one distractor, but keep the piece count stable.
- Introduce radial construction, but provide stronger hints.

Do not increase all of the following at the same time:

- Piece count.
- Rotation difficulty.
- Similar-piece difficulty.
- Distractors.
- Board size.
- Silhouette complexity.
- Reduced hints.

For each new challenge:

1. Introduce it gently.
2. Let the player practise it.
3. Combine it with an older challenge.
4. Provide a relief level.
5. Finish the chapter with a memorable milestone object.

Use a staircase-shaped difficulty curve rather than a continuous steep increase.

Within each group of 10 levels:

- Levels 1–3: introduction and confidence.
- Levels 4–7: normal challenge.
- Level 8: stronger challenge.
- Level 9: relief or visually satisfying level.
- Level 10: chapter finale.

---

## 7. Art Direction

Use the attached visual references as the exact art-style guide.

The objects should look like colorful preschool construction toys.

Visual characteristics:

- Chunky modular shapes.
- Rounded edges.
- Soft bevels.
- Slightly inflated plastic appearance.
- Soft studio lighting.
- Subtle ambient occlusion.
- Gentle contact shadows.
- Smooth matte or lightly textured toy-plastic material.
- Bright red, blue, yellow and green.
- Occasional orange or purple only when additional color variety is needed.
- Clear seams between separate puzzle pieces.
- Strong readable silhouettes.
- No realistic mechanical details.
- No thin fragile geometry.
- No sharp dangerous-looking edges.
- No complicated surface patterns.
- No text on the object.
- No characters.
- No environment in standalone object renders.

Standalone object-reference art:

- Square 1024×1024.
- Dark charcoal or black background.
- Object centered.
- Front view, direct side view or near-orthographic view.
- Minimal perspective distortion.
- Complete object fully visible.
- Consistent camera and lighting across all levels.

Gameplay presentation:

- Portrait mobile screen.
- Blue game background.
- Dark charcoal rounded puzzle board.
- Visible grid cells.
- Bright colored toy pieces.
- Soft shadows under pieces.
- Top reference card.
- Bottom piece tray.
- Large finger-friendly controls.

---

## 8. Piece-Construction Rules

Every visible colored section should be a deliberate removable puzzle piece.

Each piece must:

- Have a clear gameplay purpose.
- Be visually identifiable.
- Have visible seams.
- Be large enough for mobile interaction.
- Snap clearly into the completed object.
- Have a readable orientation.
- Use a simple collider.
- Fit on the level grid.
- Remain visible when placed.
- Avoid unnecessary overlap with other pieces.

Do not create extremely small decorative pieces.

Small visual details should be:

- Embossed.
- Engraved.
- Painted into the material.
- Included within a larger piece.

Recommended minimum touch size:

- Approximately 80–100 screen pixels.
- Use enlarged invisible touch areas for narrow pieces.

Avoid pieces thinner than half a standard grid cell.

Use only these rotation values:

- 0 degrees.
- 90 degrees.
- 180 degrees.
- 270 degrees.

Where possible, reuse modular shape families:

- Rounded rectangle.
- Rounded square.
- Circle.
- Ring.
- Triangle.
- Trapezoid.
- Arch.
- L-shape.
- Curved hook.
- Long bar.
- Short bar.
- Roof slope.
- Fin.
- Wheel.
- Window insert.
- Tower block.
- Connector.

Do not rely on layering order alone to make the puzzle difficult.

---

## 9. Board and Grid Rules

Recommended board progression:

- Levels 1–10: 5×5 or 6×6.
- Levels 11–20: 6×6.
- Levels 21–30: 7×7.
- Levels 31–40: 7×8 or 8×8.
- Levels 41–50: 8×8 or 9×9.

The object does not need to occupy every cell.

Keep the completed object centered.

Maintain approximately one empty grid-cell margin around the object where possible.

Use three main piece-scale presets:

- Large.
- Medium-large.
- Medium.

Do not calculate a completely different touch scale for every level.

---

## 10. Placement Assistance

### Levels 1–3

- Full ghost image visible.
- Correct cells highlighted.
- Large snap tolerance.
- Tutorial hand available.

### Levels 4–10

- Full ghost becomes lighter.
- Only important regions are highlighted.
- Large-to-medium snap tolerance.

### Levels 11–20

- Use an object outline instead of a full ghost.
- Hint highlights a region.
- Medium snap tolerance.

### Levels 21–30

- No permanent ghost image.
- Outline appears when holding a relevant piece.
- Hints appear after a delay.

### Levels 31–40

- No automatic placement guidance.
- Hint reveals a piece and region.
- Smaller snap tolerance.

### Levels 41–50

- Limited free hints.
- Hint reveals one piece, rotation or region at a time.
- Do not make success dependent on random trial and error.

Passive hint timing:

- After 15 seconds without meaningful progress: pulse a usable piece.
- After 30 seconds: pulse its target region.
- After repeated invalid placements: temporarily increase snap tolerance.

Active hint stages:

1. Highlight a correct piece.
2. Highlight its target region.
3. Show the required rotation.
4. Automatically place the piece.

---

## 11. Scoring

Do not use a stressful visible countdown timer.

Use a three-star system based mainly on moves and hints.

Three stars:

- Complete within the recommended move count.
- Use no automatic-placement hint.

Two stars:

- Complete with several unnecessary moves.
- Or use one strong hint.

One star:

- Complete the puzzle.

Do not block main progression based on stars during the first 50 levels.

Stars may unlock:

- Cosmetic board styles.
- Piece trails.
- Snap effects.
- Decorative collection-world items.
- Bonus puzzles.

Count only meaningful moves:

- A completed drag attempt.
- A 90-degree rotation.

Do not count:

- Selecting a piece.
- Moving a piece inside the tray.
- Accidental tiny movement.

---

## 12. Completion Rewards

Every object should have a short custom completion animation.

Examples:

- Rocket lifts slightly and emits a flame.
- Windmill blades rotate.
- Castle flag waves.
- Train wheels turn.
- Camera flashes.
- Treasure chest opens.
- Lighthouse beam sweeps.
- Robot waves.
- Ferris wheel spins.
- Helicopter rotor turns.
- Wizard hat releases stars.

Give a larger collection reward every five levels.

Recommended milestones:

- Level 5: first shelf decoration.
- Level 10: first collection-area reveal.
- Level 15: board cosmetic.
- Level 20: Toy Town expansion.
- Level 25: piece trail.
- Level 30: Adventure Harbor expansion.
- Level 35: snap-effect cosmetic.
- Level 40: Wonder Workshop expansion.
- Level 45: golden decoration.
- Level 50: Grand Toy Kingdom reveal.

---

# 13. Level Design Catalogue

Create the following 50 levels exactly in this progression.

For each level, preserve the specified object identity, visual direction and required gameplay pieces.

---

# Chapter 1 — Toy Box Basics

## Level 01 — Small House

**Category:** Building  
**Target time:** 30–40 seconds  
**Grid:** 5×5  
**Required pieces:** 4

**Visual description:**  
A tiny cheerful toy house viewed from the front. Use a warm red or blue square body, a large yellow triangular roof, one rounded door and one square window. Keep the silhouette extremely simple and symmetrical.

**Gameplay pieces:**

1. House body.
2. Roof.
3. Door.
4. Window.

**Main difficulty:** Drag and snap tutorial.

**Starting setup:**

- Every piece correctly rotated.
- Full ghost image visible.
- Very large snap tolerance.
- No distractors.

**Completion animation:**  
Door opens slightly and a soft light appears in the window.

---

## Level 02 — Sailboat

**Category:** Vehicle  
**Target time:** 30–40 seconds  
**Grid:** 5×5  
**Required pieces:** 4

**Visual description:**  
A simple side-view toy sailboat with a red rounded hull, green mast, large yellow sail and smaller blue sail.

**Gameplay pieces:**

1. Hull.
2. Mast.
3. Large sail.
4. Small sail.

**Main difficulty:** Horizontal and vertical placement.

**Starting setup:**

- One sail begins rotated.
- Full ghost image visible.
- Large snap tolerance.

**Completion animation:**  
The boat rocks gently and both sails move slightly.

---

## Level 03 — Flower Pot

**Category:** Nature and decoration  
**Target time:** 35–45 seconds  
**Grid:** 5×5  
**Required pieces:** 5

**Visual description:**  
A colorful front-view flower in a rounded red pot. Use a green stem, two green leaves and a yellow or blue flower head.

**Gameplay pieces:**

1. Pot.
2. Stem.
3. Flower head.
4. Left leaf.
5. Right leaf.

**Main difficulty:** First mirrored-looking pieces.

**Starting setup:**

- One leaf begins rotated.
- Strong region guidance.
- No distractors.

**Completion animation:**  
The flower grows upward and performs a small bounce.

---

## Level 04 — Toy Truck

**Category:** Vehicle  
**Target time:** 40–50 seconds  
**Grid:** 6×5  
**Required pieces:** 6

**Visual description:**  
Match the attached truck reference. Use a long red base, green cabin, yellow cargo block, blue cargo block and two large red wheels.

**Gameplay pieces:**

1. Truck base.
2. Cabin.
3. Yellow cargo block.
4. Blue cargo block.
5. Front wheel.
6. Rear wheel.

**Main difficulty:** Two identical circular pieces.

**Starting setup:**

- One cargo block begins rotated.
- Full ghost becomes slightly transparent.

**Completion animation:**  
The wheels rotate and the truck makes a small forward movement.

---

## Level 05 — Crown

**Category:** Fantasy  
**Target time:** 35–45 seconds  
**Grid:** 5×5  
**Required pieces:** 5

**Visual description:**  
A chunky front-view royal crown with a red base band, yellow central peak, blue and green side peaks and one large circular jewel.

**Gameplay pieces:**

1. Base band.
2. Center peak.
3. Left peak.
4. Right peak.
5. Jewel.

**Main difficulty:** Simple symmetry.

**Completion animation:**  
The jewel shines and the crown lifts slightly.

---

## Level 06 — Rocket

**Category:** Vehicle and space  
**Target time:** 40–55 seconds  
**Grid:** 5×6  
**Required pieces:** 6

**Visual description:**  
Match the attached rocket style. Use a blue central body, red nose cone, yellow circular window, two green fins and a red engine-flame block.

**Gameplay pieces:**

1. Rocket body.
2. Nose cone.
3. Window.
4. Left fin.
5. Right fin.
6. Flame or engine block.

**Main difficulty:** Mirrored left and right pieces.

**Completion animation:**  
The rocket shakes, flame appears and it lifts slightly.

---

## Level 07 — Tree

**Category:** Nature  
**Target time:** 40–55 seconds  
**Grid:** 5×6  
**Required pieces:** 6

**Visual description:**  
A friendly rounded toy tree with a red or brown trunk, three green canopy sections, one grass-base piece and one colorful fruit cluster.

**Gameplay pieces:**

1. Trunk.
2. Center canopy.
3. Left canopy.
4. Right canopy.
5. Grass base.
6. Fruit cluster.

**Main difficulty:** Vertical stacking.

**Completion animation:**  
The canopy sways and fruit sparkles.

---

## Level 08 — Airplane

**Category:** Vehicle  
**Target time:** 45–60 seconds  
**Grid:** 6×5  
**Required pieces:** 6

**Visual description:**  
Match the attached toy-airplane style. Use a blue fuselage, red nose, yellow front wing, green tail and a dark blue window insert.

**Gameplay pieces:**

1. Main fuselage.
2. Nose.
3. Main wing.
4. Rear wing.
5. Tail fin.
6. Window.

**Main difficulty:** Wing orientation.

**Completion animation:**  
The airplane tilts gently and its propeller or nose glows.

---

## Level 09 — Gift Box

**Category:** Object  
**Target time:** 45–60 seconds  
**Grid:** 5×5  
**Required pieces:** 6

**Visual description:**  
A square colorful gift box with a separate lid, vertical ribbon, horizontal ribbon and two large bow loops.

**Gameplay pieces:**

1. Box body.
2. Lid.
3. Vertical ribbon.
4. Horizontal ribbon.
5. Left bow loop.
6. Right bow loop.

**Main difficulty:** Crossing visual elements without ambiguous overlap.

**Completion animation:**  
The lid jumps slightly and confetti appears.

---

## Level 10 — Castle Tower

**Category:** Fantasy building  
**Target time:** 50–65 seconds  
**Grid:** 6×6  
**Required pieces:** 7

**Visual description:**  
A single colorful castle tower viewed from the front. Use a blue tower body, yellow roof, red door, window, green flagpole and red flag.

**Gameplay pieces:**

1. Tower base.
2. Tower body.
3. Roof.
4. Door.
5. Window.
6. Flagpole.
7. Flag.

**Main difficulty:** Reduced placement guidance.

**Completion animation:**  
The flag waves and the window lights up.

---

# Chapter 2 — Little Toy World

## Level 11 — City Bus

**Category:** Vehicle  
**Target time:** 50–65 seconds  
**Grid:** 6×6  
**Required pieces:** 7

**Visual description:**  
A side-view toy bus with a yellow main body, green roof, blue window strip, red lower chassis and two red wheels.

**Gameplay pieces:**

1. Lower chassis.
2. Main body.
3. Front cabin section.
4. Roof.
5. Window strip.
6. Front wheel.
7. Rear wheel.

**Main difficulty:** Repeated window shapes inside one larger piece.

**Completion animation:**  
The wheels rotate and the bus doors pulse.

---

## Level 12 — Umbrella

**Category:** Everyday object  
**Target time:** 50–65 seconds  
**Grid:** 6×6  
**Required pieces:** 7

**Visual description:**  
A front-view rounded umbrella with a three-part colorful canopy, green shaft, blue curved handle, top tip and closing clasp.

**Gameplay pieces:**

1. Center canopy.
2. Left canopy.
3. Right canopy.
4. Shaft.
5. Curved handle.
6. Top tip.
7. Closing clasp.

**Main difficulty:** Curved handle orientation.

**Completion animation:**  
The canopy opens slightly and water drops bounce away.

---

## Level 13 — Windmill

**Category:** Structure  
**Target time:** 55–70 seconds  
**Grid:** 6×6  
**Required pieces:** 8

**Visual description:**  
A compact toy windmill with a red tower base, green roof, yellow central hub, four blue or yellow blades and one door.

**Gameplay pieces:**

1. Tower.
2. Roof.
3. Hub.
4. Top blade.
5. Right blade.
6. Bottom blade.
7. Left blade.
8. Door.

**Main difficulty:** Four identical pieces requiring different rotations.

**Completion animation:**  
The blades rotate once.

---

## Level 14 — Toy Train

**Category:** Vehicle  
**Target time:** 55–75 seconds  
**Grid:** 7×6  
**Required pieces:** 8

**Visual description:**  
Match the attached toy-train style. Use a red base, green cabin, blue engine body, yellow chimney and three differently sized red wheels.

**Gameplay pieces:**

1. Train base.
2. Cabin.
3. Engine body.
4. Front engine cap.
5. Chimney.
6. Large wheel.
7. Medium wheel.
8. Small wheel.

**Main difficulty:** Different wheel sizes.

**Completion animation:**  
The wheels turn and a soft smoke puff appears.

---

## Level 15 — Ice-Cream Cone

**Category:** Food  
**Target time:** 50–65 seconds  
**Grid:** 6×6  
**Required pieces:** 7

**Visual description:**  
A large rounded cone with three colorful toy scoops, a topping piece, wafer stick and cherry.

**Gameplay pieces:**

1. Cone.
2. Bottom scoop.
3. Middle scoop.
4. Top scoop.
5. Topping.
6. Wafer stick.
7. Cherry.

**Main difficulty:** Layering vertically in the correct order.

**Completion animation:**  
The cherry bounces and sparkles appear.

---

## Level 16 — Lighthouse

**Category:** Building  
**Target time:** 60–75 seconds  
**Grid:** 6×7  
**Required pieces:** 8

**Visual description:**  
A tall colorful lighthouse with a red base, blue lower tower, yellow upper tower, green light room, red roof, door, window and large light beam.

**Gameplay pieces:**

1. Base.
2. Lower tower.
3. Upper tower.
4. Light room.
5. Roof.
6. Door.
7. Window.
8. Light beam.

**Main difficulty:** Tall narrow construction.

**Completion animation:**  
The light beam sweeps across the screen.

---

## Level 17 — Camera

**Category:** Everyday object  
**Target time:** 60–75 seconds  
**Grid:** 6×6  
**Required pieces:** 8

**Visual description:**  
A chunky front-view camera with blue body, green grip, yellow lens ring, red inner lens, top housing, flash and shutter button.

**Gameplay pieces:**

1. Camera body.
2. Top housing.
3. Outer lens ring.
4. Inner lens.
5. Viewfinder.
6. Shutter button.
7. Grip.
8. Flash.

**Main difficulty:** Circular pieces placed inside larger shapes.

**Completion animation:**  
The flash triggers and the object briefly scales up.

---

## Level 18 — Small Castle

**Category:** Fantasy building  
**Target time:** 65–80 seconds  
**Grid:** 7×7  
**Required pieces:** 9

**Visual description:**  
A small symmetrical castle with a center keep, two side towers, three roofs, a large gate and two flags.

**Gameplay pieces:**

1. Center keep.
2. Left tower.
3. Right tower.
4. Center roof.
5. Left roof.
6. Right roof.
7. Gate.
8. Left flag.
9. Right flag.

**Main difficulty:** Similar tower and roof pieces.

**Completion animation:**  
The gate opens and both flags wave.

---

## Level 19 — Kick Scooter

**Category:** Vehicle  
**Target time:** 60–80 seconds  
**Grid:** 7×6  
**Required pieces:** 8

**Visual description:**  
Match the attached kick-scooter style. Use a blue deck, green vertical stem, yellow handlebar and red wheels.

**Gameplay pieces:**

1. Deck.
2. Rear wheel.
3. Front wheel.
4. Vertical stem.
5. Handlebar.
6. Front fork.
7. Rear mudguard.
8. Stem connector.

**Main difficulty:** Long narrow silhouette.

**Completion animation:**  
The scooter rolls forward slightly.

---

## Level 20 — Toy Robot

**Category:** Toy and fantasy  
**Target time:** 65–85 seconds  
**Grid:** 7×7  
**Required pieces:** 9

**Visual description:**  
A friendly front-view robot with square blue torso, rounded red head, green arms, yellow legs and a small antenna.

**Gameplay pieces:**

1. Torso.
2. Head.
3. Left arm.
4. Right arm.
5. Left leg.
6. Right leg.
7. Left foot.
8. Right foot.
9. Antenna.

**Main difficulty:** Mirrored limbs.

**Completion animation:**  
The robot waves and its eyes illuminate.

---

# Chapter 3 — Adventure Shelf

## Level 21 — Hot-Air Balloon

**Category:** Vehicle  
**Target time:** 65–85 seconds  
**Grid:** 7×7  
**Required pieces:** 9

**Visual description:**  
A large rounded balloon made from colorful curved panels, connected to a small basket by thick toy ropes.

**Gameplay pieces:**

1. Balloon top.
2. Center balloon panel.
3. Left balloon panel.
4. Right balloon panel.
5. Lower balloon panel.
6. Basket.
7. Left rope.
8. Right rope.
9. Burner.

**Main difficulty:** Symmetrical curved panels.

**Completion animation:**  
The balloon floats upward gently.

---

## Level 22 — Teapot

**Category:** Everyday object  
**Target time:** 65–85 seconds  
**Grid:** 7×7  
**Required pieces:** 8

**Visual description:**  
A rounded toy teapot with blue body, yellow lid, green curved handle, red spout and decorative center panel.

**Gameplay pieces:**

1. Teapot body.
2. Lid.
3. Lid knob.
4. Spout base.
5. Spout tip.
6. Handle upper section.
7. Handle lower section.
8. Decorative center panel.

**Main difficulty:** Spout and handle orientation.

**Completion animation:**  
The lid lifts and steam curls upward.

---

## Level 23 — Pirate Ship

**Category:** Fantasy vehicle  
**Target time:** 70–95 seconds  
**Grid:** 7×7  
**Required pieces:** 10

**Visual description:**  
A colorful toy pirate ship with a red curved hull, yellow deck, green mast, two blue sails, raised stern, bow, flag and anchor.

**Gameplay pieces:**

1. Hull.
2. Deck.
3. Mast.
4. Large sail.
5. Small sail.
6. Bow section.
7. Stern section.
8. Pirate flag.
9. Cabin.
10. Anchor.

**Main difficulty:** Multiple connected horizontal and vertical elements.

**Completion animation:**  
The flag waves and the ship rocks.

---

## Level 24 — Mushroom House

**Category:** Fantasy building  
**Target time:** 70–90 seconds  
**Grid:** 7×7  
**Required pieces:** 9

**Visual description:**  
A rounded mushroom-shaped toy house with a three-part red or yellow cap, blue stem body, green door, small window, chimney, step and grass base.

**Gameplay pieces:**

1. House body.
2. Cap center.
3. Cap left.
4. Cap right.
5. Door.
6. Window.
7. Chimney.
8. Step.
9. Grass base.

**Main difficulty:** Irregular roof silhouette.

**Completion animation:**  
The chimney releases a puff and the window glows.

---

## Level 25 — Bicycle

**Category:** Vehicle  
**Target time:** 75–100 seconds  
**Grid:** 7×7  
**Required pieces:** 10

**Visual description:**  
Match the attached bicycle style. Use two large red ring wheels, yellow frame pieces, green connectors, blue seat and blue handlebar.

**Gameplay pieces:**

1. Front wheel.
2. Rear wheel.
3. Main frame.
4. Top frame bar.
5. Front fork.
6. Handlebar.
7. Seat.
8. Pedal.
9. Rear connector.
10. Front connector.

**Main difficulty:** Diagonal frame pieces.

**Completion animation:**  
The wheels and pedal rotate.

---

## Level 26 — Treasure Chest

**Category:** Fantasy object  
**Target time:** 70–90 seconds  
**Grid:** 7×7  
**Required pieces:** 9

**Visual description:**  
A rounded toy treasure chest with red base, yellow curved lid, green bands, large blue lock and side handles.

**Gameplay pieces:**

1. Chest base.
2. Curved lid.
3. Center lock.
4. Left band.
5. Right band.
6. Center band.
7. Left handle.
8. Right handle.
9. Gold or jewel insert.

**Main difficulty:** Similar decorative band pieces.

**Completion animation:**  
The lid opens and colorful stars rise.

---

## Level 27 — Fire Truck

**Category:** Vehicle  
**Target time:** 75–100 seconds  
**Grid:** 8×7  
**Required pieces:** 10

**Visual description:**  
A bright red fire truck with blue cabin, green rear body, yellow ladder, roof siren, two wheels, window, bumper and hose reel.

**Gameplay pieces:**

1. Chassis.
2. Cabin.
3. Rear body.
4. Ladder.
5. Siren.
6. Front wheel.
7. Rear wheel.
8. Window.
9. Front bumper.
10. Hose reel.

**Main difficulty:** One long ladder piece.

**Completion animation:**  
The siren flashes and ladder lifts slightly.

---

## Level 28 — Stone Bridge

**Category:** Structure  
**Target time:** 75–100 seconds  
**Grid:** 8×7  
**Required pieces:** 10

**Visual description:**  
A colorful toy stone bridge with three rounded arches, two large pillars, road deck, railings and base pieces.

**Gameplay pieces:**

1. Left pillar.
2. Right pillar.
3. Center arch.
4. Left arch.
5. Right arch.
6. Road deck.
7. Left railing.
8. Right railing.
9. Left base.
10. Right base.

**Main difficulty:** Repeated architectural supports.

**Completion animation:**  
A small toy light travels across the bridge.

---

## Level 29 — Cupcake

**Category:** Food  
**Target time:** 70–95 seconds  
**Grid:** 7×7  
**Required pieces:** 9

**Visual description:**  
A large colorful cupcake with a three-part wrapper, cake layer, three curved frosting sections, cherry and decorative topper.

**Gameplay pieces:**

1. Wrapper left.
2. Wrapper center.
3. Wrapper right.
4. Cake base.
5. Frosting left.
6. Frosting center.
7. Frosting right.
8. Cherry.
9. Decorative topper.

**Main difficulty:** Curved shapes with similar silhouettes.

**Completion animation:**  
The frosting bounces and the cherry sparkles.

---

## Level 30 — Submarine

**Category:** Vehicle  
**Target time:** 80–105 seconds  
**Grid:** 8×7  
**Required pieces:** 10

**Visual description:**  
A side-view toy submarine with blue main body, red nose, green tail, yellow tower, periscope, three circular windows, propeller and lower fin.

**Gameplay pieces:**

1. Main body.
2. Nose.
3. Tail.
4. Conning tower.
5. Periscope.
6. Front window.
7. Center window.
8. Rear window.
9. Propeller.
10. Lower fin.

**Main difficulty:** Repeated circular windows.

**Completion animation:**  
The propeller turns and bubbles appear.

---

# Chapter 4 — Wonder Workshop

## Level 31 — Excavator

**Category:** Vehicle  
**Target time:** 85–110 seconds  
**Grid:** 8×8  
**Required pieces:** 11

**Visual description:**  
A side-view construction excavator with red track, blue cabin, yellow two-part arm, green bucket and chunky engine section.

**Gameplay pieces:**

1. Chassis.
2. Track.
3. Cabin.
4. Window.
5. Upper boom.
6. Lower boom.
7. Bucket.
8. Rear counterweight.
9. Front track hub.
10. Rear track hub.
11. Exhaust.

**Main difficulty:** Multi-part angled arm.

**Completion animation:**  
The arm lifts and bucket scoops once.

---

## Level 32 — Treehouse

**Category:** Nature building  
**Target time:** 85–110 seconds  
**Grid:** 8×8  
**Required pieces:** 11

**Visual description:**  
A colorful toy treehouse with a thick trunk, two branches, small house body, roof, door, window, ladder, platform and two canopy sections.

**Gameplay pieces:**

1. Tree trunk.
2. Left branch.
3. Right branch.
4. House body.
5. Roof.
6. Door.
7. Window.
8. Ladder.
9. Platform.
10. Left canopy.
11. Right canopy.

**Main difficulty:** Irregular supporting structure.

**Completion animation:**  
The ladder swings gently and leaves move.

---

## Level 33 — Grand Piano

**Category:** Everyday object  
**Target time:** 80–105 seconds  
**Grid:** 8×8  
**Required pieces:** 10

**Visual description:**  
A side-view rounded toy grand piano with blue body, yellow lid, red keyboard housing, green legs, pedal base, music stand and small matching bench.

**Gameplay pieces:**

1. Piano body.
2. Lid.
3. Keyboard.
4. Front leg.
5. Rear leg.
6. Pedal base.
7. Music stand.
8. Left key block.
9. Right key block.
10. Bench.

**Main difficulty:** Asymmetrical silhouette.

**Relief level:**  
This is a relief level after the excavator and treehouse.

**Completion animation:**  
Several keys move and musical notes appear.

---

## Level 34 — Ferris Wheel

**Category:** Funfair structure  
**Target time:** 90–120 seconds  
**Grid:** 8×8  
**Required pieces:** 12

**Visual description:**  
A colorful toy Ferris wheel with a four-section circular ring, center hub, two supports, two base pieces and three large gondolas.

**Gameplay pieces:**

1. Left base.
2. Right base.
3. Left support.
4. Right support.
5. Center hub.
6. Ring top.
7. Ring right.
8. Ring bottom.
9. Ring left.
10. Gondola one.
11. Gondola two.
12. Gondola three.

**Main difficulty:** Radial placement.

**Completion animation:**  
The entire wheel rotates slowly.

---

## Level 35 — Helicopter

**Category:** Vehicle  
**Target time:** 90–115 seconds  
**Grid:** 8×8  
**Required pieces:** 11

**Visual description:**  
A side-view toy helicopter with blue cabin body, red nose, dark window, green tail, main rotor, tail rotor and two landing skids.

**Gameplay pieces:**

1. Cabin body.
2. Nose.
3. Windshield.
4. Tail boom.
5. Tail fin.
6. Main rotor.
7. Rotor mast.
8. Tail rotor.
9. Left skid.
10. Right skid.
11. Side window.

**Main difficulty:** Two rotor systems.

**Completion animation:**  
Both rotors spin and the helicopter rises slightly.

---

## Level 36 — Castle Gate

**Category:** Fantasy building  
**Target time:** 95–125 seconds  
**Grid:** 8×8  
**Required pieces:** 12

**Visual description:**  
A wide castle entrance with two rounded towers, center gatehouse, two roofs, side walls, gate, drawbridge, crest and two flags.

**Gameplay pieces:**

1. Center gatehouse.
2. Left tower.
3. Right tower.
4. Gate.
5. Left roof.
6. Right roof.
7. Left wall.
8. Right wall.
9. Drawbridge.
10. Left flag.
11. Right flag.
12. Crest.

**Main difficulty:** Mirrored towers with central alignment.

**Completion animation:**  
The drawbridge lowers and flags wave.

---

## Level 37 — Dragon Head

**Category:** Fantasy  
**Target time:** 95–125 seconds  
**Grid:** 8×8  
**Required pieces:** 11

**Visual description:**  
A friendly toy dragon head viewed from the side or front. Use a blue head, red snout, yellow jaw, green horns, rounded ears and colorful crest pieces. Keep it cute rather than aggressive.

**Gameplay pieces:**

1. Main head.
2. Snout.
3. Lower jaw.
4. Left horn.
5. Right horn.
6. Left ear.
7. Right ear.
8. Left eye plate.
9. Right eye plate.
10. Neck.
11. Head crest.

**Main difficulty:** Curved mirrored facial pieces.

**Completion animation:**  
The dragon blinks and releases a harmless spark puff.

---

## Level 38 — School Bus

**Category:** Vehicle  
**Target time:** 95–125 seconds  
**Grid:** 8×8  
**Required pieces:** 12

**Visual description:**  
A long side-view yellow school bus with red lower chassis, green roof, blue front window, three blue passenger windows, two wheels, door and bumper.

**Gameplay pieces:**

1. Lower chassis.
2. Main bus body.
3. Front cabin.
4. Roof.
5. Front wheel.
6. Rear wheel.
7. Front window.
8. Passenger window one.
9. Passenger window two.
10. Passenger window three.
11. Door.
12. Bumper.

**Main difficulty:** Several similar windows.

**Optional distractor:**  
Include one fair distractor window with a clearly different width.

**Completion animation:**  
The door opens and roof lights flash.

---

## Level 39 — Carousel

**Category:** Funfair  
**Target time:** 100–135 seconds  
**Grid:** 8×8  
**Required pieces:** 13

**Visual description:**  
A front-view toy carousel with circular base, platform, center pole, three-part roof, crown top, three support poles and three colorful toy horses.

**Gameplay pieces:**

1. Base.
2. Platform.
3. Center pole.
4. Roof center.
5. Roof left.
6. Roof right.
7. Crown top.
8. Support pole one.
9. Support pole two.
10. Support pole three.
11. Horse one.
12. Horse two.
13. Horse three.

**Main difficulty:** Repeated vertical supports and radial arrangement.

**Completion animation:**  
The carousel rotates and horses move gently.

---

## Level 40 — Space Shuttle

**Category:** Space vehicle  
**Target time:** 100–135 seconds  
**Grid:** 8×8  
**Required pieces:** 12

**Visual description:**  
A front-view toy space shuttle with blue main body, red nose, dark cockpit, yellow wings, green tail fin, engine block, flame, two side boosters and central tank.

**Gameplay pieces:**

1. Shuttle body.
2. Nose.
3. Cockpit.
4. Left wing.
5. Right wing.
6. Tail fin.
7. Engine block.
8. Flame.
9. Left booster.
10. Right booster.
11. External tank.
12. Belly panel.

**Main difficulty:** Large vertical symmetry with one optional distractor fin.

**Completion animation:**  
The engines ignite and the shuttle rises.

---

# Chapter 5 — Master Builder

## Level 41 — Medieval Castle

**Category:** Fantasy building  
**Target time:** 110–145 seconds  
**Grid:** 9×8  
**Required pieces:** 14

**Visual description:**  
A wide toy fortress with a large center keep, four towers, central gate, drawbridge, five roof sections and two flags.

**Gameplay pieces:**

1. Center keep.
2. Left outer tower.
3. Right outer tower.
4. Left inner tower.
5. Right inner tower.
6. Main gate.
7. Drawbridge.
8. Center roof.
9. Left outer roof.
10. Right outer roof.
11. Left inner roof.
12. Right inner roof.
13. Left flag.
14. Right flag.

**Main difficulty:** Several similar towers and roofs.

**Completion animation:**  
The drawbridge lowers, flags wave and windows illuminate.

---

## Level 42 — Bulldozer

**Category:** Vehicle  
**Target time:** 100–135 seconds  
**Grid:** 8×8  
**Required pieces:** 12

**Visual description:**  
A side-view toy bulldozer with red track, yellow front blade, blue cabin, green engine hood, two blade arms, exhaust and wheel hubs.

**Gameplay pieces:**

1. Chassis.
2. Track.
3. Front blade.
4. Upper blade arm.
5. Lower blade arm.
6. Cabin.
7. Window.
8. Engine hood.
9. Exhaust.
10. Rear block.
11. Front track hub.
12. Rear track hub.

**Main difficulty:** Angled blade assembly.

**Completion animation:**  
The blade lifts and pushes a few toy blocks.

---

## Level 43 — Unicorn Head

**Category:** Fantasy  
**Target time:** 105–140 seconds  
**Grid:** 8×8  
**Required pieces:** 12

**Visual description:**  
A cute side-view unicorn head with blue face, red muzzle, yellow horn, green ears and a four-part colorful mane.

**Gameplay pieces:**

1. Head.
2. Muzzle.
3. Horn.
4. Left ear.
5. Right ear.
6. Neck.
7. Mane top.
8. Mane upper.
9. Mane middle.
10. Mane lower.
11. Eye plate.
12. Cheek piece.

**Main difficulty:** Correct ordering of curved mane sections.

**Completion animation:**  
The unicorn blinks and the horn emits stars.

---

## Level 44 — Harbor Boat

**Category:** Vehicle  
**Target time:** 110–145 seconds  
**Grid:** 9×8  
**Required pieces:** 13

**Visual description:**  
A detailed but chunky side-view harbor boat with red hull, yellow deck, blue cabin, green roof, windows, bow and stern sections, rail, mast, flag, chimney and lifebuoy.

**Gameplay pieces:**

1. Hull.
2. Deck.
3. Cabin.
4. Roof.
5. Windshield.
6. Side window.
7. Bow block.
8. Stern block.
9. Rail.
10. Mast.
11. Flag.
12. Chimney.
13. Lifebuoy.

**Main difficulty:** Layered horizontal sections.

**Completion animation:**  
The boat rocks, chimney puffs and flag waves.

---

## Level 45 — Wizard Hat

**Category:** Fantasy object  
**Target time:** 95–130 seconds  
**Grid:** 8×8  
**Required pieces:** 11

**Visual description:**  
A large curved wizard hat with three-part brim, three-part cone, three-part band, large buckle and star decoration.

**Gameplay pieces:**

1. Brim center.
2. Brim left.
3. Brim right.
4. Cone lower.
5. Cone middle.
6. Cone tip.
7. Band center.
8. Band left.
9. Band right.
10. Buckle.
11. Star.

**Main difficulty:** Curved asymmetric silhouette.

**Relief level:**  
This is a relief level before the final five levels.

**Completion animation:**  
The hat jumps and releases magical stars.

---

## Level 46 — Toy Factory

**Category:** Building  
**Target time:** 115–155 seconds  
**Grid:** 9×9  
**Required pieces:** 14

**Visual description:**  
A colorful factory made from a center building, two side wings, three roof sections, two chimneys, door, two windows, large gear sign and loading dock.

**Gameplay pieces:**

1. Factory base.
2. Center building.
3. Left wing.
4. Right wing.
5. Main roof.
6. Left roof.
7. Right roof.
8. Chimney one.
9. Chimney two.
10. Door.
11. Window one.
12. Window two.
13. Gear sign.
14. Loading dock.

**Main difficulty:** Many architectural sections with similar proportions.

**Completion animation:**  
The gear rotates and both chimneys release soft puffs.

---

## Level 47 — Monster Truck

**Category:** Vehicle  
**Target time:** 110–150 seconds  
**Grid:** 9×8  
**Required pieces:** 13

**Visual description:**  
A tall toy monster truck with huge red ring wheels, blue body, green cabin, yellow hood, roof light, bumper and visible suspension.

**Gameplay pieces:**

1. Chassis.
2. Main body.
3. Cabin.
4. Window.
5. Hood.
6. Roof light.
7. Front bumper.
8. Rear block.
9. Front wheel outer ring.
10. Front wheel hub.
11. Rear wheel outer ring.
12. Rear wheel hub.
13. Suspension.

**Main difficulty:** Nested wheel pieces and raised body.

**Completion animation:**  
The truck bounces on its suspension.

---

## Level 48 — Amusement Park Entrance

**Category:** Funfair structure  
**Target time:** 120–160 seconds  
**Grid:** 9×9  
**Required pieces:** 14

**Visual description:**  
A cheerful symmetrical amusement-park entrance with two towers, center arch, large sign, two roofs, two flags, two gates, star crest, two ticket booths and base.

**Gameplay pieces:**

1. Left pillar.
2. Right pillar.
3. Center arch.
4. Entrance sign.
5. Left roof.
6. Right roof.
7. Left flag.
8. Right flag.
9. Left gate.
10. Right gate.
11. Star crest.
12. Left ticket booth.
13. Right ticket booth.
14. Base.

**Main difficulty:** Large mirrored structure with multiple similar pairs.

**Completion animation:**  
The gates open and small lights illuminate.

---

## Level 49 — Clock Tower

**Category:** Building  
**Target time:** 125–170 seconds  
**Grid:** 9×9  
**Required pieces:** 15

**Visual description:**  
A tall colorful clock tower with stacked base, lower, middle and upper sections, roof, clock ring, clock face, door, four windows, bell room, flagpole and flag.

**Gameplay pieces:**

1. Base.
2. Lower tower.
3. Middle tower.
4. Upper tower.
5. Roof.
6. Clock outer ring.
7. Clock face.
8. Door.
9. Lower-left window.
10. Lower-right window.
11. Upper-left window.
12. Upper-right window.
13. Bell room.
14. Flagpole.
15. Flag.

**Main difficulty:** Tall multi-section architecture and repeated windows.

**Completion animation:**  
The clock hands move as an embossed effect, the bell swings and the flag waves.

---

## Level 50 — Grand Toy Kingdom

**Category:** Final fantasy building  
**Target time:** 140–180 seconds  
**Grid:** 9×9  
**Required pieces:** 16

**Visual description:**  
The largest and most impressive castle in the game. It should look like a colorful toy palace rather than a realistic fortress. Use a large center keep, four towers, central gate, drawbridge, five roofs, two flags, crest and bridge base.

**Gameplay pieces:**

1. Center palace keep.
2. Left tall tower.
3. Right tall tower.
4. Left small tower.
5. Right small tower.
6. Central gate.
7. Drawbridge.
8. Center roof.
9. Left tall-tower roof.
10. Right tall-tower roof.
11. Left small-tower roof.
12. Right small-tower roof.
13. Left flag.
14. Right flag.
15. Royal crest.
16. Bridge base.

**Main difficulty:**  
Final combination of:

- Similar towers.
- Similar roofs.
- Mirrored pieces.
- Large board.
- Multiple starting rotations.
- One or two fair distractors.
- Limited placement guidance.

Do not make the level dependent on guessing.

**Completion animation:**  
The drawbridge lowers, all flags wave, windows illuminate, stars appear and the complete collection world is revealed.

---

## 14. Required Level Data

For every level, define:

- Level ID.
- Level name.
- Category.
- Difficulty band.
- Target completion time.
- Board width.
- Board height.
- Object occupied width.
- Object occupied height.
- Piece count.
- Distractor count.
- Reference-art description.
- Completion-animation description.
- Completion-sound category.
- Snap tolerance.
- Hint delay.
- Full ghost enabled or disabled.
- Region hints enabled or disabled.
- Maximum recommended moves for three stars.
- Maximum recommended rotations for three stars.
- Piece tray order.
- Starting rotation for every piece.
- Correct rotation for every piece.
- Correct grid coordinate for every piece.
- Width and height in cells for every piece.
- Piece visual shape.
- Piece color.
- Piece prefab ID.
- Touch-area multiplier.
- Placement dependency, if required.
- Whether identical pieces are interchangeable.
- Whether a piece is a distractor.

---

## 15. Editor-Driven Unity Requirements

The level system must be editor-driven.

Do not hardcode individual level layouts in gameplay scripts.

Use:

- Unity 2D.
- ScriptableObjects.
- Prefab-based pieces.
- Inspector-editable data.
- A custom visual level editor.
- Editor-time validation.

Create:

- PuzzleLevelData ScriptableObject.
- PuzzlePieceData serializable class.
- PuzzleLevelDatabase.
- PuzzleLevelEditorWindow.
- Visual board-grid editor.
- Piece palette.
- Drag-and-drop editor placement.
- Rotation preview.
- Tray-order editor.
- Reference preview.
- Board-size preview.
- Duplicate-level function.
- Validation function.
- Completion simulation.
- Invalid-overlap detection.
- Out-of-grid detection.
- Missing-piece detection.
- Duplicate-coordinate detection.
- Invalid-rotation detection.
- Touch-size warning.
- Distractor validation.
- Automatic object-centering tool.

---

## 16. Runtime System Requirements

Create a clean runtime architecture containing:

- PuzzleBoardController.
- PuzzlePieceController.
- PieceTrayController.
- DragInputHandler.
- PieceRotationHandler.
- GridSnappingSystem.
- BoardOccupancySystem.
- PlacementValidator.
- CompletionDetector.
- HintSystem.
- UndoSystem.
- ResetSystem.
- LevelLoader.
- LevelProgressionManager.
- CollectionWorldManager.
- ThreeStarScoringSystem.
- SaveDataSystem.
- CompletionAnimationController.
- AudioFeedbackController.
- ObjectPool.

Technical constraints:

- Target low-end Android and iOS devices.
- Avoid unnecessary Update methods.
- Avoid runtime Find calls.
- Avoid repeated Instantiate and Destroy.
- Pool reusable feedback elements.
- Support safe areas.
- Support phones and tablets.
- Maintain consistent touch size.
- Keep the tray inside the lower safe area.
- Keep the reference card visible.
- Make all tuning values editable.
- Separate visual feedback from validation logic.
- Separate level data from runtime state.
- Support deterministic level loading.

---

## 17. Output Format

Produce the work in this order:

### Phase 1 — Game Design Document

- Summarize the game direction.
- Explain the core loop.
- Explain the collection meta.
- Explain session targets.
- Explain the complete difficulty curve.
- Explain the Rule of One.
- Explain scoring and hints.

### Phase 2 — Level Design Table

For all 50 levels, provide:

- Level data.
- Target difficulty.
- Target time.
- Board size.
- Main challenge.
- Relief or milestone status.
- Piece list.
- Rotation setup.
- Hint setup.
- Three-star move target.

### Phase 3 — Art Document

For every level, provide:

- Final assembled-object art prompt.
- Camera direction.
- Silhouette description.
- Exact color allocation.
- Material description.
- Lighting description.
- Separate piece boundaries.
- List of individual asset sprites or prefabs required.

### Phase 4 — Unity Data Architecture

Provide:

- ScriptableObject structures.
- Serializable piece data.
- Runtime state structures.
- Level database.
- Save format.
- Validation rules.

### Phase 5 — Unity Editor Tools

Provide:

- Editor window architecture.
- Grid authoring.
- Piece authoring.
- Rotation authoring.
- Tray authoring.
- Reference-image generation.
- Validation.
- Preview and simulation.

### Phase 6 — Runtime Implementation

Provide:

- System responsibilities.
- Event flow.
- Input flow.
- Placement flow.
- Completion flow.
- Hint flow.
- Save and progression flow.
- Performance safeguards.

### Phase 7 — Level Generation

Generate full implementation-ready data one level at a time, beginning with Level 01.

Do not:

- Skip fields.
- Use vague placeholders.
- Simplify later levels.
- Change the object list.
- Convert the game into a different puzzle genre.
- Add tiny unusable pieces.
- Make the difficulty rise too quickly.
- Place several vehicle levels consecutively without visual variety.
