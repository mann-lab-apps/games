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
ios_xcode="/Applications/Unity/Hub/Editor/6000.3.20f1/PlaybackEngines/iOSSupport/UnityEditor.iOS.Extensions.Xcode.dll"
project="$repo_root/prototypes/10000"
shared_runtime="$repo_root/shared/unity-packages/com.mannlab.hypercasual-core/Runtime"
firebase_plugins="$repo_root/shared/unity-packages/com.mannlab.firebase-unity-sdk/Firebase/Plugins"
tmpdir="$(mktemp -d)"

runtime_dll="$tmpdir/Game10000Runtime.dll"
editor_dll="$tmpdir/Game10000Editor.dll"

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
  -r:"$firebase_plugins/Firebase.App.dll" \
  -r:"$firebase_plugins/Firebase.Analytics.dll" \
  -r:"$firebase_plugins/Firebase.Crashlytics.dll" \
  -r:"$firebase_plugins/Firebase.Platform.dll" \
  -r:"$firebase_plugins/Firebase.TaskExtension.dll" \
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
  -r:"$unity/UnityEngine.UIModule.dll" \
  -r:"$unity/UnityEngine.TextRenderingModule.dll" \
  -r:"$unity/UnityEngine.InputLegacyModule.dll" \
  -r:"$unity/UnityEngine.IMGUIModule.dll" \
  -r:"$ugui" \
  -r:"$ios_xcode" \
  -r:"$runtime_dll" \
  "$project"/Assets/_Project/Editor/*.cs

cat > "$tmpdir/VerifyBoardGenerator.cs" <<'CS'
using System;
using MannLab.Games.Game10000;

public static class VerifyBoardGenerator
{
    public static int Main()
    {
        for (var seed = 0; seed < 1000; seed++)
        {
            var board = new BoardGenerator(seed).Generate();
            if (board.TargetIndices.Count < 5)
            {
                Console.Error.WriteLine($"Seed {seed} generated too few target cells: {board.TargetIndices.Count}");
                return 1;
            }
        }

        Console.WriteLine("BoardGenerator verified for 1000 deterministic seeds.");
        return 0;
    }
}
CS

"$mono" "$csc" -nologo -nostdlib -out:"$tmpdir/VerifyBoardGenerator.exe" \
  -r:"$mono_lib/mscorlib.dll" \
  -r:"$mono_lib/System.dll" \
  -r:"$mono_lib/System.Core.dll" \
  -r:"$mono_lib/Facades/netstandard.dll" \
  -r:"$runtime_dll" \
  "$tmpdir/VerifyBoardGenerator.cs"

"$mono" "$tmpdir/VerifyBoardGenerator.exe"

echo "10000 MVP compile verification passed."
