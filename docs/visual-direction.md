# Mann Lab Games Visual Direction

For concrete tokens, component rules, and Unity reuse guidance, see `docs/design-system.md`.

## Core Direction

Mann Lab Games uses a hand-drawn sketch style inspired by whiteboard tools like Excalidraw.

The goal is to make small, fast games feel human, playful, and easy to understand without adding visual noise.

## Keywords

- Hand-drawn lines
- Slightly imperfect shapes
- Whiteboard or paper-like backgrounds
- High readability
- Minimal decoration
- Fast, clear feedback

## Palette

Default base:

- Background: white or very light paper tone
- Lines: black or dark gray
- Correct feedback: soft green marker
- Wrong feedback: red pen mark
- Warning/time pressure: amber or red accent

Avoid:

- Heavy gradients
- Glossy mobile-game surfaces
- Dense decoration behind gameplay
- Overly saturated one-color themes

## Shapes

- Use rough, slightly uneven outlines.
- Keep buttons and panels simple.
- Prefer board, paper, whiteboard, marker, and pen metaphors.
- Do not let sketch irregularity harm tap accuracy or readability.

## Typography

- Prefer a readable handwritten or hand-drawn style.
- Digits must remain highly legible at game speed.
- If a handwriting font hurts scan speed, use a clean rounded font and add sketch character through lines, borders, and animations.

## Motion

Feedback should feel like quick marks drawn on the board:

- Correct: marker highlight, circle, or underline
- Wrong: red check, slash, shake, or scribble
- Timeout: board desaturates, answer is circled

Animations should be short. The player should return to scanning almost immediately.

## Game UI

Gameplay must stay first:

- The board or core toy should dominate the screen.
- Explanatory text should be rare during play.
- Controls should be obvious, compact, and thumb-friendly.
- Decorative doodles are allowed only when they do not compete with gameplay.

## Asset Strategy

For MVPs, use procedural lines, simple sprites, or lightweight UI styling before commissioning full art.

When a game graduates from prototype, create a small reusable sketch UI kit:

- Buttons
- Timer bars
- Score labels
- Board tiles
- Correct/wrong marks
- Result screen elements
