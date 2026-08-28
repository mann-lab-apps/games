#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_root="/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents"
mono="$unity_root/Resources/Scripting/MonoBleedingEdge/bin/mono"
csc="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/4.5/csc.exe"
managed="$unity_root/Resources/Scripting/Managed"
unity="$managed/UnityEngine"
mono_lib="$unity_root/Resources/Scripting/MonoBleedingEdge/lib/mono/unityjit-macos"
ugui="$unity_root/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.2d-cross-platform-2d-6.1.6/ScriptAssemblies/UnityEngine.UI.dll"
project="$repo_root/prototypes/gather-and-shot"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
shared_ads_runtime="$repo_root/shared/unity-packages/com.mannlab.admob-core/Runtime"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/GatherAndShotRuntime.dll"
editor_dll="$tmpdir/GatherAndShotEditor.dll"

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
  "$shared_ads_runtime"/*.cs \
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

cat > "$tmpdir/VerifyGatherAndShotRules.cs" <<'CS'
using System;
using MannLab.Games.GatherAndShot;

public static class VerifyGatherAndShotRules
{
    public static int Main()
    {
        var maxAmmo = GatherAndShotBalance.MaxAmmo;
        if (maxAmmo != 8)
        {
            Console.Error.WriteLine("Ammo cap drifted from the MVP design.");
            return 1;
        }

        if (GatherAndShotBalance.PickupAmmo("Ball") != 1
            || GatherAndShotBalance.PickupAmmo("Drift") != 3
            || GatherAndShotBalance.PickupAmmo("BigSnowdrift") != 5
            || GatherAndShotBalance.PickupAmmo(PickupKind.BigSnowdrift) != 5)
        {
            Console.Error.WriteLine("Pickup ammo values are wrong.");
            return 1;
        }

        if (GatherAndShotBalance.PickupRadius(PickupKind.BigSnowdrift) <= GatherAndShotBalance.PickupRadius(PickupKind.Snowball))
        {
            Console.Error.WriteLine("Big snowdrifts should be easier to pick up than single snowballs.");
            return 1;
        }

        if (GatherAndShotBalance.MaxLivePickups > 12
            || GatherAndShotBalance.PickupSpawnGapMin(0f) < 1f
            || GatherAndShotBalance.PickupSpawnGapMax(0f) < GatherAndShotBalance.PickupSpawnGapMin(0f))
        {
            Console.Error.WriteLine("Snow pickup spawn pressure is too generous for the gathering-risk design.");
            return 1;
        }

        if (GatherAndShotBalance.PickupGatherSeconds(PickupKind.Snowball) <= 0f
            || GatherAndShotBalance.PickupGatherSeconds(PickupKind.Snowdrift) <= GatherAndShotBalance.PickupGatherSeconds(PickupKind.Snowball)
            || GatherAndShotBalance.PickupGatherSeconds(PickupKind.BigSnowdrift) <= GatherAndShotBalance.PickupGatherSeconds(PickupKind.Snowdrift))
        {
            Console.Error.WriteLine("Pickup gathering risk should scale with snow size.");
            return 1;
        }

        if (GatherAndShotBalance.StartingHealth(EnemyKind.Walker) != 1
            || GatherAndShotBalance.StartingHealth(EnemyKind.Runner) != 1
            || GatherAndShotBalance.StartingHealth(EnemyKind.Heavy) <= 1)
        {
            Console.Error.WriteLine("Enemy health values do not match the MVP enemy roles.");
            return 1;
        }

        if (GatherAndShotBalance.EnemyBaseSpeed(EnemyKind.Runner) <= GatherAndShotBalance.EnemyBaseSpeed(EnemyKind.Walker)
            || GatherAndShotBalance.EnemyBaseSpeed(EnemyKind.Heavy) >= GatherAndShotBalance.EnemyBaseSpeed(EnemyKind.Walker))
        {
            Console.Error.WriteLine("Enemy speed ordering should be Runner > Walker > Heavy.");
            return 1;
        }

        if (GatherAndShotBalance.SpawnGap(0f) <= GatherAndShotBalance.SpawnGap(180f)
            || GatherAndShotBalance.MaxLiveEnemies(0f) >= GatherAndShotBalance.MaxLiveEnemies(180f))
        {
            Console.Error.WriteLine("Difficulty spawn ramp is not increasing pressure.");
            return 1;
        }

        if (GatherAndShotBalance.PlayerSpeed(180f) <= GatherAndShotBalance.PlayerSpeed(0f)
            || GatherAndShotBalance.EnemySpeedMultiplier(180f) <= GatherAndShotBalance.EnemySpeedMultiplier(0f))
        {
            Console.Error.WriteLine("Late-game speed ramp is not active.");
            return 1;
        }

        var damaged = GatherAndShotBalance.ApplyContactDamage(GatherAndShotBalance.MaxWarmth);
        if (damaged >= GatherAndShotBalance.MaxWarmth || damaged <= 0f)
        {
            Console.Error.WriteLine($"Contact damage was unexpected: {damaged}.");
            return 1;
        }

        if (!GatherAndShotBalance.IsGameOver(0f) || GatherAndShotBalance.IsGameOver(0.1f))
        {
            Console.Error.WriteLine("Warmth game-over threshold is wrong.");
            return 1;
        }

        var random = new Random(2901);
        var sawWalker = false;
        var sawRunner = false;
        var sawHeavy = false;
        for (var i = 0; i < 200; i++)
        {
            var kind = GatherAndShotBalance.RollEnemyKind(random, 160f);
            sawWalker |= kind == EnemyKind.Walker;
            sawRunner |= kind == EnemyKind.Runner;
            sawHeavy |= kind == EnemyKind.Heavy;
        }

        if (!sawWalker || !sawRunner || !sawHeavy)
        {
            Console.Error.WriteLine("Enemy roll should produce all MVP enemy kinds.");
            return 1;
        }

        Console.WriteLine("Gather & Shot rules verified.");
        return 0;
    }
}
CS

"$mono" "$csc" -nologo -nostdlib -out:"$tmpdir/VerifyGatherAndShotRules.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifyGatherAndShotRules.cs"

"$mono" "$tmpdir/VerifyGatherAndShotRules.exe"

echo "Gather & Shot MVP compile verification passed."
