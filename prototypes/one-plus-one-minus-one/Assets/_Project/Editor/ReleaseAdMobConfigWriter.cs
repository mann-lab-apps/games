using System.IO;
using System.Text;
using UnityEditor;

namespace MannLab.Games.OnePlusOneMinusOne.EditorTools
{
    public static class ReleaseAdMobConfigWriter
    {
        private const string ResourceDirectory = "Assets/_Project/Resources";
        private const string ConfigAssetPath = ResourceDirectory + "/OnePlusOneMinusOneReleaseAdMob.txt";

        public static bool Write(string iosInterstitialAdUnitId, string androidInterstitialAdUnitId)
        {
            if (string.IsNullOrWhiteSpace(iosInterstitialAdUnitId) &&
                string.IsNullOrWhiteSpace(androidInterstitialAdUnitId))
            {
                Delete();
                return false;
            }

            Directory.CreateDirectory(ResourceDirectory);
            var contents = new StringBuilder();
            contents.AppendLine("# Generated at build time. Do not commit filled production ad unit IDs.");
            contents.Append("ios_interstitial=").AppendLine(iosInterstitialAdUnitId ?? string.Empty);
            contents.Append("android_interstitial=").AppendLine(androidInterstitialAdUnitId ?? string.Empty);
            File.WriteAllText(ConfigAssetPath, contents.ToString());
            AssetDatabase.ImportAsset(ConfigAssetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        public static void Delete()
        {
            if (File.Exists(ConfigAssetPath))
            {
                File.Delete(ConfigAssetPath);
            }

            var metaPath = ConfigAssetPath + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            AssetDatabase.Refresh();
        }
    }
}
