using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Rooms
{
    public sealed class RoomRuntimeRoot : MonoBehaviour
    {
        public const float DefaultWidthMeters = 13f;
        public const float DefaultDepthMeters = 7f;

        [SerializeField] private Vector2 roomSizeMeters = new(DefaultWidthMeters, DefaultDepthMeters);
        private readonly Dictionary<string, Renderer> doorRenderersByDirection = new();

        public Vector2 RoomSizeMeters => roomSizeMeters;

        public Vector3 CenterWorldPosition => transform.position;

        public ImportedRoomRuntimeAsset LastBuiltAsset { get; private set; }

        public RoomLayout CurrentLayout => LastBuiltAsset?.Layout;

        public Rect LocalBounds => CurrentLayout?.Bounds ?? Rect.MinMaxRect(-DefaultWidthMeters * 0.5f, -DefaultDepthMeters * 0.5f, DefaultWidthMeters * 0.5f, DefaultDepthMeters * 0.5f);

        public System.Collections.Generic.IReadOnlyList<RoomLayoutObstacle> Obstacles => CurrentLayout?.Obstacles ?? System.Array.Empty<RoomLayoutObstacle>();

        public System.Collections.Generic.IReadOnlyList<ImportedSpawnPoint> EnemySpawns => LastBuiltAsset?.EnemySpawns ?? System.Array.Empty<ImportedSpawnPoint>();

        public System.Collections.Generic.IReadOnlyList<RoomDoorPort> DoorPorts => LastBuiltAsset?.DoorPorts ?? System.Array.Empty<RoomDoorPort>();

        public Vector3 SafeStartLocalPosition => LastBuiltAsset?.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;

        public void ConfigureDefault()
        {
            roomSizeMeters = new Vector2(DefaultWidthMeters, DefaultDepthMeters);
        }

        public void BuildFrom(ImportedRoomRuntimeAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("Cannot build room runtime from a null imported asset.");
                return;
            }

            LastBuiltAsset = asset;
            roomSizeMeters = new Vector2(asset.Layout.WidthTiles, asset.Layout.HeightTiles);
            ClearChildren();
            doorRenderersByDirection.Clear();
            BuildFloor(asset.Layout);
            BuildObstacles(asset.Layout);
            BuildDoors(asset);
            BuildSpawnMarkers(asset);
        }

        public bool TryGetDoorPort(string direction, out RoomDoorPort port)
        {
            port = DoorPorts.FirstOrDefault(candidate => candidate.Direction == direction);
            return port != null;
        }

        public void SetDoorVisualState(string direction, RoomDoorVisualState state)
        {
            if (!doorRenderersByDirection.TryGetValue(direction, out var renderer) || renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = ColorForDoorState(state)
            };
        }

        private void ClearChildren()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void BuildFloor(RoomLayout layout)
        {
            foreach (var region in layout.FloorRegions)
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = $"tileGround.{region.Id}";
                floor.transform.SetParent(transform, false);
                floor.transform.localPosition = new Vector3(region.Center.x, -0.05f, region.Center.z);
                floor.transform.localScale = new Vector3(region.HalfSize.x * 2f, 0.1f, region.HalfSize.y * 2f);
                ApplyColor(floor, new Color(0.22f, 0.29f, 0.34f, 1f));
            }

            var origin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            origin.name = "originMarker_0_0";
            origin.transform.SetParent(transform, false);
            origin.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            origin.transform.localScale = new Vector3(0.28f, 0.024f, 0.28f);
            ApplyColor(origin, new Color(0.1f, 0.8f, 1f, 1f));
        }

        private void BuildObstacles(RoomLayout layout)
        {
            foreach (var obstacle in layout.Obstacles)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"{obstacle.Kind}.{obstacle.Id}";
                block.transform.SetParent(transform, false);
                block.transform.localPosition = obstacle.Center;
                block.transform.localScale = obstacle.Size;
                ApplyColor(block, new Color(0.36f, 0.34f, 0.31f, 1f));
            }
        }

        private void BuildDoors(ImportedRoomRuntimeAsset asset)
        {
            foreach (var port in asset.DoorPorts)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"doorAnchorActive.{port.Id}";
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = new Vector3(port.Position.x, 0.65f, port.Position.z);
                marker.transform.localScale = DoorScaleFor(port.Direction);
                ApplyColor(marker, new Color(0.1f, 0.48f, 0.95f, 1f));
                doorRenderersByDirection[port.Direction] = marker.GetComponent<Renderer>();

                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        private static Color ColorForDoorState(RoomDoorVisualState state)
        {
            return state switch
            {
                RoomDoorVisualState.Locked => new Color(0.82f, 0.28f, 0.18f, 1f),
                RoomDoorVisualState.Active => new Color(0.12f, 0.62f, 1f, 1f),
                RoomDoorVisualState.Cleared => new Color(0.25f, 1f, 0.45f, 1f),
                RoomDoorVisualState.Unavailable => new Color(0.2f, 0.22f, 0.24f, 0.55f),
                _ => Color.white
            };
        }

        private void BuildSpawnMarkers(ImportedRoomRuntimeAsset asset)
        {
            CreateSpawnMarker(asset.SafeStart.id, asset.SafeStart.kind, asset.SafeStart.position.ToUnityVector3(), new Color(0.36f, 1f, 0.54f, 1f), addPlayerSpawnComponent: true);

            foreach (var spawn in asset.EnemySpawns)
            {
                CreateSpawnMarker(spawn.id, spawn.kind, spawn.position.ToUnityVector3(), new Color(1f, 0.25f, 0.22f, 1f), addPlayerSpawnComponent: false);
            }

            foreach (var spawn in asset.ItemSpawns)
            {
                CreateSpawnMarker(spawn.id, spawn.kind, spawn.position.ToUnityVector3(), new Color(1f, 0.82f, 0.18f, 1f), addPlayerSpawnComponent: false);
            }
        }

        private void CreateSpawnMarker(string id, string kind, Vector3 position, Color color, bool addPlayerSpawnComponent)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"{kind}.{id}";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(position.x, 0.16f, position.z);
            marker.transform.localScale = Vector3.one * 0.32f;
            ApplyColor(marker, color);

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            if (addPlayerSpawnComponent)
            {
                marker.AddComponent<Hollow.Entities.PlayerSpawnPoint>();
            }
        }

        private static Vector3 DoorScaleFor(string direction)
        {
            return direction == "east" || direction == "west"
                ? new Vector3(0.18f, 1.3f, 1f)
                : new Vector3(1f, 1.3f, 0.18f);
        }

        private static void ApplyColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = color
            };
        }
    }
}
