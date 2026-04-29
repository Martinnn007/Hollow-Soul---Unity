using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Presentation;

namespace Hollow.Rooms
{
    public sealed class RoomRuntimeRoot : MonoBehaviour
    {
        public const float DefaultWidthMeters = 13f;
        public const float DefaultDepthMeters = 7f;

        [SerializeField] private Vector2 roomSizeMeters = new(DefaultWidthMeters, DefaultDepthMeters);
        private readonly Dictionary<string, List<Renderer>> doorRenderersByDirection = new();
        private readonly Dictionary<string, Renderer> doorRenderersByPortId = new();
        private readonly Dictionary<string, GameObject> doorMarkersByPortId = new();
        private readonly List<RoomHazardMarker> hazardMarkers = new();
        private readonly List<RoomInteractiveObjectMarker> interactiveObjectMarkers = new();

        public Vector2 RoomSizeMeters => roomSizeMeters;

        public Vector3 CenterWorldPosition => transform.position;

        public ImportedRoomRuntimeAsset LastBuiltAsset { get; private set; }

        public RoomLayout CurrentLayout => LastBuiltAsset?.Layout;

        public Rect LocalBounds => CurrentLayout?.Bounds ?? Rect.MinMaxRect(-DefaultWidthMeters * 0.5f, -DefaultDepthMeters * 0.5f, DefaultWidthMeters * 0.5f, DefaultDepthMeters * 0.5f);

        public System.Collections.Generic.IReadOnlyList<RoomLayoutObstacle> Obstacles => CurrentLayout?.Obstacles ?? System.Array.Empty<RoomLayoutObstacle>();

        public System.Collections.Generic.IReadOnlyList<RoomHazardMarker> HazardMarkers => hazardMarkers;

        public System.Collections.Generic.IReadOnlyList<RoomInteractiveObjectMarker> InteractiveObjectMarkers => interactiveObjectMarkers;

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
            doorRenderersByPortId.Clear();
            doorMarkersByPortId.Clear();
            hazardMarkers.Clear();
            interactiveObjectMarkers.Clear();
            BuildFloor(asset.Layout);
            BuildObstacles(asset.Layout);
            BuildHazards(asset);
            BuildInteractiveObjects(asset);
            BuildDoors(asset);
            BuildSpawnMarkers(asset);
        }

        public void ClearRuntime()
        {
            LastBuiltAsset = null;
            ClearChildren();
            doorRenderersByDirection.Clear();
            doorRenderersByPortId.Clear();
            doorMarkersByPortId.Clear();
            hazardMarkers.Clear();
            interactiveObjectMarkers.Clear();
        }

        public bool TryGetDoorPort(string direction, out RoomDoorPort port)
        {
            port = DoorPorts.FirstOrDefault(candidate => candidate.Direction == direction);
            return port != null;
        }

        public bool TryGetDoorPortById(string portId, out RoomDoorPort port)
        {
            port = DoorPorts.FirstOrDefault(candidate => candidate.Id == portId);
            return port != null;
        }

