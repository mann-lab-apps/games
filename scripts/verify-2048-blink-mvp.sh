#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_root="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents"
mono="$unity_root/Resources/Scripting/MonoBleedingEdge/bin/mono"
csc="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/4.5/csc.exe"
managed="$unity_root/Resources/Scripting/Managed"
unity="$managed/UnityEngine"
xcode_dll="$unity_root/PlaybackEngines/MacStandaloneSupport/UnityEditor.iOS.Extensions.Xcode.dll"
mono_lib="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/unityjit-macos"
ugui="$(find "$unity_root/Resources/PackageManager/ProjectTemplates/libcache" -path '*/ScriptAssemblies/UnityEngine.UI.dll' -type f | sort | head -n 1)"
project="$repo_root/prototypes/2048-blink"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/Game2048BlinkRuntime.dll"
editor_dll="$tmpdir/Game2048BlinkEditor.dll"

if [[ ! -x "$mono" || ! -f "$csc" || ! -f "$ugui" ]]; then
  echo "Unity 6000.3.22f1 scripting tools or UGUI assemblies are missing." >&2
  exit 1
fi

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

cat > "$tmpdir/VerifyBlink2048Rules.cs" <<'CS'
using System;
using MannLab.Games.Game2048Blink;

public static class VerifyBlink2048Rules
{
    public static int Main()
    {
        VerifyStartState();
        VerifyStandardMergeAndScore();
        VerifyGrayScanlineAdvancesOnlyAfterValidMove();
        VerifyNoMoveDoesNotAdvanceGrayScanline();
        VerifyGameOverDetection();

        Console.WriteLine("2048 Blink board rules verified.");
        return 0;
    }

    private static void VerifyStartState()
    {
        var board = new Blink2048Board(17);
        board.StartNew();
        var normalTiles = 0;
        for (var i = 0; i < Blink2048Board.CellCount; i++)
        {
            if (board.GetValueAtIndex(i) > 0)
            {
                normalTiles++;
            }
        }

        Require(normalTiles == 2, $"start should create two tiles, got {normalTiles}");
        Require(board.Score == 0, "start score should be 0");
        Require(board.Turn == 0, "start turn should be 0");
        Require(board.HiddenRow == 0, "start should hide row 1");
        Require(board.HiddenColumn == 2, "start should hide column 3");
        Require(board.GrayCrossPhase == 0, "start should use gray cross phase 1");
        Require(board.GrayCrossName == "Cross 1/4", "start should label gray cross phase 1");
        Require(board.IsHiddenIndex(0), "cell 1 should be hidden by the first row");
        Require(!board.IsHiddenIndex(4), "cell 5 should be visible at start");
        Require(board.IsHiddenIndex(2), "cell 3 should be hidden by the first row and third column");
        Require(board.IsHiddenIndex(6), "cell 7 should be hidden by the third column");
    }

    private static void VerifyStandardMergeAndScore()
    {
        var board = new Blink2048Board(23);
        board.LoadForTests(
            new[]
            {
                2, 2, 2, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            });

        var result = board.Move(Blink2048Direction.Left);
        Require(result.Moved, "merge move should be accepted");
        Require(result.ScoreGained == 4, "left merge should gain 4 score");
        Require(board.Score == 4, "score should be updated");
        Require(board.GetValueAtIndex(0) == 4, "left merge should create 4 at index 0");
        Require(board.GetValueAtIndex(1) == 2, "left merge should leave 2 at index 1");
        Require(board.Turn == 1, "valid move should increment turn");
        Require(board.HiddenRow == 1, "turn 1 should hide row 2");
        Require(board.HiddenColumn == 3, "turn 1 should hide column 4");
        Require(board.GrayCrossName == "Cross 2/4", "turn 1 should label gray cross phase 2");
        Require(result.SpawnedTileIndex >= 0, "valid move should spawn a tile");
    }

    private static void VerifyGrayScanlineAdvancesOnlyAfterValidMove()
    {
        var board = new Blink2048Board(29);
        board.LoadForTests(
            new[]
            {
                2, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            });

        var result = board.Move(Blink2048Direction.Right);
        Require(result.Moved, "single tile should slide right");
        Require(board.Turn == 1, "valid slide should advance the Gray Cross once");
        Require(board.HiddenRow == 1, "first move should hide row 2");
        Require(board.HiddenColumn == 3, "first move should hide column 4");
        Require(board.IsHiddenIndex(4), "cell 5 should be hidden by row 2 after first move");

        result = board.Move(Blink2048Direction.Down);
        Require(result.Moved, "next slide should be valid");
        Require(board.Turn == 2, "second valid move should advance the Gray Cross again");
        Require(board.HiddenRow == 2, "second move should hide row 3");
        Require(board.HiddenColumn == 0, "second move should hide column 1");
        Require(board.IsHiddenIndex(8), "cell 9 should be hidden by row 3 and column 1 after second move");
    }

    private static void VerifyNoMoveDoesNotAdvanceGrayScanline()
    {
        var board = new Blink2048Board(31);
        board.LoadForTests(
            new[]
            {
                2, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0
            },
            turn: 0);

        var result = board.Move(Blink2048Direction.Left);
        Require(!result.Moved, "tile already at left edge should not move left");
        Require(board.Turn == 0, "invalid move should not advance the Gray Cross");
        Require(board.HiddenRow == 0, "invalid move should keep current hidden row");
        Require(board.HiddenColumn == 2, "invalid move should keep current hidden column");
    }

    private static void VerifyGameOverDetection()
    {
        var board = new Blink2048Board(37);
        board.LoadForTests(
            new[]
            {
                2, 4, 2, 4,
                4, 2, 4, 2,
                2, 4, 2, 4,
                4, 2, 4, 2
            });
        Require(board.IsGameOver(), "full board with no merge should be game over");

        board.LoadForTests(
            new[]
            {
                2, 4, 2, 4,
                4, 2, 4, 2,
                2, 4, 2, 4,
                4, 2, 2, 4
            });
        Require(!board.IsGameOver(), "adjacent matching tiles should keep run alive");
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

"$mono" "$csc" -target:exe -nologo -nostdlib -out:"$tmpdir/VerifyBlink2048Rules.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifyBlink2048Rules.cs"

"$mono" "$tmpdir/VerifyBlink2048Rules.exe"
