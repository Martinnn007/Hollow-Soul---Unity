using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine.AI;

namespace Hollow.Rooms
{
    public sealed class RoomRuntimeRoot : MonoBehaviour
    {
        public const float DefaultWidthMeters = 13f;
        public const float DefaultDepthMeters = 7f;
        public const float PerimeterWallHeightMeters = 1.35f;
        public const float PerimeterWallThicknessMeters = 0.12f;
        public const float PerimeterWallDoorGapMeters = 1.6f;
        private const float DoorVisualHeightMeters = 2.2f;
        private const float DoorVisualCenterY = DoorVisualHeightMeters * 0.5f;
        private const float MinimumWallSegmentLengthMeters = 0.08f;
        private const string StagingRootName = "__RoomRuntimeStaging";

        [SerializeField] private Vector2 roomSizeMeters = new(DefaultWidthMeters, DefaultDepthMeters);
        private readonly Dictionary<string, List<Renderer>> doorRenderersByDirection = new();
        private readonly Dictionary<string, Renderer> doorRenderersByPortId = new();
        private readonly Dictionary<string, GameObject> doorMarkersByPortId = new();
        private readonly Dictionary<string, RoomDynamicNavigationObjectMarker> doorNavigationByPortId = new();
        private readonly Dictionary<string, string> doorDirectionByPortId = new();
        private readonly Dictionary<string, RoomDoorVisualState> doorVisualStateByPortId = new();
        private readonly HashSet<string> openDoorPortIds = new();
        private readonly HashSet<Renderer> renderersSuppressedForReveal = new();
        private readonly List<Renderer> rendererSuppressionScratch = new();
        private readonly List<RoomHazardMarker> hazardMarkers = new();
        private readonly List<RoomInteractiveObjectMarker> interactiveObjectMarkers = new();
        private readonly List<RoomDynamicNavigationObjectMarker> dynamicNavigationObjects = new();
        private NavMeshDataInstance navMeshDataInstance;
        private NavMeshData activeNavMeshData;
        private string navMeshBakeError = string.Empty;
        private bool activeNavMeshWasRuntimeBuilt;
        private string navMeshBakeSource = string.Empty;
        private GameObject perimeterWallsRoot;
        private Transform buildParentOverride;
        private GameObject pendingStagingRoot;
        private List<GameObject> pendingOldChildren;

        private Transform BuildParent => buildParentOverride != null ? buildParentOverride : transform;

        public Vector2 RoomSizeMeters => roomSizeMeters;

        public Vector3 CenterWorldPosition => transform.position;

        public ImportedRoomRuntimeAsset LastBuiltAsset { get; private set; }

        public RoomLayout CurrentLayout => LastBuiltAsset?.Layout;

        public bool HasNavMeshBake => activeNavMeshData != null && navMeshDataInstance.valid;

        public string NavMeshBakeError => navMeshBakeError;

        public bool HasRuntimeBuiltNavMesh => HasNavMeshBake && activeNavMeshWasRuntimeBuilt;

        public string NavMeshBakeSource => navMeshBakeSource;

        public Rect LocalBounds => CurrentLayout?.Bounds ?? Rect.MinMaxRect(-DefaultWidthMeters * 0.5f, -DefaultDepthMeters * 0.5f, DefaultWidthMeters * 0.5f, DefaultDepthMeters * 0.5f);

        public string BiomeId => LastBuiltAsset != null ? RoomBiomeIds.Normalize(LastBuiltAsset.BiomeId) : RoomBiomeIds.HollowThreshold;

        public System.Collections.Generic.IReadOnlyList<RoomLayoutObstacle> Obstacles => CurrentLayout?.Obstacles ?? System.Array.Empty<RoomLayoutObstacle>();

        public System.Collections.Generic.IReadOnlyList<RoomHazardMarker> HazardMarkers => hazardMarkers;

        public System.Collections.Generic.IReadOnlyList<RoomInteractiveObjectMarker> InteractiveObjectMarkers => interactiveObjectMarkers;

        public System.Collections.Generic.IReadOnlyList<RoomDynamicNavigationObjectMarker> DynamicNavigationObjects => dynamicNavigationObjects;

        public System.Collections.Generic.IReadOnlyList<ImportedSpawnPoint> EnemySpawns => LastBuiltAsset?.EnemySpawns ?? System.Array.Empty<ImportedSpawnPoint>();

        public System.Collections.Generic.IReadOnlyList<RoomDoorPort> DoorPorts => LastBuiltAsset?.DoorPorts ?? System.Array.Empty<RoomDoorPort>();

        public Vector3 SafeStartLocalPosition => LastBuiltAsset?.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;

        public void ConfigureDefault()
        {
            roomSizeMeters = new Vector2(DefaultWidthMeters, DefaultDepthMeters);
        }

        public void BuildFrom(ImportedRoomRuntimeAsset asset)
        {
            BuildFrom(asset, RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake);
        }

