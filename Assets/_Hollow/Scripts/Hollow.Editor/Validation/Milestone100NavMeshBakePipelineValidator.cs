using System.Collections.Generic;
using System.IO;
using Hollow.Editor.Generation;
using Hollow.Editor.Navigation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone100NavMeshBakePipelineValidator
    {
        [MenuItem("Hollow/Validation/Validate Milestone 100 NavMesh Bake Pipeline")]
        public static void Validate()
        {
            var failures = CollectFailures();
            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("M100 validation failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log("M100 NavMesh bake pipeline validation passed.");
        }

        public static List<string> CollectFailures()
        {
            var failures = new List<string>();
            if (!File.Exists(Milestone100NavMeshBakePipelineAssetGenerator.DocsPath))
            {
                failures.Add($"Missing docs: {Milestone100NavMeshBakePipelineAssetGenerator.DocsPath}.");
            }

            if (!File.Exists(Milestone100NavMeshBakePipelineAssetGenerator.ReportPath))
            {
                failures.Add($"Missing report: {Milestone100NavMeshBakePipelineAssetGenerator.ReportPath}.");
            }

            var runtimeBuilderSource = "Assets/_Hollow/Scripts/Hollow.Rooms/RoomRuntimeNavMeshBuilder.cs";
            if (File.Exists(runtimeBuilderSource) && !File.ReadAllText(runtimeBuilderSource).Contains("RoomNavMeshBuildUtility.BuildRoom", System.StringComparison.Ordinal))
            {
                failures.Add("RoomRuntimeNavMeshBuilder must delegate to RoomNavMeshBuildUtility.");
            }

            var editorBakerSource = "Assets/_Hollow/Scripts/Hollow.Editor/Navigation/RoomNavMeshBakeUtility.cs";
            if (File.Exists(editorBakerSource) && !File.ReadAllText(editorBakerSource).Contains("RoomNavMeshBuildUtility.BuildRoom", System.StringComparison.Ordinal))
            {
                failures.Add("RoomNavMeshBakeUtility must delegate to RoomNavMeshBuildUtility.");
            }

            foreach (var root in RoomNavMeshBakeUtility.RuntimeRoomRoots)
            {
                if (!Directory.Exists(root))
                {
                    failures.Add($"Runtime room source root is missing: {root}.");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RoomNavMeshCatalogDefinition>(RoomNavMeshBakeUtility.CatalogAssetPath);
            if (catalog == null)
            {
                failures.Add($"Missing RoomNavMeshCatalog: {RoomNavMeshBakeUtility.CatalogAssetPath}. Run {RoomNavMeshBakeUtility.PreferredBakeMenuPath}.");
            }
            else
            {
                foreach (var roomPath in RoomNavMeshBakeUtility.CollectRuntimeRoomPaths())
                {
                    if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(roomPath), out var room, out var importError))
                    {
                        failures.Add($"Cannot import runtime room for NavMesh validation '{roomPath}': {importError}.");
                        continue;
                    }

                    if (!catalog.TryGetNavMeshData(room.Id, out var navMeshData) || navMeshData == null)
                    {
                        failures.Add($"Missing NavMesh bake for '{room.Id}' from '{roomPath}'. Run {RoomNavMeshBakeUtility.PreferredBakeMenuPath}.");
                    }
                }
            }

            return failures;
        }
    }
}
