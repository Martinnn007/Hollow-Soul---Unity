using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone6AssetGenerator
    {
        [MenuItem("Hollow/Generation/Generate Milestone 6 Assets")]
        public static void Generate()
        {
            Milestone5AssetGenerator.Generate();
            Debug.Log("Generated Hollow Milestone 6 branch traversal, minimap, reward, and hub-return assets.");
        }
    }
}