        public void BuildFrom(ImportedRoomRuntimeAsset asset, RoomNavMeshRuntimeFallbackMode fallbackMode)
        {
            CancelPendingStagedBuild();
            var descriptor = RoomRuntimeDescriptorCache.GetOrCreate(asset);
            if (descriptor == null)
            {
                Debug.LogError("Cannot build room runtime from a null imported asset.");
                return;
            }

            asset = descriptor.Asset;
            LastBuiltAsset = asset;
            roomSizeMeters = new Vector2(descriptor.Layout.WidthTiles, descriptor.Layout.HeightTiles);
            ClearChildren();
            perimeterWallsRoot = null;
            doorRenderersByDirection.Clear();
            doorRenderersByPortId.Clear();
            doorMarkersByPortId.Clear();
            doorNavigationByPortId.Clear();
            doorDirectionByPortId.Clear();
            doorVisualStateByPortId.Clear();
            openDoorPortIds.Clear();
            foreach (var port in descriptor.DoorPorts)
            {
                openDoorPortIds.Add(port.Id);
            }

            hazardMarkers.Clear();
            interactiveObjectMarkers.Clear();
            dynamicNavigationObjects.Clear();
            var biomeId = descriptor.BiomeId;
            BuildFloor(descriptor.Layout, biomeId);
            BuildPerimeterWalls(descriptor.Layout, descriptor.DoorPorts, biomeId);
            BuildHoleMarkers(descriptor.Layout);
            BuildObstacles(descriptor.Layout, biomeId);
            BuildHazards(descriptor);
            BuildInteractiveObjects(descriptor);
            BuildDecor(descriptor, biomeId);
            BuildDoors(descriptor, biomeId);
            BuildSpawnMarkers(descriptor);
            AttachNavMesh(asset, fallbackMode);
            ConfigureCarvingObstacles();
        }

        public IEnumerator BuildFromStaged(ImportedRoomRuntimeAsset asset, RoomNavMeshRuntimeFallbackMode fallbackMode, bool revealOnCommit = true)
        {
            CancelPendingStagedBuild();
            var descriptor = RoomRuntimeDescriptorCache.GetOrCreate(asset);
            if (descriptor == null)
            {
                Debug.LogError("Cannot build room runtime from a null imported asset.");
                yield break;
            }

            var oldChildren = SnapshotCurrentChildren();
            var stagingRoot = CreateStagingRoot();
            asset = descriptor.Asset;
            LastBuiltAsset = asset;
            roomSizeMeters = new Vector2(descriptor.Layout.WidthTiles, descriptor.Layout.HeightTiles);
            buildParentOverride = stagingRoot.transform;
            try
            {
                ResetRuntimeCollectionsForBuild(descriptor);
                yield return ReportBuildStageAndYield(stagingRoot);

                var biomeId = descriptor.BiomeId;
                BuildFloor(descriptor.Layout, biomeId);
                yield return ReportBuildStageAndYield(stagingRoot);
                BuildPerimeterWalls(descriptor.Layout, descriptor.DoorPorts, biomeId);
                yield return ReportBuildStageAndYield(stagingRoot);
                BuildHoleMarkers(descriptor.Layout);
                BuildObstacles(descriptor.Layout, biomeId);
                yield return ReportBuildStageAndYield(stagingRoot);
                BuildHazards(descriptor);
                BuildInteractiveObjects(descriptor);
                yield return ReportBuildStageAndYield(stagingRoot);
                BuildDecor(descriptor, biomeId);
                yield return ReportBuildStageAndYield(stagingRoot);
                BuildDoors(descriptor, biomeId);
                BuildSpawnMarkers(descriptor);
                yield return ReportBuildStageAndYield(stagingRoot);
                AttachNavMesh(asset, fallbackMode);
                yield return ReportBuildStageAndYield(stagingRoot);
                ConfigureCarvingObstacles();
                yield return ReportBuildStageAndYield(stagingRoot);
            }
            finally
            {
                if (revealOnCommit)
                {
                    buildParentOverride = null;
                }
            }

            if (!revealOnCommit)
            {
                pendingStagingRoot = stagingRoot;
                pendingOldChildren = oldChildren;
                yield break;
            }

            CommitStagedBuild(stagingRoot, oldChildren, suppressRenderersForReveal: !revealOnCommit);
            if (revealOnCommit)
            {
                M136PerformanceOperationCounters.ReportNormalTraversalReveal(0);
            }

            yield return ReportBuildStageAndYield();
            yield return DestroyHiddenChildrenStaged(oldChildren);
        }

        public void CommitPendingStagedBuildForReveal()
        {
            if (pendingStagingRoot == null)
            {
                RevealSuppressedRuntimeRenderers();
                return;
            }

            var stagingRoot = pendingStagingRoot;
            var oldChildren = pendingOldChildren ?? new List<GameObject>();
            pendingStagingRoot = null;
            pendingOldChildren = null;
            buildParentOverride = null;

            CommitStagedBuild(stagingRoot, oldChildren, suppressRenderersForReveal: false);
            M136PerformanceOperationCounters.ReportNormalTraversalReveal(0);
            if (Application.isPlaying && isActiveAndEnabled)
            {
                StartCoroutine(DestroyHiddenChildrenStaged(oldChildren));
            }
            else
            {
                for (var index = 0; index < oldChildren.Count; index++)
                {
                    if (oldChildren[index] != null)
                    {
                        DestroyRuntimeChild(oldChildren[index]);
                    }
                }
            }
        }

