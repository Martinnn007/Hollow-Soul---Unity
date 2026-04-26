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
        public int widthTiles = 13;
        public int heightTiles = 7;
        public long createdAtUtcTicks;
        public long updatedAtUtcTicks;
        public List<RoomDesignerCell> cells = new();
        public List<RoomDesignerMarker> markers = new();
        public List<RoomDesignerDoorPortState> doorPorts = new();

        public static RoomDesignerProject CreateDefault(string displayName = "Designer Draft 13x7")
        {
            var now = DateTime.UtcNow.Ticks;
            var project = new RoomDesignerProject
            {
                projectId = Guid.NewGuid().ToString("N"),
                displayName = string.IsNullOrWhiteSpace(displayName) ? "Designer Draft 13x7" : displayName,
                createdAtUtcTicks = now,
                updatedAtUtcTicks = now
            };

            for (var z = -3; z <= 3; z++)
            {
                for (var x = -6; x <= 6; x++)
                {
                    project.cells.Add(new RoomDesignerCell(x, z, 0, RoomDesignerCellKinds.Ground));
                }
            }

            foreach (var position in new[] { new Vector2Int(-3, -1), new Vector2Int(-1, 1), new Vector2Int(2, -1), new Vector2Int(4, 1) })
            {
                project.cells.Add(new RoomDesignerCell(position.x, position.y, 1, RoomDesignerCellKinds.Rock));
            }

            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, 0f, 0f, 0f));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_0", RoomDesignerMarkerKinds.Enemy, -4f, 0f, -2f));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_1", RoomDesignerMarkerKinds.Enemy, 4f, 0f, -2f));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_2", RoomDesignerMarkerKinds.Enemy, -4f, 0f, 2f));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_3", RoomDesignerMarkerKinds.Enemy, 4f, 0f, 2f));
            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, 0f, 0f, 2f));

            project.doorPorts.Add(RoomDesignerDoorPortState.Create("north", 0, 0f, -3.5f, RoomDesignerDoorKinds.Available));
            project.doorPorts.Add(RoomDesignerDoorPortState.Create("south", 0, 0f, 3.5f, RoomDesignerDoorKinds.Available));
            project.doorPorts.Add(RoomDesignerDoorPortState.Create("east", 0, 6.5f, 0f, RoomDesignerDoorKinds.Available));
            project.doorPorts.Add(RoomDesignerDoorPortState.Create("west", 0, -6.5f, 0f, RoomDesignerDoorKinds.Available));
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
        public float x;
        public float z;
        public string state;

        public static RoomDesignerDoorPortState Create(string direction, int laneIndex, float x, float z, string state)
        {
            return new RoomDesignerDoorPortState
            {
                id = $"{direction}_{laneIndex}",
                direction = direction,
                laneIndex = laneIndex,
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
