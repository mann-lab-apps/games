# Local Unity Setup

Use this setup for Android-first Unity prototypes.

## Install

Install Unity through Unity Hub.

Recommended editor line:

- Unity 6 LTS
- Prefer the latest Unity 6 LTS patch available in Unity Hub

Install these modules with the editor:

- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

Unity Editor was not found on this machine when this repo was initialized, so install it before opening a project.

## Git LFS

Git LFS was not installed locally when this repo was initialized.

Install it before committing production image, audio, video, model, or build artifacts:

```sh
brew install git-lfs
git lfs install
```

After Git LFS is installed, uncomment the binary asset rules in `.gitattributes`.

## Create A Game

```sh
./scripts/new-unity-game.sh prototypes stack-jump
```

Then:

1. Open the generated game directory from Unity Hub.
2. Let Unity import and generate missing project settings.
3. Switch platform to Android.
4. Set package name, orientation, scripting backend, and target architectures.
5. Commit Unity-generated settings after confirming the project opens cleanly.

## Promote A Game

Move a project from `prototypes/` to `releases/` only when:

- The core loop is playable.
- It has a target orientation.
- It has a package name.
- It can build to an Android device.
- The release checklist in `docs/android-release-baseline.md` is relevant.

