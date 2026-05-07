using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Hollow.Editor.Navigation
{
    public static class RoomNavMeshBakeUtility
    {
        public const string ApprovedRoomsRoot = "Assets/_Hollow/Data/Rooms/DesignerApproved";
        public const string MacroFixturesRoot = "Assets/_Hollow/Data/Rooms/MacroFixtures";
        public const string DeveloperLabRoomsRoot = "Assets/_Hollow/Data/Rooms/DeveloperLab";
        public const string OutputFolder = "Assets/_Hollow/Data/NavMesh/Rooms";
        public const string CatalogAssetPath = "Assets/_Hollow/Resources/Navigation/RoomNavMeshCatalog.asset";
        public const string PreferredBakeMenuPath = "Hollow/Navigation/Bake Runtime Room NavMeshes";

        public static readonly string[] RuntimeRoomRoots =
        {
            ApprovedRoomsRoot,
            MacroFixturesRoot,
            DeveloperLabRoomsRoot
        };

        [MenuItem(PreferredBakeMenuPath)]
        public static void BakeRuntimeRoomNavMeshes()
        {
            var entries = new List<RoomNavMeshCatalogEntry>();
            Directory.CreateDirectory(OutputFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogAssetPath) ?? "Assets/_Hollow/Resources/Navigation");

            foreach (var roomPath in CollectRuntimeRoomPaths())
            {
                if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(roomPath), out var room, out var importError))
                {
                    Debug.LogError($"NavMesh bake skipped '{roomPath}': {importError}");
                    continue;
                }

                var navMeshData = BakeRoom(room);
                if (navMeshData == null)
                {
                    Debug.LogError($"NavMesh bake failed for '{room.Id}'.");
                    continue;
                }

                var safeRoomId = MakeSafeAssetName(room.Id);
                var assetPath = $"{OutputFolder}/{safeRoomId}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(navMeshData, assetPath);
                    existing = navMeshData;
                }
                else
                {
                    EditorUtility.CopySerialized(navMeshData, existing);
                    EditorUtility.SetDirty(existing);
                }

                entries.Add(new RoomNavMeshCatalogEntry(room.Id, existing));
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RoomNavMeshCatalogDefinition>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RoomNavMeshCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            catalog.Configure(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RoomNavMeshCatalogDefinition.ResetRuntimeCacheForTests();
            Debug.Log($"Baked {entries.Count} runtime room NavMeshes into {CatalogAssetPath}. Agent settings: {RoomNavMeshBuildUtility.DescribeAgentSettings()}.");
        }

        [MenuItem("Hollow/Navigation/Bake Approved Room NavMeshes")]
        public static void BakeApprovedRoomNavMeshes()
        {
            BakeRuntimeRoomNavMeshes();
        }

        public static IReadOnlyList<string> CollectRuntimeRoomPaths()
        {
            return RuntimeRoomRoots
                .Where(Directory.Exists)
                .SelectMany(root => Directory.GetFiles(root, "*.hollowruntime.json", SearchOption.AllDirectories))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        public static NavMeshData BakeRoom(ImportedRoomRuntimeAsset room)
        {
            return RoomNavMeshBuildUtility.BuildRoom(room, "NavMesh", out _);
        }

        public static bool TryDescribeMissingBake(ImportedRoomRuntimeAsset room, out string message)
        {
            var roomId = room?.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roomId))
            {
                message = "Room has no canonical id, so it cannot be checked against the NavMesh catalog.";
                return true;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<RoomNavMeshCatalogDefinition>(CatalogAssetPath);
            if (catalog == null)
            {
                message = $"Missing NavMesh catalog at {CatalogAssetPath}. Run {PreferredBakeMenuPath}.";
                return true;
            }

            if (!catalog.TryGetNavMeshData(roomId, out var navMeshData) || navMeshData == null)
            {
                message = $"Room '{roomId}' is missing a prebaked NavMesh entry in {CatalogAssetPath}. Run {PreferredBakeMenuPath}. Source roots: {string.Join(", ", RuntimeRoomRoots)}.";
                return true;
            }

            message = string.Empty;
            return false;
        }

        private static string MakeSafeAssetName(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return "room_navmesh";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var chars = roomId.ToCharArray();
            for (var index = 0; index < chars.Length; index++)
            {
                if (Array.IndexOf(invalid, chars[index]) >= 0)
                {
                    chars[index] = '_';
                }
            }

            return new string(chars);
        }
    }
}
