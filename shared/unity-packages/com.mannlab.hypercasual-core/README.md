# Mann Lab Hypercasual Core

Shared Unity runtime package for small Android-first hyper-casual games.

Add it to a game project's `Packages/manifest.json` after Unity creates the project:

```json
{
  "dependencies": {
    "com.mannlab.hypercasual-core": "file:../../shared/unity-packages/com.mannlab.hypercasual-core"
  }
}
```

Included runtime helpers:

- `MobileRuntime`: mobile defaults for fast prototypes
- `SketchPalette`: shared paper, ink, marker, and warning colors
- `SketchMetrics`: shared sketch line and spacing metrics
- `SketchUiFactory`: small UI helper methods
- `SketchOutlineGraphic`: procedural rough rectangle outline for Unity UI
- `SketchHatchFillGraphic`: procedural paper fill with clipped blue hatching for target/blocked tiles

Keep this package small. Code should move here when it defines the Mann Lab Games baseline or when at least two games need it.