        public void SuppressRuntimeRenderersForReveal()
        {
            rendererSuppressionScratch.Clear();
            GetComponentsInChildren<Renderer>(true, rendererSuppressionScratch);
            for (var index = 0; index < rendererSuppressionScratch.Count; index++)
            {
                var renderer = rendererSuppressionScratch[index];
                if (renderer == null ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                renderer.enabled = false;
                renderersSuppressedForReveal.Add(renderer);
            }

            rendererSuppressionScratch.Clear();
        }

        public void RevealSuppressedRuntimeRenderers()
        {
            if (renderersSuppressedForReveal.Count == 0)
            {
                return;
            }

            foreach (var renderer in renderersSuppressedForReveal)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            renderersSuppressedForReveal.Clear();
            M136PerformanceOperationCounters.ReportNormalTraversalReveal(0);
        }

        public void ClearRuntime()
        {
            CancelPendingStagedBuild();
            LastBuiltAsset = null;
            ReleaseNavMesh();
            ClearChildren();
            perimeterWallsRoot = null;
            doorRenderersByDirection.Clear();
            doorRenderersByPortId.Clear();
            doorMarkersByPortId.Clear();
            doorNavigationByPortId.Clear();
            doorDirectionByPortId.Clear();
            doorVisualStateByPortId.Clear();
            openDoorPortIds.Clear();
            hazardMarkers.Clear();
            interactiveObjectMarkers.Clear();
            dynamicNavigationObjects.Clear();
            ClearWallVisibilityController();
        }

        public bool SetRuntimeNavMeshActive(bool active, RoomNavMeshRuntimeFallbackMode fallbackMode = RoomNavMeshRuntimeFallbackMode.RequireCatalogBake)
        {
            if (!active)
            {
                ReleaseNavMesh();
                return true;
            }

            if (HasNavMeshBake)
            {
                return true;
            }

            if (LastBuiltAsset == null)
            {
                navMeshBakeError = "no_room_asset_for_navmesh_attach";
                return false;
            }

            AttachNavMesh(LastBuiltAsset, fallbackMode);
            return HasNavMeshBake;
        }

        private void OnDestroy()
        {
            ReleaseNavMesh();
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

        public void ApplyAvailableDoorPorts(System.Collections.Generic.IEnumerable<string> availablePortIds)
        {
            if (LastBuiltAsset == null)
            {
                return;
            }

            var knownPortIds = new HashSet<string>(DoorPorts.Select(port => port.Id));
            var nextOpenPortIds = new HashSet<string>((availablePortIds ?? System.Array.Empty<string>())
                .Where(portId => knownPortIds.Contains(portId)));
            if (openDoorPortIds.SetEquals(nextOpenPortIds))
            {
                return;
            }

            openDoorPortIds.Clear();
            foreach (var portId in nextOpenPortIds)
            {
                openDoorPortIds.Add(portId);
            }

            foreach (var port in DoorPorts)
            {
                ApplyDoorAvailability(port.Id, openDoorPortIds.Contains(port.Id));
            }

            RebuildPerimeterWalls();
        }

        public void SetDoorAvailabilityById(string portId, bool available)
        {
            if (LastBuiltAsset == null || !DoorPorts.Any(port => port.Id == portId))
            {
                return;
            }

            if (openDoorPortIds.Contains(portId) == available)
            {
                return;
            }

            if (available)
            {
                openDoorPortIds.Add(portId);
            }
            else
            {
                openDoorPortIds.Remove(portId);
            }

            ApplyDoorAvailability(portId, available);
            RebuildPerimeterWalls();
        }

        public void SetDoorVisualState(string direction, RoomDoorVisualState state)
        {
            if (!doorRenderersByDirection.TryGetValue(direction, out var renderers))
            {
                return;
            }

            var material = RoomBiomePresentationResolver.ResolveMaterial(BiomeId, MaterialRoleForDoorState(state));
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    var portId = PortIdForDoorRenderer(renderer);
                    if (!string.IsNullOrEmpty(portId) &&
                        doorVisualStateByPortId.TryGetValue(portId, out var existingState) &&
                        existingState == state)
                    {
                        continue;
                    }

                    renderer.sharedMaterial = material;
                    ClearArtPassChildren(renderer.transform);
                    RoomBiomePresentationResolver.InstantiateVisual(BiomeId, PrefabRoleForDoorState(state), renderer.transform, Vector3.zero, Vector3.one);
                    renderer.enabled = false;
                    if (!string.IsNullOrEmpty(portId))
                    {
                        doorVisualStateByPortId[portId] = state;
                    }
                }
            }

            foreach (var pair in doorDirectionByPortId)
            {
                if (pair.Value == direction && doorNavigationByPortId.TryGetValue(pair.Key, out var navigation) && navigation != null)
                {
                    navigation.ApplyDoorState(state);
                }
            }
        }

        public void SetDoorVisualStateById(string portId, RoomDoorVisualState state)
        {
            if (!doorRenderersByPortId.TryGetValue(portId, out var renderer) || renderer == null)
            {
                return;
            }

            if (doorVisualStateByPortId.TryGetValue(portId, out var existingState) && existingState == state)
            {
                return;
            }

            renderer.sharedMaterial = RoomBiomePresentationResolver.ResolveMaterial(BiomeId, MaterialRoleForDoorState(state));
            if (doorMarkersByPortId.TryGetValue(portId, out var marker) && marker != null)
            {
                ClearArtPassChildren(marker.transform);
                RoomBiomePresentationResolver.InstantiateVisual(BiomeId, PrefabRoleForDoorState(state), marker.transform, Vector3.zero, Vector3.one);
                renderer.enabled = false;
            }

            doorVisualStateByPortId[portId] = state;
            if (doorNavigationByPortId.TryGetValue(portId, out var navigation) && navigation != null)
            {
                navigation.ApplyDoorState(state);
            }
        }

