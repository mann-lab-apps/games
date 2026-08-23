#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_root="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents"
mono="$unity_root/Resources/Scripting/MonoBleedingEdge/bin/mono"
csc="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/4.5/csc.exe"
managed="$unity_root/Resources/Scripting/Managed"
unity="$managed/UnityEngine"
mono_lib="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/unityjit-macos"
ugui="$unity_root/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-2d-6.1.6/ScriptAssemblies/UnityEngine.UI.dll"
project="$repo_root/prototypes/standing"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/StandingRuntime.dll"
editor_dll="$tmpdir/StandingEditor.dll"

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
  "$project"/Assets/_Project/Editor/CreateGameScene.cs \
  "$project"/Assets/_Project/Editor/BuildWebGL.cs

cat > "$tmpdir/VerifyStandingRules.cs" <<'CS'
using System;
using MannLab.Games.Standing;

public static class VerifyStandingRules
{
    public static int Main()
    {
        var standing = StandingBalance.TickHealth(100f, false, 2f);
        if (standing >= 100f || standing <= 0f)
        {
            Console.Error.WriteLine($"Standing drain was unexpected: {standing}.");
            return 1;
        }

        var sitting = StandingBalance.TickHealth(40f, true, 1.5f);
        if (sitting <= 40f || sitting > StandingBalance.MaxHealth)
        {
            Console.Error.WriteLine($"Sitting recovery was unexpected: {sitting}.");
            return 1;
        }

        if (!StandingBalance.ShouldCatch(true, VisitorPhase.Passing))
        {
            Console.Error.WriteLine("Sitting during visitor passing should catch the player.");
            return 1;
        }

        if (StandingBalance.ShouldCatch(true, VisitorPhase.Passing, false))
        {
            Console.Error.WriteLine("Sitting during harmless passer should not catch the player.");
            return 1;
        }

        if (StandingBalance.ShouldCatch(false, VisitorPhase.Passing)
            || StandingBalance.ShouldCatch(true, VisitorPhase.Warning)
            || StandingBalance.ShouldCatch(true, VisitorPhase.Empty))
        {
            Console.Error.WriteLine("Catch condition was too broad.");
            return 1;
        }

        if (!StandingBalance.IsExhausted(0f) || StandingBalance.IsExhausted(0.1f))
        {
            Console.Error.WriteLine("Exhaustion threshold was wrong.");
            return 1;
        }

        if (StandingBalance.VisitorDetectionLeftX >= StandingBalance.VisitorDetectionRightX
            || StandingBalance.IsVisitorInDetectionZone(StandingBalance.VisitorDetectionLeftX - 0.01f)
            || !StandingBalance.IsVisitorInDetectionZone(
                (StandingBalance.VisitorDetectionLeftX + StandingBalance.VisitorDetectionRightX) * 0.5f)
            || StandingBalance.IsVisitorInDetectionZone(StandingBalance.VisitorDetectionRightX + 0.01f))
        {
            Console.Error.WriteLine("Visitor detection zone was not aligned to the carpet bounds.");
            return 1;
        }

        var rng = new Random(1820);
        for (var i = 0; i < 100; i++)
        {
            var gap = StandingBalance.NextVisitorGap(rng);
            if (gap < StandingBalance.MinVisitorGapSeconds || gap > StandingBalance.MaxVisitorGapSeconds)
            {
                Console.Error.WriteLine($"Visitor gap out of range: {gap}.");
                return 1;
            }

            var walkSpeed = StandingBalance.NextVisitorWalkSpeed(rng);
            if (walkSpeed < StandingBalance.MinVisitorWalkSpeed
                || walkSpeed > StandingBalance.MaxVisitorWalkSpeed)
            {
                Console.Error.WriteLine($"Visitor walk speed out of range: {walkSpeed}.");
                return 1;
            }

            var lateWalkSpeed = StandingBalance.NextVisitorWalkSpeed(rng, 120f);
            var maxLateWalkSpeed = StandingBalance.MaxVisitorWalkSpeed
                + StandingBalance.MaxVisitorRampBonus
                + StandingBalance.MaxVisitorHurryBonus;
            if (lateWalkSpeed < StandingBalance.MinVisitorWalkSpeed
                || lateWalkSpeed > maxLateWalkSpeed)
            {
                Console.Error.WriteLine($"Late visitor walk speed out of range: {lateWalkSpeed}.");
                return 1;
            }
        }

        Console.WriteLine("Standing rules verified.");
        return 0;
    }
}
CS

"$mono" "$csc" -nologo -nostdlib -out:"$tmpdir/VerifyStandingRules.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifyStandingRules.cs"

"$mono" "$tmpdir/VerifyStandingRules.exe"

echo "Standing MVP compile verification passed."
