#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_root="/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents"
mono="$unity_root/Resources/Scripting/MonoBleedingEdge/bin/mono"
csc="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/4.5/csc.exe"
managed="$unity_root/Resources/Scripting/Managed"
unity="$managed/UnityEngine"
mono_lib="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/unityjit-macos"
ugui="$unity_root/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-2d-6.1.2/ScriptAssemblies/UnityEngine.UI.dll"
project="$repo_root/prototypes/sitting"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/SittingRuntime.dll"
editor_dll="$tmpdir/SittingEditor.dll"

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
  -r:"$unity/UnityEngine.CoreModule.dll" \
  -r:"$unity/UnityEngine.AudioModule.dll" \
  -r:"$unity/UnityEngine.UIModule.dll" \
  -r:"$unity/UnityEngine.TextRenderingModule.dll" \
  -r:"$unity/UnityEngine.InputLegacyModule.dll" \
  -r:"$unity/UnityEngine.IMGUIModule.dll" \
  -r:"$ugui" \
  -r:"$runtime_dll" \
  "$project"/Assets/_Project/Editor/*.cs

cat > "$tmpdir/VerifySittingRules.cs" <<'CS'
using System;
using MannLab.Games.Sitting;

public static class VerifySittingRules
{
    public static int Main()
    {
        var standing = SittingBalance.TickHealth(100f, false, 2f);
        if (standing >= 100f || standing <= 0f)
        {
            Console.Error.WriteLine($"Standing drain was unexpected: {standing}.");
            return 1;
        }

        var sitting = SittingBalance.TickHealth(40f, true, 1.5f);
        if (sitting <= 40f || sitting > SittingBalance.MaxHealth)
        {
            Console.Error.WriteLine($"Sitting recovery was unexpected: {sitting}.");
            return 1;
        }

        if (!SittingBalance.ShouldCatch(true, VisitorPhase.Passing))
        {
            Console.Error.WriteLine("Sitting during visitor passing should catch the player.");
            return 1;
        }

        if (SittingBalance.ShouldCatch(false, VisitorPhase.Passing)
            || SittingBalance.ShouldCatch(true, VisitorPhase.Warning)
            || SittingBalance.ShouldCatch(true, VisitorPhase.Empty))
        {
            Console.Error.WriteLine("Catch condition was too broad.");
            return 1;
        }

        if (!SittingBalance.IsExhausted(0f) || SittingBalance.IsExhausted(0.1f))
        {
            Console.Error.WriteLine("Exhaustion threshold was wrong.");
            return 1;
        }

        var rng = new Random(1820);
        for (var i = 0; i < 100; i++)
        {
            var gap = SittingBalance.NextVisitorGap(rng);
            if (gap < SittingBalance.MinVisitorGapSeconds || gap > SittingBalance.MaxVisitorGapSeconds)
            {
                Console.Error.WriteLine($"Visitor gap out of range: {gap}.");
                return 1;
            }
        }

        Console.WriteLine("Sitting rules verified.");
        return 0;
    }
}
CS

"$mono" "$csc" -nologo -nostdlib -out:"$tmpdir/VerifySittingRules.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifySittingRules.cs"

"$mono" "$tmpdir/VerifySittingRules.exe"

echo "Sitting MVP compile verification passed."