        public void SetDoorVisualState(string direction, RoomDoorVisualState state)
        {
            if (!doorRenderersByDirection.TryGetValue(direction, out var renderers))
            {
                return;
            }

            var material = MaterialResolver.Resolve(MaterialRoleForDoorState(state));
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                    ClearArtPassChildren(renderer.transform);
                    PresentationPrefabResolver.InstantiateVisual(PrefabRoleForDoorState(state), renderer.transform, Vector3.zero, Vector3.one);
                }
            }
        }

        public void SetDoorVisualStateById(string portId, RoomDoorVisualState state)
        {
            if (!doorRenderersByPortId.TryGetValue(portId, out var renderer) || renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = MaterialResolver.Resolve(MaterialRoleForDoorState(state));
            if (doorMarkersByPortId.TryGetValue(portId, out var marker) && marker != null)
            {
                ClearArtPassChildren(marker.transform);
                PresentationPrefabResolver.InstantiateVisual(PrefabRoleForDoorState(state), marker.transform, Vector3.zero, Vector3.one);
            }
        }

        public void ApplyInteractiveObjectState(System.Collections.Generic.IEnumerable<string> destroyedObjectIds)
        {
            var destroyed = new HashSet<string>(destroyedObjectIds ?? System.Array.Empty<string>());
            foreach (var marker in interactiveObjectMarkers)
            {
                if (marker == null || !destroyed.Contains(marker.ObjectId))
                {
                    continue;
                }

                marker.MarkDestroyed();
                marker.gameObject.SetActive(false);
            }
        }

        public void ClearHazardsAndInteractiveObjects()
        {
            foreach (var hazard in hazardMarkers.ToArray())
            {
                if (hazard != null)
                {
                    DestroyRuntimeChild(hazard.gameObject);
                }
            }

            foreach (var marker in interactiveObjectMarkers.ToArray())
            {
                if (marker != null)
                {
                    DestroyRuntimeChild(marker.gameObject);
                }
            }

            hazardMarkers.Clear();
            interactiveObjectMarkers.Clear();
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
                MaterialResolver.ApplyTo(floor, MaterialRole.RoomFloor);
                PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.RoomFloor, floor.transform, Vector3.zero, Vector3.one);
            }

            var origin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            origin.name = "originMarker_0_0";
            origin.transform.SetParent(transform, false);
            origin.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            origin.transform.localScale = new Vector3(0.28f, 0.024f, 0.28f);
            MaterialResolver.ApplyTo(origin, MaterialRole.RoomOriginMarker);
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
                MaterialResolver.ApplyTo(block, MaterialRole.RoomObstacleRock);
                PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.RoomObstacleRock, block.transform, Vector3.zero, Vector3.one);
            }
        }

        private void BuildHazards(ImportedRoomRuntimeAsset asset)
        {
            foreach (var hazard in asset.Hazards ?? System.Array.Empty<ImportedRoomHazard>())
            {
                if (hazard == null)
                {
                    continue;
                }

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = $"{hazard.kind}.{hazard.id}";
                marker.transform.SetParent(transform, false);
                var position = hazard.center?.ToUnityVector3() ?? Vector3.zero;
                marker.transform.localPosition = new Vector3(position.x, 0.025f, position.z);
                marker.transform.localScale = new Vector3(0.72f, 0.05f, 0.72f);
                MaterialResolver.ApplyTo(marker, MaterialRole.RoomHazardSpike);
                PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.RoomHazardSpike, marker.transform, Vector3.zero, Vector3.one);
                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }

                var hazardMarker = marker.AddComponent<RoomHazardMarker>();
                hazardMarker.Configure(hazard);
                hazardMarkers.Add(hazardMarker);
            }
        }

        private void BuildInteractiveObjects(ImportedRoomRuntimeAsset asset)
        {
            foreach (var roomObject in asset.InteractiveObjects ?? System.Array.Empty<ImportedRoomInteractiveObject>())
            {
                if (roomObject == null)
                {
                    continue;
                }

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"{roomObject.kind}.{roomObject.id}";
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = roomObject.center?.ToUnityVector3() ?? Vector3.zero;
                marker.transform.localScale = roomObject.size?.ToUnityVector3() ?? Vector3.one;
                var materialRole = roomObject.kind == RoomInteractiveObjectKind.ExplosiveBarrel
                    ? MaterialRole.RoomExplosiveBarrel
                    : MaterialRole.RoomBarrel;
                var prefabRole = roomObject.kind == RoomInteractiveObjectKind.ExplosiveBarrel
                    ? PresentationPrefabRole.ExplosiveBarrel
                    : PresentationPrefabRole.StandardBarrel;
                MaterialResolver.ApplyTo(marker, materialRole);
                PresentationPrefabResolver.InstantiateVisual(prefabRole, marker.transform, Vector3.zero, Vector3.one);
                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }

                var objectMarker = marker.AddComponent<RoomInteractiveObjectMarker>();
                objectMarker.Configure(roomObject);
                interactiveObjectMarkers.Add(objectMarker);
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
                MaterialResolver.ApplyTo(marker, MaterialRole.DoorActive);
                PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.DoorActive, marker.transform, Vector3.zero, Vector3.one);
                var renderer = marker.GetComponent<Renderer>();
                doorRenderersByPortId[port.Id] = renderer;
                doorMarkersByPortId[port.Id] = marker;
                if (!doorRenderersByDirection.TryGetValue(port.Direction, out var renderers))
                {
                    renderers = new List<Renderer>();
                    doorRenderersByDirection[port.Direction] = renderers;
                }

                renderers.Add(renderer);

                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        private static MaterialRole MaterialRoleForDoorState(RoomDoorVisualState state)
        {
            return state switch
            {
                RoomDoorVisualState.Locked => MaterialRole.DoorLocked,
                RoomDoorVisualState.Active => MaterialRole.DoorActive,
                RoomDoorVisualState.Cleared => MaterialRole.DoorCleared,
                RoomDoorVisualState.Unavailable => MaterialRole.DoorUnavailable,
                _ => MaterialRole.DoorActive
            };
        }

        private static PresentationPrefabRole PrefabRoleForDoorState(RoomDoorVisualState state)
        {
            return state switch
            {
                RoomDoorVisualState.Locked => PresentationPrefabRole.DoorLocked,
                RoomDoorVisualState.Active => PresentationPrefabRole.DoorActive,
                RoomDoorVisualState.Cleared => PresentationPrefabRole.DoorCleared,
                RoomDoorVisualState.Unavailable => PresentationPrefabRole.DoorUnavailable,
                _ => PresentationPrefabRole.DoorActive
            };
        }

        private void BuildSpawnMarkers(ImportedRoomRuntimeAsset asset)
        {
            CreateSpawnMarker(asset.SafeStart.id, asset.SafeStart.kind, asset.SafeStart.position.ToUnityVector3(), MaterialRole.SpawnSafeStart, addPlayerSpawnComponent: true);

            foreach (var spawn in asset.EnemySpawns)
            {
                CreateSpawnMarker(spawn.id, spawn.kind, spawn.position.ToUnityVector3(), MaterialRole.SpawnEnemy, addPlayerSpawnComponent: false);
            }

            foreach (var spawn in asset.ItemSpawns)
            {
                CreateSpawnMarker(spawn.id, spawn.kind, spawn.position.ToUnityVector3(), MaterialRole.SpawnReward, addPlayerSpawnComponent: false);
            }
        }

        private void CreateSpawnMarker(string id, string kind, Vector3 position, MaterialRole role, bool addPlayerSpawnComponent)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"{kind}.{id}";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(position.x, 0.16f, position.z);
            marker.transform.localScale = Vector3.one * 0.32f;
            MaterialResolver.ApplyTo(marker, role);

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

        private static void ClearArtPassChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                if (child.GetComponent<PresentationVisualMarker>() == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void DestroyRuntimeChild(GameObject child)
        {
            if (child == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        private static Vector3 DoorScaleFor(string direction)
        {
            return direction == "east" || direction == "west"
                ? new Vector3(0.18f, 1.3f, 1f)
                : new Vector3(1f, 1.3f, 0.18f);
        }

    }
}
