#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_root="/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents"
mono="$unity_root/Resources/Scripting/MonoBleedingEdge/bin/mono"
csc="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/4.5/csc.exe"
managed="$unity_root/Resources/Scripting/Managed"
unity="$managed/UnityEngine"
mono_lib="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/unityjit-macos"
ios_xcode="$unity_root/PlaybackEngines/iOSSupport/UnityEditor.iOS.Extensions.Xcode.dll"
ugui="$unity_root/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-2d-6.1.7/ScriptAssemblies/UnityEngine.UI.dll"
nunit="$unity_root/Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom/nunit.framework.dll"
project="$repo_root/prototypes/walking"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/WalkingRuntime.dll"
editor_dll="$tmpdir/WalkingEditor.dll"
tests_dll="$tmpdir/WalkingTests.dll"

if [[ ! -x "$mono" || ! -f "$csc" ]]; then
  echo "Unity 6000.3.23f1 scripting tools were not found at $unity_root." >&2
  exit 1
fi

if [[ ! -f "$ugui" ]]; then
  ugui="$(find "$unity_root/Resources/PackageManager/ProjectTemplates/libcache" -path '*/ScriptAssemblies/UnityEngine.UI.dll' | head -n 1)"
fi

if [[ ! -f "$ios_xcode" ]]; then
  ios_xcode="$(find "$unity_root" -name 'UnityEditor.iOS.Extensions.Xcode.dll' | head -n 1)"
fi

"$mono" "$csc" -target:library -nologo -nostdlib -out:"$runtime_dll" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$unity/UnityEngine.CoreModule.dll" \
  -r:"$unity/UnityEngine.AudioModule.dll" \
  -r:"$unity/UnityEngine.UIModule.dll" \
  -r:"$unity/UnityEngine.TextRenderingModule.dll" \
  -r:"$unity/UnityEngine.InputLegacyModule.dll" \
  -r:"$unity/UnityEngine.IMGUIModule.dll" \
  -r:"$unity/UnityEngine.PhysicsModule.dll" \
  -r:"$unity/UnityEngine.JSONSerializeModule.dll" \
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
  -r:"$ios_xcode" \
  -r:"$unity/UnityEngine.CoreModule.dll" \
  -r:"$unity/UnityEngine.AudioModule.dll" \
  -r:"$unity/UnityEngine.UIModule.dll" \
  -r:"$unity/UnityEngine.TextRenderingModule.dll" \
  -r:"$unity/UnityEngine.InputLegacyModule.dll" \
  -r:"$unity/UnityEngine.IMGUIModule.dll" \
  -r:"$unity/UnityEngine.PhysicsModule.dll" \
  -r:"$ugui" \
  -r:"$runtime_dll" \
  "$project"/Assets/_Project/Editor/*.cs

cat > "$tmpdir/VerifyWalkingRules.cs" <<'CS'
using System;
using MannLab.Games.Walking;
using UnityEngine;

public static class VerifyWalkingRules
{
    public static int Main()
    {
        if (Convert.ToBoolean(WalkingController.DefaultDebugFootMarkers))
        {
            return Fail("Default play mode must hide foot markers.");
        }

        var maze = WalkingMaze.Generate(11, 18, 20260830, WalkingRules.TileSize);
        for (var y = 1; y <= 4; y++)
        {
            for (var x = 1; x <= 4; x++)
            {
                if (maze.IsSolidGrid(x, y))
                {
                    return Fail($"Start area tile {x},{y} is solid.");
                }
            }
        }

        var start = maze.GridToWorld(2, 2);
        if (WalkingRules.IsBodyColliding(start, maze))
        {
            return Fail("Body collides in the cleared start area.");
        }

        var emptyMaze = WalkingMaze.CreateForTests(new bool[9, 9], 1f);
        var rightStep = WalkingRules.ValidateFootPlacement(
            WalkingFootSide.Right,
            Vector2.zero,
            Vector2.up * 0.72f + Vector2.right * 0.25f,
            Vector2.up,
            emptyMaze);
        if (!rightStep.IsValid)
        {
            return Fail($"Expected a valid right step, got {rightStep.Reason}.");
        }

        var crossedLeft = WalkingRules.ValidateFootPlacement(
            WalkingFootSide.Left,
            Vector2.zero,
            Vector2.up * 0.72f + Vector2.right * 0.18f,
            Vector2.up,
            emptyMaze);
        if (crossedLeft.IsValid || crossedLeft.Reason != "cross")
        {
            return Fail("Crossed left step was not rejected.");
        }

        var longStep = WalkingRules.ValidateFootPlacement(
            WalkingFootSide.Right,
            Vector2.zero,
            Vector2.up * (WalkingRules.MaxStepDistance + 0.3f) + Vector2.right * 0.25f,
            Vector2.up,
            emptyMaze);
        if (longStep.IsValid || longStep.Reason != "long")
        {
            return Fail("Overlong step was not rejected.");
        }

        var solid = new bool[7, 7];
        solid[3, 4] = true;
        var wallMaze = WalkingMaze.CreateForTests(solid, 1f);
        if (!WalkingRules.IsBodyColliding(wallMaze.GridToWorld(3, 4), wallMaze))
        {
            return Fail("Body-wall collision was not detected.");
        }

        var wallLanding = WalkingRules.ValidateFootPlacement(
            WalkingFootSide.Right,
            wallMaze.GridToWorld(3, 4) - Vector2.up * 0.7f - Vector2.right * 0.25f,
            wallMaze.GridToWorld(3, 4),
            Vector2.up,
            wallMaze);
        if (wallLanding.IsValid || wallLanding.Reason != "wall")
        {
            return Fail("Wall landing was not rejected.");
        }

        var screen = new Vector2(1080f, 1920f);
        if (!WalkingRules.IsReturnGesturePosition(new Vector2(540f, 420f), screen))
        {
            return Fail("Low return touch was not accepted.");
        }

        if (WalkingRules.IsReturnGesturePosition(new Vector2(540f, 1280f), screen))
        {
            return Fail("High step touch was incorrectly treated as return.");
        }

        if (WalkingRules.FootSideForScreenPosition(new Vector2(820f, 960f), screen) != WalkingFootSide.Right)
        {
            return Fail("Right-half touch did not target the right foot.");
        }

        Console.WriteLine("Walking rules verified.");
        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
CS

"$mono" "$csc" -nologo -nostdlib -out:"$tmpdir/VerifyWalkingRules.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$unity/UnityEngine.CoreModule.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifyWalkingRules.cs"

MONO_PATH="$tmpdir:$unity:$mono_lib" "$mono" "$tmpdir/VerifyWalkingRules.exe"

if [[ -f "$project/Assets/_Project/Tests/Editor/WalkingRulesTests.cs" ]]; then
  "$mono" "$csc" -target:library -nologo -nostdlib -out:"$tests_dll" \
    -r:"$mono_lib/mscorlib.dll" \
    -r:"$mono_lib/System.dll" \
    -r:"$mono_lib/System.Core.dll" \
    -r:"$mono_lib/Facades/netstandard.dll" \
    -r:"$unity/UnityEngine.CoreModule.dll" \
    -r:"$runtime_dll" \
    -r:"$nunit" \
    "$project"/Assets/_Project/Tests/Editor/*.cs
fi

echo "Walking MVP compile verification passed."
