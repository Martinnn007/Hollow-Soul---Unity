using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Hollow.Rooms
{
    public static class RoomNavMeshBuildUtility
    {
        public const float AgentRadiusMeters = 0.24f;
        public const float AgentHeightMeters = 1.05f;
        public const float AgentClimbMeters = 0.18f;
        public const float AgentSlopeDegrees = 20f;
        public const float MinRegionAreaMeters = 0.25f;
        public const float FloorThicknessMeters = 0.1f;
        public const float StaticBlockerHeightMeters = 1.2f;
        public const float NavMeshSampleRadiusMeters = 1.25f;

        public static NavMeshData BuildRoom(ImportedRoomRuntimeAsset room, string namePrefix, out string error)
        {
            error = string.Empty;
            if (room == null)
            {
                error = "missing_room_asset";
                return null;
            }

            if (room.Layout == null)
            {
                error = $"missing_room_layout:{room.Id}";
                return null;
            }

            var sources = new List<NavMeshBuildSource>();
            AppendFloorSources(sources, room.Layout);
            AppendHoleBlockerSources(sources, room.Layout);
            AppendStaticBlockerSources(sources, room.Layout);

            if (sources.Count == 0)
            {
                error = $"missing_navmesh_sources:{room.Id}";
                return null;
            }

            var data = NavMeshBuilder.BuildNavMeshData(
                BuildSettings(),
                sources,
                BuildBounds(room.Layout),
                Vector3.zero,
                Quaternion.identity);
            if (data == null)
            {
                error = $"navmesh_build_failed:{room.Id}";
                return null;
            }

            data.name = $"{(string.IsNullOrWhiteSpace(namePrefix) ? "NavMesh" : namePrefix)}.{room.Id}";
            return data;
        }

        public static NavMeshBuildSettings BuildSettings()
        {
            var settings = NavMesh.GetSettingsCount() > 0
                ? NavMesh.GetSettingsByIndex(0)
                : NavMesh.CreateSettings();
            settings.agentRadius = AgentRadiusMeters;
            settings.agentHeight = AgentHeightMeters;
            settings.agentClimb = AgentClimbMeters;
            settings.agentSlope = AgentSlopeDegrees;
            settings.minRegionArea = MinRegionAreaMeters;
            return settings;
        }

        public static string DescribeAgentSettings()
        {
            return $"radius={AgentRadiusMeters:0.00}m height={AgentHeightMeters:0.00}m climb={AgentClimbMeters:0.00}m slope={AgentSlopeDegrees:0}deg minRegion={MinRegionAreaMeters:0.00}m";
        }

        private static void AppendFloorSources(List<NavMeshBuildSource> sources, RoomLayout layout)
        {
            if (layout.WalkableTiles != null && layout.WalkableTiles.Count > 0)
            {
                foreach (var tile in layout.WalkableTiles)
                {
                    AddBoxSource(
                        sources,
                        new Vector3(tile.x, -FloorThicknessMeters * 0.5f, tile.y),
                        new Vector3(1f, FloorThicknessMeters, 1f),
                        area: 0);
                }

                return;
            }

            foreach (var region in layout.FloorRegions ?? Array.Empty<RoomLayoutFloorRegion>())
            {
                AddBoxSource(
                    sources,
                    new Vector3(region.Center.x, -FloorThicknessMeters * 0.5f, region.Center.z),
                    new Vector3(region.HalfSize.x * 2f, FloorThicknessMeters, region.HalfSize.y * 2f),
                    area: 0);
            }
        }

        private static void AppendStaticBlockerSources(List<NavMeshBuildSource> sources, RoomLayout layout)
        {
            var notWalkableArea = ResolveNotWalkableArea();
            foreach (var obstacle in layout.Obstacles ?? Array.Empty<RoomLayoutObstacle>())
            {
                var size = obstacle.Size;
                AddBoxSource(
                    sources,
                    new Vector3(obstacle.Center.x, StaticBlockerHeightMeters * 0.5f, obstacle.Center.z),
                    new Vector3(Mathf.Max(0.1f, size.x), StaticBlockerHeightMeters, Mathf.Max(0.1f, size.z)),
                    notWalkableArea);
            }
        }

        private static void AppendHoleBlockerSources(List<NavMeshBuildSource> sources, RoomLayout layout)
        {
            var notWalkableArea = ResolveNotWalkableArea();
            foreach (var hole in layout.HoleTiles ?? Array.Empty<Vector2Int>())
            {
                AddBoxSource(
                    sources,
                    new Vector3(hole.x, StaticBlockerHeightMeters * 0.5f, hole.y),
                    new Vector3(1f, StaticBlockerHeightMeters, 1f),
                    notWalkableArea);
            }
        }

        private static void AddBoxSource(List<NavMeshBuildSource> sources, Vector3 center, Vector3 size, int area)
        {
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one),
                size = size,
                area = area
            });
        }

        private static Bounds BuildBounds(RoomLayout layout)
        {
            var rect = layout.Bounds;
            var center = new Vector3(rect.center.x, 0f, rect.center.y);
            var size = new Vector3(Mathf.Max(1f, rect.width + 4f), 4f, Mathf.Max(1f, rect.height + 4f));
            return new Bounds(center, size);
        }

        private static int ResolveNotWalkableArea()
        {
            var area = NavMesh.GetAreaFromName("Not Walkable");
            return area >= 0 ? area : 1;
        }
    }
}
