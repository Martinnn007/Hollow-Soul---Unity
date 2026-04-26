using Hollow.Branches;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone21AssetGenerator
    {
        [MenuItem("Hollow/Generation/Generate Milestone 21 Assets")]
        public static void Generate()
        {
            Milestone20AssetGenerator.Generate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 21 shop card UI assets and retained M20 scene wiring.");
        }
    }
}
