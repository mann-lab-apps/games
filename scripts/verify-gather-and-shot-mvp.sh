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
asset_dir="$project/Assets/Resources/GatherAndShot"
controller="$project/Assets/_Project/Scripts/GatherAndShotController.cs"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
shared_ads_runtime="$repo_root/shared/unity-packages/com.mannlab.admob-core/Runtime"
ios_xcode="$unity_root/PlaybackEngines/MacStandaloneSupport/UnityEditor.iOS.Extensions.Xcode.dll"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/GatherAndShotRuntime.dll"
editor_dll="$tmpdir/GatherAndShotEditor.dll"

for asset in \
  player.png \
  player_idle.png \
  player_move.png \
  player_gather.png \
  player_throw.png \
  player_hit.png \
  walker.png \
  runner.png \
  heavy.png
do
  if [[ ! -s "$asset_dir/$asset" ]]; then
    echo "Missing Stop & Snow character asset: $asset_dir/$asset" >&2
    exit 1
  fi
done

for expected in \
  "CreateUpgradeIcon" \
  "ShowZoneBanner" \
  "PACKED THROW" \
  "GIANT THROW" \
  "Stop: build a GIANT snowball"
do
  if ! grep -q "$expected" "$controller"; then
    echo "Missing Stop & Snow differentiation marker in controller: $expected" >&2
    exit 1
  fi
