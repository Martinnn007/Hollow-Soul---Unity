using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Hollow.Editor.Build
{
    [InitializeOnLoad]
    public static class AddressablesBuildReportSafety
    {
        static AddressablesBuildReportSafety()
        {
            DisableAddressablesBuildReportVisualizer(logWhenChanged: true);
        }

        [MenuItem("Hollow/Platform QA/Disable Addressables Build Report Visualizer")]
        public static void DisableAddressablesBuildReportVisualizerMenu()
        {
            DisableAddressablesBuildReportVisualizer(logWhenChanged: true);
        }

        public static void DisableAddressablesBuildReportVisualizer(bool logWhenChanged)
        {
            if (!ProjectConfigData.GenerateBuildLayout)
            {
                return;
            }

            ProjectConfigData.GenerateBuildLayout = false;
            ProjectConfigData.ClearBuildReportFilePaths();

            if (logWhenChanged)
            {
                Debug.Log("Hollow disabled Addressables Debug Build Layout to avoid the Unity Addressables Build Report Visualizer null-reference during local/player builds.");
            }
        }
    }
}
