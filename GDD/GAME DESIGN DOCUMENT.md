# Arrow Cube Escape

## Game Design Document

Status: Active  
Source of truth: This document supersedes `GDD/GAME DESIGN DOCUMENT.docx` and reflects the actual design intent shown by the current Unity project and gameplay code.

## 1. Product Summary

`Arrow Cube Escape` is a single-level-at-a-time 3D puzzle game built around a rotatable cube. The player examines the cube from different angles, identifies which arrow-shaped pieces have a clear exit path, and taps them to release them from the structure.

The game is not designed as a generic block-removal toy. Its core identity is the combination of:

- A cube with 6 playable faces.
- Arrow-shaped pieces with fixed exit directions.
- Body shapes that can wrap across multiple faces.
- A clean, readable puzzle loop based on observation, rotation, and removal order.

## 2. Core Fantasy

The player is solving a spatial disentanglement puzzle on the surface of a cube.

The intended feeling is:

- Rotate the cube to understand the full structure.
- Read the direction of each arrow head.
- Find a valid release order.
- Watch each solved piece peel away from the cube in a satisfying chained motion.

## 3. Genre and Positioning

Genre: 3D spatial puzzle  
Platform target: Unity, touch-first controls, suitable for desktop and mobile prototypes  
Session style: Short puzzle attempts with immediate interaction  
Fail state: None in the current design baseline  
Win state: Clear every arrow piece from the cube

## 4. Design Pillars

- Readable 3D spatial reasoning. The player must rotate the cube to inspect blocked and free directions.
- One-tap decisive actions. Every tap is a binary test of the current puzzle state: this piece can leave now, or it cannot.
- Strong shape identity. Each piece is a continuous arrow path, not an isolated cube block.
- Cross-face composition. Pieces may extend across edges and onto neighboring faces, making the whole cube feel interconnected.
- Clean feedback. Successful removals are animated clearly; blocked attempts give immediate visual and motion feedback.

## 5. Core Gameplay Loop

1. Observe the cube and locate arrow heads.
2. Rotate the full structure to inspect visible faces and hidden dependencies.
3. Tap an arrow piece that appears to have a valid exit.
4. If the path is clear, the full arrow escapes and is removed from the board.
5. If the path is blocked, the piece bumps and highlights as invalid.
6. Repeat until all arrow pieces are cleared.

## 6. Input Model

The project currently supports two primary player actions:

- Drag: rotates the full cube through a shared `StructurePivot`.
- Tap: raycasts into the scene and triggers `ArrowTile.Slide()` on a selected piece.

This makes the game readable and touch-friendly without requiring any mode switching or UI-heavy control scheme.

## 7. Puzzle Model

### 7.1 Board Structure

The puzzle board is a cube composed of 6 faces:

- `0`: Front
- `1`: Back
- `2`: Left
- `3`: Right
- `4`: Top
- `5`: Bottom

Each face owns a square grid. The grid size is configurable per level.

### 7.2 Piece Structure

Each puzzle piece is an `arrow tile` with:

- One head cell.
- One fixed movement direction.
- Zero or more body cells.
- A body that may continue across multiple cube faces.

The body path is authored manually as an ordered list of cells. That ordered path defines both the piece silhouette and the chained removal animation.

### 7.3 Movement Rule

Each arrow has exactly one valid escape direction, defined at the head.

The current gameplay rule is:

- The game checks from the head forward only on the head's current face.
- If another registered arrow cell occupies any cell ahead on that face, the piece is blocked.
- If the scan reaches the edge of the face without obstruction, the piece is considered free and exits the cube.

Important clarification:

- Runtime escape does not continue sliding across neighboring faces.
- Reaching the face boundary means the piece leaves the puzzle volume.
- Cross-face body data exists to define the shape, occupancy, and escape animation path, not to create continuous runtime surface sliding.

This rule is central to the game's intended identity and replaces earlier incorrect assumptions that the game was a standard full-surface cube slider.

### 7.4 Occupancy and Blocking

All body cells are registered into face-based occupancy maps.

This means:

- A piece may be blocked by the head or body of another piece.
- Cross-face shapes matter because their cells can occupy and obstruct other faces.
- The correct solve order emerges from global occupancy, even though the final head-path check is local to the head face.

## 8. Success and Failure States

### 8.1 Success

The level is complete when every `ArrowTile` has escaped.

Current game-state tracking counts remaining arrow pieces rather than counting individual cells.

### 8.2 Failure

There is no loss state in the current intended design.

An incorrect tap does not punish the player beyond feedback. The piece:

- Bumps slightly in its intended exit direction.
- Shows a temporary red outline.
- Returns to idle state.

This supports a low-friction puzzle experience based on experimentation.

## 9. Visual Direction

The visual language implied by the current project is minimal, geometric, and diagram-like.

Key visual traits already present in code and scene generation:

- A transparent or semi-transparent central cube.
- Dark body segments that read like ink strokes or structural marks.
- Cyan arrow heads for directional emphasis.
- Red blocked-state outlines for invalid input feedback.
- Clean background and simple lighting.

This is a better match for the current game than a candy-colored block-removal aesthetic. The visual identity should emphasize clarity, structure, and legibility over decoration.

## 10. Piece Removal Presentation

When a piece escapes:

- The piece unregisters its occupied cells.
- Each segment detaches from the root.
- The head flies outward from the cube.
- Following segments trace the path of the segments in front of them, then fly out after the head.

