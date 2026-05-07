using System.Collections.Generic;
using System.IO;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone103DynamicNavigationObjectsValidator
    {
        [MenuItem("Hollow/Validation/Validate Milestone 103 Dynamic Navigation Objects")]
        public static void Validate()
        {
            var failures = CollectFailures();
            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("M103 validation failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log("M103 dynamic navigation objects validation passed.");
        }

        public static List<string> CollectFailures()
        {
            var failures = new List<string>();
            if (!File.Exists(Milestone103DynamicNavigationObjectsAssetGenerator.DocsPath))
            {
                failures.Add($"Missing docs: {Milestone103DynamicNavigationObjectsAssetGenerator.DocsPath}.");
            }

            if (!File.Exists(Milestone103DynamicNavigationObjectsAssetGenerator.ReportPath))
            {
                failures.Add($"Missing report: {Milestone103DynamicNavigationObjectsAssetGenerator.ReportPath}.");
            }

            var markerPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomDynamicNavigationObjectMarker.cs";
            if (!File.Exists(markerPath))
            {
                failures.Add("Missing RoomDynamicNavigationObjectMarker runtime bridge.");
            }
            else
            {
                var marker = File.ReadAllText(markerPath);
                if (!marker.Contains("NavMeshObstacle", System.StringComparison.Ordinal) ||
                    !marker.Contains("SetCarvingActive", System.StringComparison.Ordinal) ||
                    !marker.Contains("StatusSummary", System.StringComparison.Ordinal))
                {
                    failures.Add("RoomDynamicNavigationObjectMarker must safely own carving, status, and debug diagnostics.");
                }
            }

            var rootPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomRuntimeRoot.cs";
            if (File.Exists(rootPath))
            {
                var root = File.ReadAllText(rootPath);
                if (!root.Contains("DynamicNavigationObjects", System.StringComparison.Ordinal) ||
                    !root.Contains("BuildHoleMarkers", System.StringComparison.Ordinal) ||
                    !root.Contains("doorNavigationByPortId", System.StringComparison.Ordinal) ||
                    !root.Contains("ConfigureDoor", System.StringComparison.Ordinal) ||
                    !root.Contains("SetDynamicNavigationDebugLabelsVisible", System.StringComparison.Ordinal))
                {
                    failures.Add("RoomRuntimeRoot must register rocks, holes, doors, and interactive blockers as dynamic navigation objects.");
                }
            }

            var interactivePath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomInteractiveObjectMarker.cs";
            if (File.Exists(interactivePath))
            {
                var interactive = File.ReadAllText(interactivePath);
                if (!interactive.Contains("RoomDynamicNavigationObjectMarker", System.StringComparison.Ordinal) ||
                    !interactive.Contains("dynamicNavigation.MarkDestroyed", System.StringComparison.Ordinal))
                {
                    failures.Add("Interactive object destruction must disable dynamic navigation carving through the M103 marker.");
                }
            }

            var navBuildPath = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomNavMeshBuildUtility.cs";
            if (File.Exists(navBuildPath))
            {
                var navBuild = File.ReadAllText(navBuildPath);
                if (!navBuild.Contains("AppendHoleBlockerSources", System.StringComparison.Ordinal))
                {
                    failures.Add("RoomNavMeshBuildUtility must bake authored holes as not-walkable blockers.");
                }

                if (navBuild.Contains("AppendInteractiveBlockerSources(sources", System.StringComparison.Ordinal))
                {
                    failures.Add("Interactive blockers must be runtime-carved only, not baked into static room NavMesh data.");
                }
            }

            return failures;
        }
    }
}
