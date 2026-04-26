using UnityEditor;

namespace Hollow.Editor.Generation
{
    public static class Milestone2AssetGenerator
    {
        [MenuItem("Hollow/Generation/Generate Milestone 2 Assets")]
        public static void Generate()
        {
            Milestone1AssetGenerator.Generate();
        }
    }
}
