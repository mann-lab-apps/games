#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_root="/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents"
mono="$unity_root/Resources/Scripting/MonoBleedingEdge/bin/mono"
csc="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/4.5/csc.exe"
managed="$unity_root/Resources/Scripting/Managed"
unity="$managed/UnityEngine"
xcode_dll="$unity_root/PlaybackEngines/MacStandaloneSupport/UnityEditor.iOS.Extensions.Xcode.dll"
mono_lib="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/unityjit-macos"
ugui="$unity_root/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-2d-6.1.2/ScriptAssemblies/UnityEngine.UI.dll"
project="$repo_root/prototypes/2048-crash"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/Game2048CrashRuntime.dll"
editor_dll="$tmpdir/Game2048CrashEditor.dll"

"$mono" "$csc" -target:library -nologo -nostdlib -out:"$runtime_dll" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$unity/UnityEngine.CoreModule.dll" \
  -r:"$unity/UnityEngine.UIModule.dll" \
  -r:"$unity/UnityEngine.TextRenderingModule.dll" \
  -r:"$unity/UnityEngine.InputLegacyModule.dll" \
  -r:"$unity/UnityEngine.IMGUIModule.dll" \
  -r:"$ugui" \
  "$shared_runtime"/*.cs \
  "$project"/Assets/_Project/Scripts/*.cs

"$mono" "$csc" -target:library -nologo -nostdlib -out:"$editor_dll" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$managed/UnityEngine/UnityEditor.CoreModule.dll" \
  -r:"$managed/UnityEngine/UnityEditor.SceneViewModule.dll" \
  -r:"$xcode_dll" \
  -r:"$unity/UnityEngine.CoreModule.dll" \
  -r:"$unity/UnityEngine.UIModule.dll" \
  -r:"$unity/UnityEngine.TextRenderingModule.dll" \
  -r:"$unity/UnityEngine.InputLegacyModule.dll" \
  -r:"$unity/UnityEngine.IMGUIModule.dll" \
  -r:"$ugui" \
  -r:"$runtime_dll" \
  "$project"/Assets/_Project/Editor/*.cs

cat > "$tmpdir/VerifyCrash2048Rules.cs" <<'CS'
using System;
using MannLab.Games.Game2048Crash;

public static class VerifyCrash2048Rules
{
    public static int Main()
    {
        VerifyStartState();
        VerifyStandardMerge();
        VerifySpecialCrash();
        VerifyConnectedStageAfterCrash();
        VerifySpecialBlocksMismatchedTile();
        VerifyGameOverDetection();

        Console.WriteLine("2048 Crash board rules verified.");
        return 0;
    }

    private static void VerifyStartState()
    {
        var board = new Crash2048Board(17);
        board.StartNew();
        var normalTiles = 0;
        for (var i = 0; i < Crash2048Board.CellCount; i++)
        {
            if (board.GetValueAtIndex(i) > 0)
            {
                normalTiles++;
            }
        }

        Require(board.SpecialIndex >= 0, "start should create a special block");
        Require(board.SpecialValue == 2, "start special value should be 2");
        Require(board.Stage == 0, "start stage should be 0");
        Require(normalTiles == 2, $"start should create two normal tiles, got {normalTiles}");
    }

    private static void VerifyStandardMerge()
    {
        var board = new Crash2048Board(23);
        board.LoadForTests(
            new[]
            {
                2, 2, 2, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            },
            15,
            8,
            0);

        var result = board.Move(Crash2048Direction.Left);
        Require(result.Moved, "merge move should be accepted");
        Require(!result.SpecialCrashed, "standard merge should not crash special");
        Require(board.GetValueAtIndex(0) == 4, "left merge should create 4 at index 0");
        Require(board.GetValueAtIndex(1) == 2, "left merge should leave 2 at index 1");
        Require(board.Stage == 0, "standard merge should not increment stage");
    }

    private static void VerifySpecialCrash()
    {
        var board = new Crash2048Board(29);
        board.LoadForTests(
            new[]
            {
                0, 2, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            },
            0,
            2,
            0);

        var result = board.Move(Crash2048Direction.Left);
        Require(result.Moved, "matching tile should move into special block");
        Require(result.SpecialCrashed, "matching tile should crash special block");
        Require(board.Stage == 1, "special crash should increment stage");
        Require(board.SpecialValue == 4, "next special value should double");
        Require(board.GetValueAtIndex(0) == 0, "crashing tile should be destroyed with the special block");
        Require(board.SpecialIndex != 0, "next special block should be spawned elsewhere");
    }

    private static void VerifyConnectedStageAfterCrash()
    {
        var board = new Crash2048Board(41);
        board.LoadForTests(
            new[]
            {
                0, 2, 4, 0,
                8, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            },
            0,
            2,
            0);

        var result = board.Move(Crash2048Direction.Left);
        Require(result.SpecialCrashed, "matching tile should crash special in continuity check");
        Require(board.Stage == 1, "crash should advance to the next connected stage");
        Require(board.GetValueAtIndex(0) == 0, "crashing tile should not remain on the continuing board");
        Require(board.GetValueAtIndex(1) == 4, "existing board tiles should remain after stage advance");
        Require(board.GetValueAtIndex(4) == 8, "unrelated existing tiles should remain after stage advance");
        Require(result.Motions.Length > 0, "move should expose tile motions for animation");
        Require(result.SpawnedTileIndex >= 0, "connected stage move should spawn a normal tile");
        Require(result.NewSpecialIndex >= 0, "connected stage move should spawn the next special block");
    }

    private static void VerifySpecialBlocksMismatchedTile()
    {
        var board = new Crash2048Board(31);
        board.LoadForTests(
            new[]
            {
                0, 2, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            },
            0,
            4,
            0);

        var result = board.Move(Crash2048Direction.Left);
        Require(!result.Moved, "mismatched tile should be blocked by special");
        Require(!result.SpecialCrashed, "mismatched tile should not crash special");
        Require(board.Stage == 0, "blocked move should not increment stage");
        Require(board.GetValueAtIndex(1) == 2, "blocked tile should stay in place");
    }

    private static void VerifyGameOverDetection()
    {
        var board = new Crash2048Board(37);
        board.LoadForTests(
            new[]
            {
                2, 4, 2, 4,
                4, 2, 4, 2,
                2, 4, 2, 4,
                4, 2, 4, 0
            },
            15,
            2048,
            0);
        Require(board.IsGameOver(), "full board with no merge or crash should be game over");

        board.LoadForTests(
            new[]
            {
                2, 4, 2, 4,
                4, 2, 4, 2,
                2, 4, 2, 4,
                4, 2, 2, 0
            },
            15,
            2,
            0);
        Require(!board.IsGameOver(), "matching tile beside special should keep the run alive");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
CS

"$mono" "$csc" -nologo -nostdlib -out:"$tmpdir/VerifyCrash2048Rules.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifyCrash2048Rules.cs"

"$mono" "$tmpdir/VerifyCrash2048Rules.exe"

echo "2048 Crash MVP compile verification passed."
