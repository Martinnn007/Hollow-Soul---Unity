using System.Collections.Generic;
using System.Linq;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    public static class RoomDesignerCompiler
    {
        public static ImportedRoomRuntimeAsset Compile(RoomDesignerProject project)
        {
            var json = ExportRuntimeJson(project, prettyPrint: false);
            return HollowRuntimeV2Importer.Import(json);
        }

        public static string ExportRuntimeJson(RoomDesignerProject project, bool prettyPrint = true)
        {
            var manifest = BuildManifest(project);
            return JsonUtility.ToJson(manifest, prettyPrint);
        }

        public static ImportedHollowRoomManifest BuildManifest(RoomDesignerProject project)
        {
            if (project == null)
            {
                throw new System.ArgumentNullException(nameof(project));
            }

            var footprintPreset = project.footprintPreset;
            var presetDimensions = RoomDesignerFootprintUtility.Dimensions(footprintPreset);
            var occupiedCells = RoomDesignerFootprintUtility.OccupiedCells(footprintPreset);
            RoomDesignerFootprintUtility.RoomBounds(footprintPreset, out var minX, out var maxX, out var minZ, out var maxZ);
            var runtime = new ImportedHollowRuntime
            {
                schemaVersion = HollowRuntimeV2Importer.SupportedSchemaVersion,
                sourceProjectId = project.projectId,
                canonicalRoomId = $"designer_{project.projectId}",
                displayName = project.displayName,
                roomType = "designer",
                rewardType = "designer",
                prototypeStatus = "prototype",
                tileSizeMeters = 1f,
                dimensions = new ImportedRoomDimensions
                {
                    widthTiles = presetDimensions.x,
                    heightTiles = presetDimensions.y,
                    bounds = new ImportedRoomBounds
                    {
                        minX = minX,
                        maxX = maxX,
                        minZ = minZ,
                        maxZ = maxZ
                    }
                },
                footprint = new ImportedRoomFootprint
                {
                    primaryCell = new ImportedGridPosition { x = 0, z = 0 },
                    occupiedBranchCells = occupiedCells.Select(cell => new ImportedGridPosition { x = cell.x, z = cell.y }).ToList(),
                    chunkBasisTiles = new ImportedChunkBasis { width = RoomDesignerFootprintUtility.ChunkWidthTiles, height = RoomDesignerFootprintUtility.ChunkHeightTiles }
                }
            };

            var cells = project.cells ?? new List<RoomDesignerCell>();
            var holes = cells.Where(cell => cell.kind == RoomDesignerCellKinds.Hole && cell.layer == 0).ToHashSet(new CellXZComparer());
            var groundCells = cells.Where(cell => cell.kind == RoomDesignerCellKinds.Ground && cell.layer == 0);
            foreach (var cell in groundCells)
            {
                if (holes.Contains(cell))
                {
                    continue;
                }

                runtime.walkableTiles.Add(new ImportedGridPosition { x = cell.x, z = cell.z });
            }

            foreach (var cell in holes)
            {
                runtime.holeTiles.Add(new ImportedGridPosition { x = cell.x, z = cell.z });
            }

            foreach (var cell in occupiedCells)
            {
                var center = RoomDesignerFootprintUtility.ChunkCenter(footprintPreset, cell);
                runtime.floorRegions.Add(new ImportedRoomFloorRegion
                {
                    id = $"designer_floor_cell_{cell.x}_{cell.y}",
                    center = new ImportedVector3 { x = center.x, y = 0f, z = center.y },
                    halfSize = new ImportedHalfSize
                    {
                        x = RoomDesignerFootprintUtility.ChunkWidthTiles * 0.5f,
                        z = RoomDesignerFootprintUtility.ChunkHeightTiles * 0.5f
                    }
                });
            }

            var rockIndex = 0;
            foreach (var cell in cells.Where(cell => cell.kind == RoomDesignerCellKinds.Rock))
            {
                runtime.obstacles.Add(new ImportedRoomObstacle
                {
                    id = $"rock_{rockIndex++:00}",
                    kind = RoomDesignerCellKinds.Rock,
                    center = new ImportedVector3 { x = cell.x, y = cell.layer + 0.5f, z = cell.z },
                    size = new ImportedVector3 { x = 1f, y = 1f, z = 1f },
                    blocksProjectiles = true
                });
            }

            var spikeIndex = 0;
            foreach (var cell in cells.Where(cell => cell.kind == RoomDesignerCellKinds.Spike))
            {
                runtime.hazards.Add(new ImportedRoomHazard
                {
                    id = $"spike_{spikeIndex++:00}",
                    kind = RoomHazardKind.Spike,
                    center = new ImportedVector3 { x = cell.x, y = 0f, z = cell.z },
                    radius = 0.45f
                });
            }

            runtime.playerSafeStart = SafeStart(project);
            BuildMarkers(project, runtime);
            BuildDoorPorts(project, runtime);
            return new ImportedHollowRoomManifest { hollowRuntime = runtime };
        }

        private static ImportedVector3 SafeStart(RoomDesignerProject project)
        {
            var marker = project.markers?.FirstOrDefault(candidate => candidate.kind == RoomDesignerMarkerKinds.SafeStart);
            return marker != null
                ? new ImportedVector3 { x = marker.x, y = marker.y, z = marker.z }
                : new ImportedVector3 { x = 0f, y = 0f, z = 0f };
        }

        private static void BuildMarkers(RoomDesignerProject project, ImportedHollowRuntime runtime)
        {
            var enemyIndex = 0;
            var rewardIndex = 0;
            foreach (var marker in project.markers ?? new List<RoomDesignerMarker>())
            {
                if (RoomDesignerMarkerKinds.IsEnemy(marker.kind))
                {
                    runtime.enemySpawns.Add(new ImportedSpawnPoint
                    {
                        id = string.IsNullOrWhiteSpace(marker.id) ? $"enemy_{enemyIndex++}" : marker.id,
                        kind = RoomDesignerMarkerKinds.RuntimeEnemyKind(marker.kind),
                        position = new ImportedVector3 { x = marker.x, y = marker.y, z = marker.z }
                    });
                }
                else if (marker.kind == RoomDesignerMarkerKinds.RoomReward)
                {
                    runtime.itemSpawns.Add(new ImportedSpawnPoint
                    {
                        id = string.IsNullOrWhiteSpace(marker.id) ? $"roomReward_{rewardIndex++}" : marker.id,
                        kind = RoomDesignerMarkerKinds.RoomReward,
                        position = new ImportedVector3 { x = marker.x, y = marker.y, z = marker.z }
                    });
                }
                else if (RoomDesignerMarkerKinds.IsInteractiveObject(marker.kind))
                {
                    runtime.interactiveObjects.Add(new ImportedRoomInteractiveObject
                    {
                        id = string.IsNullOrWhiteSpace(marker.id) ? $"interactive_{runtime.interactiveObjects.Count:00}" : marker.id,
                        kind = marker.kind,
                        center = new ImportedVector3 { x = marker.x, y = 0.5f, z = marker.z },
                        size = new ImportedVector3 { x = 0.82f, y = 1f, z = 0.82f },
                        blocksMovement = true,
                        blocksProjectiles = true
                    });
                }
            }
        }

        private static void BuildDoorPorts(RoomDesignerProject project, ImportedHollowRuntime runtime)
        {
            foreach (var door in project.doorPorts ?? new List<RoomDesignerDoorPortState>())
            {
                if (door.state == RoomDesignerDoorKinds.Inactive)
                {
                    continue;
                }

                runtime.doorPorts.Add(new ImportedRoomDoorPort
                {
                    id = string.IsNullOrWhiteSpace(door.id) ? $"{door.direction}_{door.laneIndex}" : door.id,
                    direction = door.direction,
                    laneIndex = door.laneIndex,
                    hostCell = new ImportedGridPosition { x = door.hostCellX, z = door.hostCellZ },
                    gridEdgeCenter = new ImportedEdgeCenter { x = door.x, z = door.z },
                    positionMeters = new ImportedVector3 { x = door.x, y = 0f, z = door.z },
                    kind = string.IsNullOrWhiteSpace(door.state) ? RoomDesignerDoorKinds.Available : door.state
                });
            }
        }

        private sealed class CellXZComparer : IEqualityComparer<RoomDesignerCell>
        {
            public bool Equals(RoomDesignerCell left, RoomDesignerCell right)
            {
                return left != null && right != null && left.x == right.x && left.z == right.z;
            }

            public int GetHashCode(RoomDesignerCell obj)
            {
                return (obj.x * 397) ^ obj.z;
            }
        }
    }
}
