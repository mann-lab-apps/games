using System;
using UnityEditor;
using UnityEngine;

namespace MannLab.Games.Game2048Blink.EditorTools
{
    internal static class BlinkRuntimeAssets
    {
        private const string ResourcesPath = "Assets/_Project/Resources";
        private const string UiMaterialPath = ResourcesPath + "/BlinkUiDefault.mat";
        private const string UiShaderName = "MannLab/2048Blink/UIUnlit";

        public static void EnsureUiDefaultMaterial()
        {
            EnsureResourcesFolder();

            var shader = Shader.Find(UiShaderName) ?? Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("2048 Blink requires a UI shader for runtime UI rendering.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(UiMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "BlinkUiDefault"
                };
                AssetDatabase.CreateAsset(material, UiMaterialPath);
                AssetDatabase.SaveAssets();
                return;
            }

            if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
            }
        }

        private static void EnsureResourcesFolder()
        {
            if (AssetDatabase.IsValidFolder(ResourcesPath))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                throw new InvalidOperationException("2048 Blink project assets folder is missing.");
            }

            AssetDatabase.CreateFolder("Assets/_Project", "Resources");
        }
    }
}
