using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone3AssetGenerator
    {
        private const string SampleRoomRuntimePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [MenuItem("Hollow/Generation/Generate Milestone 3 Assets")]
        public static void Generate()
        {
            AssetDatabase.ImportAsset(SampleRoomRuntimePath, ImportAssetOptions.ForceUpdate);
            Milestone2AssetGenerator.Generate();
            Debug.Log("Generated Hollow Milestone 3 imported sample room scenes and prefabs.");
        }
    }
}