        private string PortIdForDoorRenderer(Renderer renderer)
        {
            foreach (var pair in doorRenderersByPortId)
            {
                if (pair.Value == renderer)
                {
                    return pair.Key;
                }
            }

            return string.Empty;
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

        public void SetDynamicNavigationDebugLabelsVisible(bool visible)
        {
            foreach (var marker in dynamicNavigationObjects)
            {
                if (marker != null)
                {
                    marker.SetDebugLabelVisible(visible);
                }
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
            dynamicNavigationObjects.RemoveAll(marker => marker == null);
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

        private IEnumerator ClearChildrenStaged()
        {
            const int destroyBudgetPerFrame = 24;
            var destroyedThisFrame = 0;
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index).gameObject;
                child.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }

                destroyedThisFrame++;
                if (destroyedThisFrame >= destroyBudgetPerFrame)
                {
                    M136PerformanceOperationCounters.ReportTransitionDestroyedObjects(destroyedThisFrame);
                    M136PerformanceOperationCounters.ReportRoomBuildStage();
                    destroyedThisFrame = 0;
                    yield return null;
                }
            }

            M136PerformanceOperationCounters.ReportTransitionDestroyedObjects(destroyedThisFrame);
        }

        private void CancelPendingStagedBuild()
        {
            if (pendingStagingRoot != null)
            {
                DestroyRuntimeChild(pendingStagingRoot);
            }

            pendingStagingRoot = null;
            pendingOldChildren = null;
            if (buildParentOverride != null && buildParentOverride.name == StagingRootName)
            {
                buildParentOverride = null;
            }
        }

        private List<GameObject> SnapshotCurrentChildren()
        {
            var children = new List<GameObject>(transform.childCount);
            for (var index = 0; index < transform.childCount; index++)
            {
                var child = transform.GetChild(index);
                if (child != null)
                {
                    children.Add(child.gameObject);
                }
            }

            return children;
        }

        private GameObject CreateStagingRoot()
        {
            var stagingRoot = new GameObject(StagingRootName);
            stagingRoot.transform.SetParent(transform, false);
            stagingRoot.SetActive(false);
            return stagingRoot;
        }

        private void CommitStagedBuild(GameObject stagingRoot, List<GameObject> oldChildren, bool suppressRenderersForReveal)
        {
            if (stagingRoot == null)
            {
                return;
            }

            for (var index = 0; index < oldChildren.Count; index++)
            {
                if (oldChildren[index] != null)
                {
                    oldChildren[index].SetActive(false);
                }
            }

            stagingRoot.SetActive(true);
            while (stagingRoot.transform.childCount > 0)
            {
                stagingRoot.transform.GetChild(0).SetParent(transform, worldPositionStays: false);
            }

            stagingRoot.SetActive(false);
            DestroyRuntimeChild(stagingRoot);
            if (suppressRenderersForReveal)
            {
                SuppressRuntimeRenderersForReveal();
            }
        }

        private IEnumerator DestroyHiddenChildrenStaged(List<GameObject> oldChildren)
        {
            const int destroyBudgetPerFrame = 24;
            var destroyedThisFrame = 0;
            for (var index = 0; index < oldChildren.Count; index++)
            {
                var child = oldChildren[index];
                if (child == null)
                {
                    continue;
                }

                child.SetActive(false);
                DestroyRuntimeChild(child);
                destroyedThisFrame++;
                if (destroyedThisFrame >= destroyBudgetPerFrame)
                {
                    M136PerformanceOperationCounters.ReportTransitionDestroyedObjects(destroyedThisFrame);
                    M136PerformanceOperationCounters.ReportRoomBuildStage();
                    destroyedThisFrame = 0;
                    yield return null;
                }
            }

            M136PerformanceOperationCounters.ReportTransitionDestroyedObjects(destroyedThisFrame);
        }

        private void ResetRuntimeCollectionsForBuild(RoomRuntimeBuildDescriptor descriptor)
        {
            ReleaseNavMesh();
            perimeterWallsRoot = null;
            doorRenderersByDirection.Clear();
            doorRenderersByPortId.Clear();
            doorMarkersByPortId.Clear();
            doorNavigationByPortId.Clear();
            doorDirectionByPortId.Clear();
            doorVisualStateByPortId.Clear();
            openDoorPortIds.Clear();
            foreach (var port in descriptor.DoorPorts)
            {
                openDoorPortIds.Add(port.Id);
            }

            hazardMarkers.Clear();
            interactiveObjectMarkers.Clear();
            dynamicNavigationObjects.Clear();
        }

        private static IEnumerator ReportBuildStageAndYield(GameObject stagingRoot = null)
        {
            if (stagingRoot != null && stagingRoot.activeInHierarchy)
            {
                M136PerformanceOperationCounters.ReportStagedRoomVisibleRendererFrame();
            }

            M136PerformanceOperationCounters.ReportRoomBuildStage();
            yield return null;
        }

