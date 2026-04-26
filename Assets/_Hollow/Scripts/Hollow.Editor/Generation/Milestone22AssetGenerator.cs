using Hollow.RoomDesigner;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone22AssetGenerator
    {
        [MenuItem("Hollow/Generation/Generate Milestone 22 Assets")]
        public static void Generate()
        {
            Milestone21AssetGenerator.Generate();
            foreach (var preset in System.Enum.GetValues(typeof(RoomDesignerFootprintPreset)))
            {
                _ = RoomDesignerProject.CreateDefault((RoomDesignerFootprintPreset)preset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 22 room designer macro authoring polish assets.");
        }
    }
}
