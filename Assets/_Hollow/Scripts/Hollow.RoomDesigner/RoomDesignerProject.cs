using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow.RoomDesigner
{
    [Serializable]
    public sealed class RoomDesignerProject
    {
        public int schemaVersion = 1;
        public string projectId = string.Empty;
        public string displayName = "Designer Draft 13x7";
        public RoomDesignerFootprintPreset footprintPreset = RoomDesignerFootprintPreset.Single1x1;
        public int widthTiles = 13;
        public int heightTiles = 7;
        public long createdAtUtcTicks;
        public long updatedAtUtcTicks;
        public List<RoomDesignerCell> cells = new();
        public List<RoomDesignerMarker> markers = new();
        public List<RoomDesignerDoorPortState> doorPorts = new();

        public static RoomDesignerProject CreateDefault(string displayName = "Designer Draft 13x7")
        {
            return CreateDefault(RoomDesignerFootprintPreset.Single1x1, displayName);
        }

        public static RoomDesignerProject CreateDefault(RoomDesignerFootprintPreset preset, string displayName = null)
        {
            var now = DateTime.UtcNow.Ticks;
            var dimensions = RoomDesignerFootprintUtility.Dimensions(preset);
            var project = new RoomDesignerProject
            {
                projectId = Guid.NewGuid().ToString("N"),
                displayName = string.IsNullOrWhiteSpace(displayName) ? RoomDesignerFootprintUtility.DisplayName(preset) : displayName,
                footprintPreset = preset,
                widthTiles = dimensions.x,
                heightTiles = dimensions.y,
                createdAtUtcTicks = now,
                updatedAtUtcTicks = now
            };

            foreach (var tile in RoomDesignerFootprintUtility.GroundTiles(preset))
            {
                project.cells.Add(new RoomDesignerCell(tile.x, tile.y, 0, RoomDesignerCellKinds.Ground));
            }

            foreach (var position in new[] { new Vector2Int(-3, -1), new Vector2Int(-1, 1), new Vector2Int(2, -1), new Vector2Int(4, 1) })
            {
                if (RoomDesignerFootprintUtility.ContainsTile(preset, position.x, position.y))
                {
                    project.cells.Add(new RoomDesignerCell(position.x, position.y, 1, RoomDesignerCellKinds.Rock));
                }
            }

            var safeStart = RoomDesignerFootprintUtility.NearestContainedTile(preset, 0, 0);
            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, safeStart.x, 0f, safeStart.y));

            var enemyTargets = new[] { new Vector2Int(-4, -2), new Vector2Int(4, -2), new Vector2Int(-4, 2), new Vector2Int(4, 2) };
            for (var index = 0; index < enemyTargets.Length; index++)
            {
                var enemy = RoomDesignerFootprintUtility.NearestContainedTile(preset, enemyTargets[index].x, enemyTargets[index].y);
                project.markers.Add(new RoomDesignerMarker($"spawn_enemy_{index}", RoomDesignerMarkerKinds.Enemy, enemy.x, 0f, enemy.y));
            }

            var reward = RoomDesignerFootprintUtility.NearestContainedTile(preset, 0, 2);
            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, reward.x, 0f, reward.y));

            project.doorPorts.AddRange(RoomDesignerFootprintUtility.CreateAvailableDoorPorts(preset));
            return project;
        }

        public RoomDesignerProject CloneAsDuplicate()
        {
            var clone = JsonUtility.FromJson<RoomDesignerProject>(JsonUtility.ToJson(this));
            clone.projectId = Guid.NewGuid().ToString("N");
            clone.displayName = $"{displayName} Copy";
            clone.createdAtUtcTicks = DateTime.UtcNow.Ticks;
            clone.updatedAtUtcTicks = clone.createdAtUtcTicks;
            return clone;
        }
    }

    [Serializable]
    public sealed class RoomDesignerCell
    {
        public int x;
        public int z;
        public int layer;
        public string kind;

        public RoomDesignerCell()
        {
        }

        public RoomDesignerCell(int x, int z, int layer, string kind)
        {
            this.x = x;
            this.z = z;
            this.layer = layer;
            this.kind = kind;
        }
    }

    [Serializable]
    public sealed class RoomDesignerMarker
    {
        public string id;
        public string kind;
        public float x;
        public float y;
        public float z;

        public RoomDesignerMarker()
        {
        }

        public RoomDesignerMarker(string id, string kind, float x, float y, float z)
        {
            this.id = id;
            this.kind = kind;
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [Serializable]
    public sealed class RoomDesignerDoorPortState
    {
        public string id;
        public string direction;
        public int laneIndex;
        public int hostCellX;
        public int hostCellZ;
        public float x;
        public float z;
        public string state;

        public static RoomDesignerDoorPortState Create(string direction, int laneIndex, float x, float z, string state, int hostCellX = 0, int hostCellZ = 0)
        {
            return new RoomDesignerDoorPortState
            {
                id = $"{direction}_{laneIndex}",
                direction = direction,
                laneIndex = laneIndex,
                hostCellX = hostCellX,
                hostCellZ = hostCellZ,
                x = x,
                z = z,
                state = state
            };
        }
    }

    public static class RoomDesignerCellKinds
    {
        public const string Ground = "tileGround";
        public const string Hole = "tileHole";
        public const string Rock = "rockTile";
    }

    public static class RoomDesignerMarkerKinds
    {
        public const string SafeStart = "spawn_point_safeStart";
        public const string Enemy = "spawn_point_enemy";
        public const string RoomReward = "spawn_point_roomReward";
    }

    public static class RoomDesignerDoorKinds
    {
        public const string Available = "available";
        public const string Door = "door";
        public const string Secret = "secret";
        public const string Inactive = "inactive";
    }
}