The intended read is that the arrow is being peeled away from the cube like a continuous chain.

This animation is a defining part of the game's feel and should remain a core presentation feature.

## 11. Level Design Principles

### 11.1 What Makes a Good Level

A good level should:

- Hide the correct removal order behind viewing angle and occupancy.
- Use cross-face bodies to create spatial ambiguity.
- Force the player to rotate before the answer becomes obvious.
- Reward pattern recognition without requiring complex controls.

### 11.2 Difficulty Drivers

Difficulty should increase through:

- More arrow pieces.
- Longer body shapes.
- More cross-face wrapping.
- Denser interlocking occupancy.
- Greater need to inspect hidden faces before acting.

Difficulty should not rely on:

- Fast reaction timing.
- Precision dragging.
- Random generation.
- Punitive failure states.

### 11.3 Recommended Difficulty Curve

Early levels:

- Few pieces.
- Mostly single-face bodies.
- Clear, visible exits.

Mid levels:

- More interlocking pieces.
- Hidden blockers revealed by rotation.
- Multiple plausible moves with only one or two actually free.

Advanced levels:

- Long cross-face arrows.
- Dense overlapping occupancy patterns.
- Strong dependence on understanding the full cube as one connected puzzle.

## 12. Level Authoring Workflow

Levels are authored manually through a custom Unity editor workflow built around `ArrowLevelData`.

The current pipeline is:

1. Create or open an `ArrowLevelData` asset.
2. Place a head on any face in a cube-net editor.
3. Assign the arrow's exit direction.
4. Extend the body cell-by-cell.
5. Cross edges to neighboring faces through the editor's topology helper.
6. Generate the level into the scene.

This authoring workflow is part of the intended product design, not just a temporary tool. Manual authorship is important because the puzzle quality depends on deliberate spatial composition.

## 13. Scene Structure

### 13.1 Gameplay Scene

The gameplay scene is organized around:

- `GameManager`
- `InputManager`
- `StructurePivot`
- Six generated `FaceGrid` objects
- Generated arrow piece hierarchies
- A simple UI canvas

The gameplay scene is currently the only scene included in Build Settings.

### 13.2 Home Scene

A `HomeScene` exists as an initial menu shell with play and settings buttons, but it is not yet wired into the active build flow.

This means the real product currently starts from the puzzle scene rather than from a complete menu-to-game loop.

## 14. UI and UX Scope

The current project contains only lightweight in-game UI.

What the current design baseline supports:

- A gameplay scene with simple HUD elements.
- A placeholder level label.
- A placeholder count display.
- Basic menu shell objects in `HomeScene`.

What is not part of the current validated design baseline:

- Undo
- Hint or auto-rotate systems
- Reward economy
- Shop or skins
- Meta progression
- Star ratings
- Timers
- Special multi-hit block types

These features should not be treated as required unless they are intentionally added in a later design phase.

## 15. Audio Direction

Audio is not implemented in the current project baseline, but the design implied by the code supports a restrained, tactile approach:

- Soft success pop or release sound on escape.
- Short blocked tap sound on invalid attempts.
- Light ambient or minimal background music if added later.

Audio should reinforce clarity and puzzle feedback rather than push the game toward loud arcade presentation.

## 16. Technical Design Intent

The following technical choices are part of the intended design, not incidental implementation details:

- Face-based occupancy tracking for puzzle logic.
- A fixed cube topology for editor-side cross-face authoring.
- Data-driven puzzle layout through `ArrowLevelData`.
- Generated runtime visuals from authored level data.
- Piece-level win tracking.
- Direct interaction through raycast selection.

The project should continue evolving as a data-driven puzzle game with custom level tooling, not as a physics toy or freeform block sandbox.

## 17. Current Prototype Scope

The project already proves these design goals:

- Rotating a 3D cube is central to understanding the puzzle.
- Arrow pieces have unique directional identity.
- Cross-face shapes are readable and meaningful.
- The clear-vs-blocked interaction loop works.
- Removal feedback is satisfying and readable.

The project does not yet prove these areas:

- Long-form content progression.
- Final UI flow.
- Audio polish.
- Save/load progression.
- Production-ready onboarding.

## 18. Design Boundaries

To keep future development aligned with the real intent of the prototype:

- Do not redefine the game as a generic `Tap Away` clone with independent blocks.
- Do not assume pieces should slide continuously across multiple faces during play.
- Do not design around heavy fail states, timers, or punishment.
- Do not add meta systems before the core puzzle content and clarity are stable.
- Do keep the identity centered on arrow-shaped cross-face pieces and cube-based solve order.

## 19. Next Design Priorities

The most logical next design steps, based on the current codebase, are:

- Bind gameplay UI to real runtime data.
- Load authored `ArrowLevelData` directly rather than relying on baked scene state.
- Define a proper level progression structure.
- Finish the Home-to-Gameplay scene flow.
- Add light audio and polish without changing the core puzzle rules.

## 20. Final Statement

`Arrow Cube Escape` is a rotational 3D puzzle about clearing arrow-shaped pieces from a cube by reading direction, understanding obstruction, and choosing a valid release order.

That is the intended design shown by the current project.

Any future documentation, implementation, and planning should treat this document as the correct baseline unless the gameplay code itself is intentionally redesigned.
