# Mann Lab Games Design System

Mann Lab Games uses a sketch-first design system for small, fast games.

The reference mood is the clarity and looseness of whiteboard tools such as Excalidraw, but the system should feel like Mann Lab Games rather than a copy. Gameplay readability wins over decorative sketchiness.

## Design Principles

- Fast to read: numbers, targets, timers, and results must scan instantly.
- Human but precise: outlines can wobble, hit areas cannot.
- Paper first: the screen should feel like a playable sheet, board, or notebook.
- Marks are feedback: correct, wrong, warning, and completion states should feel drawn onto the board.
- Quiet brand layer: shared style should connect games without forcing every game into the same layout.

## Palette

Core colors:

- Paper: `#FAF7EF`
- Tile paper: `#FFFDF7`
- Ink: `#282724`
- Muted ink: `#66615A`
- Warm shadow: `#E6DCC8`

Feedback colors:

- Correct marker: `#5ED481` at marker transparency
- Wrong marker: `#E64440` at marker transparency
- Warning amber: `#EEA840`
- Focus blue: `#4F8BFF` only for system focus, links, or non-game UI

Usage rules:

- Use paper and ink as the dominant pair.
- Use green/red/amber as momentary feedback, not as page themes.
- Avoid heavy gradients, glossy mobile surfaces, dense doodle backgrounds, and saturated one-color palettes.

## Surface And Texture

MVP surfaces should be simple flat colors. Texture can be added later with lightweight noise or scanned paper assets, but it must stay subtle enough that digits remain readable.

Surface roles:

- Screen background: paper
- Tiles and buttons: tile paper
- Result panel: tile paper with high opacity
- Highlight marks: translucent marker layers above the tile
- Separators and outlines: ink lines with slight jitter

## Lines

Sketch lines should look drawn, not broken.

Default metrics:

- Thin line: `2 px`
- Default line: `2.5 px`
- Heavy line: `4 px`
- Default jitter: `2.5 px`
- Default strokes: `2`
- Board gap: `5 px`
- Marker inset: `8 px`

Rules:

- Keep outlines close to the actual hit area.
- Use two passes for most rectangles.
- Use heavier lines only for active/focus states or modal/result boundaries.
- Jitter should never make adjacent controls visually collide.

## Typography

Text should feel casual but remain fast to scan.

Rules:

- Digits are the highest priority; use a clean system font until a licensed handwritten font is chosen.
- Letter spacing stays at `0`.
- Use larger type for gameplay digits than labels.
- Keep text short during play.
- Do not use decorative handwriting if it slows target search.

Initial scale:

- Board digits: `44`
- Header stage: `46`
- Header best: `34`
- Result title: `52`
- Result score: `38`
- Button text: `36`
- Opening hint digits: `64`

## Components

### Board Tile

- Fill: tile paper
- Outline: default sketch outline
- Text: ink
- Pressed state: warm paper
- Correct state: green marker overlay
- Wrong state: red marker flash or pen-like overlay

### Button

- Fill: tile paper
- Outline: default sketch outline
- Hover/pressed: warm paper shifts
- Label: direct action text or icon when the action is familiar

### Timer

- Track: tile paper with sketch outline
- Fill: amber
- Danger: blend amber toward wrong marker below 25%
- Animation: continuous fill only; avoid noisy pulses while scanning

### Result Panel

- Fill: tile paper with slight opacity
- Outline: sketch outline
- Content: score first, then restart action
- Motion: quick reveal; no long celebration in MVPs

## Feedback And Motion

Feedback should feel like quick marks added to the page.

- Correct: marker highlight, circle, or underline, `0.12-0.22s`
- Wrong: red flash, slash, or short shake, `0.08-0.16s`
- New run hint: target tiles appear, settle, and vanish within about `1s`
- Timeout: reveal answer and show result immediately

Motion should never block the next scan longer than necessary.

## Sound Direction

Sound is not part of the current MVP kit, but the direction is:

- Short pencil taps for tile selection
- Soft marker swipe for correct
- Dry red-pen tick for wrong
- Paper flip or small desk tap for restart

Avoid arcade-heavy coin, slot, or casino-like sound language.

## Unity Reuse Plan

Shared runtime package:

- `shared/unity-packages/com.mannlab.hypercasual-core`

Initial shared code:

- `SketchPalette`: shared colors
- `SketchMetrics`: shared line, gap, and marker measurements
- `SketchUiFactory`: reusable button color helpers
- `SketchOutlineGraphic`: procedural rough rectangle outline

Future shared candidates:

- `SketchTheme` ScriptableObject for game-level overrides
- Marker stroke graphics for circles, slashes, and underlines
- Paper background material or subtle noise texture
- Shared UI prefabs once at least two games need the same layout
- Sound token names once sound assets exist

Game override rules:

- A game may override accent colors or motion timing when the core mechanic needs it.
- A game should keep paper, ink, sketch outline behavior, and marker-like feedback unless it has a strong reason to diverge.
- Shared code should stay small until two games need the abstraction.

## Mann Lab Scope

Games-owned system:

- Sketch board/tile/button/panel language
- Marker feedback
- Fast game motion
- Mobile gameplay readability rules

Potential Mann Lab-wide system:

- Paper and ink brand feel
- Human, lightweight illustrations
- Simple document-like layouts
- Warm but restrained tone

Keep site/app productivity interfaces calmer than games. The Games sketch system can influence Mann Lab broadly, but game-specific marker feedback and exaggerated outlines should not be forced into every Mann Lab product.
