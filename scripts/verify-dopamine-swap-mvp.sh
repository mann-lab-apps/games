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
project="$repo_root/prototypes/dopamine-swap"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/DopamineSwapRuntime.dll"
editor_dll="$tmpdir/DopamineSwapEditor.dll"

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

cat > "$tmpdir/VerifyDopamineSwapRules.cs" <<'CS'
using System;
using MannLab.Games.DopamineSwap;

public static class VerifyDopamineSwapRules
{
    public static int Main()
    {
        var rng = new Random(1208);
        var previousTimeLimit = float.MaxValue;
        for (var round = 1; round <= 40; round++)
        {
            var data = DopamineRoundRules.CreateRound(round, rng);
            if (data.PlayerCards.Length != DopamineRoundRules.CardCount)
            {
                Console.Error.WriteLine($"Round {round} produced {data.PlayerCards.Length} cards.");
                return 1;
            }

            var hasWinningCard = false;
            for (var i = 0; i < data.PlayerCards.Length; i++)
            {
                var card = data.PlayerCards[i];
                if (card < DopamineRoundRules.MinScore || card > DopamineRoundRules.MaxScore)
                {
                    Console.Error.WriteLine($"Round {round} card out of range: {card}.");
                    return 1;
                }

                for (var j = 0; j < i; j++)
                {
                    if (card == data.PlayerCards[j])
                    {
                        Console.Error.WriteLine($"Round {round} has duplicate card score: {card}.");
                        return 1;
                    }
                }

                hasWinningCard |= card > data.OpponentScore;
            }

            if (!hasWinningCard)
            {
                Console.Error.WriteLine($"Round {round} has no winning card against {data.OpponentScore}.");
                return 1;
            }

            if (!data.VisibleRange.Contains(data.OpponentScore))
            {
                Console.Error.WriteLine($"Round {round} visible range {data.VisibleRange} does not include {data.OpponentScore}.");
                return 1;
            }

            if (data.RevealsOpponentScore != (round <= DopamineRoundRules.RevealedOpponentRounds))
            {
                Console.Error.WriteLine($"Round {round} reveal mode is wrong.");
                return 1;
            }

            if (data.TimeLimitSeconds > previousTimeLimit)
            {
                Console.Error.WriteLine($"Round {round} time limit regressed upward.");
                return 1;
            }

            previousTimeLimit = data.TimeLimitSeconds;
        }

        Console.WriteLine("Dopamine Swap round rules verified through round 40.");
        return 0;
    }
}
CS

"$mono" "$csc" -nologo -nostdlib -out:"$tmpdir/VerifyDopamineSwapRules.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifyDopamineSwapRules.cs"

"$mono" "$tmpdir/VerifyDopamineSwapRules.exe"

echo "Dopamine Swap MVP compile verification passed."
