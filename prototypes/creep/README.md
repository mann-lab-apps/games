# Please Move to the Back of the Mart

Unity mobile stealth-survival prototype for "Please Move to the Back of the Mart".

## Project

- Unity editor: 6000.3.23f1
- Platform: Android
- Package name: com.mannlab.games.creep
- Namespace: MannLab.Games.Creep

## MVP

- Portrait maze run with chunked upward progression.
- Virtual joystick movement where input strength controls speed and noise.
- Mist-touched watchers with vision cones, hearing checks, suspicion, and chase states.
- Encroaching fog rising from the bottom with a distinct engulfing game-over animation.
- Distance, best score, noise meter, threat state, restart flow, and keyboard fallback for editor testing.

## First Open

Open this directory from Unity Hub. Unity may generate missing project settings on first import.

## WebGL

Build and verify the local WebGL output:

```sh
./scripts/verify-creep-webgl.sh
```

Serve the generated build locally:

```sh
./scripts/serve-creep-webgl.sh
```

The default local URL is `http://127.0.0.1:8094`.

Run the browser smoke check while the local server is running:

```sh
node scripts/smoke-creep-webgl.mjs
```

## Release Notes

Keep prototype learnings, build links, and store-readiness notes here.