done

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
  -r:"$ios_xcode" \
  -r:"$runtime_dll" \
  "$project"/Assets/_Project/Editor/*.cs

cat > "$tmpdir/VerifyGatherAndShotRules.cs" <<'CS'
using System;
using MannLab.Games.GatherAndShot;

public static class VerifyGatherAndShotRules
{
    public static int Main()
    {
        var maxAmmo = GatherAndShotBalance.MaxAmmoForLevel(0);
        if (maxAmmo != 10 || GatherAndShotBalance.MaxAmmoForLevel(2) <= maxAmmo)
        {
            Console.Error.WriteLine("Ammo capacity growth drifted from the market loop design.");
            return 1;
        }

        if (GatherAndShotBalance.StationaryGatherDelaySeconds <= 0f
            || GatherAndShotBalance.StationaryGatherAmmo != 1
            || GatherAndShotBalance.StationaryGatherCycleSeconds(0f, 0) <= GatherAndShotBalance.StationaryGatherCycleSeconds(180f, 0)
            || GatherAndShotBalance.StationaryGatherCycleSeconds(0f, 0) <= GatherAndShotBalance.StationaryGatherCycleSeconds(0f, 2))
        {
            Console.Error.WriteLine("Stationary snow gathering should be active and improve with time/upgrades.");
            return 1;
        }

        if (GatherAndShotBalance.PickupAmmo("Ball") != 2
            || GatherAndShotBalance.PickupAmmo("Drift") != 4
            || GatherAndShotBalance.PickupAmmo("BigSnowdrift") != 6
            || GatherAndShotBalance.PickupAmmo("IcySnowdrift") != 5
            || GatherAndShotBalance.PickupAmmo(PickupKind.BigSnowdrift) != 6
            || GatherAndShotBalance.PickupAmmo(PickupKind.WeaponCache) < 1)
        {
            Console.Error.WriteLine("Snow resource and emergency bonus pickup ammo values are wrong.");
            return 1;
        }

        if (!GatherAndShotBalance.IsSnowResource(PickupKind.Snowdrift)
            || !GatherAndShotBalance.IsSnowResource(PickupKind.BigSnowdrift)
            || !GatherAndShotBalance.IsSnowResource(PickupKind.IcySnowdrift)
            || GatherAndShotBalance.IsSnowResource(PickupKind.WeaponCache))
        {
            Console.Error.WriteLine("Snow resource pickup kinds are not configured.");
            return 1;
        }

        if (GatherAndShotBalance.SnowResourceCapacity(PickupKind.Snowdrift) != 3
            || GatherAndShotBalance.SnowResourceCapacity(PickupKind.BigSnowdrift) <= GatherAndShotBalance.SnowResourceCapacity(PickupKind.Snowdrift)
            || GatherAndShotBalance.SnowResourceGatherMultiplier(PickupKind.BigSnowdrift) >= 1f
            || GatherAndShotBalance.SnowResourceCoinReward(PickupKind.IcySnowdrift) <= 0)
        {
            Console.Error.WriteLine("Snow resource capacity, gather speed, and depletion rewards are not configured.");
            return 1;
        }

        if (GatherAndShotBalance.PickupRadius(PickupKind.BigSnowdrift) <= GatherAndShotBalance.PickupRadius(PickupKind.Snowball)
            || GatherAndShotBalance.PickupRadius(PickupKind.Snowball, 2) <= GatherAndShotBalance.PickupRadius(PickupKind.Snowball, 0))
        {
            Console.Error.WriteLine("Big snowdrifts should have stronger interaction range than single snowballs.");
            return 1;
        }

        if (GatherAndShotBalance.MaxLivePickups > 5
            || GatherAndShotBalance.PickupSpawnGapMin(0f) < 5f
            || GatherAndShotBalance.PickupSpawnGapMax(0f) < GatherAndShotBalance.PickupSpawnGapMin(0f))
        {
            Console.Error.WriteLine("Snow resource and emergency pickup pressure is too generous for the stationary-gather design.");
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

        var damaged = GatherAndShotBalance.ApplyContactDamage(GatherAndShotBalance.BaseMaxWarmth);
        if (damaged >= GatherAndShotBalance.BaseMaxWarmth || damaged <= 0f)
        {
            Console.Error.WriteLine($"Contact damage was unexpected: {damaged}.");
            return 1;
        }

        if (GatherAndShotBalance.MaxWarmth(2) <= GatherAndShotBalance.BaseMaxWarmth
            || GatherAndShotBalance.FireCooldownSeconds(2, false) >= GatherAndShotBalance.FireCooldownSeconds(0, false)
            || GatherAndShotBalance.FireCooldownSeconds(0, true) >= GatherAndShotBalance.FireCooldownSeconds(0, false)
            || GatherAndShotBalance.SnowballDamage(2) <= GatherAndShotBalance.SnowballDamage(0))
        {
            Console.Error.WriteLine("Upgrade effects should improve warmth, throw rate, rapid fire, and damage.");
            return 1;
        }

        if (GatherAndShotBalance.UpgradeCost(UpgradeKind.AmmoCapacity, 1) <= GatherAndShotBalance.UpgradeCost(UpgradeKind.AmmoCapacity, 0)
            || GatherAndShotBalance.EnemyCoinReward(EnemyKind.Heavy) <= GatherAndShotBalance.EnemyCoinReward(EnemyKind.Walker)
            || GatherAndShotBalance.BonusChestCoins(3) <= 0)
        {
            Console.Error.WriteLine("Snow Coin economy values are not increasing correctly.");
            return 1;
        }

        if (GatherAndShotBalance.WaveStage(30f) != 1
            || GatherAndShotBalance.WaveStage(75f) != 2
            || GatherAndShotBalance.WaveStage(150f) != 3
            || GatherAndShotBalance.WaveStage(260f) != 4)
        {
            Console.Error.WriteLine("First five-minute wave staging is wrong.");
            return 1;
        }

        if (GatherAndShotBalance.WeaponDurationSeconds(WeaponKind.BigSnowball) <= 0f
            || GatherAndShotBalance.WeaponDurationSeconds(WeaponKind.SplitSnowball) <= 0f
            || GatherAndShotBalance.WeaponDurationSeconds(WeaponKind.IceShot) <= 0f
            || GatherAndShotBalance.WeaponDurationSeconds(WeaponKind.SnowBurst) <= 0f)
        {
            Console.Error.WriteLine("Weapon variation durations are not configured.");
            return 1;
        }

        var missionNames = Enum.GetNames(typeof(MissionKind));
        if (missionNames.Length != 5
            || missionNames[0] != "FirstSnowLoop"
            || missionNames[4] != "DefeatHeavy")
        {
            Console.Error.WriteLine("Early mission chain ordering is not configured.");
            return 1;
        }

        var zoneNames = Enum.GetNames(typeof(SnowZoneKind));
        if (zoneNames.Length != 5
            || zoneNames[0] != "FrostYard"
            || zoneNames[1] != "SnowPatchField"
            || zoneNames[2] != "RedScarfLane"
            || zoneNames[3] != "HeavySnowbank"
            || zoneNames[4] != "BlizzardGate")
        {
            Console.Error.WriteLine("Stop & Snow map zone chain is not configured.");
            return 1;
        }

        var snowballStages = Enum.GetNames(typeof(SnowballBuildStage));
        if (snowballStages.Length != 4
            || snowballStages[0] != "None"
            || snowballStages[1] != "Small"
            || snowballStages[2] != "Packed"
            || snowballStages[3] != "Giant")
        {
            Console.Error.WriteLine("Stop & Snow build-snowball stages are not configured.");
            return 1;
        }

        if (GatherAndShotBalance.SnowballBuildSeconds(SnowballBuildStage.Small, 0, 0) >= GatherAndShotBalance.SnowballBuildSeconds(SnowballBuildStage.Packed, 0, 0)
            || GatherAndShotBalance.SnowballBuildSeconds(SnowballBuildStage.Packed, 0, 0) >= GatherAndShotBalance.SnowballBuildSeconds(SnowballBuildStage.Giant, 0, 0)
            || GatherAndShotBalance.SnowballBuildSeconds(SnowballBuildStage.Packed, 2, 2) >= GatherAndShotBalance.SnowballBuildSeconds(SnowballBuildStage.Packed, 0, 0)
            || GatherAndShotBalance.SnowballBuildDamageBonus(SnowballBuildStage.Giant) <= GatherAndShotBalance.SnowballBuildDamageBonus(SnowballBuildStage.Packed)
            || GatherAndShotBalance.SnowballBuildName(SnowballBuildStage.Giant) != "Giant")
        {
            Console.Error.WriteLine("Build-snowball timing, upgrade scaling, or damage bonuses are wrong.");
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

        Console.WriteLine("Stop & Snow rules verified.");
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

echo "Stop & Snow MVP compile verification passed."
