using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone4AssetGenerator
    {
        [MenuItem("Hollow/Generation/Generate Milestone 4 Assets")]
        public static void Generate()
        {
            Milestone3AssetGenerator.Generate();
            Debug.Log("Generated Hollow Milestone 4 playable combat loop scenes and prefabs.");
        }
    }
}
