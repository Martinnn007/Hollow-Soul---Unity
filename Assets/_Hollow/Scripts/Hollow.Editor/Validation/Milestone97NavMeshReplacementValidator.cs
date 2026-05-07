using System.Collections.Generic;
using System.IO;
using Hollow.Combat;
using Hollow.Editor.Generation;
using Hollow.Editor.Navigation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone97NavMeshReplacementValidator
    {
        [MenuItem("Hollow/Validation/Validate Milestone 97 Unity NavMesh Replacement")]
        public static void Validate()
        {
            var failures = CollectFailures();
            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("M97 validation failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log("M97 Unity NavMesh replacement validation passed.");
        }

        public static List<string> CollectFailures()
        {
            var failures = new List<string>();
            var currentBackend = (EnemyNavigationBackend)System.Enum.Parse(
                typeof(EnemyNavigationBackend),
                EnemyNavigationAdapter.CurrentBackend.ToString());
            if (currentBackend != EnemyNavigationBackend.UnityNavMesh)
            {
                failures.Add("EnemyNavigationAdapter.CurrentBackend must be UnityNavMesh.");
            }

            var adapterPath = "Assets/_Hollow/Scripts/Hollow.Combat/EnemyNavigationAdapter.cs";
            if (File.Exists(adapterPath) && File.ReadAllText(adapterPath).Contains("RoomGridAStarPathfinder", System.StringComparison.Ordinal))
            {
                failures.Add("EnemyNavigationAdapter must not call or reference RoomGridAStarPathfinder in M97.");
            }

            if (!File.Exists(Milestone97NavMeshReplacementAssetGenerator.DocsPath))
            {
                failures.Add($"Missing docs: {Milestone97NavMeshReplacementAssetGenerator.DocsPath}.");
            }

            if (!File.Exists(Milestone97NavMeshReplacementAssetGenerator.ReportPath))
            {
                failures.Add($"Missing report: {Milestone97NavMeshReplacementAssetGenerator.ReportPath}.");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RoomNavMeshCatalogDefinition>(RoomNavMeshBakeUtility.CatalogAssetPath);
            if (catalog == null)
            {
                failures.Add($"Missing RoomNavMeshCatalog: {RoomNavMeshBakeUtility.CatalogAssetPath}.");
            }
            else
            {
                if (catalog.Entries.Count == 0)
                {
                    failures.Add("RoomNavMeshCatalog must contain at least one approved room entry.");
                }

                if (!catalog.TryGetNavMeshData("Room_Small_RatRoom_001", out var ratRoomNavMesh) || ratRoomNavMesh == null)
                {
                    failures.Add("Room_Small_RatRoom_001 must resolve a baked NavMeshData entry.");
                }
            }

            return failures;
        }
    }
}
