using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    public static class RoomDesignerRuntimeDraftImporter
    {
        public static RoomDesignerProject FromRuntimeJson(string json, string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Runtime room JSON cannot be empty.", nameof(json));
            }

            var asset = HollowRuntimeV2Importer.Import(json);
            var runtime = asset.SourceManifest.hollowRuntime;
            var preset = PresetFromFootprint(asset.Footprint);
            var dimensions = RoomDesignerFootprintUtility.Dimensions(preset);
            var project = new RoomDesignerProject
            {
                projectId = $"curated_{Sanitize(asset.Id)}",
                displayName = string.IsNullOrWhiteSpace(runtime.displayName) ? asset.Id : runtime.displayName,
                biomeId = RoomBiomeIds.Normalize(runtime.biomeId),
                footprintPreset = preset,
                widthTiles = dimensions.x,
                heightTiles = dimensions.y,
                createdAtUtcTicks = 0,
                updatedAtUtcTicks = 0,
                cells = new List<RoomDesignerCell>(),
                markers = new List<RoomDesignerMarker>(),
                doorPorts = new List<RoomDesignerDoorPortState>()
            };

            foreach (var tile in runtime.walkableTiles ?? new List<ImportedGridPosition>())
            {
                project.cells.Add(new RoomDesignerCell(tile.x, tile.z, 0, RoomDesignerCellKinds.Ground));
            }

            foreach (var tile in runtime.holeTiles ?? new List<ImportedGridPosition>())
            {
                project.cells.Add(new RoomDesignerCell(tile.x, tile.z, 0, RoomDesignerCellKinds.Hole));
            }

            foreach (var obstacle in runtime.obstacles ?? new List<ImportedRoomObstacle>())
            {
                if (!string.Equals(obstacle.kind, RoomDesignerCellKinds.Rock, StringComparison.Ordinal))
                {
                    continue;
                }

                var center = obstacle.center?.ToUnityVector3() ?? Vector3.zero;
                var layer = Mathf.Max(0, Mathf.RoundToInt(center.y - 0.5f));
                project.cells.Add(new RoomDesignerCell(
                    Mathf.RoundToInt(center.x),
                    Mathf.RoundToInt(center.z),
                    layer,
                    RoomDesignerCellKinds.Rock));
            }

            foreach (var hazard in runtime.hazards ?? new List<ImportedRoomHazard>())
            {
                if (hazard?.kind != RoomHazardKind.Spike)
                {
                    continue;
                }

                var center = hazard.center?.ToUnityVector3() ?? Vector3.zero;
                project.cells.Add(new RoomDesignerCell(
                    Mathf.RoundToInt(center.x),
                    Mathf.RoundToInt(center.z),
                    0,
                    RoomDesignerCellKinds.Spike));
            }

            var safeStart = runtime.playerSafeStart ?? new ImportedVector3();
            project.markers.Add(new RoomDesignerMarker(
                "spawn_safeStart",
                RoomDesignerMarkerKinds.SafeStart,
                safeStart.x,
                safeStart.y,
                safeStart.z));

            var enemyIndex = 0;
            foreach (var spawn in runtime.enemySpawns ?? new List<ImportedSpawnPoint>())
            {
                var position = spawn.position ?? new ImportedVector3();
                project.markers.Add(new RoomDesignerMarker(
                    string.IsNullOrWhiteSpace(spawn.id) ? $"spawn_enemy_{enemyIndex}" : spawn.id,
                    string.IsNullOrWhiteSpace(spawn.kind) ? RoomDesignerMarkerKinds.EnemyNormal : spawn.kind,
                    position.x,
                    position.y,
                    position.z));
                enemyIndex++;
            }

            var rewardIndex = 0;
            foreach (var spawn in runtime.itemSpawns ?? new List<ImportedSpawnPoint>())
            {
                var position = spawn.position ?? new ImportedVector3();
                var markerKind = RoomDesignerMarkerKinds.IsChest(spawn.kind)
                    ? spawn.kind
                    : RoomDesignerMarkerKinds.RoomReward;
                project.markers.Add(new RoomDesignerMarker(
                    string.IsNullOrWhiteSpace(spawn.id) ? $"spawn_reward_{rewardIndex}" : spawn.id,
                    markerKind,
                    position.x,
                    position.y,
                    position.z));
                rewardIndex++;
            }

            var interactiveIndex = 0;
            foreach (var roomObject in runtime.interactiveObjects ?? new List<ImportedRoomInteractiveObject>())
            {
                if (!RoomDesignerMarkerKinds.IsInteractiveObject(roomObject?.kind))
                {
                    continue;
                }

                var center = roomObject.center?.ToUnityVector3() ?? Vector3.zero;
                project.markers.Add(new RoomDesignerMarker(
                    string.IsNullOrWhiteSpace(roomObject.id) ? $"interactive_{interactiveIndex}" : roomObject.id,
                    roomObject.kind,
                    center.x,
                    0f,
                    center.z));
                interactiveIndex++;
            }

            var decorIndex = 0;
            foreach (var decor in runtime.decor ?? new List<ImportedRoomDecor>())
            {
                if (!RoomDesignerMarkerKinds.IsDecor(decor?.kind))
                {
                    continue;
                }

                var center = decor.center?.ToUnityVector3() ?? Vector3.zero;
                project.markers.Add(new RoomDesignerMarker(
                    string.IsNullOrWhiteSpace(decor.id) ? $"decor_{decorIndex}" : decor.id,
                    decor.kind,
                    center.x,
                    center.y,
                    center.z));
                decorIndex++;
            }

            var runtimePortsById = (runtime.doorPorts ?? new List<ImportedRoomDoorPort>())
                .Where(port => !string.IsNullOrWhiteSpace(port.id))
                .ToDictionary(port => port.id, port => port, StringComparer.Ordinal);
            foreach (var door in RoomDesignerFootprintUtility.CreateAvailableDoorPorts(preset))
            {
                if (runtimePortsById.TryGetValue(door.id, out var runtimeDoor))
                {
                    door.state = string.IsNullOrWhiteSpace(runtimeDoor.kind) ? RoomDesignerDoorKinds.Available : runtimeDoor.kind;
                    door.direction = runtimeDoor.direction;
                    door.laneIndex = runtimeDoor.laneIndex;
                    door.hostCellX = runtimeDoor.hostCell?.x ?? door.hostCellX;
                    door.hostCellZ = runtimeDoor.hostCell?.z ?? door.hostCellZ;
                    door.x = runtimeDoor.gridEdgeCenter?.x ?? runtimeDoor.positionMeters?.x ?? door.x;
                    door.z = runtimeDoor.gridEdgeCenter?.z ?? runtimeDoor.positionMeters?.z ?? door.z;
                }
                else
                {
                    door.state = RoomDesignerDoorKinds.Inactive;
                }

                project.doorPorts.Add(door);
            }

            return project;
        }

        private static RoomDesignerFootprintPreset PresetFromFootprint(RoomInstanceFootprint footprint)
        {
            var normalized = (footprint?.OccupiedCells ?? new[] { Vector2Int.zero })
                .Select(cell => new Vector2Int(cell.x, cell.y))
                .ToArray();
            var minX = normalized.Min(cell => cell.x);
            var minZ = normalized.Min(cell => cell.y);
            var signature = string.Join("|", normalized
                .Select(cell => new Vector2Int(cell.x - minX, cell.y - minZ))
                .OrderBy(cell => cell.x)
                .ThenBy(cell => cell.y)
                .Select(cell => $"{cell.x},{cell.y}"));

            return signature switch
            {
                "0,0|1,0" => RoomDesignerFootprintPreset.Wide2x1,
                "0,0|0,1" => RoomDesignerFootprintPreset.Tall1x2,
                "0,0|0,1|1,0|1,1" => RoomDesignerFootprintPreset.Block2x2,
                "0,0|0,1|1,0" => RoomDesignerFootprintPreset.L3Cell,
                _ => RoomDesignerFootprintPreset.Single1x1
            };
        }

        private static string Sanitize(string value)
        {
            var sanitized = new string((value ?? string.Empty)
                .Select(character => char.IsLetterOrDigit(character) || character == '_' || character == '-' ? character : '_')
                .ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
        }
    }
}
