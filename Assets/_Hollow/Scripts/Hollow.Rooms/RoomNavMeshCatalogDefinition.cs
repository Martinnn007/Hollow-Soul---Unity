using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Hollow.Rooms
{
    [CreateAssetMenu(menuName = "Hollow/Rooms/Room NavMesh Catalog", fileName = "RoomNavMeshCatalog")]
    public sealed class RoomNavMeshCatalogDefinition : ScriptableObject
    {
        public const string ResourcesPath = "Navigation/RoomNavMeshCatalog";
        public const string EditorCatalogAssetPath = "Assets/_Hollow/Resources/Navigation/RoomNavMeshCatalog.asset";
        public const string PreferredBakeMenuPath = "Hollow/Navigation/Bake Runtime Room NavMeshes";

        [SerializeField] private List<RoomNavMeshCatalogEntry> entries = new();

        private static RoomNavMeshCatalogDefinition cachedDefault;

        public IReadOnlyList<RoomNavMeshCatalogEntry> Entries => entries != null ? entries : Array.Empty<RoomNavMeshCatalogEntry>();

        public static RoomNavMeshCatalogDefinition LoadDefault()
        {
            if (cachedDefault == null)
            {
                cachedDefault = Resources.Load<RoomNavMeshCatalogDefinition>(ResourcesPath);
            }

            return cachedDefault;
        }

        public static void ResetRuntimeCacheForTests()
        {
            cachedDefault = null;
        }

        public static string MissingCatalogMessage()
        {
            return $"missing_navmesh_catalog:{ResourcesPath}:asset={EditorCatalogAssetPath}:run={PreferredBakeMenuPath}";
        }

        public static string MissingBakeMessage(string roomId)
        {
            return $"missing_navmesh_bake:{(string.IsNullOrWhiteSpace(roomId) ? "<unknown>" : roomId)}:catalog={EditorCatalogAssetPath}:run={PreferredBakeMenuPath}";
        }

        public bool TryGetNavMeshData(string roomId, out NavMeshData navMeshData)
        {
            return TryGetNavMeshData(roomId, out navMeshData, out _);
        }

        public bool TryGetNavMeshData(string roomId, out NavMeshData navMeshData, out string resolvedRoomId)
        {
            navMeshData = null;
            resolvedRoomId = string.Empty;
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return false;
            }

            if (entries == null)
            {
                return false;
            }

            foreach (var entry in entries)
            {
                if (entry == null ||
                    entry.NavMeshData == null ||
                    !string.Equals(entry.RoomId, roomId, StringComparison.Ordinal))
                {
                    continue;
                }

                navMeshData = entry.NavMeshData;
                resolvedRoomId = entry.RoomId;
                return true;
            }

            if (!TryResolveSharedBakeRoomId(roomId, out var sharedRoomId) ||
                string.Equals(sharedRoomId, roomId, StringComparison.Ordinal))
            {
                return false;
            }

            foreach (var entry in entries)
            {
                if (entry == null ||
                    entry.NavMeshData == null ||
                    !string.Equals(entry.RoomId, sharedRoomId, StringComparison.Ordinal))
                {
                    continue;
                }

                navMeshData = entry.NavMeshData;
                resolvedRoomId = entry.RoomId;
                return true;
            }

            return false;
        }

        public static bool TryResolveSharedBakeRoomId(string roomId, out string sharedRoomId)
        {
            sharedRoomId = string.Empty;
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return false;
            }

            if (roomId is "corrupted_chest_single_1x1" or
                "wave_room_single_1x1" or
                "special_soul_eater_single_1x1" or
                "special_escapist_single_1x1")
            {
                sharedRoomId = "combat_macro_single_1x1";
                return true;
            }

            return TryResolveBetaBiomeMacroBake(roomId, "before_teeth_macro_", out sharedRoomId) ||
                   TryResolveBetaBiomeMacroBake(roomId, "sunken_cartouche_macro_", out sharedRoomId) ||
                   TryResolveBetaBiomeMacroBake(roomId, "rust_choir_macro_", out sharedRoomId);
        }

        private static bool TryResolveBetaBiomeMacroBake(string roomId, string prefix, out string sharedRoomId)
        {
            sharedRoomId = string.Empty;
            if (!roomId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            sharedRoomId = "combat_macro_" + roomId.Substring(prefix.Length);
            return true;
        }

        public void Configure(IEnumerable<RoomNavMeshCatalogEntry> nextEntries)
        {
            entries ??= new List<RoomNavMeshCatalogEntry>();
            entries.Clear();
            if (nextEntries == null)
            {
                return;
            }

            entries.AddRange(nextEntries);
        }
    }

    [Serializable]
    public sealed class RoomNavMeshCatalogEntry
    {
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private NavMeshData navMeshData;

        public RoomNavMeshCatalogEntry(string nextRoomId, NavMeshData nextNavMeshData)
        {
            roomId = nextRoomId ?? string.Empty;
            navMeshData = nextNavMeshData;
        }

        public string RoomId => roomId;

        public NavMeshData NavMeshData => navMeshData;
    }
}
