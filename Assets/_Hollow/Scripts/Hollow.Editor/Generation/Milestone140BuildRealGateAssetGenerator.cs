using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone140BuildRealGateAssetGenerator
    {
        [MenuItem("Hollow/Generation/Generate Milestone 140 Assets")]
        public static void GenerateAssets()
        {
            EnsureProfile();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated M140 build-real gate assets.");
        }

        public static M140BuildRealGateProfileDefinition EnsureProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<M140BuildRealGateProfileDefinition>(M140BuildRealGateRunner.ProfilePath);
            if (profile != null)
            {
                return profile;
            }

            var directory = Path.GetDirectoryName(M140BuildRealGateRunner.ProfilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            profile = ScriptableObject.CreateInstance<M140BuildRealGateProfileDefinition>();
            AssetDatabase.CreateAsset(profile, M140BuildRealGateRunner.ProfilePath);
            return profile;
        }
    }
}
