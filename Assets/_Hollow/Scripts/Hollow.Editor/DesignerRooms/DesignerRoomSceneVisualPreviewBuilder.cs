using System;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hollow.Editor.DesignerRooms
{
    public static class DesignerRoomSceneVisualPreviewBuilder
    {
        public const string PreviewRootName = "RuntimePreview_DO_NOT_EXPORT";

        private const HideFlags PreviewHideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        public static bool HasPreview(Scene scene)
        {
            return FindPreviewRoot(scene) != null;
        }

        public static GameObject BuildPreview(Scene scene, bool includeLighting = true, bool includeCamera = true)
        {
            var roomRoot = DesignerRoomSceneAuthoringUtility.FindRoomRoot(scene);
            if (roomRoot == null)
            {
                throw new InvalidOperationException("Active scene is missing a DesignerRoom root marker.");
            }

            ClearPreview(scene);

            var project = DesignerRoomSceneAuthoringUtility.BuildRoomDesignerProject(scene);
            var asset = RoomDesignerCompiler.Compile(project);
            var previewRoot = CreatePreviewObject(PreviewRootName, roomRoot.transform);
            previewRoot.transform.localPosition = Vector3.zero;
            previewRoot.transform.localRotation = Quaternion.identity;
            previewRoot.transform.localScale = Vector3.one;

            BuildFloor(previewRoot.transform, asset.Layout);
            BuildHoles(previewRoot.transform, asset.Layout);
            BuildObstacles(previewRoot.transform, asset.Layout);
            BuildHazards(previewRoot.transform, asset);
            BuildInteractiveObjects(previewRoot.transform, asset);
            BuildDoors(previewRoot.transform, asset);
            BuildSpawns(previewRoot.transform, asset);

            if (includeLighting)
            {
                BuildLighting(previewRoot.transform, asset.Layout.Bounds);
            }

            if (includeCamera)
            {
                BuildCamera(previewRoot.transform, asset.Layout.Bounds);
            }

            StripColliders(previewRoot);
            ApplyPreviewFlags(previewRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
            return previewRoot;
        }

        public static bool ClearPreview(Scene scene)
        {
            var cleared = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                cleared |= ClearPreviewUnder(root.transform);
            }

            if (cleared)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                SceneView.RepaintAll();
            }

            return cleared;
        }

        public static GameObject FindPreviewRoot(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindPreviewRoot(root.transform);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static bool ClearPreviewUnder(Transform root)
        {
            var cleared = false;
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index);
                if (child.name == PreviewRootName)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    cleared = true;
                    continue;
                }

                cleared |= ClearPreviewUnder(child);
            }

            return cleared;
        }

        private static Transform FindPreviewRoot(Transform root)
        {
            if (root.name == PreviewRootName)
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var found = FindPreviewRoot(root.GetChild(index));
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void BuildFloor(Transform previewRoot, RoomLayout layout)
        {
            var parent = CreatePreviewObject("Floor", previewRoot).transform;
            foreach (var region in layout.FloorRegions)
            {
                var anchor = CreatePreviewObject($"Floor.{region.Id}", parent).transform;
                anchor.localPosition = new Vector3(region.Center.x, -0.05f, region.Center.z);
                var scale = new Vector3(region.HalfSize.x * 2f, 0.1f, region.HalfSize.y * 2f);
                InstantiateVisual(PresentationPrefabRole.RoomFloor, anchor, Vector3.zero, scale);
            }

            var origin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            origin.name = "Origin.0_0";
            origin.transform.SetParent(parent, false);
            origin.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            origin.transform.localScale = new Vector3(0.25f, 0.04f, 0.25f);
            MaterialResolver.ApplyTo(origin, MaterialRole.RoomOriginMarker);
        }

        private static void BuildHoles(Transform previewRoot, RoomLayout layout)
        {
            if (layout.HoleTiles == null || layout.HoleTiles.Count == 0)
            {
                return;
            }

            var parent = CreatePreviewObject("Holes", previewRoot).transform;
            foreach (var tile in layout.HoleTiles)
            {
                var hole = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hole.name = $"Hole.{tile.x}_{tile.y}";
                hole.transform.SetParent(parent, false);
                hole.transform.localPosition = new Vector3(tile.x, 0.01f, tile.y);
                hole.transform.localScale = new Vector3(0.96f, 0.03f, 0.96f);
                MaterialResolver.ApplyTo(hole, MaterialRole.DesignerHole);
            }
        }

        private static void BuildObstacles(Transform previewRoot, RoomLayout layout)
        {
            var parent = CreatePreviewObject("Obstacles", previewRoot).transform;
            foreach (var obstacle in layout.Obstacles)
            {
                var anchor = CreatePreviewObject($"{obstacle.Kind}.{obstacle.Id}", parent).transform;
                anchor.localPosition = obstacle.Center;
                InstantiateVisual(PresentationPrefabRole.RoomObstacleRock, anchor, Vector3.zero, obstacle.Size);
            }
        }

        private static void BuildHazards(Transform previewRoot, ImportedRoomRuntimeAsset asset)
        {
            var parent = CreatePreviewObject("Hazards", previewRoot).transform;
            foreach (var hazard in asset.Hazards ?? Array.Empty<ImportedRoomHazard>())
            {
                if (hazard == null)
                {
                    continue;
                }

                var position = hazard.center?.ToUnityVector3() ?? Vector3.zero;
                var anchor = CreatePreviewObject($"{hazard.kind}.{hazard.id}", parent).transform;
                anchor.localPosition = new Vector3(position.x, 0.05f, position.z);
                InstantiateVisual(PresentationPrefabRole.RoomHazardSpike, anchor, Vector3.zero, Vector3.one);
            }
        }

        private static void BuildInteractiveObjects(Transform previewRoot, ImportedRoomRuntimeAsset asset)
        {
            var parent = CreatePreviewObject("InteractiveObjects", previewRoot).transform;
            foreach (var roomObject in asset.InteractiveObjects ?? Array.Empty<ImportedRoomInteractiveObject>())
            {
                if (roomObject == null)
                {
                    continue;
                }

                var role = roomObject.kind == RoomDesignerMarkerKinds.ExplosiveBarrel
                    ? PresentationPrefabRole.ExplosiveBarrel
                    : PresentationPrefabRole.StandardBarrel;
                var anchor = CreatePreviewObject($"{roomObject.kind}.{roomObject.id}", parent).transform;
                anchor.localPosition = roomObject.center?.ToUnityVector3() ?? Vector3.zero;
                InstantiateVisual(role, anchor, Vector3.zero, roomObject.size?.ToUnityVector3() ?? Vector3.one);
            }
        }

        private static void BuildDoors(Transform previewRoot, ImportedRoomRuntimeAsset asset)
        {
            var parent = CreatePreviewObject("Doors", previewRoot).transform;
            foreach (var port in asset.DoorPorts)
            {
                var role = DoorRoleFor(port.Kind);
                var anchor = CreatePreviewObject($"Door.{port.Id}.{port.Direction}", parent).transform;
                anchor.localPosition = new Vector3(port.Position.x, 0.65f, port.Position.z);
                anchor.localRotation = DoorRotationFor(port.Direction);
                InstantiateVisual(role, anchor, Vector3.zero, DoorScaleFor(port.Direction));
            }
        }

        private static void BuildSpawns(Transform previewRoot, ImportedRoomRuntimeAsset asset)
        {
            var parent = CreatePreviewObject("Spawns", previewRoot).transform;
            if (asset.SafeStart != null)
            {
                var anchor = CreatePreviewObject($"SafeStart.{asset.SafeStart.id}", parent).transform;
                anchor.localPosition = SpawnPosition(asset.SafeStart.position?.ToUnityVector3() ?? Vector3.zero);
                InstantiateVisual(PresentationPrefabRole.Player, anchor, Vector3.zero, Vector3.one * 0.7f);
            }

            var enemies = CreatePreviewObject("Enemies", parent).transform;
            foreach (var spawn in asset.EnemySpawns ?? Array.Empty<ImportedSpawnPoint>())
            {
                if (spawn == null)
                {
                    continue;
                }

                var role = RoomDesignerScenePreviewBuilder.PrefabRoleForMarker(spawn.kind);
                var anchor = CreatePreviewObject($"{DesignerRoomSceneAuthoringUtility.DisplayNameForRuntimeKind(spawn.kind)}.{spawn.id}", enemies).transform;
                anchor.localPosition = SpawnPosition(spawn.position?.ToUnityVector3() ?? Vector3.zero);
                InstantiateVisual(role, anchor, Vector3.zero, EnemyScaleFor(spawn.kind));
            }

            var items = CreatePreviewObject("Items", parent).transform;
            foreach (var spawn in asset.ItemSpawns ?? Array.Empty<ImportedSpawnPoint>())
            {
                if (spawn == null)
                {
                    continue;
                }

                var role = RoomDesignerScenePreviewBuilder.PrefabRoleForMarker(spawn.kind);
                var anchor = CreatePreviewObject($"{DesignerRoomSceneAuthoringUtility.DisplayNameForRuntimeKind(spawn.kind)}.{spawn.id}", items).transform;
                anchor.localPosition = SpawnPosition(spawn.position?.ToUnityVector3() ?? Vector3.zero);
                InstantiateVisual(role, anchor, Vector3.zero, Vector3.one * 0.8f);
            }
        }

        private static void BuildLighting(Transform previewRoot, Rect bounds)
        {
            var parent = CreatePreviewObject("PreviewLighting", previewRoot).transform;
            var center = new Vector3(bounds.center.x, 0f, bounds.center.y);
            var radius = Mathf.Max(bounds.width, bounds.height);

            var key = CreatePreviewObject("PreviewKeyLight", parent);
            key.transform.localPosition = center + new Vector3(-radius * 0.25f, 7f, -radius * 0.25f);
            key.transform.localRotation = Quaternion.Euler(55f, -35f, 0f);
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.2f;
            keyLight.color = new Color(1f, 0.92f, 0.82f);

            var fill = CreatePreviewObject("PreviewFillLight", parent);
            fill.transform.localPosition = center + new Vector3(radius * 0.25f, 4.5f, radius * 0.25f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = Mathf.Max(8f, radius * 1.4f);
            fillLight.intensity = 0.85f;
            fillLight.color = new Color(0.55f, 0.72f, 1f);

            var rim = CreatePreviewObject("PreviewRimLight", parent);
            rim.transform.localPosition = center + new Vector3(0f, 3.2f, -radius * 0.45f);
            var rimLight = rim.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.range = Mathf.Max(6f, radius);
            rimLight.intensity = 0.45f;
            rimLight.color = new Color(0.45f, 1f, 0.78f);
        }

        private static void BuildCamera(Transform previewRoot, Rect bounds)
        {
            var cameraObject = CreatePreviewObject("PreviewCamera_TopDown", previewRoot);
            var center = new Vector3(bounds.center.x, 0f, bounds.center.y);
            cameraObject.transform.localPosition = center + Vector3.up * 14f;
            cameraObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.width, bounds.height) * 0.62f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 40f;
            camera.enabled = false;
        }

        private static GameObject CreatePreviewObject(string name, Transform parent)
        {
            var go = new GameObject(name)
            {
                hideFlags = PreviewHideFlags
            };
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void InstantiateVisual(PresentationPrefabRole role, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            var visual = PresentationPrefabResolver.InstantiateVisual(role, parent, localPosition, localScale);
            if (visual == null)
            {
                return;
            }

            visual.hideFlags = PreviewHideFlags;
            ApplyPreviewFlags(visual);
        }

        private static void ApplyPreviewFlags(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            root.hideFlags = PreviewHideFlags;
            foreach (Transform child in root.transform)
            {
                ApplyPreviewFlags(child.gameObject);
            }
        }

        private static void StripColliders(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static PresentationPrefabRole DoorRoleFor(string kind)
        {
            return kind switch
            {
                RoomDesignerDoorKinds.Secret => PresentationPrefabRole.SecretDoorDebug,
                RoomDesignerDoorKinds.Available => PresentationPrefabRole.DoorUnavailable,
                RoomDesignerDoorKinds.Inactive => PresentationPrefabRole.DoorUnavailable,
                _ => PresentationPrefabRole.DoorActive
            };
        }

        private static Quaternion DoorRotationFor(string direction)
        {
            return direction is "east" or "west"
                ? Quaternion.Euler(0f, 90f, 0f)
                : Quaternion.identity;
        }

        private static Vector3 DoorScaleFor(string direction)
        {
            return direction is "east" or "west"
                ? new Vector3(0.18f, 1.3f, 1f)
                : new Vector3(1f, 1.3f, 0.18f);
        }

        private static Vector3 SpawnPosition(Vector3 position)
        {
            return new Vector3(position.x, Mathf.Max(0.18f, position.y + 0.25f), position.z);
        }

        private static Vector3 EnemyScaleFor(string spawnKind)
        {
            return spawnKind switch
            {
                RoomDesignerMarkerKinds.EnemySpittingPod => new Vector3(0.78f, 0.58f, 0.78f),
                RoomDesignerMarkerKinds.EnemyRat => new Vector3(0.46f, 0.22f, 0.28f),
                RoomDesignerMarkerKinds.EnemySpider => new Vector3(0.5f, 0.2f, 0.5f),
                RoomDesignerMarkerKinds.EnemyHollowBird => new Vector3(0.48f, 0.28f, 0.58f),
                RoomDesignerMarkerKinds.EnemyHollowBeast => new Vector3(0.68f, 0.42f, 0.52f),
                RoomDesignerMarkerKinds.EnemySkeletonSword => new Vector3(0.54f, 0.78f, 0.42f),
                RoomDesignerMarkerKinds.EnemySkeletonSpear => new Vector3(0.54f, 0.78f, 0.5f),
                RoomDesignerMarkerKinds.EnemyKnight => new Vector3(0.68f, 0.98f, 0.52f),
                RoomDesignerMarkerKinds.EnemyGiant => new Vector3(1.05f, 1.35f, 0.82f),
                RoomDesignerMarkerKinds.EnemyHollowArcher => new Vector3(0.52f, 0.82f, 0.42f),
                RoomDesignerMarkerKinds.EnemyPowderGunner => new Vector3(0.62f, 0.86f, 0.5f),
                RoomDesignerMarkerKinds.EnemyKnifeThrower => new Vector3(0.5f, 0.72f, 0.42f),
                RoomDesignerMarkerKinds.EnemyRepeaterTurret => new Vector3(0.78f, 0.66f, 0.78f),
                RoomDesignerMarkerKinds.EnemyClockworkSentry => new Vector3(0.82f, 0.92f, 0.72f),
                RoomDesignerMarkerKinds.EnemyStarforgedOctantSentry => new Vector3(0.92f, 0.86f, 0.92f),
                RoomDesignerMarkerKinds.EnemyCrimsonRailSpider => new Vector3(0.9f, 0.72f, 1.05f),
                RoomDesignerMarkerKinds.EnemyAzureMinigunTurret => new Vector3(0.94f, 0.78f, 0.94f),
                RoomDesignerMarkerKinds.EnemyHollowAcolyte => new Vector3(0.56f, 0.78f, 0.44f),
                RoomDesignerMarkerKinds.EnemyWraith => new Vector3(0.5f, 0.86f, 0.46f),
                RoomDesignerMarkerKinds.EnemySoulEater => new Vector3(0.74f, 0.96f, 0.62f),
                RoomDesignerMarkerKinds.EnemyCurseBinder => new Vector3(0.58f, 0.82f, 0.48f),
                RoomDesignerMarkerKinds.EnemyGraveLantern => new Vector3(0.72f, 0.9f, 0.72f),
                RoomDesignerMarkerKinds.EnemyFlying => new Vector3(0.56f, 0.36f, 0.56f),
                RoomDesignerMarkerKinds.EnemyFast => new Vector3(0.52f, 0.38f, 0.52f),
                RoomDesignerMarkerKinds.EnemyHeavy => new Vector3(0.78f, 0.58f, 0.78f),
                RoomDesignerMarkerKinds.EnemyCharger => new Vector3(0.7f, 0.46f, 0.62f),
                RoomDesignerMarkerKinds.EnemyTurret => new Vector3(0.74f, 0.62f, 0.74f),
                RoomDesignerMarkerKinds.EnemySplitter => new Vector3(0.62f, 0.44f, 0.62f),
                _ => new Vector3(0.6f, 0.42f, 0.6f)
            };
        }
    }
}