        private void BuildFloor(RoomLayout layout, string biomeId)
        {
            foreach (var region in layout.FloorRegions)
            {
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = $"tileGround.{region.Id}";
                floor.transform.SetParent(BuildParent, false);
                floor.transform.localPosition = new Vector3(region.Center.x, -0.05f, region.Center.z);
                floor.transform.localScale = new Vector3(region.HalfSize.x * 2f, 0.1f, region.HalfSize.y * 2f);
                RoomBiomePresentationResolver.ApplyTo(biomeId, floor, MaterialRole.RoomFloor);
                RoomBiomePresentationResolver.InstantiateVisual(biomeId, PresentationPrefabRole.RoomFloor, floor.transform, Vector3.zero, Vector3.one);
            }

        }

        private void BuildPerimeterWalls(RoomLayout layout, IReadOnlyList<RoomDoorPort> doorPorts, string biomeId)
        {
            if (layout == null)
            {
                ClearWallVisibilityController();
                return;
            }

            var parent = new GameObject("PerimeterWalls");
            parent.transform.SetParent(BuildParent, false);
            perimeterWallsRoot = parent;
            var bindings = new List<RoomWallVisibilityController.WallBinding>();

            foreach (var edge in RoomWallOutlineUtility.BuildExposedEdges(layout))
            {
                BuildWallSide(
                    parent.transform,
                    edge.Side,
                    edge.AxisMin,
                    edge.AxisMax,
                    OffsetWallCoordinate(edge.Side, edge.FixedCoordinate),
                    edge.HorizontalOnX,
                    DoorGapsForSide(edge.Side, doorPorts),
                    bindings,
                    biomeId);
            }

            var controller = GetComponent<RoomWallVisibilityController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<RoomWallVisibilityController>();
            }

            controller.Configure(bindings, layout.Bounds, biomeId);
        }

        private void RebuildPerimeterWalls()
        {
            DestroyPerimeterWallRoots();

            BuildPerimeterWalls(CurrentLayout, OpenDoorPorts(), BiomeId);
        }

