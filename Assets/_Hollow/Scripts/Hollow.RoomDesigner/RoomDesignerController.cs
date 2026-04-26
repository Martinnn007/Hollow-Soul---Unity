using System;
using System.Linq;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Persistence;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerController : MonoBehaviour
    {
        private const float RepeatDelaySeconds = 0.16f;

        [SerializeField] private Transform previewRoot;
        [SerializeField] private Canvas hudCanvas;

        private readonly RoomDesignerTool[] tools = Enum.GetValues(typeof(RoomDesignerTool)).Cast<RoomDesignerTool>().ToArray();
        private RoomDesignerStore store;
        private RoomDesignerDraftLibraryState libraryState;
        private ProfileSlotId slotId;
        private RoomDesignerProject currentProject;
        private Text hudText;
        private float nextMoveTime;
        private int toolIndex;
        private string status = "Ready";

        public RoomDesignerProject CurrentProject => currentProject;

        public RoomDesignerDraftLibraryState LibraryState => libraryState;

        public RoomDesignerValidationReport LastValidationReport { get; private set; }

        public RoomDesignerTool CurrentTool => tools[Mathf.Clamp(toolIndex, 0, tools.Length - 1)];

        public int CursorX { get; private set; }

        public int CursorZ { get; private set; }

        public int CursorLayer { get; private set; }

        public bool LabelsVisible { get; private set; } = true;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            store = new RoomDesignerStore();
            slotId = ResolveSlotId();
            libraryState = new RoomDesignerDraftLibraryState(store, slotId);
            currentProject = libraryState.SelectedDraft;
            SnapCursorToFootprint();
            EnsureRoots();
            RebuildPreview();
            RefreshHud();
        }

        public void InitializeForTest(RoomDesignerStore nextStore, ProfileSlotId nextSlotId, RoomDesignerProject nextProject)
        {
            store = nextStore;
            slotId = nextSlotId;
            store.SaveDraft(slotId, nextProject);
            libraryState = new RoomDesignerDraftLibraryState(store, slotId);
            currentProject = libraryState.OpenDraft(nextProject.projectId);
            SnapCursorToFootprint();
            EnsureRoots();
            RebuildPreview();
            RefreshHud();
        }

        public RoomDesignerProject CreateDraft(RoomDesignerFootprintPreset preset)
        {
            libraryState ??= new RoomDesignerDraftLibraryState(store ?? new RoomDesignerStore(), slotId);
            currentProject = libraryState.CreateDraft(preset);
            CursorX = 0;
            CursorZ = 0;
            CursorLayer = 0;
            SnapCursorToFootprint();
            status = libraryState.LatestMessage;
            RebuildPreview();
            RefreshHud();
            return currentProject;
        }

        public RoomDesignerProject OpenDraft(string projectId)
        {
            currentProject = libraryState.OpenDraft(projectId);
            SnapCursorToFootprint();
            status = libraryState.LatestMessage;
            RebuildPreview();
            RefreshHud();
            return currentProject;
        }

        public RoomDesignerProject DuplicateDraft(string projectId)
        {
            currentProject = libraryState.DuplicateDraft(projectId);
            SnapCursorToFootprint();
            status = libraryState.LatestMessage;
            RebuildPreview();
            RefreshHud();
            return currentProject;
        }

        public RoomDesignerProject DeleteDraft(string projectId)
        {
            currentProject = libraryState.DeleteDraft(projectId);
            SnapCursorToFootprint();
            status = libraryState.LatestMessage;
            RebuildPreview();
            RefreshHud();
            return currentProject;
        }

        public void SelectTool(RoomDesignerTool tool)
        {
            var index = Array.IndexOf(tools, tool);
            if (index >= 0)
            {
                toolIndex = index;
                RefreshHud();
            }
        }

        private void Update()
        {
            ApplyInput(RoomDesignerInputReader.ReadCurrent(), Time.time);
        }

        public void ApplyInput(RoomDesignerInputSnapshot input, float timeSeconds = 999f)
        {
            SnapCursorToFootprint();
            var changed = false;
            if ((input.MoveX != 0 || input.MoveZ != 0) && timeSeconds >= nextMoveTime)
            {
                RoomDesignerFootprintUtility.TileCoordinateBounds(currentProject.footprintPreset, out var minX, out var maxX, out var minZ, out var maxZ);
                var nextX = Mathf.Clamp(CursorX + input.MoveX, minX, maxX);
                var nextZ = Mathf.Clamp(CursorZ + input.MoveZ, minZ, maxZ);
                if (RoomDesignerFootprintUtility.ContainsTile(currentProject.footprintPreset, nextX, nextZ))
                {
                    CursorX = nextX;
                    CursorZ = nextZ;
                }

                nextMoveTime = timeSeconds + RepeatDelaySeconds;
                changed = true;
            }

            if (input.ToolDelta != 0)
            {
                toolIndex = Mod(toolIndex + input.ToolDelta, tools.Length);
                changed = true;
            }

            if (input.LayerDelta != 0)
            {
                CursorLayer = Mathf.Clamp(CursorLayer + input.LayerDelta, 0, 4);
                changed = true;
            }

            if (input.ToggleLabelsPressed)
            {
                LabelsVisible = !LabelsVisible;
                changed = true;
            }

            if (input.EyedropperPressed || CurrentTool == RoomDesignerTool.Eyedropper && input.PlacePressed)
            {
                Eyedropper();
                changed = true;
            }
            else if (input.ErasePressed || CurrentTool == RoomDesignerTool.Erase && input.PlacePressed)
            {
                EraseAtCursor();
                VfxPresenter.Play(VfxCueId.DesignerErase, CursorWorldPosition(), previewRoot);
                AudioPresenter.Play(AudioCueId.DesignerErase, CursorWorldPosition());
                SaveDraft("Erased cell/entity");
                changed = true;
            }
            else if (input.PlacePressed)
            {
                PlaceCurrentTool();
                VfxPresenter.Play(VfxCueId.DesignerPlace, CursorWorldPosition(), previewRoot);
                AudioPresenter.Play(AudioCueId.DesignerPlace, CursorWorldPosition());
                SaveDraft($"Placed {CurrentTool}");
                changed = true;
            }

            if (input.ExportJsonPressed)
            {
                ExportCurrentDraft();
            }

            if (input.ExportUsdaPressed)
            {
                ExportCurrentUsda();
            }

            if (input.PlaytestPressed)
            {
                PlaytestCurrentDraft();
            }

            if (input.BackPressed)
            {
                ReturnToMainMenu();
            }

            if (changed)
            {
                RebuildPreview();
                RefreshHud();
            }
        }

        private void SnapCursorToFootprint()
        {
            if (currentProject == null || RoomDesignerFootprintUtility.ContainsTile(currentProject.footprintPreset, CursorX, CursorZ))
            {
                return;
            }

            var nearest = RoomDesignerFootprintUtility.NearestContainedTile(currentProject.footprintPreset, CursorX, CursorZ);
            CursorX = nearest.x;
            CursorZ = nearest.y;
        }

        public void PlaytestCurrentDraft()
        {
            LastValidationReport = RoomDesignerDraftValidator.Validate(currentProject);
            if (!LastValidationReport.IsValid)
            {
                status = $"Playtest blocked: {LastValidationReport.Summary()}";
                RefreshHud();
                return;
            }

            var runtimeJson = RoomDesignerCompiler.ExportRuntimeJson(currentProject, prettyPrint: false);
            RoomPlaytestHandoff.Set(runtimeJson, RuntimeSessionMode.TransientRoomDesignerPlaytest, AppShellRoute.RoomDesigner);
            status = "Launching transient playtest";
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(AppShellRoute.GameWindows);
                SceneLoaderService.LoadRouteAsync(AppShellRoute.GameWindows);
            }
        }

        public string ExportCurrentDraft()
        {
            LastValidationReport = RoomDesignerDraftValidator.Validate(currentProject);
            if (!LastValidationReport.IsValid)
            {
                status = $"Export blocked: {LastValidationReport.Summary()}";
                RefreshHud();
                return string.Empty;
            }

            var bundle = RoomDesignerExportBundle.Export(currentProject);
            status = $"Exported bundle: {bundle.directory}";
            RefreshHud();
            return bundle.projectJsonPath;
        }

        public string ExportCurrentUsda()
        {
            LastValidationReport = RoomDesignerDraftValidator.Validate(currentProject);
            if (!LastValidationReport.IsValid)
            {
                status = $"USDA export blocked: {LastValidationReport.Summary()}";
                RefreshHud();
                return string.Empty;
            }

            var bundle = RoomDesignerExportBundle.Export(currentProject);
            status = $"Exported USDA bundle: {bundle.usdaPath}";
            RefreshHud();
            return bundle.usdaPath;
        }

        private void PlaceCurrentTool()
        {
            switch (CurrentTool)
            {
                case RoomDesignerTool.Ground:
                    SetBaseCell(RoomDesignerCellKinds.Ground);
                    break;
                case RoomDesignerTool.Hole:
                    SetBaseCell(RoomDesignerCellKinds.Hole);
                    break;
                case RoomDesignerTool.Rock:
                    SetStackCell(RoomDesignerCellKinds.Rock);
                    break;
                case RoomDesignerTool.EnemySpawn:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.Enemy, "spawn_enemy");
                    break;
                case RoomDesignerTool.RewardSpawn:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.RoomReward, "spawn_reward");
                    break;
                case RoomDesignerTool.SafeStart:
                    MoveSafeStart();
                    break;
                case RoomDesignerTool.EnemyNormal:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemyNormal, "spawn_enemy_normal");
                    break;
                case RoomDesignerTool.EnemyFlying:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemyFlying, "spawn_enemy_flying");
                    break;
                case RoomDesignerTool.EnemyFast:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemyFast, "spawn_enemy_fast");
                    break;
                case RoomDesignerTool.EnemyHeavy:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemyHeavy, "spawn_enemy_heavy");
                    break;
                case RoomDesignerTool.EnemyCharger:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemyCharger, "spawn_enemy_charger");
                    break;
                case RoomDesignerTool.EnemyTurret:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemyTurret, "spawn_enemy_turret");
                    break;
                case RoomDesignerTool.EnemySplitter:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemySplitter, "spawn_enemy_splitter");
                    break;
                case RoomDesignerTool.ActiveDoor:
                    SetNearestDoor(RoomDesignerDoorKinds.Door);
                    break;
                case RoomDesignerTool.SecretDoor:
                    SetNearestDoor(RoomDesignerDoorKinds.Secret);
                    break;
                case RoomDesignerTool.InactiveDoor:
                    SetNearestDoor(RoomDesignerDoorKinds.Inactive);
                    break;
            }
        }

        private Vector3 CursorWorldPosition()
        {
            return previewRoot != null
                ? previewRoot.TransformPoint(new Vector3(CursorX, CursorLayer + 0.5f, CursorZ))
                : transform.TransformPoint(new Vector3(CursorX, CursorLayer + 0.5f, CursorZ));
        }

        private void SetBaseCell(string kind)
        {
            currentProject.cells.RemoveAll(cell => cell.x == CursorX && cell.z == CursorZ && cell.layer == 0 && (cell.kind == RoomDesignerCellKinds.Ground || cell.kind == RoomDesignerCellKinds.Hole));
            currentProject.cells.Add(new RoomDesignerCell(CursorX, CursorZ, 0, kind));
        }

        private void SetStackCell(string kind)
        {
            currentProject.cells.RemoveAll(cell => cell.x == CursorX && cell.z == CursorZ && cell.layer == CursorLayer && cell.kind == kind);
            currentProject.cells.Add(new RoomDesignerCell(CursorX, CursorZ, CursorLayer, kind));
        }

        private void AddOrReplaceMarker(string kind, string idPrefix)
        {
            currentProject.markers.RemoveAll(marker => marker.kind == kind && Mathf.RoundToInt(marker.x) == CursorX && Mathf.RoundToInt(marker.z) == CursorZ);
            currentProject.markers.Add(new RoomDesignerMarker($"{idPrefix}_{currentProject.markers.Count:00}", kind, CursorX, 0f, CursorZ));
        }

        private void MoveSafeStart()
        {
            currentProject.markers.RemoveAll(marker => marker.kind == RoomDesignerMarkerKinds.SafeStart);
            currentProject.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, CursorX, 0f, CursorZ));
        }

        private void SetNearestDoor(string state)
        {
            var nearest = currentProject.doorPorts
                .OrderBy(door => Vector2.Distance(new Vector2(door.x, door.z), new Vector2(CursorX, CursorZ)))
                .FirstOrDefault();
            if (nearest != null)
            {
                nearest.state = state;
            }
        }

        private void EraseAtCursor()
        {
            currentProject.cells.RemoveAll(cell => cell.x == CursorX && cell.z == CursorZ && (cell.layer == CursorLayer || cell.kind == RoomDesignerCellKinds.Hole));
            currentProject.markers.RemoveAll(marker => Mathf.RoundToInt(marker.x) == CursorX && Mathf.RoundToInt(marker.z) == CursorZ && marker.kind != RoomDesignerMarkerKinds.SafeStart);
            var nearDoor = currentProject.doorPorts
                .OrderBy(door => Vector2.Distance(new Vector2(door.x, door.z), new Vector2(CursorX, CursorZ)))
                .FirstOrDefault();
            if (nearDoor != null && Vector2.Distance(new Vector2(nearDoor.x, nearDoor.z), new Vector2(CursorX, CursorZ)) < 1.25f)
            {
                nearDoor.state = RoomDesignerDoorKinds.Available;
            }
        }

        private void Eyedropper()
        {
            var marker = currentProject.markers.FirstOrDefault(candidate => Mathf.RoundToInt(candidate.x) == CursorX && Mathf.RoundToInt(candidate.z) == CursorZ);
            if (marker != null && RoomDesignerMarkerKinds.IsEnemy(marker.kind))
            {
                toolIndex = Array.IndexOf(tools, ToolForEnemyKind(marker.kind));
                return;
            }

            if (marker?.kind == RoomDesignerMarkerKinds.RoomReward)
            {
                toolIndex = Array.IndexOf(tools, RoomDesignerTool.RewardSpawn);
                return;
            }

            var cell = currentProject.cells.LastOrDefault(candidate => candidate.x == CursorX && candidate.z == CursorZ && candidate.layer == CursorLayer);
            toolIndex = cell?.kind switch
            {
                RoomDesignerCellKinds.Hole => Array.IndexOf(tools, RoomDesignerTool.Hole),
                RoomDesignerCellKinds.Rock => Array.IndexOf(tools, RoomDesignerTool.Rock),
                _ => Array.IndexOf(tools, RoomDesignerTool.Ground)
            };
        }

        private static RoomDesignerTool ToolForEnemyKind(string kind)
        {
            return kind switch
            {
                RoomDesignerMarkerKinds.EnemyFlying => RoomDesignerTool.EnemyFlying,
                RoomDesignerMarkerKinds.EnemyFast => RoomDesignerTool.EnemyFast,
                RoomDesignerMarkerKinds.EnemyHeavy => RoomDesignerTool.EnemyHeavy,
                RoomDesignerMarkerKinds.EnemyCharger => RoomDesignerTool.EnemyCharger,
                RoomDesignerMarkerKinds.EnemyTurret => RoomDesignerTool.EnemyTurret,
                RoomDesignerMarkerKinds.EnemySplitter => RoomDesignerTool.EnemySplitter,
                RoomDesignerMarkerKinds.EnemyNormal => RoomDesignerTool.EnemyNormal,
                _ => RoomDesignerTool.EnemySpawn
            };
        }

        private void SaveDraft(string nextStatus)
        {
            store.SaveDraft(slotId, currentProject);
            status = nextStatus;
        }

        private void RebuildPreview()
        {
            EnsureRoots();
            ClearChildren(previewRoot);
            BuildGrid();
            foreach (var cell in currentProject.cells)
            {
                BuildCell(cell);
            }

            foreach (var door in currentProject.doorPorts)
            {
                BuildDoor(door);
            }

            foreach (var marker in currentProject.markers)
            {
                BuildMarker(marker);
            }

            var cursor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cursor.name = $"cursor_{CursorX}_{CursorZ}_{CursorLayer}";
            cursor.transform.SetParent(previewRoot, false);
            cursor.transform.localPosition = new Vector3(CursorX, CursorLayer + 0.55f, CursorZ);
            cursor.transform.localScale = new Vector3(1.08f, 0.08f, 1.08f);
            MaterialResolver.ApplyTo(cursor, MaterialRole.DesignerCursor);
            AutoFitCamera();
        }

        private void AutoFitCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var dimensions = RoomDesignerFootprintUtility.Dimensions(currentProject.footprintPreset);
            var longest = Mathf.Max(dimensions.x, dimensions.y);
            camera.transform.position = new Vector3(0f, Mathf.Max(10f, longest * 0.55f), Mathf.Max(8f, longest * 0.45f));
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            if (camera.orthographic)
            {
                camera.orthographicSize = Mathf.Max(dimensions.x * 0.42f, dimensions.y * 0.72f, 6f);
            }
            else
            {
                camera.fieldOfView = 54f;
            }
        }

        private void BuildGrid()
        {
            RoomDesignerFootprintUtility.RoomBounds(currentProject.footprintPreset, out var minX, out var maxX, out var minZ, out var maxZ);
            for (var x = minX; x <= maxX + 0.01f; x += 1f)
            {
                BuildCube($"grid_x_{x}", new Vector3(x, 0.02f, (minZ + maxZ) * 0.5f), new Vector3(0.02f, 0.02f, maxZ - minZ), MaterialRole.DesignerGrid);
            }

            for (var z = minZ; z <= maxZ + 0.01f; z += 1f)
            {
                BuildCube($"grid_z_{z}", new Vector3((minX + maxX) * 0.5f, 0.025f, z), new Vector3(maxX - minX, 0.02f, 0.02f), MaterialRole.DesignerGrid);
            }

            BuildMacroGuides();
        }

        private void BuildMacroGuides()
        {
            var occupied = RoomDesignerFootprintUtility.OccupiedCells(currentProject.footprintPreset);
            var occupiedSet = occupied.ToHashSet();
            foreach (var cell in occupied)
            {
                var center = RoomDesignerFootprintUtility.ChunkCenter(currentProject.footprintPreset, cell);
                var halfX = RoomDesignerFootprintUtility.ChunkWidthTiles * 0.5f;
                var halfZ = RoomDesignerFootprintUtility.ChunkHeightTiles * 0.5f;
                var y = 0.09f;
                BuildCube($"chunk_{cell.x}_{cell.y}_north", new Vector3(center.x, y, center.y - halfZ), new Vector3(RoomDesignerFootprintUtility.ChunkWidthTiles, 0.04f, 0.06f), MaterialRole.DesignerDoorAvailable);
                BuildCube($"chunk_{cell.x}_{cell.y}_south", new Vector3(center.x, y, center.y + halfZ), new Vector3(RoomDesignerFootprintUtility.ChunkWidthTiles, 0.04f, 0.06f), MaterialRole.DesignerDoorAvailable);
                BuildCube($"chunk_{cell.x}_{cell.y}_east", new Vector3(center.x + halfX, y, center.y), new Vector3(0.06f, 0.04f, RoomDesignerFootprintUtility.ChunkHeightTiles), MaterialRole.DesignerDoorAvailable);
                BuildCube($"chunk_{cell.x}_{cell.y}_west", new Vector3(center.x - halfX, y, center.y), new Vector3(0.06f, 0.04f, RoomDesignerFootprintUtility.ChunkHeightTiles), MaterialRole.DesignerDoorAvailable);

                foreach (var direction in new[] { "north", "south", "east", "west" })
                {
                    if (!occupiedSet.Contains(cell + RoomDesignerFootprintUtility.DirectionOffsetInt(direction)))
                    {
                        continue;
                    }

                    var seamCenter = direction switch
                    {
                        "east" => center + new Vector2(halfX, 0f),
                        "west" => center + new Vector2(-halfX, 0f),
                        "south" => center + new Vector2(0f, halfZ),
                        _ => center + new Vector2(0f, -halfZ)
                    };
                    var scale = direction is "east" or "west"
                        ? new Vector3(0.1f, 0.08f, RoomDesignerFootprintUtility.ChunkHeightTiles)
                        : new Vector3(RoomDesignerFootprintUtility.ChunkWidthTiles, 0.08f, 0.1f);
                    BuildCube($"internalSeam_{cell.x}_{cell.y}_{direction}", new Vector3(seamCenter.x, 0.14f, seamCenter.y), scale, MaterialRole.DesignerGrid);
                }
            }
        }

        private void BuildCell(RoomDesignerCell cell)
        {
            if (cell.kind == RoomDesignerCellKinds.Ground)
            {
                BuildCube($"tileGround_{cell.x}_{cell.z}", new Vector3(cell.x, -0.5f, cell.z), Vector3.one, MaterialRole.DesignerGround);
            }
            else if (cell.kind == RoomDesignerCellKinds.Hole)
            {
                BuildCube($"tileHole_{cell.x}_{cell.z}", new Vector3(cell.x, 0.03f, cell.z), new Vector3(0.86f, 0.06f, 0.86f), MaterialRole.DesignerHole);
            }
            else if (cell.kind == RoomDesignerCellKinds.Rock)
            {
                BuildCube($"rockTile_{cell.x}_{cell.z}_{cell.layer}", new Vector3(cell.x, cell.layer + 0.5f, cell.z), Vector3.one, MaterialRole.DesignerRock);
            }

            if (LabelsVisible)
            {
                BuildLabel(cell.kind, new Vector3(cell.x, cell.layer + 1.08f, cell.z));
            }
        }

        private void BuildDoor(RoomDesignerDoorPortState door)
        {
            var role = door.state switch
            {
                RoomDesignerDoorKinds.Door => MaterialRole.DesignerDoorActive,
                RoomDesignerDoorKinds.Secret => MaterialRole.DesignerDoorSecret,
                RoomDesignerDoorKinds.Inactive => MaterialRole.DesignerGrid,
                _ => MaterialRole.DesignerDoorAvailable
            };
            BuildCube($"doorAnchor_{door.id}_{door.state}", new Vector3(door.x, 0.65f, door.z), door.direction is "east" or "west" ? new Vector3(0.18f, 1.3f, 1f) : new Vector3(1f, 1.3f, 0.18f), role);
            if (LabelsVisible)
            {
                BuildLabel($"{door.id} {door.state} host({door.hostCellX},{door.hostCellZ}) lane {door.laneIndex}", new Vector3(door.x, 1.5f, door.z));
            }
        }

        private void BuildMarker(RoomDesignerMarker marker)
        {
            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerObject.name = $"{marker.kind}_{marker.id}";
            markerObject.transform.SetParent(previewRoot, false);
            markerObject.transform.localPosition = new Vector3(marker.x, marker.y + 0.2f, marker.z);
            markerObject.transform.localScale = Vector3.one * 0.36f;
            MaterialResolver.ApplyTo(markerObject, RoleForMarker(marker.kind));
            if (LabelsVisible)
            {
                BuildLabel(marker.kind, new Vector3(marker.x, marker.y + 0.72f, marker.z));
            }
        }

        private GameObject BuildCube(string name, Vector3 position, Vector3 scale, MaterialRole role)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(previewRoot, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            MaterialResolver.ApplyTo(cube, role);
            return cube;
        }

        private static MaterialRole RoleForMarker(string markerKind)
        {
            if (RoomDesignerMarkerKinds.IsEnemy(markerKind))
            {
                return MaterialRole.DesignerSpawnEnemy;
            }

            return markerKind switch
            {
                RoomDesignerMarkerKinds.SafeStart => MaterialRole.DesignerSpawnSafeStart,
                _ => MaterialRole.DesignerSpawnReward
            };
        }

        private void BuildLabel(string label, Vector3 position)
        {
            var textObject = new GameObject($"label_{label}", typeof(TextMesh));
            textObject.transform.SetParent(previewRoot, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            var text = textObject.GetComponent<TextMesh>();
            text.text = label;
            text.fontSize = 32;
            text.characterSize = 0.08f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private void EnsureRoots()
        {
            if (previewRoot == null)
            {
                previewRoot = transform.Find("RoomDesignerPreviewRoot");
                if (previewRoot == null)
                {
                    var preview = new GameObject("RoomDesignerPreviewRoot");
                    preview.transform.SetParent(transform, false);
                    previewRoot = preview.transform;
                }
            }

            if (hudCanvas == null)
            {
                var canvasObject = new GameObject("RoomDesignerHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                hudCanvas = canvasObject.GetComponent<Canvas>();
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudCanvas.sortingOrder = 20;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (hudText == null)
            {
                var textObject = new GameObject("RoomDesignerHUD.Text", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(hudCanvas.transform, false);
                var rect = (RectTransform)textObject.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(32f, -32f);
                rect.sizeDelta = new Vector2(900f, 220f);
                hudText = textObject.GetComponent<Text>();
                hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hudText.fontSize = 24;
                hudText.alignment = TextAnchor.UpperLeft;
                hudText.color = Color.white;
                hudText.raycastTarget = false;
            }
        }

        private void RefreshHud()
        {
            if (hudText == null)
            {
                return;
            }

            LastValidationReport = RoomDesignerDraftValidator.Validate(currentProject);
            var dimensions = RoomDesignerFootprintUtility.Dimensions(currentProject.footprintPreset);
            var selectedDoor = SelectedDoorSummary();
            var enabledPorts = currentProject.doorPorts.Count(door => door.state != RoomDesignerDoorKinds.Inactive);
            var draftCount = libraryState?.Drafts.Count ?? 0;
            hudText.text =
                $"Room Designer - Macro Authoring\nDrafts: {draftCount} | Footprint: {currentProject.footprintPreset} ({dimensions.x}x{dimensions.y}m) | Enabled ports: {enabledPorts}/{currentProject.doorPorts.Count}\n" +
                $"Tool: {CurrentTool} | Cursor: ({CursorX}, {CursorLayer}, {CursorZ}) | Door: {selectedDoor} | Labels: {(LabelsVisible ? "On" : "Off")}\n" +
                $"Validation: {LastValidationReport.Summary()} | Errors: {LastValidationReport.Errors.Count} | Warnings: {LastValidationReport.Warnings.Count}\n" +
                $"WASD/Arrows move | Q/E tool | Z/X layer | Space place | Delete erase | F eyedropper | Tab labels\n" +
                $"P playtest | J export validated bundle | U export USDA bundle | Esc menu\n{status}";
        }

        private string SelectedDoorSummary()
        {
            var nearest = currentProject.doorPorts
                .OrderBy(door => Vector2.Distance(new Vector2(door.x, door.z), new Vector2(CursorX, CursorZ)))
                .FirstOrDefault();
            return nearest == null
                ? "none"
                : $"{nearest.id} {nearest.state} host({nearest.hostCellX},{nearest.hostCellZ})";
        }

        private void ReturnToMainMenu()
        {
            status = "Returning to menu";
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(AppShellRoute.MainMenu);
                SceneLoaderService.LoadRouteAsync(AppShellRoute.MainMenu);
            }
        }

        private ProfileSlotId ResolveSlotId()
        {
            var selected = ProfileSessionHost.Instance?.SelectedProfileContext?.SelectedProfile;
            return selected != null && !selected.IsEmpty ? new ProfileSlotId(selected.SlotIndex) : new ProfileSlotId(0);
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                var child = root.GetChild(index).gameObject;
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

        private static int Mod(int value, int length)
        {
            return (value % length + length) % length;
        }
    }
}
