using System.IO;
using MannLab.Games.Rainwalker;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MannLab.Games.Rainwalker.EditorTools
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
            camera.backgroundColor = new Color32(248, 245, 235, 255);
            camera.orthographic = true;

            new GameObject("Rainwalker Game", typeof(RainwalkerController));

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Game.unity");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Game.unity", true)
            };

            PlayerSettings.companyName = "Mann Lab";
            PlayerSettings.productName = "Rainwalker";
            PlayerSettings.bundleVersion = "0.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mannlab.games.rainwalker");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.mannlab.games.rainwalker");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