        private void DestroyPerimeterWallRoots()
        {
            var roots = new List<GameObject>();
            if (perimeterWallsRoot != null)
            {
                roots.Add(perimeterWallsRoot);
            }

            var root = BuildParent;
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index).gameObject;
                if (child.name == "PerimeterWalls" && !roots.Contains(child))
                {
                    roots.Add(child);
                }
            }

            foreach (var wallRoot in roots)
            {
                HideAndDestroyRuntimeChild(wallRoot);
            }

            perimeterWallsRoot = null;
        }

        private IReadOnlyList<RoomDoorPort> OpenDoorPorts()
        {
            return DoorPorts
                .Where(port => openDoorPortIds.Contains(port.Id))
                .ToArray();
        }

        private void ApplyDoorAvailability(string portId, bool available)
        {
            if (doorMarkersByPortId.TryGetValue(portId, out var marker) && marker != null)
            {
                marker.SetActive(available);
            }

            if (!available &&
                doorNavigationByPortId.TryGetValue(portId, out var navigation) &&
                navigation != null)
            {
                navigation.ApplyDoorState(RoomDoorVisualState.Active);
            }
        }

        private static float OffsetWallCoordinate(RoomWallSide side, float fixedCoordinate)
        {
            return side is RoomWallSide.North or RoomWallSide.West
                ? fixedCoordinate - PerimeterWallThicknessMeters * 0.5f
                : fixedCoordinate + PerimeterWallThicknessMeters * 0.5f;
        }

        private static void BuildWallSide(
            Transform parent,
            RoomWallSide side,
            float axisMin,
            float axisMax,
            float fixedCoordinate,
            bool horizontalOnX,
            List<Vector2> gaps,
            List<RoomWallVisibilityController.WallBinding> bindings,
            string biomeId)
        {
            gaps.Sort((left, right) => left.x.CompareTo(right.x));
            var cursor = axisMin;
            var segmentIndex = 0;
            foreach (var gap in gaps)
            {
                var clampedStart = Mathf.Clamp(gap.x, axisMin, axisMax);
                var clampedEnd = Mathf.Clamp(gap.y, axisMin, axisMax);
                if (clampedEnd <= cursor)
                {
                    continue;
                }

                segmentIndex = CreateWallSegment(
                    parent,
                    side,
                    segmentIndex,
                    cursor,
                    Mathf.Max(cursor, clampedStart),
                    fixedCoordinate,
                    horizontalOnX,
                    bindings,
                    biomeId);
                cursor = Mathf.Max(cursor, clampedEnd);
            }

            CreateWallSegment(parent, side, segmentIndex, cursor, axisMax, fixedCoordinate, horizontalOnX, bindings, biomeId);
        }

        private static int CreateWallSegment(
            Transform parent,
            RoomWallSide side,
            int segmentIndex,
            float axisStart,
            float axisEnd,
            float fixedCoordinate,
            bool horizontalOnX,
            List<RoomWallVisibilityController.WallBinding> bindings,
            string biomeId)
        {
            var length = axisEnd - axisStart;
            if (length < MinimumWallSegmentLengthMeters)
            {
                return segmentIndex;
            }

            var localPosition = Vector3.zero;
            var localScale = Vector3.one;
            if (horizontalOnX)
            {
                localPosition = new Vector3((axisStart + axisEnd) * 0.5f, PerimeterWallHeightMeters * 0.5f, fixedCoordinate);
                localScale = new Vector3(length, PerimeterWallHeightMeters, PerimeterWallThicknessMeters);
            }
            else
            {
                localPosition = new Vector3(fixedCoordinate, PerimeterWallHeightMeters * 0.5f, (axisStart + axisEnd) * 0.5f);
                localScale = new Vector3(PerimeterWallThicknessMeters, PerimeterWallHeightMeters, length);
            }

            var wall = RoomWallMeshUtility.CreateSegment(
                $"wall.{side.ToString().ToLowerInvariant()}.{segmentIndex}",
                parent,
                localPosition,
                localScale,
                RoomBiomePresentationResolver.ResolveMaterial(biomeId, MaterialRole.RoomWall));
            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                bindings.Add(new RoomWallVisibilityController.WallBinding(side, renderer));
            }

            return segmentIndex + 1;
        }

        private static List<Vector2> DoorGapsForSide(RoomWallSide side, IReadOnlyList<RoomDoorPort> doorPorts)
        {
            var gaps = new List<Vector2>();
            if (doorPorts == null)
            {
                return gaps;
            }

            var direction = DirectionForWallSide(side);
            foreach (var port in doorPorts)
            {
                if (port == null || port.Direction != direction)
                {
                    continue;
                }

                var center = side is RoomWallSide.North or RoomWallSide.South
                    ? port.Position.x
                    : port.Position.z;
                var halfGap = PerimeterWallDoorGapMeters * 0.5f;
                gaps.Add(new Vector2(center - halfGap, center + halfGap));
            }

            return gaps;
        }

        private static string DirectionForWallSide(RoomWallSide side)
        {
            return side switch
            {
                RoomWallSide.North => "north",
                RoomWallSide.South => "south",
                RoomWallSide.East => "east",
                RoomWallSide.West => "west",
                _ => "north"
            };
        }

        private void BuildObstacles(RoomLayout layout, string biomeId)
        {
            foreach (var obstacle in layout.Obstacles)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"{obstacle.Kind}.{obstacle.Id}";
                block.transform.SetParent(BuildParent, false);
                block.transform.localPosition = obstacle.Center;
                block.transform.localScale = obstacle.Size;
                RoomBiomePresentationResolver.ApplyTo(biomeId, block, MaterialRole.RoomObstacleRock);
                var visualScale = UniformObstacleVisualScale(obstacle.Size);
                var visualCenter = obstacle.Center;
                visualCenter.y = obstacle.Center.y - obstacle.Size.y * 0.5f + visualScale * 0.5f;
                var visualAnchor = new GameObject($"ArtPassAnchor.{obstacle.Kind}.{obstacle.Id}");
                visualAnchor.transform.SetParent(BuildParent, false);
                visualAnchor.transform.localPosition = visualCenter;
                visualAnchor.transform.localRotation = Quaternion.identity;
                visualAnchor.transform.localScale = Vector3.one * visualScale;
                var visual = RoomBiomePresentationResolver.InstantiateVisual(biomeId, PresentationPrefabRole.RoomObstacleRock, visualAnchor.transform, Vector3.zero, Vector3.one);
                if (visual != null && block.TryGetComponent<Renderer>(out var renderer))
                {
                    renderer.enabled = false;
                }

                var navigationMarker = block.AddComponent<RoomDynamicNavigationObjectMarker>();
                navigationMarker.ConfigureStaticBaked(
                    obstacle.Id,
                    obstacle.Kind,
                    obstacle.Size,
                    RoomDynamicNavigationObjectCategory.StaticBakedBlocker,
                    "static_rock_baked_into_room_navmesh");
                dynamicNavigationObjects.Add(navigationMarker);
            }
        }

        private static float UniformObstacleVisualScale(Vector3 size)
        {
            var scale = Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            return scale > 0.0001f ? scale : 1f;
        }

        private void BuildHoleMarkers(RoomLayout layout)
        {
            foreach (var hole in layout.HoleTiles ?? System.Array.Empty<Vector2Int>())
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"holeTile.{hole.x}_{hole.y}";
                marker.transform.SetParent(BuildParent, false);
                marker.transform.localPosition = new Vector3(hole.x, 0.012f, hole.y);
                marker.transform.localScale = new Vector3(0.88f, 0.024f, 0.88f);
                MaterialResolver.ApplyTo(marker, MaterialRole.RoomHazardSpike);
                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }

                var navigationMarker = marker.AddComponent<RoomDynamicNavigationObjectMarker>();
                navigationMarker.ConfigureStaticBaked(
                    $"hole_{hole.x}_{hole.y}",
                    "hole",
                    new Vector3(1f, RoomNavMeshBuildUtility.StaticBlockerHeightMeters, 1f),
                    RoomDynamicNavigationObjectCategory.HoleBakedBlocker,
                    "hole_baked_into_room_navmesh");
                dynamicNavigationObjects.Add(navigationMarker);
            }
        }

        private void BuildHazards(RoomRuntimeBuildDescriptor descriptor)
        {
            foreach (var hazard in descriptor.Hazards)
            {
                if (hazard == null)
                {
                    continue;
                }

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = $"{hazard.kind}.{hazard.id}";
                marker.transform.SetParent(BuildParent, false);
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

        private void BuildInteractiveObjects(RoomRuntimeBuildDescriptor descriptor)
        {
            foreach (var roomObject in descriptor.InteractiveObjects)
            {
                if (roomObject == null)
                {
                    continue;
                }

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"{roomObject.kind}.{roomObject.id}";
                marker.transform.SetParent(BuildParent, false);
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

                var navigationMarker = marker.AddComponent<RoomDynamicNavigationObjectMarker>();
                if (objectMarker.BlocksMovement)
                {
                    navigationMarker.ConfigureDynamicCarver(
                        objectMarker.ObjectId,
                        objectMarker.ObjectKind,
                        objectMarker.SizeMeters,
                        active: true,
                        reason: "interactive_blocker_live");
                }
                else
                {
                    navigationMarker.ConfigureStaticBaked(
                        objectMarker.ObjectId,
                        objectMarker.ObjectKind,
                        objectMarker.SizeMeters,
                        RoomDynamicNavigationObjectCategory.NonBlocking,
                        "interactive_non_blocking");
                }

                dynamicNavigationObjects.Add(navigationMarker);
            }
        }

        private void BuildDecor(RoomRuntimeBuildDescriptor descriptor, string biomeId)
        {
            foreach (var decor in descriptor.Decor)
            {
                if (decor == null ||
                    !RoomBiomePresentationResolver.TryResolveDecorPrefabRole(biomeId, decor.kind, out var prefabRole))
                {
                    continue;
                }

                var marker = new GameObject($"{decor.kind}.{decor.id}");
                marker.transform.SetParent(BuildParent, false);
                marker.transform.localPosition = decor.center?.ToUnityVector3() ?? Vector3.zero;
                marker.transform.localRotation = Quaternion.identity;
                marker.transform.localScale = decor.size?.ToUnityVector3() ?? Vector3.one;
                RoomBiomePresentationResolver.InstantiateVisual(biomeId, prefabRole, marker.transform, Vector3.zero, Vector3.one);
            }
        }


        private void AttachNavMesh(ImportedRoomRuntimeAsset asset, RoomNavMeshRuntimeFallbackMode fallbackMode)
        {
            ReleaseNavMesh();
            navMeshBakeError = string.Empty;
            navMeshBakeSource = string.Empty;
            var roomId = asset?.Id ?? string.Empty;
            var catalog = RoomNavMeshCatalogDefinition.LoadDefault();
            if (catalog != null &&
                catalog.TryGetNavMeshData(roomId, out activeNavMeshData, out var resolvedBakeRoomId) &&
                activeNavMeshData != null)
            {
                var source = string.Equals(resolvedBakeRoomId, roomId, System.StringComparison.Ordinal)
                    ? "catalog"
                    : $"catalog-shared:{resolvedBakeRoomId}";
                M136PerformanceOperationCounters.ReportNavMeshCatalogAttach();
                AttachResolvedNavMeshData(roomId, runtimeBuilt: false, source: source);
                return;
            }

            navMeshBakeError = catalog == null
                ? RoomNavMeshCatalogDefinition.MissingCatalogMessage()
                : RoomNavMeshCatalogDefinition.MissingBakeMessage(roomId);
            if (CanUseRuntimeNavMeshFallback(fallbackMode))
            {
                var isStressHarnessBake = fallbackMode == RoomNavMeshRuntimeFallbackMode.AutomatedStressHarnessRuntimeBake;
                if (isStressHarnessBake && !IsAutomatedStressHarnessRoom(roomId))
                {
                    navMeshBakeError = $"{navMeshBakeError}:stress_harness_bake_requires_m138_room";
                    Debug.LogError(
                        $"Room '{roomId}' requested automated stress-harness NavMesh baking outside an m138_* generated stress room.",
                        this);
                    return;
                }

                activeNavMeshData = RoomNavMeshBuildUtility.BuildRoom(
                    asset,
                    isStressHarnessBake ? "NavMesh.M138StressHarness" : "NavMesh.DevRuntime",
                    out var runtimeBuildError);
                if (activeNavMeshData != null)
                {
                    if (isStressHarnessBake)
                    {
                        M136PerformanceOperationCounters.ReportStressHarnessNavMeshBake();
                        AttachResolvedNavMeshData(roomId, runtimeBuilt: true, source: "m138-stress-harness-runtime-bake");
                    }
                    else
                    {
                        M136PerformanceOperationCounters.ReportRuntimeNavMeshFallback();
                        Debug.LogWarning(
                            $"Room '{roomId}' is using a dev-only runtime Unity NavMesh fallback because no catalog bake was found. Run {RoomNavMeshCatalogDefinition.PreferredBakeMenuPath} before shipping or locking QA. Missing bake: {navMeshBakeError}",
                            this);
                        AttachResolvedNavMeshData(roomId, runtimeBuilt: true, source: "dev-runtime-fallback");
                    }

                    return;
                }

                navMeshBakeError = $"{navMeshBakeError}:runtime_build_failed={runtimeBuildError}";
            }

            Debug.LogError(
                $"Room '{roomId}' has no usable Unity NavMesh. {navMeshBakeError}. Run {RoomNavMeshCatalogDefinition.PreferredBakeMenuPath}. Dev-only runtime fallback mode: {fallbackMode}.",
                this);
        }

        private void AttachResolvedNavMeshData(string roomId, bool runtimeBuilt, string source)
        {
            activeNavMeshWasRuntimeBuilt = runtimeBuilt;
            navMeshBakeError = string.Empty;
            navMeshBakeSource = source ?? string.Empty;

            navMeshDataInstance = NavMesh.AddNavMeshData(activeNavMeshData, transform.position, transform.rotation);
            if (!navMeshDataInstance.valid)
            {
                navMeshBakeError = runtimeBuilt ? $"invalid_runtime_navmesh:{roomId}" : $"invalid_navmesh_bake:{roomId}";
                navMeshBakeSource = string.Empty;
                Debug.LogError($"Room '{roomId}' has invalid Unity NavMesh data.", this);
            }
        }

        private static bool CanUseRuntimeNavMeshFallback(RoomNavMeshRuntimeFallbackMode fallbackMode)
        {
            return (fallbackMode == RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake ||
                    fallbackMode == RoomNavMeshRuntimeFallbackMode.AutomatedStressHarnessRuntimeBake) &&
                (Application.isEditor || Debug.isDebugBuild);
        }

        private static bool IsAutomatedStressHarnessRoom(string roomId)
        {
            return !string.IsNullOrWhiteSpace(roomId) &&
                roomId.StartsWith("m138_", System.StringComparison.Ordinal);
        }

        private void ReleaseNavMesh()
        {
            if (navMeshDataInstance.valid)
            {
                navMeshDataInstance.Remove();
            }

            navMeshDataInstance = default;
            if (activeNavMeshWasRuntimeBuilt && activeNavMeshData != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(activeNavMeshData);
                }
                else
                {
                    DestroyImmediate(activeNavMeshData);
                }
            }

            activeNavMeshData = null;
            activeNavMeshWasRuntimeBuilt = false;
            navMeshBakeError = string.Empty;
            navMeshBakeSource = string.Empty;
        }

        private void ConfigureCarvingObstacles()
        {
            foreach (var marker in interactiveObjectMarkers)
            {
                if (marker != null)
                {
                    ConfigureCarvingObstacle(marker.gameObject, marker);
                }
            }
        }

        private static void ConfigureCarvingObstacle(GameObject marker, RoomInteractiveObjectMarker objectMarker)
        {
            if (marker == null || objectMarker == null || !objectMarker.BlocksMovement)
            {
                return;
            }

            if (!marker.TryGetComponent<RoomDynamicNavigationObjectMarker>(out var navigationMarker))
            {
                navigationMarker = marker.AddComponent<RoomDynamicNavigationObjectMarker>();
            }

            navigationMarker.ConfigureDynamicCarver(
                objectMarker.ObjectId,
                objectMarker.ObjectKind,
                objectMarker.SizeMeters,
                active: true,
                reason: "interactive_blocker_refreshed");
        }

        private void BuildDoors(RoomRuntimeBuildDescriptor descriptor, string biomeId)
        {
            foreach (var port in descriptor.DoorPorts)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"doorAnchorActive.{port.Id}";
                marker.transform.SetParent(BuildParent, false);
                marker.transform.localPosition = new Vector3(port.Position.x, DoorVisualCenterY, port.Position.z);
                marker.transform.localScale = DoorScaleFor(port.Direction);
                RoomBiomePresentationResolver.ApplyTo(biomeId, marker, MaterialRole.DoorActive);
                RoomBiomePresentationResolver.InstantiateVisual(biomeId, PresentationPrefabRole.DoorActive, marker.transform, Vector3.zero, Vector3.one);
                var renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }

                doorRenderersByPortId[port.Id] = renderer;
                doorMarkersByPortId[port.Id] = marker;
                doorDirectionByPortId[port.Id] = port.Direction;
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

                var navigationMarker = marker.AddComponent<RoomDynamicNavigationObjectMarker>();
                navigationMarker.ConfigureDoor(port.Id, port.Kind, DoorScaleFor(port.Direction), RoomDoorVisualState.Active);
                doorNavigationByPortId[port.Id] = navigationMarker;
                dynamicNavigationObjects.Add(navigationMarker);
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

        private void BuildSpawnMarkers(RoomRuntimeBuildDescriptor descriptor)
        {
            if (descriptor.SafeStart != null)
            {
                CreateSpawnMarker(descriptor.SafeStart.id, descriptor.SafeStart.kind, descriptor.SafeStart.position.ToUnityVector3(), addPlayerSpawnComponent: true);
            }

            foreach (var spawn in descriptor.EnemySpawns)
            {
                CreateSpawnMarker(spawn.id, spawn.kind, spawn.position.ToUnityVector3(), addPlayerSpawnComponent: false);
            }

            foreach (var spawn in descriptor.ItemSpawns)
            {
                CreateSpawnMarker(spawn.id, spawn.kind, spawn.position.ToUnityVector3(), addPlayerSpawnComponent: false);
            }
        }

        private void CreateSpawnMarker(string id, string kind, Vector3 position, bool addPlayerSpawnComponent)
        {
            var marker = new GameObject($"{kind}.{id}");
            marker.transform.SetParent(BuildParent, false);
            marker.transform.localPosition = position;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one;

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

        private static void HideAndDestroyRuntimeChild(GameObject child)
        {
            if (child == null)
            {
                return;
            }

            child.SetActive(false);
            DestroyRuntimeChild(child);
        }

        private static void RemoveGeneratedCollider(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            collider.enabled = false;
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        private void ClearWallVisibilityController()
        {
            var controller = GetComponent<RoomWallVisibilityController>();
            if (controller != null)
            {
                controller.Configure(System.Array.Empty<RoomWallVisibilityController.WallBinding>());
            }
        }

        private static Vector3 DoorScaleFor(string direction)
        {
            return direction == "east" || direction == "west"
                ? new Vector3(0.18f, DoorVisualHeightMeters, 1f)
                : new Vector3(1f, DoorVisualHeightMeters, 0.18f);
        }

    }
}
