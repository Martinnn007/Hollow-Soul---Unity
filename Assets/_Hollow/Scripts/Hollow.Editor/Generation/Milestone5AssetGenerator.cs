using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone5AssetGenerator
    {
        [MenuItem("Hollow/Generation/Generate Milestone 5 Assets")]
        public static void Generate()
        {
            Milestone4AssetGenerator.Generate();
            Debug.Log("Generated Hollow Milestone 5 enemy archetype, difficulty, and diagnostics assets.");
        }
    }
}
