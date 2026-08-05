using System.IO;
using MannLab.Games.Game2048Crash;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MannLab.Games.Game2048Crash.EditorTools
{
    public static class CreateGameScene
    {
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(250, 247, 239, 255);
            camera.orthographic = true;

            new GameObject("2048 Crash Game", typeof(Crash2048Controller));

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Game.unity");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Game.unity", true)
            };

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = "2048 Crash";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mannlab.games.game2048crash");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.mannlab.games.game2048crash");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
