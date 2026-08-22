#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_root="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents"
mono="$unity_root/Resources/Scripting/MonoBleedingEdge/bin/mono"
csc="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/4.5/csc.exe"
managed="$unity_root/Resources/Scripting/Managed"
unity="$managed/UnityEngine"
ios_support="/Applications/Unity/Hub/Editor/6000.3.22f1/PlaybackEngines/iOSSupport"
mono_lib="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/unityjit-macos"
ugui="$unity_root/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-2d-6.1.6/ScriptAssemblies/UnityEngine.UI.dll"
project="$repo_root/prototypes/rainwalker"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/RainwalkerRuntime.dll"
editor_dll="$tmpdir/RainwalkerEditor.dll"

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
  -r:"$ios_support/UnityEditor.iOS.Extensions.dll" \
  -r:"$ios_support/UnityEditor.iOS.Extensions.Common.dll" \
  -r:"$ios_support/UnityEditor.iOS.Extensions.Xcode.dll" \
  -r:"$unity/UnityEngine.CoreModule.dll" \
  -r:"$unity/UnityEngine.AudioModule.dll" \
  -r:"$unity/UnityEngine.UIModule.dll" \
  -r:"$unity/UnityEngine.TextRenderingModule.dll" \
  -r:"$unity/UnityEngine.InputLegacyModule.dll" \
  -r:"$unity/UnityEngine.IMGUIModule.dll" \
  -r:"$ugui" \
  -r:"$runtime_dll" \
  "$project"/Assets/_Project/Editor/*.cs

cat > "$tmpdir/VerifyRainwalkerRules.cs" <<'CS'
using System;
using MannLab.Games.Rainwalker;

public static class VerifyRainwalkerRules
{
    public static int Main()
    {
        if (RainwalkerRules.ScoreForHits(0) != RainwalkerRules.PerfectScore)
        {
            Console.Error.WriteLine("A dry run should keep the perfect score.");
            return 1;
        }

        if (RainwalkerRules.ScoreForHits(200) != 0)
        {
            Console.Error.WriteLine("Very wet runs should clamp score to zero.");
            return 1;
        }

        if (RainwalkerRules.GradeForScore(960) != "S" || RainwalkerRules.GradeForScore(300) != "Soaked")
        {
            Console.Error.WriteLine("Score grades are not mapped as expected.");
            return 1;
        }

        if (RainwalkerRules.SpawnIntervalForProgress(1f) >= RainwalkerRules.SpawnIntervalForProgress(0f))
        {
            Console.Error.WriteLine("Rain density should increase toward the end of the round.");
            return 1;
        }

        if (RainwalkerRules.RainSpeedForProgress(1f) <= RainwalkerRules.RainSpeedForProgress(0f))
        {
            Console.Error.WriteLine("Rain speed should increase toward the end of the round.");
            return 1;
        }

        Console.WriteLine("Rainwalker rules verified.");
        return 0;
    }
}
CS

"$mono" "$csc" -nologo -nostdlib -out:"$tmpdir/VerifyRainwalkerRules.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifyRainwalkerRules.cs"

"$mono" "$tmpdir/VerifyRainwalkerRules.exe"

echo "Rainwalker MVP compile verification passed."
