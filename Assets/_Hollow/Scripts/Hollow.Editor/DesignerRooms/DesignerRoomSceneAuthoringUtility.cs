using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Editor.Navigation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hollow.Editor.DesignerRooms
{
    public sealed class DesignerRoomSceneValidationResult
    {
        public readonly List<string> Errors = new();
        public readonly List<string> Warnings = new();

        public bool IsValid => Errors.Count == 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Errors.Add(message);
            }
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Warnings.Add(message);
            }
        }

        public string Summary()
        {
            if (!IsValid)
            {
                return $"Blocked: {Errors.Count} error(s), {Warnings.Count} warning(s)";
            }

            return Warnings.Count == 0 ? "Ready to export" : $"Ready with {Warnings.Count} warning(s)";
        }
    }

    public static class DesignerRoomSceneAuthoringUtility
    {
        public const string DesignerRoomsDirectory = "Assets/_Hollow/Scenes/DesignerRooms";
        public const string ManualExportDirectory = "Assets/_Hollow/Data/Rooms/DesignerDrafts/ManualSceneExports";

        private static readonly string[] DoorStates =
        {
            RoomDesignerDoorKinds.Door,
            RoomDesignerDoorKinds.Secret,
            RoomDesignerDoorKinds.Available,
            RoomDesignerDoorKinds.Inactive
        };

        private static readonly string[] ItemKinds =
        {
            RoomDesignerMarkerKinds.RoomReward,
            RoomDesignerMarkerKinds.ChestSpawn,
            RoomDesignerMarkerKinds.GoldenChestSpawn,
            RoomDesignerMarkerKinds.CorruptedChestSpawn
        };

        private static readonly string[] InteractiveKinds =
        {
            RoomDesignerMarkerKinds.StandardBarrel,
            RoomDesignerMarkerKinds.ExplosiveBarrel
        };

        public static IReadOnlyList<DesignerRoomSceneMarker> MarkersInScene(Scene scene)
        {
            return Object.FindObjectsByType<DesignerRoomSceneMarker>(FindObjectsInactive.Include)
                .Where(marker => marker != null && marker.gameObject.scene == scene)
                .OrderBy(marker => marker.MarkerKind)
                .ThenBy(marker => marker.MarkerId, StringComparer.Ordinal)
                .ToArray();
        }

        public static DesignerRoomSceneMarker FindRoomRoot(Scene scene)
        {
            return MarkersInScene(scene)
                .FirstOrDefault(marker => marker.MarkerKind == DesignerRoomSceneMarkerKind.RoomRoot);
        }

        public static string[] RuntimeKindsFor(DesignerRoomSceneMarkerKind markerKind)
        {
            return markerKind switch
            {
                DesignerRoomSceneMarkerKind.RoomRoot => new[] { "combat" },
                DesignerRoomSceneMarkerKind.FloorRegion => new[] { RoomDesignerCellKinds.Ground },
                DesignerRoomSceneMarkerKind.DoorPort => DoorStates,
                DesignerRoomSceneMarkerKind.SafeStart => new[] { RoomDesignerMarkerKinds.SafeStart },
                DesignerRoomSceneMarkerKind.EnemySpawn => RoomDesignerMarkerKinds.EnemyKinds,
                DesignerRoomSceneMarkerKind.ItemSpawn => ItemKinds,
                DesignerRoomSceneMarkerKind.Obstacle => new[] { RoomDesignerCellKinds.Rock },
                DesignerRoomSceneMarkerKind.Hazard => new[] { RoomDesignerCellKinds.Spike },
                DesignerRoomSceneMarkerKind.InteractiveObject => InteractiveKinds,
                DesignerRoomSceneMarkerKind.HoleTile => new[] { RoomDesignerCellKinds.Hole },
                _ => Array.Empty<string>()
            };
        }

        public static string DefaultRuntimeKind(DesignerRoomSceneMarkerKind markerKind)
        {
            return RuntimeKindsFor(markerKind).FirstOrDefault() ?? string.Empty;
        }

        public static string DisplayNameForRuntimeKind(string runtimeKind)
        {
            return runtimeKind switch
            {
                RoomDesignerMarkerKinds.Enemy => "Generic Enemy",
                RoomDesignerMarkerKinds.EnemyNormal => "Normal Chaser",
                RoomDesignerMarkerKinds.EnemyFlying => "Flying Chaser",
                RoomDesignerMarkerKinds.EnemyFast => "Fast Chaser",
                RoomDesignerMarkerKinds.EnemyHeavy => "Heavy Chaser",
                RoomDesignerMarkerKinds.EnemyCharger => "Ash Charger",
                RoomDesignerMarkerKinds.EnemyTurret => "Bone Turret",
                RoomDesignerMarkerKinds.EnemySplitter => "Husk Splitter",
                RoomDesignerMarkerKinds.EnemySpittingPod => "Spitting Pod",
                RoomDesignerMarkerKinds.EnemyRat => "Rat",
                RoomDesignerMarkerKinds.EnemySpider => "Spider",
                RoomDesignerMarkerKinds.EnemyHollowBird => "Hollow Bird",
                RoomDesignerMarkerKinds.EnemyHollowBeast => "Hollow Beast",
                RoomDesignerMarkerKinds.EnemySkeletonSword => "Skeleton Sword",
                RoomDesignerMarkerKinds.EnemySkeletonSpear => "Skeleton Spear",
                RoomDesignerMarkerKinds.EnemyKnight => "Knight",
                RoomDesignerMarkerKinds.EnemyGiant => "Giant",
                RoomDesignerMarkerKinds.EnemyHollowArcher => "Hollow Archer",
                RoomDesignerMarkerKinds.EnemyPowderGunner => "Powder Gunner",
                RoomDesignerMarkerKinds.EnemyKnifeThrower => "Knife Thrower",
                RoomDesignerMarkerKinds.EnemyRepeaterTurret => "Repeater Turret",
                RoomDesignerMarkerKinds.EnemyClockworkSentry => "Clockwork Sentry",
                RoomDesignerMarkerKinds.EnemyStarforgedOctantSentry => "Starforged Octant Sentry",
                RoomDesignerMarkerKinds.EnemyCrimsonRailSpider => "Crimson Rail Spider",
                RoomDesignerMarkerKinds.EnemyAzureMinigunTurret => "Azure Minigun Turret",
                RoomDesignerMarkerKinds.EnemyHollowAcolyte => "Hollow Acolyte",
                RoomDesignerMarkerKinds.EnemyWraith => "Wraith",
                RoomDesignerMarkerKinds.EnemyEscapist => "Escapist",
                RoomDesignerMarkerKinds.EnemySoulEater => "Soul Eater",
                RoomDesignerMarkerKinds.EnemyCurseBinder => "Curse Binder",
                RoomDesignerMarkerKinds.EnemyGraveLantern => "Grave Lantern",
                RoomDesignerMarkerKinds.SafeStart => "Safe Start",
                RoomDesignerMarkerKinds.RoomReward => "Room Reward",
                RoomDesignerMarkerKinds.ChestSpawn => "Chest",
                RoomDesignerMarkerKinds.GoldenChestSpawn => "Golden Chest",
                RoomDesignerMarkerKinds.CorruptedChestSpawn => "Corrupted Chest",
                RoomDesignerMarkerKinds.StandardBarrel => "Standard Barrel",
                RoomDesignerMarkerKinds.ExplosiveBarrel => "Explosive Barrel",
                RoomDesignerCellKinds.Ground => "Ground",
                RoomDesignerCellKinds.Hole => "Hole",
                RoomDesignerCellKinds.Rock => "Rock",
                RoomDesignerCellKinds.Spike => "Spike",
                RoomDesignerDoorKinds.Door => "Door",
                RoomDesignerDoorKinds.Secret => "Secret Door",
                RoomDesignerDoorKinds.Available => "Available Door Port",
                RoomDesignerDoorKinds.Inactive => "Inactive Door",
                _ => string.IsNullOrWhiteSpace(runtimeKind) ? "None" : runtimeKind
            };
        }

        public static string FolderNameFor(DesignerRoomSceneMarkerKind markerKind)
        {
            return markerKind switch
            {
                DesignerRoomSceneMarkerKind.FloorRegion => "FloorRegions",
                DesignerRoomSceneMarkerKind.DoorPort => "DoorPorts",
                DesignerRoomSceneMarkerKind.SafeStart => "SpawnPoints",
                DesignerRoomSceneMarkerKind.EnemySpawn => "EnemySpawns",
                DesignerRoomSceneMarkerKind.ItemSpawn => "ItemSpawns",
                DesignerRoomSceneMarkerKind.Obstacle => "Obstacles",
                DesignerRoomSceneMarkerKind.Hazard => "Hazards",
                DesignerRoomSceneMarkerKind.InteractiveObject => "InteractiveObjects",
                DesignerRoomSceneMarkerKind.HoleTile => "HoleTiles",
                _ => string.Empty
            };
        }

        public static DesignerRoomSceneMarker CreateMarker(
            DesignerRoomSceneMarker root,
            DesignerRoomSceneMarkerKind markerKind,
            string runtimeKind,
            Vector3 localPosition,
            string markerId = null,
            bool recordUndo = true)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var folder = FindOrCreateFolder(root, FolderNameFor(markerKind), recordUndo);
            var kind = string.IsNullOrWhiteSpace(runtimeKind) ? DefaultRuntimeKind(markerKind) : runtimeKind;
            var id = string.IsNullOrWhiteSpace(markerId)
                ? NextMarkerId(root.gameObject.scene, markerKind, kind)
                : markerId;
            var label = DisplayNameForRuntimeKind(kind);
            var go = GameObject.CreatePrimitive(PrimitiveFor(markerKind));
            if (recordUndo)
            {
                Undo.RegisterCreatedObjectUndo(go, "Create Designer Room Marker");
            }

            go.name = $"{label}.{id}";
            go.transform.SetParent(folder != null ? folder.transform : root.transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = DefaultScaleFor(markerKind);

            var marker = go.AddComponent<DesignerRoomSceneMarker>();
            marker.ConfigureAuthoring(
                id,
                markerKind,
                kind,
                root.SourceRoomId,
                root.SourceRuntimePath,
                string.Empty,
                true,
                string.Empty,
                true,
                false,
                DefaultPreviewRadiusFor(markerKind));
            SnapMarker(marker, recordUndo: false);
            return marker;
        }

        public static void SnapMarker(DesignerRoomSceneMarker marker, bool recordUndo = true)
        {
            if (marker == null || marker.LockedLayer)
            {
                return;
            }

            if (recordUndo)
            {
                Undo.RecordObject(marker.transform, "Snap Designer Room Marker");
                Undo.RecordObject(marker, "Snap Designer Room Marker");
            }

            var root = FindRootForMarker(marker);
            var rootTransform = root != null ? root.transform : marker.transform.parent;
            var local = rootTransform != null
                ? rootTransform.InverseTransformPoint(marker.transform.position)
                : marker.transform.localPosition;

            if (marker.MarkerKind == DesignerRoomSceneMarkerKind.DoorPort &&
                TryResolveBaseProject(root, out var project) &&
                TryFindClosestDoor(project, local, out var door))
            {
                local = new Vector3(door.x, 0f, door.z);
                marker.ConfigureDoor(door.direction, door.laneIndex, door.hostCellX, door.hostCellZ, marker.DoorState);
            }
            else
            {
                local.x = Mathf.Round(local.x);
                local.z = Mathf.Round(local.z);
                local.y = YFor(marker.MarkerKind);
            }

            if (rootTransform != null)
            {
                marker.transform.position = rootTransform.TransformPoint(local);
            }
            else
            {
                marker.transform.localPosition = local;
            }

            if (marker.MarkerKind is DesignerRoomSceneMarkerKind.Obstacle or DesignerRoomSceneMarkerKind.Hazard or DesignerRoomSceneMarkerKind.FloorRegion or DesignerRoomSceneMarkerKind.HoleTile)
            {
                var scale = marker.transform.localScale;
                scale.x = Mathf.Max(1f, Mathf.Round(Mathf.Abs(scale.x)));
                scale.z = Mathf.Max(1f, Mathf.Round(Mathf.Abs(scale.z)));
                scale.y = Mathf.Max(0.08f, Mathf.Abs(scale.y));
                marker.transform.localScale = scale;
            }

            EditorUtility.SetDirty(marker);
        }

        public static void SnapAllInScene(Scene scene)
        {
            foreach (var marker in MarkersInScene(scene))
            {
                if (marker.MarkerKind is DesignerRoomSceneMarkerKind.Folder or DesignerRoomSceneMarkerKind.RoomRoot)
                {
                    continue;
                }

                SnapMarker(marker);
            }
        }

        public static RoomDesignerProject BuildRoomDesignerProject(Scene scene)
        {
            var root = FindRoomRoot(scene);
            if (root == null)
            {
                throw new InvalidOperationException("Active scene is missing a DesignerRoomSceneMarker room root.");
            }

            return BuildRoomDesignerProject(root, MarkersInScene(scene));
        }

        public static RoomDesignerProject BuildRoomDesignerProject(DesignerRoomSceneMarker root, IEnumerable<DesignerRoomSceneMarker> markers)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var project = TryResolveBaseProject(root, out var sourceProject)
                ? sourceProject
                : RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, root.SourceRoomId);
            var sourceId = string.IsNullOrWhiteSpace(root.SourceRoomId) ? root.gameObject.scene.name : root.SourceRoomId;
            project.projectId = $"{Sanitize(sourceId)}_manual_scene";
            project.displayName = $"{(string.IsNullOrWhiteSpace(root.SourceRoomId) ? root.gameObject.scene.name : root.SourceRoomId)} Manual Scene Draft";
            project.updatedAtUtcTicks = DateTime.UtcNow.Ticks;
            project.cells.RemoveAll(cell => cell == null || cell.kind != RoomDesignerCellKinds.Ground);
            project.markers.Clear();
            foreach (var door in project.doorPorts ?? new List<RoomDesignerDoorPortState>())
            {
                door.state = RoomDesignerDoorKinds.Inactive;
            }

            var addedCells = new HashSet<string>((project.cells ?? new List<RoomDesignerCell>()).Select(CellKey), StringComparer.Ordinal);
            foreach (var marker in (markers ?? Array.Empty<DesignerRoomSceneMarker>()).Where(marker => marker != null))
            {
                if (marker.MarkerKind is DesignerRoomSceneMarkerKind.RoomRoot or DesignerRoomSceneMarkerKind.Folder or DesignerRoomSceneMarkerKind.FloorRegion)
                {
                    continue;
                }

                var position = root.transform.InverseTransformPoint(marker.transform.position);
                switch (marker.MarkerKind)
                {
                    case DesignerRoomSceneMarkerKind.SafeStart:
                        project.markers.Add(new RoomDesignerMarker(
                            SafeId(marker.MarkerId, "spawn_safeStart"),
                            RoomDesignerMarkerKinds.SafeStart,
                            Mathf.Round(position.x),
                            0f,
                            Mathf.Round(position.z)));
                        break;
                    case DesignerRoomSceneMarkerKind.EnemySpawn:
                        project.markers.Add(new RoomDesignerMarker(
                            SafeId(marker.MarkerId, "spawn_enemy"),
                            string.IsNullOrWhiteSpace(marker.RuntimeKind) ? RoomDesignerMarkerKinds.EnemyNormal : marker.RuntimeKind,
                            Mathf.Round(position.x),
                            0f,
                            Mathf.Round(position.z)));
                        break;
                    case DesignerRoomSceneMarkerKind.ItemSpawn:
                        project.markers.Add(new RoomDesignerMarker(
                            SafeId(marker.MarkerId, "spawn_item"),
                            string.IsNullOrWhiteSpace(marker.RuntimeKind) ? RoomDesignerMarkerKinds.RoomReward : marker.RuntimeKind,
                            Mathf.Round(position.x),
                            0f,
                            Mathf.Round(position.z)));
                        break;
                    case DesignerRoomSceneMarkerKind.InteractiveObject:
                        project.markers.Add(new RoomDesignerMarker(
                            SafeId(marker.MarkerId, "interactive"),
                            string.IsNullOrWhiteSpace(marker.RuntimeKind) ? RoomDesignerMarkerKinds.StandardBarrel : marker.RuntimeKind,
                            Mathf.Round(position.x),
                            0f,
                            Mathf.Round(position.z)));
                        break;
                    case DesignerRoomSceneMarkerKind.Obstacle:
                        AddCells(project.cells, addedCells, CellsForMarker(marker, position, RoomDesignerCellKinds.Rock));
                        break;
                    case DesignerRoomSceneMarkerKind.Hazard:
                        AddCells(project.cells, addedCells, CellsForMarker(marker, position, RoomDesignerCellKinds.Spike));
                        break;
                    case DesignerRoomSceneMarkerKind.HoleTile:
                        AddCells(project.cells, addedCells, CellsForMarker(marker, position, RoomDesignerCellKinds.Hole));
                        break;
                    case DesignerRoomSceneMarkerKind.DoorPort:
                        ApplyDoorMarker(project, marker, position);
                        break;
                }
            }

            return project;
        }

        public static DesignerRoomSceneValidationResult ValidateScene(Scene scene)
        {
            var result = new DesignerRoomSceneValidationResult();
            var root = FindRoomRoot(scene);
            if (root == null)
            {
                result.AddError("Missing DesignerRoom root marker.");
                return result;
            }

            var markers = MarkersInScene(scene);
            var duplicateIds = markers
                .Where(marker => marker.MarkerKind != DesignerRoomSceneMarkerKind.Folder && !string.IsNullOrWhiteSpace(marker.MarkerId))
                .GroupBy(marker => marker.MarkerId, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);
            foreach (var duplicateId in duplicateIds)
            {
                result.AddError($"Duplicate marker id '{duplicateId}'.");
            }

            foreach (var offGrid in markers.Where(marker => marker.MarkerKind != DesignerRoomSceneMarkerKind.Folder && marker.MarkerKind != DesignerRoomSceneMarkerKind.RoomRoot && IsOffGrid(marker)))
            {
                result.AddWarning($"Marker '{offGrid.MarkerId}' is off grid or off a valid door port.");
            }

            try
            {
                var project = BuildRoomDesignerProject(root, markers);
                var validation = RoomDesignerDraftValidator.Validate(project);
                foreach (var error in validation.Errors)
                {
                    result.AddError(error);
                }

                foreach (var warning in validation.Warnings)
                {
                    result.AddWarning(warning);
                }

                var runtimeAsset = RoomDesignerCompiler.Compile(project);
                if (RoomNavMeshBakeUtility.TryDescribeMissingBake(runtimeAsset, out var navMeshMessage))
                {
                    result.AddWarning($"NavMesh bake: {navMeshMessage} Designer Room playtests can use the dev-only runtime fallback, but approved runtime rooms should be baked before QA.");
                }
            }
            catch (Exception exception)
            {
                result.AddError(exception.Message);
            }

            return result;
        }

        public static string ExportScene(Scene scene)
        {
            var project = BuildRoomDesignerProject(scene);
            var validation = RoomDesignerDraftValidator.Validate(project);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"DesignerRoom scene export failed: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = $"manual_{Sanitize(project.projectId)}";
            manifest.hollowRuntime.displayName = project.displayName;
            manifest.hollowRuntime.roomType = "designer-manual-draft";
            manifest.hollowRuntime.rewardType = "designer";
            manifest.hollowRuntime.prototypeStatus = "manual-scene-export";

            Directory.CreateDirectory(ManualExportDirectory);
            var path = $"{ManualExportDirectory}/{Sanitize(project.projectId)}.hollowruntime.json";
            File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        public static string DiffAgainstSource(Scene scene)
        {
            var root = FindRoomRoot(scene);
            if (root == null)
            {
                return "Missing DesignerRoom root marker.";
            }

            if (!TryResolveBaseProject(root, out var sourceProject))
            {
                return "No source JSON is available for this scene.";
            }

            var current = BuildRoomDesignerProject(scene);
            var lines = new List<string> { $"Diff for {scene.name}" };
            AddMarkerDiff(lines, sourceProject.markers, current.markers);
            AddDoorDiff(lines, sourceProject.doorPorts, current.doorPorts);
            AddCellDiff(lines, sourceProject.cells, current.cells, RoomDesignerCellKinds.Rock, "rocks");
            AddCellDiff(lines, sourceProject.cells, current.cells, RoomDesignerCellKinds.Spike, "spikes");
            AddCellDiff(lines, sourceProject.cells, current.cells, RoomDesignerCellKinds.Hole, "holes");
            if (lines.Count == 1)
            {
                lines.Add("No changes detected.");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static void RefreshSceneFromSource(DesignerRoomSceneMarker root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (!TryResolveBaseProject(root, out var project))
            {
                throw new InvalidOperationException("No source JSON is available for this scene.");
            }

            var scene = root.gameObject.scene;
            foreach (var marker in MarkersInScene(scene).Where(IsRefreshableMarker).ToArray())
            {
                Undo.DestroyObjectImmediate(marker.gameObject);
            }

            CreateMarkersFromProject(root, project);
        }

        public static bool TryResolveBounds(Scene scene, out Rect bounds)
        {
            var root = FindRoomRoot(scene);
            if (TryResolveBaseProject(root, out var project))
            {
                RoomDesignerFootprintUtility.RoomBounds(project.footprintPreset, out var minX, out var maxX, out var minZ, out var maxZ);
                bounds = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
                return true;
            }

            bounds = Rect.MinMaxRect(-6.5f, -3.5f, 6.5f, 3.5f);
            return false;
        }

        private static void CreateMarkersFromProject(DesignerRoomSceneMarker root, RoomDesignerProject project)
        {
            foreach (var marker in project.markers ?? new List<RoomDesignerMarker>())
            {
                var kind = DesignerRoomSceneMarkerKind.ItemSpawn;
                if (marker.kind == RoomDesignerMarkerKinds.SafeStart)
                {
                    kind = DesignerRoomSceneMarkerKind.SafeStart;
                }
                else if (RoomDesignerMarkerKinds.IsEnemy(marker.kind))
                {
                    kind = DesignerRoomSceneMarkerKind.EnemySpawn;
                }
                else if (RoomDesignerMarkerKinds.IsInteractiveObject(marker.kind))
                {
                    kind = DesignerRoomSceneMarkerKind.InteractiveObject;
                }

                CreateMarker(root, kind, marker.kind, new Vector3(marker.x, YFor(kind), marker.z), marker.id);
            }

            foreach (var cell in project.cells ?? new List<RoomDesignerCell>())
            {
                var kind = cell.kind switch
                {
                    RoomDesignerCellKinds.Rock => DesignerRoomSceneMarkerKind.Obstacle,
                    RoomDesignerCellKinds.Spike => DesignerRoomSceneMarkerKind.Hazard,
                    RoomDesignerCellKinds.Hole => DesignerRoomSceneMarkerKind.HoleTile,
                    _ => DesignerRoomSceneMarkerKind.Folder
                };

                if (kind != DesignerRoomSceneMarkerKind.Folder)
                {
                    CreateMarker(root, kind, cell.kind, new Vector3(cell.x, YFor(kind), cell.z), $"{cell.kind}_{cell.x}_{cell.z}");
                }
            }

            foreach (var door in project.doorPorts ?? new List<RoomDesignerDoorPortState>())
            {
                var marker = CreateMarker(root, DesignerRoomSceneMarkerKind.DoorPort, door.state, new Vector3(door.x, 0f, door.z), door.id);
                marker.ConfigureDoor(door.direction, door.laneIndex, door.hostCellX, door.hostCellZ, door.state);
            }
        }

        private static void ApplyDoorMarker(RoomDesignerProject project, DesignerRoomSceneMarker marker, Vector3 position)
        {
            var state = marker.DoorState;
            if (string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(marker.RuntimeKind))
            {
                state = marker.RuntimeKind;
            }

            if (!TryFindClosestDoor(project, position, out var closest))
            {
                closest = RoomDesignerDoorPortState.Create(
                    InferDirection(marker, position, project.footprintPreset),
                    InferLaneIndex(marker),
                    Mathf.Round(position.x),
                    Mathf.Round(position.z),
                    string.IsNullOrWhiteSpace(state) ? RoomDesignerDoorKinds.Door : state,
                    marker.HostCellX,
                    marker.HostCellZ);
                project.doorPorts.Add(closest);
            }

            var door = project.doorPorts.FirstOrDefault(candidate => candidate.id == closest.id);
            if (door == null)
            {
                door = closest;
                project.doorPorts.Add(door);
            }

            door.direction = closest.direction;
            door.laneIndex = closest.laneIndex;
            door.hostCellX = closest.hostCellX;
            door.hostCellZ = closest.hostCellZ;
            door.x = closest.x;
            door.z = closest.z;
            door.state = string.IsNullOrWhiteSpace(state) ? RoomDesignerDoorKinds.Door : state;
        }

        private static bool TryFindClosestDoor(RoomDesignerProject project, Vector3 localPosition, out RoomDesignerDoorPortState door)
        {
            door = null;
            if (project?.doorPorts == null || project.doorPorts.Count == 0)
            {
                return false;
            }

            door = project.doorPorts
                .OrderBy(port => (port.x - localPosition.x) * (port.x - localPosition.x) + (port.z - localPosition.z) * (port.z - localPosition.z))
                .FirstOrDefault();
            return door != null;
        }

        private static bool TryResolveBaseProject(DesignerRoomSceneMarker root, out RoomDesignerProject project)
        {
            project = null;
            if (root == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(root.SourceRuntimePath) && File.Exists(root.SourceRuntimePath))
            {
                project = RoomDesignerRuntimeDraftImporter.FromRuntimeJson(File.ReadAllText(root.SourceRuntimePath), root.SourceRuntimePath);
                return true;
            }

            project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, root.SourceRoomId);
            return true;
        }

        private static DesignerRoomSceneMarker FindRootForMarker(DesignerRoomSceneMarker marker)
        {
            if (marker == null)
            {
                return null;
            }

            var current = marker.transform;
            while (current != null)
            {
                var candidate = current.GetComponent<DesignerRoomSceneMarker>();
                if (candidate != null && candidate.MarkerKind == DesignerRoomSceneMarkerKind.RoomRoot)
                {
                    return candidate;
                }

                current = current.parent;
            }

            return FindRoomRoot(marker.gameObject.scene);
        }

        private static DesignerRoomSceneMarker FindOrCreateFolder(DesignerRoomSceneMarker root, string folderName, bool recordUndo)
        {
            if (root == null || string.IsNullOrWhiteSpace(folderName))
            {
                return root;
            }

            foreach (var marker in MarkersInScene(root.gameObject.scene))
            {
                if (marker.MarkerKind == DesignerRoomSceneMarkerKind.Folder && marker.MarkerId == folderName)
                {
                    return marker;
                }
            }

            var go = new GameObject(folderName);
            if (recordUndo)
            {
                Undo.RegisterCreatedObjectUndo(go, "Create Designer Room Folder");
            }

            go.transform.SetParent(root.transform, false);
            var folder = go.AddComponent<DesignerRoomSceneMarker>();
            folder.ConfigureAuthoring(
                folderName,
                DesignerRoomSceneMarkerKind.Folder,
                string.Empty,
                root.SourceRoomId,
                root.SourceRuntimePath,
                string.Empty,
                false,
                folderName,
                false,
                true,
                0.5f);
            return folder;
        }

        private static bool IsOffGrid(DesignerRoomSceneMarker marker)
        {
            var root = FindRootForMarker(marker);
            var local = root != null
                ? root.transform.InverseTransformPoint(marker.transform.position)
                : marker.transform.localPosition;

            if (marker.MarkerKind == DesignerRoomSceneMarkerKind.DoorPort)
            {
                return !TryResolveBaseProject(root, out var project) ||
                       !TryFindClosestDoor(project, local, out var door) ||
                       Mathf.Abs(local.x - door.x) > 0.05f ||
                       Mathf.Abs(local.z - door.z) > 0.05f;
            }

            return Mathf.Abs(local.x - Mathf.Round(local.x)) > 0.05f ||
                   Mathf.Abs(local.z - Mathf.Round(local.z)) > 0.05f;
        }

        private static IEnumerable<RoomDesignerCell> CellsForMarker(DesignerRoomSceneMarker marker, Vector3 position, string kind)
        {
            var scale = marker.transform.localScale;
            var width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.x)));
            var depth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(scale.z)));
            var centerX = Mathf.RoundToInt(position.x);
            var centerZ = Mathf.RoundToInt(position.z);
            var minX = centerX - width / 2;
            var minZ = centerZ - depth / 2;
            for (var z = 0; z < depth; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    yield return new RoomDesignerCell(minX + x, minZ + z, 0, kind);
                }
            }
        }

        private static void AddCells(ICollection<RoomDesignerCell> cells, HashSet<string> addedCells, IEnumerable<RoomDesignerCell> nextCells)
        {
            foreach (var cell in nextCells)
            {
                if (addedCells.Add(CellKey(cell)))
                {
                    cells.Add(cell);
                }
            }
        }

        private static string CellKey(RoomDesignerCell cell)
        {
            return $"{cell.kind}:{cell.x}:{cell.z}:{cell.layer}";
        }

        private static string SafeId(string id, string fallbackPrefix)
        {
            return string.IsNullOrWhiteSpace(id) ? $"{fallbackPrefix}_{Guid.NewGuid():N}" : id;
        }

        private static string NextMarkerId(Scene scene, DesignerRoomSceneMarkerKind kind, string runtimeKind)
        {
            var prefix = kind switch
            {
                DesignerRoomSceneMarkerKind.DoorPort => "door",
                DesignerRoomSceneMarkerKind.SafeStart => "spawn_safeStart",
                DesignerRoomSceneMarkerKind.EnemySpawn => "spawn_enemy",
                DesignerRoomSceneMarkerKind.ItemSpawn => "spawn_item",
                DesignerRoomSceneMarkerKind.Obstacle => "rock",
                DesignerRoomSceneMarkerKind.Hazard => "spike",
                DesignerRoomSceneMarkerKind.InteractiveObject => "interactive",
                DesignerRoomSceneMarkerKind.HoleTile => "hole",
                _ => Sanitize(runtimeKind)
            };

            var existing = new HashSet<string>(MarkersInScene(scene).Select(marker => marker.MarkerId), StringComparer.Ordinal);
            for (var index = 0; index < 999; index++)
            {
                var id = $"{prefix}_{index:00}";
                if (!existing.Contains(id))
                {
                    return id;
                }
            }

            return $"{prefix}_{Guid.NewGuid():N}";
        }

        private static PrimitiveType PrimitiveFor(DesignerRoomSceneMarkerKind kind)
        {
            return kind switch
            {
                DesignerRoomSceneMarkerKind.SafeStart => PrimitiveType.Sphere,
                DesignerRoomSceneMarkerKind.EnemySpawn => PrimitiveType.Sphere,
                DesignerRoomSceneMarkerKind.ItemSpawn => PrimitiveType.Cylinder,
                DesignerRoomSceneMarkerKind.Hazard => PrimitiveType.Cylinder,
                _ => PrimitiveType.Cube
            };
        }

        private static Vector3 DefaultScaleFor(DesignerRoomSceneMarkerKind kind)
        {
            return kind switch
            {
                DesignerRoomSceneMarkerKind.SafeStart => new Vector3(0.5f, 0.18f, 0.5f),
                DesignerRoomSceneMarkerKind.EnemySpawn => new Vector3(0.52f, 0.3f, 0.52f),
                DesignerRoomSceneMarkerKind.ItemSpawn => new Vector3(0.45f, 0.12f, 0.45f),
                DesignerRoomSceneMarkerKind.DoorPort => new Vector3(1.2f, 0.18f, 0.35f),
                DesignerRoomSceneMarkerKind.Hazard => new Vector3(0.9f, 0.08f, 0.9f),
                DesignerRoomSceneMarkerKind.InteractiveObject => new Vector3(0.82f, 1f, 0.82f),
                DesignerRoomSceneMarkerKind.HoleTile => new Vector3(1f, 0.05f, 1f),
                _ => Vector3.one
            };
        }

        private static float DefaultPreviewRadiusFor(DesignerRoomSceneMarkerKind kind)
        {
            return kind switch
            {
                DesignerRoomSceneMarkerKind.EnemySpawn => 1.5f,
                DesignerRoomSceneMarkerKind.DoorPort => 0.5f,
                DesignerRoomSceneMarkerKind.SafeStart => 0.7f,
                _ => 0.5f
            };
        }

        private static float YFor(DesignerRoomSceneMarkerKind kind)
        {
            return kind switch
            {
                DesignerRoomSceneMarkerKind.Obstacle => 0.5f,
                DesignerRoomSceneMarkerKind.InteractiveObject => 0.5f,
                DesignerRoomSceneMarkerKind.EnemySpawn => 0.25f,
                DesignerRoomSceneMarkerKind.SafeStart => 0.12f,
                DesignerRoomSceneMarkerKind.ItemSpawn => 0.12f,
                DesignerRoomSceneMarkerKind.Hazard => 0.04f,
                DesignerRoomSceneMarkerKind.HoleTile => 0.02f,
                _ => 0f
            };
        }

        private static bool IsRefreshableMarker(DesignerRoomSceneMarker marker)
        {
            return marker.MarkerKind is DesignerRoomSceneMarkerKind.DoorPort
                or DesignerRoomSceneMarkerKind.SafeStart
                or DesignerRoomSceneMarkerKind.EnemySpawn
                or DesignerRoomSceneMarkerKind.ItemSpawn
                or DesignerRoomSceneMarkerKind.Obstacle
                or DesignerRoomSceneMarkerKind.Hazard
                or DesignerRoomSceneMarkerKind.InteractiveObject
                or DesignerRoomSceneMarkerKind.HoleTile;
        }

        private static string InferDirection(DesignerRoomSceneMarker marker, Vector3 position, RoomDesignerFootprintPreset preset)
        {
            if (!string.IsNullOrWhiteSpace(marker.DoorDirection))
            {
                return marker.DoorDirection;
            }

            if (!string.IsNullOrWhiteSpace(marker.MarkerId))
            {
                var separator = marker.MarkerId.IndexOf('_');
                if (separator > 0)
                {
                    var prefix = marker.MarkerId.Substring(0, separator);
                    if (prefix is "north" or "south" or "east" or "west")
                    {
                        return prefix;
                    }
                }
            }

            RoomDesignerFootprintUtility.RoomBounds(preset, out var minX, out var maxX, out var minZ, out var maxZ);
            var distances = new Dictionary<string, float>
            {
                ["west"] = Mathf.Abs(position.x - minX),
                ["east"] = Mathf.Abs(position.x - maxX),
                ["north"] = Mathf.Abs(position.z - minZ),
                ["south"] = Mathf.Abs(position.z - maxZ)
            };
            return distances.OrderBy(pair => pair.Value).First().Key;
        }

        private static int InferLaneIndex(DesignerRoomSceneMarker marker)
        {
            if (!string.IsNullOrWhiteSpace(marker.MarkerId))
            {
                var separator = marker.MarkerId.IndexOf('_');
                if (separator >= 0 && int.TryParse(marker.MarkerId.Substring(separator + 1), out var parsed))
                {
                    return Mathf.Max(0, parsed);
                }
            }

            return marker.DoorLaneIndex;
        }

        private static void AddMarkerDiff(List<string> lines, IReadOnlyList<RoomDesignerMarker> source, IReadOnlyList<RoomDesignerMarker> current)
        {
            var sourceById = (source ?? Array.Empty<RoomDesignerMarker>()).ToDictionary(marker => marker.id, marker => marker, StringComparer.Ordinal);
            var currentById = (current ?? Array.Empty<RoomDesignerMarker>()).ToDictionary(marker => marker.id, marker => marker, StringComparer.Ordinal);
            foreach (var id in currentById.Keys.Except(sourceById.Keys, StringComparer.Ordinal))
            {
                lines.Add($"+ marker {id}");
            }

            foreach (var id in sourceById.Keys.Except(currentById.Keys, StringComparer.Ordinal))
            {
                lines.Add($"- marker {id}");
            }

            foreach (var id in sourceById.Keys.Intersect(currentById.Keys, StringComparer.Ordinal))
            {
                var before = sourceById[id];
                var after = currentById[id];
                if (before.kind != after.kind || Mathf.Abs(before.x - after.x) > 0.05f || Mathf.Abs(before.z - after.z) > 0.05f)
                {
                    lines.Add($"~ marker {id}: {before.kind} ({before.x:0.#},{before.z:0.#}) -> {after.kind} ({after.x:0.#},{after.z:0.#})");
                }
            }
        }

        private static void AddDoorDiff(List<string> lines, IReadOnlyList<RoomDesignerDoorPortState> source, IReadOnlyList<RoomDesignerDoorPortState> current)
        {
            var sourceById = (source ?? Array.Empty<RoomDesignerDoorPortState>()).ToDictionary(door => door.id, door => door, StringComparer.Ordinal);
            var currentById = (current ?? Array.Empty<RoomDesignerDoorPortState>()).ToDictionary(door => door.id, door => door, StringComparer.Ordinal);
            foreach (var id in sourceById.Keys.Intersect(currentById.Keys, StringComparer.Ordinal))
            {
                var before = sourceById[id];
                var after = currentById[id];
                if (before.state != after.state || Mathf.Abs(before.x - after.x) > 0.05f || Mathf.Abs(before.z - after.z) > 0.05f)
                {
                    lines.Add($"~ door {id}: {before.state} -> {after.state}");
                }
            }
        }

        private static void AddCellDiff(List<string> lines, IReadOnlyList<RoomDesignerCell> source, IReadOnlyList<RoomDesignerCell> current, string kind, string label)
        {
            var sourceSet = new HashSet<string>((source ?? Array.Empty<RoomDesignerCell>()).Where(cell => cell.kind == kind).Select(cell => $"{cell.x},{cell.z}"), StringComparer.Ordinal);
            var currentSet = new HashSet<string>((current ?? Array.Empty<RoomDesignerCell>()).Where(cell => cell.kind == kind).Select(cell => $"{cell.x},{cell.z}"), StringComparer.Ordinal);
            var added = currentSet.Except(sourceSet, StringComparer.Ordinal).Count();
            var removed = sourceSet.Except(currentSet, StringComparer.Ordinal).Count();
            if (added > 0 || removed > 0)
            {
                lines.Add($"~ {label}: +{added}, -{removed}");
            }
        }

        private static string Sanitize(string value)
        {
            var sanitized = new string((value ?? string.Empty)
                .Select(character => char.IsLetterOrDigit(character) || character is '_' or '-' ? character : '_')
                .ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
        }
    }
}
