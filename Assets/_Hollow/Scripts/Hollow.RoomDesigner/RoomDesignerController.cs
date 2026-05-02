using System;
using System.Linq;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Persistence;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hollow.RoomDesigner
{
    public sealed class RoomDesignerController : MonoBehaviour
    {
        private const float RepeatDelaySeconds = 0.16f;
        private const float GridOriginY = 0f;
        private const float GridLineThickness = 0.02f;
        private const float FlatSurfaceThickness = 0.06f;
        private const float CursorTileThickness = 0.08f;
        private const float DoorAnchorHeight = 1.3f;
        private const float MacroGuideThickness = 0.04f;
        private const float InternalSeamThickness = 0.08f;
        private const float SpatialTopDownTiltDegrees = 55f;
        private const float CameraZoomStep = 0.15f;
        private const float LibraryListViewportHeight = 398f;
        private const float LibraryListViewportWidth = 778f;
        private const float LibraryListRowHeight = 34f;
        private const float LibraryListRowGap = 6f;

        [SerializeField] private Transform previewRoot;
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private RoomDesignerCuratedDraftCatalogDefinition curatedDraftCatalog;

        private readonly RoomDesignerTool[] tools = Enum.GetValues(typeof(RoomDesignerTool)).Cast<RoomDesignerTool>().ToArray();
        private readonly RoomDesignerFootprintPreset[] templatePresets = Enum.GetValues(typeof(RoomDesignerFootprintPreset)).Cast<RoomDesignerFootprintPreset>().ToArray();
        private readonly RoomDesignerCameraController cameraController = new();
        private readonly RoomDesignerLightingController lightingController = new();
        private RoomDesignerStore store;
        private RoomDesignerDraftLibraryState libraryState;
        private ProfileSlotId slotId;
        private RoomDesignerProject currentProject;
        private Text hudText;
        private RectTransform libraryPanel;
        private RectTransform libraryListViewport;
        private RectTransform libraryListContent;
        private ScrollRect libraryScrollRect;
        private RectTransform infoPanel;
        private RectTransform controlsPanel;
        private Text controlsText;
        private RectTransform toolToolbarPanel;
        private RectTransform previewModeButtonPanel;
        private Text previewModeButtonText;
        private RectTransform cameraModeButtonPanel;
        private Text cameraModeButtonText;
        private RectTransform zoomOutButtonPanel;
        private RectTransform zoomInButtonPanel;
        private float nextMoveTime;
        private int toolIndex;
        private int librarySelectedIndex;
        private float libraryScrollOffset;
        private int scenePreviewMissingBindings;
        private bool forceSpatialTopDownTiltForTests;
        private string pendingDeleteProjectId = string.Empty;
        private string pendingDeleteDisplayName = string.Empty;
        private string status = "Ready";

        public RoomDesignerMode Mode { get; private set; } = RoomDesignerMode.Loading;

        public RoomDesignerProject CurrentProject => currentProject;

        public RoomDesignerDraftLibraryState LibraryState => libraryState;

        public RoomDesignerLibraryViewModel LibraryViewModel { get; private set; }

        public RoomDesignerCuratedDraftCatalogDefinition CuratedDraftCatalog => curatedDraftCatalog;

        public RoomDesignerValidationReport LastValidationReport { get; private set; }

        public RoomDesignerTool CurrentTool => tools[Mathf.Clamp(toolIndex, 0, tools.Length - 1)];

        public Vector3 CameraTargetPosition => cameraController.TargetPosition;

        public RoomDesignerPreviewMode PreviewMode { get; private set; } = RoomDesignerPreviewMode.Graybox;

        public RoomDesignerCameraViewMode CameraViewMode { get; private set; } = RoomDesignerCameraViewMode.Perspective;

        public float CameraZoomMultiplier => cameraController.ZoomMultiplier;

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
            Mode = RoomDesignerMode.Loading;
            store = new RoomDesignerStore();
            slotId = ResolveSlotId();
            libraryState = new RoomDesignerDraftLibraryState(store, slotId, autoCreateDefaultDraft: false, curatedDraftCatalog);
            currentProject = null;
            EnsureRoots();
            ShowLibrary();
        }

        public void InitializeForTest(RoomDesignerStore nextStore, ProfileSlotId nextSlotId, RoomDesignerProject nextProject)
        {
            store = nextStore;
            slotId = nextSlotId;
            store.SaveDraft(slotId, nextProject);
            libraryState = new RoomDesignerDraftLibraryState(store, slotId, autoCreateDefaultDraft: true, curatedDraftCatalog);
            EnsureRoots();
            EnterEditing(libraryState.OpenDraft(nextProject.projectId));
        }

        public void InitializeLibraryForTest(RoomDesignerStore nextStore, ProfileSlotId nextSlotId)
        {
            store = nextStore;
            slotId = nextSlotId;
            libraryState = new RoomDesignerDraftLibraryState(store, slotId, autoCreateDefaultDraft: false, curatedDraftCatalog);
            currentProject = null;
            EnsureRoots();
            ShowLibrary();
        }

        public void ConfigureCuratedDraftCatalog(RoomDesignerCuratedDraftCatalogDefinition nextCuratedDraftCatalog)
        {
            curatedDraftCatalog = nextCuratedDraftCatalog;
            if (store != null)
            {
                libraryState = new RoomDesignerDraftLibraryState(store, slotId, autoCreateDefaultDraft: false, curatedDraftCatalog);
            }
        }

        public void ShowLibrary()
        {
            EnsureRoots();
            Mode = RoomDesignerMode.Library;
            pendingDeleteProjectId = string.Empty;
            pendingDeleteDisplayName = string.Empty;
            currentProject = null;
            store ??= new RoomDesignerStore();
            libraryState ??= new RoomDesignerDraftLibraryState(store, slotId, autoCreateDefaultDraft: false, curatedDraftCatalog);
            libraryState.Reload();
            if (libraryState.Drafts.Count == 0 && libraryState.CuratedDrafts.Count == 0)
            {
                ShowCreateTemplates();
                return;
            }

            librarySelectedIndex = Mathf.Clamp(librarySelectedIndex, 0, Mathf.Max(0, LibraryOptionCount() - 1));
            status = libraryState.LatestMessage;
            ClearPreview();
            RefreshHud();
            RefreshLibraryPanel();
        }

        public void ShowCreateTemplates()
        {
            EnsureRoots();
            Mode = RoomDesignerMode.CreateTemplate;
            currentProject = null;
            librarySelectedIndex = Mathf.Clamp(librarySelectedIndex, 0, templatePresets.Length - 1);
            pendingDeleteProjectId = string.Empty;
            pendingDeleteDisplayName = string.Empty;
            status = libraryState != null && libraryState.Drafts.Count == 0 && libraryState.CuratedDrafts.Count == 0
                ? "No drafts yet. Choose a template to create your first room."
                : "Choose a room template.";
            ClearPreview();
            RefreshHud();
            RefreshLibraryPanel();
        }

        public RoomDesignerProject CreateDraft(RoomDesignerFootprintPreset preset)
        {
            store ??= new RoomDesignerStore();
            libraryState ??= new RoomDesignerDraftLibraryState(store, slotId, autoCreateDefaultDraft: true, curatedDraftCatalog);
            currentProject = libraryState.CreateDraft(preset);
            status = libraryState.LatestMessage;
            EnterEditing(currentProject);
            return currentProject;
        }

        public RoomDesignerProject OpenDraft(string projectId)
        {
            currentProject = libraryState.OpenDraft(projectId);
            status = libraryState.LatestMessage;
            EnterEditing(currentProject);
            return currentProject;
        }

        public RoomDesignerProject DuplicateDraft(string projectId)
        {
            currentProject = libraryState.DuplicateDraft(projectId);
            status = libraryState.LatestMessage;
            EnterEditing(currentProject);
            return currentProject;
        }

        public RoomDesignerProject DeleteDraft(string projectId)
        {
            currentProject = libraryState.DeleteDraft(projectId);
            status = libraryState.LatestMessage;
            if (currentProject != null)
            {
                EnterEditing(currentProject);
            }
            else
            {
                if (HasLibraryRows())
                {
                    ShowLibrary();
                }
                else
                {
                    ShowCreateTemplates();
                }
            }
            return currentProject;
        }

        public void OpenSelectedDraft()
        {
            if (Mode != RoomDesignerMode.Library || libraryState == null)
            {
                return;
            }

            var curatedIndex = CuratedIndexFromSelection();
            if (curatedIndex >= 0)
            {
                currentProject = libraryState.OpenCuratedAsEditableCopy(libraryState.CuratedDrafts[curatedIndex].projectId);
                status = libraryState.LatestMessage;
                EnterEditing(currentProject);
                return;
            }

            var curatedCount = libraryState.CuratedDrafts.Count;
            var draftCount = libraryState.Drafts.Count;
            if (librarySelectedIndex == curatedCount + draftCount)
            {
                librarySelectedIndex = 0;
                ShowCreateTemplates();
                return;
            }

            if (librarySelectedIndex == curatedCount + draftCount + 1)
            {
                ReturnToMainMenu();
                return;
            }

            var draftIndex = DraftIndexFromSelection();
            if (draftIndex < 0)
            {
                ShowCreateTemplates();
                return;
            }

            OpenDraft(libraryState.Drafts[draftIndex].projectId);
        }

        public void CreateDraftFromSelectedTemplate()
        {
            if (Mode == RoomDesignerMode.CreateTemplate && librarySelectedIndex >= templatePresets.Length)
            {
                if (HasLibraryRows())
                {
                    ShowLibrary();
                }
                else
                {
                    ReturnToMainMenu();
                }

                return;
            }

            var index = Mathf.Clamp(librarySelectedIndex, 0, templatePresets.Length - 1);
            CreateDraft(templatePresets[index]);
        }

        public void RequestDeleteDraft()
        {
            if (Mode != RoomDesignerMode.Library || libraryState == null || libraryState.Drafts.Count == 0)
            {
                return;
            }

            var draftIndex = DraftIndexFromSelection();
            if (draftIndex < 0)
            {
                return;
            }

            var draft = libraryState.Drafts[draftIndex];
            pendingDeleteProjectId = draft.projectId;
            pendingDeleteDisplayName = draft.displayName;
            Mode = RoomDesignerMode.ConfirmDelete;
            librarySelectedIndex = 0;
            status = $"Delete {pendingDeleteDisplayName}?";
            RefreshHud();
            RefreshLibraryPanel();
        }

        public void ConfirmDeleteDraft()
        {
            if (Mode != RoomDesignerMode.ConfirmDelete || string.IsNullOrWhiteSpace(pendingDeleteProjectId))
            {
                return;
            }

            libraryState.DeleteDraft(pendingDeleteProjectId);
            librarySelectedIndex = 0;
            pendingDeleteProjectId = string.Empty;
            pendingDeleteDisplayName = string.Empty;
            ShowLibrary();
        }

        public void SelectTool(RoomDesignerTool tool)
        {
            var index = Array.IndexOf(tools, tool);
            if (index >= 0)
            {
                toolIndex = index;
                RefreshHud();
                RefreshToolToolbar();
            }
        }

        public void TogglePreviewMode()
        {
            PreviewMode = PreviewMode == RoomDesignerPreviewMode.Graybox
                ? RoomDesignerPreviewMode.Scene
                : RoomDesignerPreviewMode.Graybox;
            status = $"Preview: {PreviewMode}";
            RebuildPreview();
            RefreshHud();
        }

        public void ToggleCameraViewMode()
        {
            CameraViewMode = CameraViewMode == RoomDesignerCameraViewMode.Perspective
                ? RoomDesignerCameraViewMode.TopDown
                : RoomDesignerCameraViewMode.Perspective;
            status = $"Camera: {CameraViewMode}";
            ApplyCameraViewPresentation(immediate: false);
            RefreshHud();
        }

        public void ZoomIn()
        {
            cameraController.AdjustZoom(-CameraZoomStep);
            status = $"Zoom: {cameraController.ZoomMultiplier:0.00}x";
            ApplyCameraViewPresentation(immediate: false);
            RefreshHud();
        }

        public void ZoomOut()
        {
            cameraController.AdjustZoom(CameraZoomStep);
            status = $"Zoom: {cameraController.ZoomMultiplier:0.00}x";
            ApplyCameraViewPresentation(immediate: false);
            RefreshHud();
        }

        public void SetSpatialTopDownTiltForTests(bool enabled)
        {
            forceSpatialTopDownTiltForTests = enabled;
            ApplyCameraViewPresentation(immediate: true);
        }

        private void Update()
        {
            ApplyInput(RoomDesignerInputReader.ReadCurrent(), Time.time);
            if (Mode == RoomDesignerMode.Editing)
            {
                cameraController.Tick(Camera.main, Time.deltaTime);
            }
        }

        public void ApplyInput(RoomDesignerInputSnapshot input, float timeSeconds = 999f)
        {
            if (Mode != RoomDesignerMode.Editing)
            {
                ApplyLibraryInput(input, timeSeconds);
                return;
            }

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

            if (input.TogglePreviewModePressed)
            {
                TogglePreviewMode();
            }

            if (input.ToggleCameraModePressed)
            {
                ToggleCameraViewMode();
            }

            if (input.ZoomDelta > 0)
            {
                ZoomIn();
            }
            else if (input.ZoomDelta < 0)
            {
                ZoomOut();
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
                ShowLibrary();
            }

            if (changed)
            {
                RebuildPreview();
                RefreshHud();
            }
        }

        private void ApplyLibraryInput(RoomDesignerInputSnapshot input, float timeSeconds)
        {
            var optionCount = LibraryOptionCount();
            if ((input.MoveX != 0 || input.MoveZ != 0) && optionCount > 0 && timeSeconds >= nextMoveTime)
            {
                var delta = input.MoveZ != 0 ? input.MoveZ : input.MoveX;
                librarySelectedIndex = Mod(librarySelectedIndex + delta, optionCount);
                nextMoveTime = timeSeconds + RepeatDelaySeconds;
                RefreshHud();
                RefreshLibraryPanel();
            }

            if (input.PlacePressed)
            {
                if (Mode == RoomDesignerMode.CreateTemplate)
                {
                    CreateDraftFromSelectedTemplate();
                }
                else if (Mode == RoomDesignerMode.Library)
                {
                    var curatedCount = libraryState?.CuratedDrafts.Count ?? 0;
                    var draftCount = libraryState?.Drafts.Count ?? 0;
                    if (libraryState != null && librarySelectedIndex == curatedCount + draftCount)
                    {
                        librarySelectedIndex = 0;
                        ShowCreateTemplates();
                    }
                    else if (libraryState != null && librarySelectedIndex == curatedCount + draftCount + 1)
                    {
                        ReturnToMainMenu();
                    }
                    else
                    {
                        OpenSelectedDraft();
                    }
                }
                else if (Mode == RoomDesignerMode.ConfirmDelete)
                {
                    if (librarySelectedIndex == 0)
                    {
                        ConfirmDeleteDraft();
                    }
                    else
                    {
                        ShowLibrary();
                    }
                }
            }

            if (input.ErasePressed && Mode == RoomDesignerMode.Library)
            {
                RequestDeleteDraft();
            }

            if (input.BackPressed)
            {
                if (Mode == RoomDesignerMode.ConfirmDelete)
                {
                    ShowLibrary();
                }
                else if (Mode == RoomDesignerMode.CreateTemplate && libraryState != null && libraryState.Drafts.Count > 0)
                {
                    ShowLibrary();
                }
                else
                {
                    ReturnToMainMenu();
                }
            }
        }

        private void EnterEditing(RoomDesignerProject project)
        {
            if (project == null)
            {
                ShowCreateTemplates();
                return;
            }

            Mode = RoomDesignerMode.Editing;
            currentProject = project;
            CursorX = 0;
            CursorZ = 0;
            CursorLayer = 0;
            SnapCursorToFootprint();
            if (libraryPanel != null)
            {
                libraryPanel.gameObject.SetActive(false);
            }

            RebuildPreview();
            RefreshHud();
            ApplyCameraViewPresentation(immediate: true);
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
                case RoomDesignerTool.Spike:
                    SetHazardCell(RoomDesignerCellKinds.Spike);
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
                case RoomDesignerTool.EnemySpittingPod:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemySpittingPod, "spawn_enemy_spitting_pod");
                    break;
                case RoomDesignerTool.EnemyRat:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemyRat, "spawn_enemy_rat");
                    break;
                case RoomDesignerTool.EnemySpider:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.EnemySpider, "spawn_enemy_spider");
                    break;
                case RoomDesignerTool.StandardBarrel:
                    AddOrReplaceInteractiveObject(RoomDesignerMarkerKinds.StandardBarrel, "barrel_standard");
                    break;
                case RoomDesignerTool.ExplosiveBarrel:
                    AddOrReplaceInteractiveObject(RoomDesignerMarkerKinds.ExplosiveBarrel, "barrel_explosive");
                    break;
                case RoomDesignerTool.ChestSpawn:
                    AddOrReplaceMarker(RoomDesignerMarkerKinds.ChestSpawn, "spawn_chest");
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
                ? previewRoot.TransformPoint(new Vector3(CursorX, CursorLayer + CenterAboveGrid(CursorTileThickness), CursorZ))
                : transform.TransformPoint(new Vector3(CursorX, CursorLayer + CenterAboveGrid(CursorTileThickness), CursorZ));
        }

        private void SetBaseCell(string kind)
        {
            currentProject.cells.RemoveAll(cell => cell.x == CursorX && cell.z == CursorZ && cell.layer == 0 && (cell.kind == RoomDesignerCellKinds.Ground || cell.kind == RoomDesignerCellKinds.Hole || cell.kind == RoomDesignerCellKinds.Spike));
            currentProject.cells.Add(new RoomDesignerCell(CursorX, CursorZ, 0, kind));
            if (kind == RoomDesignerCellKinds.Hole)
            {
                currentProject.markers.RemoveAll(marker => Mathf.RoundToInt(marker.x) == CursorX && Mathf.RoundToInt(marker.z) == CursorZ && RoomDesignerMarkerKinds.IsInteractiveObject(marker.kind));
            }
        }

        private void SetStackCell(string kind)
        {
            currentProject.cells.RemoveAll(cell => cell.x == CursorX && cell.z == CursorZ && cell.layer == CursorLayer && cell.kind == kind);
            currentProject.cells.Add(new RoomDesignerCell(CursorX, CursorZ, CursorLayer, kind));
        }

        private void SetHazardCell(string kind)
        {
            if (!currentProject.cells.Any(cell => cell.x == CursorX && cell.z == CursorZ && cell.layer == 0 && cell.kind == RoomDesignerCellKinds.Ground))
            {
                currentProject.cells.Add(new RoomDesignerCell(CursorX, CursorZ, 0, RoomDesignerCellKinds.Ground));
            }

            currentProject.cells.RemoveAll(cell => cell.x == CursorX && cell.z == CursorZ && cell.layer == 0 && cell.kind == RoomDesignerCellKinds.Hole);
            currentProject.cells.RemoveAll(cell => cell.x == CursorX && cell.z == CursorZ && cell.layer == 0 && cell.kind == kind);
            currentProject.cells.Add(new RoomDesignerCell(CursorX, CursorZ, 0, kind));
        }

        private void AddOrReplaceMarker(string kind, string idPrefix)
        {
            currentProject.markers.RemoveAll(marker => marker.kind == kind && Mathf.RoundToInt(marker.x) == CursorX && Mathf.RoundToInt(marker.z) == CursorZ);
            currentProject.markers.Add(new RoomDesignerMarker($"{idPrefix}_{currentProject.markers.Count:00}", kind, CursorX, 0f, CursorZ));
        }

        private void AddOrReplaceInteractiveObject(string kind, string idPrefix)
        {
            currentProject.markers.RemoveAll(marker => Mathf.RoundToInt(marker.x) == CursorX && Mathf.RoundToInt(marker.z) == CursorZ && RoomDesignerMarkerKinds.IsInteractiveObject(marker.kind));
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

            if (marker?.kind == RoomDesignerMarkerKinds.ChestSpawn)
            {
                toolIndex = Array.IndexOf(tools, RoomDesignerTool.ChestSpawn);
                return;
            }

            var cell = currentProject.cells.LastOrDefault(candidate => candidate.x == CursorX && candidate.z == CursorZ && candidate.layer == CursorLayer);
            toolIndex = cell?.kind switch
            {
                RoomDesignerCellKinds.Hole => Array.IndexOf(tools, RoomDesignerTool.Hole),
                RoomDesignerCellKinds.Rock => Array.IndexOf(tools, RoomDesignerTool.Rock),
                RoomDesignerCellKinds.Spike => Array.IndexOf(tools, RoomDesignerTool.Spike),
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
                RoomDesignerMarkerKinds.EnemySpittingPod => RoomDesignerTool.EnemySpittingPod,
                RoomDesignerMarkerKinds.EnemyRat => RoomDesignerTool.EnemyRat,
                RoomDesignerMarkerKinds.EnemySpider => RoomDesignerTool.EnemySpider,
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
            scenePreviewMissingBindings = 0;
            lightingController.Ensure(transform);
            lightingController.Apply(PreviewMode);
            if (currentProject == null)
            {
                ClearPreview();
                return;
            }

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
            cursor.transform.localPosition = new Vector3(CursorX, CursorLayer + CenterAboveGrid(CursorTileThickness), CursorZ);
            cursor.transform.localScale = new Vector3(1.08f, CursorTileThickness, 1.08f);
            MaterialResolver.ApplyTo(cursor, MaterialRole.DesignerCursor);
            ApplyCameraViewPresentation(immediate: false);
        }

        private void UpdateCameraTarget(bool immediate)
        {
            if (currentProject == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var target = new Vector3(CursorX, 0f, CursorZ);
            cameraController.SetTarget(target, currentProject.footprintPreset);
            if (immediate)
            {
                cameraController.ApplyImmediate(camera);
            }
        }

        private void ApplyCameraViewPresentation(bool immediate)
        {
            var useSpatialTilt = CameraViewMode == RoomDesignerCameraViewMode.TopDown && ShouldUseSpatialTopDownTilt();
            if (previewRoot != null)
            {
                previewRoot.localRotation = useSpatialTilt
                    ? Quaternion.Euler(SpatialTopDownTiltDegrees, 0f, 0f)
                    : Quaternion.identity;
            }

            cameraController.SetViewMode(useSpatialTilt ? RoomDesignerCameraViewMode.Perspective : CameraViewMode);
            UpdateCameraTarget(immediate);
            RefreshCameraModeButton();
        }

        private bool ShouldUseSpatialTopDownTilt()
        {
            return forceSpatialTopDownTiltForTests
                || Application.platform.ToString().IndexOf("Vision", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void BuildGrid()
        {
            RoomDesignerFootprintUtility.RoomBounds(currentProject.footprintPreset, out var minX, out var maxX, out var minZ, out var maxZ);
            for (var x = minX; x <= maxX + 0.01f; x += 1f)
            {
                BuildCube($"grid_x_{x}", new Vector3(x, CenterAboveGrid(GridLineThickness), (minZ + maxZ) * 0.5f), new Vector3(0.02f, GridLineThickness, maxZ - minZ), MaterialRole.DesignerGrid);
            }

            for (var z = minZ; z <= maxZ + 0.01f; z += 1f)
            {
                BuildCube($"grid_z_{z}", new Vector3((minX + maxX) * 0.5f, CenterAboveGrid(GridLineThickness), z), new Vector3(maxX - minX, GridLineThickness, 0.02f), MaterialRole.DesignerGrid);
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
                var y = CenterAboveGrid(MacroGuideThickness);
                BuildCube($"chunk_{cell.x}_{cell.y}_north", new Vector3(center.x, y, center.y - halfZ), new Vector3(RoomDesignerFootprintUtility.ChunkWidthTiles, MacroGuideThickness, 0.06f), MaterialRole.DesignerDoorAvailable);
                BuildCube($"chunk_{cell.x}_{cell.y}_south", new Vector3(center.x, y, center.y + halfZ), new Vector3(RoomDesignerFootprintUtility.ChunkWidthTiles, MacroGuideThickness, 0.06f), MaterialRole.DesignerDoorAvailable);
                BuildCube($"chunk_{cell.x}_{cell.y}_east", new Vector3(center.x + halfX, y, center.y), new Vector3(0.06f, MacroGuideThickness, RoomDesignerFootprintUtility.ChunkHeightTiles), MaterialRole.DesignerDoorAvailable);
                BuildCube($"chunk_{cell.x}_{cell.y}_west", new Vector3(center.x - halfX, y, center.y), new Vector3(0.06f, MacroGuideThickness, RoomDesignerFootprintUtility.ChunkHeightTiles), MaterialRole.DesignerDoorAvailable);

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
                        ? new Vector3(0.1f, InternalSeamThickness, RoomDesignerFootprintUtility.ChunkHeightTiles)
                        : new Vector3(RoomDesignerFootprintUtility.ChunkWidthTiles, InternalSeamThickness, 0.1f);
                    BuildCube($"internalSeam_{cell.x}_{cell.y}_{direction}", new Vector3(seamCenter.x, CenterAboveGrid(InternalSeamThickness), seamCenter.y), scale, MaterialRole.DesignerGrid);
                }
            }
        }

        private void BuildCell(RoomDesignerCell cell)
        {
            if (cell.kind == RoomDesignerCellKinds.Ground)
            {
                var host = BuildCube($"tileGround_{cell.x}_{cell.z}", new Vector3(cell.x, CenterAboveGrid(FlatSurfaceThickness), cell.z), new Vector3(1f, FlatSurfaceThickness, 1f), MaterialRole.DesignerGround);
                AttachSceneCellVisual(host, cell);
            }
            else if (cell.kind == RoomDesignerCellKinds.Hole)
            {
                var host = BuildCube($"tileHole_{cell.x}_{cell.z}", new Vector3(cell.x, CenterAboveGrid(FlatSurfaceThickness), cell.z), new Vector3(0.86f, FlatSurfaceThickness, 0.86f), MaterialRole.DesignerHole);
                AttachSceneCellVisual(host, cell);
            }
            else if (cell.kind == RoomDesignerCellKinds.Rock)
            {
                var host = BuildCube($"rockTile_{cell.x}_{cell.z}_{cell.layer}", new Vector3(cell.x, cell.layer + 0.5f, cell.z), Vector3.one, MaterialRole.DesignerRock);
                AttachSceneCellVisual(host, cell);
            }
            else if (cell.kind == RoomDesignerCellKinds.Spike)
            {
                var host = BuildCube($"hazardSpike_{cell.x}_{cell.z}", new Vector3(cell.x, CenterAboveGrid(FlatSurfaceThickness), cell.z), new Vector3(0.82f, FlatSurfaceThickness, 0.82f), MaterialRole.DesignerSpike);
                AttachSceneCellVisual(host, cell);
            }

            if (LabelsVisible && cell.kind != RoomDesignerCellKinds.Ground)
            {
                BuildLabel(RoomDesignerDisplayNames.ForCellKind(cell.kind), new Vector3(cell.x, cell.layer + 1.08f, cell.z));
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
            var host = BuildCube($"doorAnchor_{door.id}_{door.state}", new Vector3(door.x, CenterAboveGrid(DoorAnchorHeight), door.z), door.direction is "east" or "west" ? new Vector3(0.18f, DoorAnchorHeight, 1f) : new Vector3(1f, DoorAnchorHeight, 0.18f), role);
            AttachSceneDoorVisual(host, door);
            if (LabelsVisible)
            {
                BuildLabel(RoomDesignerDisplayNames.ForDoor(door), new Vector3(door.x, 1.5f, door.z));
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
            AttachSceneMarkerVisual(markerObject, marker);
            if (LabelsVisible)
            {
                BuildLabel(RoomDesignerDisplayNames.ForMarkerKind(marker.kind), new Vector3(marker.x, marker.y + 0.72f, marker.z));
            }
        }

        private void AttachSceneCellVisual(GameObject host, RoomDesignerCell cell)
        {
            if (PreviewMode != RoomDesignerPreviewMode.Scene || cell.kind == RoomDesignerCellKinds.Hole)
            {
                return;
            }

            if (!RoomDesignerScenePreviewBuilder.BuildVisualForCell(host, cell))
            {
                scenePreviewMissingBindings++;
            }
        }

        private void AttachSceneDoorVisual(GameObject host, RoomDesignerDoorPortState door)
        {
            if (PreviewMode != RoomDesignerPreviewMode.Scene)
            {
                return;
            }

            if (!RoomDesignerScenePreviewBuilder.BuildVisualForDoor(host, door))
            {
                scenePreviewMissingBindings++;
            }
        }

        private void AttachSceneMarkerVisual(GameObject host, RoomDesignerMarker marker)
        {
            if (PreviewMode != RoomDesignerPreviewMode.Scene)
            {
                return;
            }

            if (!RoomDesignerScenePreviewBuilder.BuildVisualForMarker(host, marker))
            {
                scenePreviewMissingBindings++;
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

        private static float CenterAboveGrid(float height)
        {
            return GridOriginY + height * 0.5f;
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
                RoomDesignerMarkerKinds.StandardBarrel => MaterialRole.DesignerBarrel,
                RoomDesignerMarkerKinds.ExplosiveBarrel => MaterialRole.DesignerExplosiveBarrel,
                RoomDesignerMarkerKinds.ChestSpawn => MaterialRole.DesignerChest,
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
            if (EventSystem.current == null)
            {
                var eventSystemObject = new GameObject("RoomDesignerEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystemObject.transform.SetParent(transform, false);
            }

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
            else
            {
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudCanvas.sortingOrder = Mathf.Max(hudCanvas.sortingOrder, 20);
                var scaler = hudCanvas.GetComponent<CanvasScaler>() ?? hudCanvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                if (hudCanvas.GetComponent<GraphicRaycaster>() == null)
                {
                    hudCanvas.gameObject.AddComponent<GraphicRaycaster>();
                }
            }

            if (infoPanel == null)
            {
                var panelObject = new GameObject("RoomDesignerInfoPanel", typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(hudCanvas.transform, false);
                infoPanel = (RectTransform)panelObject.transform;
                infoPanel.anchorMin = new Vector2(0f, 1f);
                infoPanel.anchorMax = new Vector2(0f, 1f);
                infoPanel.pivot = new Vector2(0f, 1f);
                infoPanel.anchoredPosition = new Vector2(24f, -132f);
                infoPanel.sizeDelta = new Vector2(620f, 152f);
                panelObject.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.05f, 0.78f);
            }

            if (hudText == null)
            {
                var textObject = new GameObject("RoomDesignerHUD.Text", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(infoPanel, false);
                var rect = (RectTransform)textObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(14f, 10f);
                rect.offsetMax = new Vector2(-14f, -10f);
                hudText = textObject.GetComponent<Text>();
                hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                hudText.fontSize = 18;
                hudText.alignment = TextAnchor.UpperLeft;
                hudText.color = Color.white;
                hudText.raycastTarget = false;
            }

            if (controlsPanel == null)
            {
                var panelObject = new GameObject("RoomDesignerControlsPanel", typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(hudCanvas.transform, false);
                controlsPanel = (RectTransform)panelObject.transform;
                controlsPanel.anchorMin = new Vector2(0f, 0f);
                controlsPanel.anchorMax = new Vector2(0f, 0f);
                controlsPanel.pivot = new Vector2(0f, 0f);
                controlsPanel.anchoredPosition = new Vector2(24f, 24f);
                controlsPanel.sizeDelta = new Vector2(860f, 86f);
                panelObject.GetComponent<Image>().color = new Color(0.04f, 0.045f, 0.05f, 0.72f);
            }

            if (controlsText == null)
            {
                var textObject = new GameObject("RoomDesignerControls.Text", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(controlsPanel, false);
                var rect = (RectTransform)textObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(14f, 8f);
                rect.offsetMax = new Vector2(-14f, -8f);
                controlsText = textObject.GetComponent<Text>();
                controlsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                controlsText.fontSize = 17;
                controlsText.alignment = TextAnchor.UpperLeft;
                controlsText.color = Color.white;
                controlsText.raycastTarget = false;
            }

            EnsurePreviewModeButton();
            EnsureCameraModeButton();
            EnsureZoomButtons();
            EnsureToolToolbar();

            if (libraryPanel == null)
            {
                var panelObject = new GameObject("RoomDesignerLibraryPanel", typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(hudCanvas.transform, false);
                libraryPanel = (RectTransform)panelObject.transform;
                libraryPanel.anchorMin = new Vector2(0.5f, 0.5f);
                libraryPanel.anchorMax = new Vector2(0.5f, 0.5f);
                libraryPanel.pivot = new Vector2(0.5f, 0.5f);
                libraryPanel.anchoredPosition = Vector2.zero;
                libraryPanel.sizeDelta = new Vector2(860f, 780f);
                var image = panelObject.GetComponent<Image>();
                image.color = new Color(0.05f, 0.055f, 0.06f, 0.92f);
            }
        }

        private void RefreshLibraryPanel()
        {
            EnsureRoots();
            if (libraryPanel == null)
            {
                return;
            }

            if (Mode == RoomDesignerMode.Editing)
            {
                libraryPanel.gameObject.SetActive(false);
                return;
            }

            libraryPanel.gameObject.SetActive(true);
            CaptureLibraryScrollOffset();
            ClearChildren(libraryPanel);
            libraryListViewport = null;
            libraryListContent = null;
            libraryScrollRect = null;
            LibraryViewModel = BuildLibraryViewModel();

            AddPanelText("LibraryTitle", Mode switch
            {
                RoomDesignerMode.CreateTemplate => "Create Room From Template",
                RoomDesignerMode.ConfirmDelete => "Delete Draft?",
                _ => "Room Designer"
            }, 32, FontStyle.Bold, new Vector2(36f, -34f), new Vector2(780f, 52f));

            AddPanelText("LibrarySubtitle", Mode switch
            {
                RoomDesignerMode.CreateTemplate => "Choose a macro footprint. The footprint is locked when the draft is created.",
                RoomDesignerMode.ConfirmDelete => $"Delete \"{pendingDeleteDisplayName}\"? This only removes the designer draft.",
                _ => "Open a saved draft, create a new room, or delete the selected draft."
            }, 20, FontStyle.Normal, new Vector2(36f, -88f), new Vector2(780f, 58f));

            if (Mode == RoomDesignerMode.CreateTemplate)
            {
                RefreshCreateTemplatePanel();
            }
            else if (Mode == RoomDesignerMode.ConfirmDelete)
            {
                RefreshConfirmDeletePanel();
            }
            else
            {
                RefreshLibraryDraftPanel();
            }

            AddPanelText("LibraryStatus", status, 16, FontStyle.Italic, new Vector2(36f, -690f), new Vector2(780f, 30f));
            AddPanelText(
                "LibraryControls",
                "Keyboard/controller: WASD/Arrows/D-pad select | Mouse wheel scroll | Enter/A open/create | Delete/B delete | Esc/Menu back",
                15,
                FontStyle.Normal,
                new Vector2(36f, -724f),
                new Vector2(780f, 44f));
        }

        private void RefreshLibraryDraftPanel()
        {
            EnsureLibraryListViewport();
            if (libraryListContent == null)
            {
                return;
            }

            ClearChildren(libraryListContent);
            var y = 0f;
            var selectedTop = -1f;
            var selectedBottom = -1f;
            var curatedCount = libraryState?.CuratedDrafts.Count ?? 0;
            var draftCount = libraryState?.Drafts.Count ?? 0;
            if (curatedCount > 0)
            {
                AddPanelText(libraryListContent, "CuratedHeader", "Curated Runtime Rooms - opening creates an editable copy", 16, FontStyle.Bold, new Vector2(0f, y), new Vector2(740f, 26f));
                y -= 32f;
            }

            var currentGroup = string.Empty;
            for (var index = 0; index < curatedCount; index++)
            {
                var draft = libraryState.CuratedDrafts[index];
                var nextGroup = RoomDesignerCatalogGroups.ForProject(draft);
                if (!string.Equals(currentGroup, nextGroup, StringComparison.Ordinal))
                {
                    currentGroup = nextGroup;
                    AddPanelText(libraryListContent, $"CuratedGroup_{index}", currentGroup, 15, FontStyle.Bold, new Vector2(0f, y), new Vector2(740f, 24f));
                    y -= 28f;
                }

                var selected = librarySelectedIndex == index;
                var label = $"{(selected ? "> " : string.Empty)}{draft.displayName}  |  {draft.footprintPreset}  |  {draft.widthTiles}x{draft.heightTiles}m  |  Open Edit Copy";
                var capturedIndex = index;
                if (selected)
                {
                    selectedTop = -y;
                    selectedBottom = selectedTop + LibraryListRowHeight;
                }

                AddPanelButton(libraryListContent, $"CuratedDraft_{index}", label, index, new Vector2(0f, y), new Vector2(744f, LibraryListRowHeight), () =>
                {
                    librarySelectedIndex = capturedIndex;
                    OpenSelectedDraft();
                }, 16);
                y -= LibraryListRowHeight + LibraryListRowGap;
            }

            if (draftCount > 0)
            {
                y -= curatedCount > 0 ? 8f : 0f;
                AddPanelText(libraryListContent, "LocalDraftHeader", "Local Editable Drafts", 16, FontStyle.Bold, new Vector2(0f, y), new Vector2(740f, 26f));
                y -= 32f;
            }

            for (var index = 0; index < draftCount; index++)
            {
                var draft = libraryState.Drafts[index];
                var selectionIndex = curatedCount + index;
                var selected = librarySelectedIndex == selectionIndex;
                var label = $"{(selected ? "> " : string.Empty)}{draft.displayName}  |  {draft.footprintPreset}  |  {draft.widthTiles}x{draft.heightTiles}m";
                var capturedIndex = index;
                if (selected)
                {
                    selectedTop = -y;
                    selectedBottom = selectedTop + LibraryListRowHeight;
                }

                AddPanelButton(libraryListContent, $"Draft_{index}", label, selectionIndex, new Vector2(0f, y), new Vector2(610f, LibraryListRowHeight), () =>
                {
                    librarySelectedIndex = curatedCount + capturedIndex;
                    OpenDraft(draft.projectId);
                }, 16);
                AddPanelButton(libraryListContent, $"Delete_{index}", "Delete", selectionIndex, new Vector2(620f, y), new Vector2(124f, LibraryListRowHeight), () =>
                {
                    librarySelectedIndex = curatedCount + capturedIndex;
                    RequestDeleteDraft();
                }, 16);
                y -= LibraryListRowHeight + LibraryListRowGap;
            }

            var contentHeight = Mathf.Max(LibraryListViewportHeight, -y + 10f);
            libraryListContent.sizeDelta = new Vector2(LibraryListViewportWidth - 18f, contentHeight);
            KeepLibrarySelectionVisible(selectedTop, selectedBottom, contentHeight);

            var createIndex = curatedCount + draftCount;
            AddPanelButton(
                "CreateNewRoom",
                $"{(librarySelectedIndex == createIndex ? "> " : string.Empty)}Create New Room",
                createIndex,
                new Vector2(36f, -568f),
                new Vector2(778f, 50f),
                () =>
                {
                    librarySelectedIndex = 0;
                    ShowCreateTemplates();
                });
            var backIndex = createIndex + 1;
            AddPanelButton(
                "BackToMenu",
                $"{(librarySelectedIndex == backIndex ? "> " : string.Empty)}Back To Main Menu",
                backIndex,
                new Vector2(36f, -628f),
                new Vector2(778f, 50f),
                ReturnToMainMenu);
        }

        private void RefreshCreateTemplatePanel()
        {
            var y = -166f;
            for (var index = 0; index < templatePresets.Length; index++)
            {
                var row = new RoomDesignerTemplateRow(templatePresets[index]);
                var selected = librarySelectedIndex == index;
                var capturedIndex = index;
                AddPanelButton(
                    $"Template_{row.Preset}",
                    $"{(selected ? "> " : string.Empty)}{row.DisplayName}  |  {row.WidthTiles}x{row.HeightTiles}m",
                    index,
                    new Vector2(36f, y),
                    new Vector2(778f, 54f),
                    () =>
                    {
                        librarySelectedIndex = capturedIndex;
                        CreateDraftFromSelectedTemplate();
                    });
                y -= 62f;
            }

            AddPanelButton(
                "TemplateBack",
                $"{(librarySelectedIndex == templatePresets.Length ? "> " : string.Empty)}Back",
                templatePresets.Length,
                new Vector2(36f, y - 18f),
                new Vector2(778f, 54f),
                () =>
                {
                    if (HasLibraryRows())
                    {
                        ShowLibrary();
                    }
                    else
                    {
                        ReturnToMainMenu();
                    }
                });
        }

        private void RefreshConfirmDeletePanel()
        {
            AddPanelButton(
                "ConfirmDelete",
                $"{(librarySelectedIndex == 0 ? "> " : string.Empty)}Delete Draft",
                0,
                new Vector2(36f, -188f),
                new Vector2(778f, 60f),
                ConfirmDeleteDraft);
            AddPanelButton(
                "CancelDelete",
                $"{(librarySelectedIndex == 1 ? "> " : string.Empty)}Cancel",
                1,
                new Vector2(36f, -260f),
                new Vector2(778f, 60f),
                ShowLibrary);
        }

        private void EnsureLibraryListViewport()
        {
            if (libraryPanel == null || libraryListViewport != null && libraryListContent != null)
            {
                return;
            }

            var viewportObject = new GameObject("RoomDesignerLibraryScrollViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewportObject.transform.SetParent(libraryPanel, false);
            libraryListViewport = (RectTransform)viewportObject.transform;
            libraryListViewport.anchorMin = new Vector2(0f, 1f);
            libraryListViewport.anchorMax = new Vector2(0f, 1f);
            libraryListViewport.pivot = new Vector2(0f, 1f);
            libraryListViewport.anchoredPosition = new Vector2(36f, -150f);
            libraryListViewport.sizeDelta = new Vector2(LibraryListViewportWidth, LibraryListViewportHeight);

            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0.02f, 0.025f, 0.032f, 0.34f);

            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(libraryListViewport, false);
            libraryListContent = (RectTransform)contentObject.transform;
            libraryListContent.anchorMin = new Vector2(0f, 1f);
            libraryListContent.anchorMax = new Vector2(0f, 1f);
            libraryListContent.pivot = new Vector2(0f, 1f);
            libraryListContent.anchoredPosition = new Vector2(0f, libraryScrollOffset);
            libraryListContent.sizeDelta = new Vector2(LibraryListViewportWidth - 18f, LibraryListViewportHeight);

            libraryScrollRect = viewportObject.GetComponent<ScrollRect>();
            libraryScrollRect.viewport = libraryListViewport;
            libraryScrollRect.content = libraryListContent;
            libraryScrollRect.horizontal = false;
            libraryScrollRect.vertical = true;
            libraryScrollRect.movementType = ScrollRect.MovementType.Clamped;
            libraryScrollRect.scrollSensitivity = 32f;
            libraryScrollRect.inertia = true;
        }

        private void CaptureLibraryScrollOffset()
        {
            if (libraryListContent != null)
            {
                libraryScrollOffset = Mathf.Max(0f, libraryListContent.anchoredPosition.y);
            }
        }

        private void KeepLibrarySelectionVisible(float selectedTop, float selectedBottom, float contentHeight)
        {
            var maxScroll = Mathf.Max(0f, contentHeight - LibraryListViewportHeight);
            if (selectedTop >= 0f && selectedBottom >= 0f)
            {
                if (selectedTop < libraryScrollOffset)
                {
                    libraryScrollOffset = selectedTop;
                }
                else if (selectedBottom > libraryScrollOffset + LibraryListViewportHeight)
                {
                    libraryScrollOffset = selectedBottom - LibraryListViewportHeight;
                }
            }

            libraryScrollOffset = Mathf.Clamp(libraryScrollOffset, 0f, maxScroll);
            if (libraryListContent != null)
            {
                libraryListContent.anchoredPosition = new Vector2(0f, libraryScrollOffset);
            }
        }

        private RoomDesignerLibraryViewModel BuildLibraryViewModel()
        {
            var drafts = libraryState == null
                ? Array.Empty<RoomDesignerLibraryRow>()
                : libraryState.Drafts
                    .Select(draft => new RoomDesignerLibraryRow(
                        draft.projectId,
                        draft.displayName,
                        draft.footprintPreset,
                        draft.widthTiles,
                        draft.heightTiles))
                    .ToArray();
            var curatedDrafts = libraryState == null
                ? Array.Empty<RoomDesignerLibraryRow>()
                : libraryState.CuratedDrafts
                    .Select(draft => new RoomDesignerLibraryRow(
                        draft.projectId,
                        draft.displayName,
                        draft.footprintPreset,
                        draft.widthTiles,
                        draft.heightTiles))
                    .ToArray();
            var templates = templatePresets.Select(preset => new RoomDesignerTemplateRow(preset)).ToArray();
            return new RoomDesignerLibraryViewModel(
                Mode,
                librarySelectedIndex,
                drafts,
                curatedDrafts,
                templates,
                status,
                pendingDeleteProjectId,
                pendingDeleteDisplayName);
        }

        private Text AddPanelText(string name, string text, int fontSize, FontStyle style, Vector2 position, Vector2 size)
        {
            return AddPanelText(libraryPanel, name, text, fontSize, style, position, size);
        }

        private Text AddPanelText(Transform parent, string name, string text, int fontSize, FontStyle style, Vector2 position, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var textComponent = textObject.GetComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.color = Color.white;
            textComponent.text = text;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private Button AddPanelButton(string name, string label, int selectionIndex, Vector2 position, Vector2 size, Action onClick)
        {
            return AddPanelButton(libraryPanel, name, label, selectionIndex, position, size, onClick);
        }

        private Button AddPanelButton(Transform parent, string name, string label, int selectionIndex, Vector2 position, Vector2 size, Action onClick, int labelFontSize = 20)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = librarySelectedIndex == selectionIndex
                ? new Color(0.22f, 0.34f, 0.42f, 0.95f)
                : new Color(0.13f, 0.14f, 0.16f, 0.92f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                librarySelectedIndex = selectionIndex;
                onClick?.Invoke();
            });

            AddButtonLabel(buttonObject.transform, label, labelFontSize);
            return button;
        }

        private void AddButtonLabel(Transform parent, string label, int fontSize = 20)
        {
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 0f);
            rect.offsetMax = new Vector2(-18f, 0f);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
        }

        public void EnsureToolToolbar()
        {
            if (hudCanvas == null || toolToolbarPanel != null)
            {
                return;
            }

            var panelObject = new GameObject("RoomDesignerToolToolbar", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(hudCanvas.transform, false);
            toolToolbarPanel = (RectTransform)panelObject.transform;
            toolToolbarPanel.anchorMin = new Vector2(0.5f, 1f);
            toolToolbarPanel.anchorMax = new Vector2(0.5f, 1f);
            toolToolbarPanel.pivot = new Vector2(0.5f, 1f);
            toolToolbarPanel.anchoredPosition = new Vector2(0f, -18f);
            toolToolbarPanel.sizeDelta = new Vector2(1440f, 94f);
            panelObject.GetComponent<Image>().color = new Color(0.035f, 0.04f, 0.045f, 0.8f);
            toolToolbarPanel.gameObject.SetActive(false);
        }

        public void RefreshToolToolbar()
        {
            EnsureRoots();
            if (toolToolbarPanel == null)
            {
                return;
            }

            if (Mode != RoomDesignerMode.Editing)
            {
                toolToolbarPanel.gameObject.SetActive(false);
                return;
            }

            toolToolbarPanel.gameObject.SetActive(true);
            ClearChildren(toolToolbarPanel);

            const float tileWidth = 70f;
            const float tileHeight = 72f;
            const float gap = 7f;
            var totalWidth = tools.Length * tileWidth + (tools.Length - 1) * gap;
            var startX = -totalWidth * 0.5f + tileWidth * 0.5f;

            for (var index = 0; index < tools.Length; index++)
            {
                var tool = tools[index];
                var selected = index == toolIndex;
                var capturedTool = tool;
                var tileObject = new GameObject($"ToolTile_{tool}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                tileObject.transform.SetParent(toolToolbarPanel, false);
                var rect = (RectTransform)tileObject.transform;
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(tileWidth, tileHeight);
                rect.anchoredPosition = new Vector2(startX + index * (tileWidth + gap), -11f);

                var image = tileObject.GetComponent<Image>();
                image.color = selected
                    ? new Color(0.16f, 0.26f, 0.19f, 0.98f)
                    : new Color(0.1f, 0.11f, 0.13f, 0.92f);

                var outline = tileObject.GetComponent<Outline>();
                outline.effectColor = selected ? new Color(0.15f, 1f, 0.48f, 1f) : new Color(0.38f, 0.42f, 0.46f, 0.55f);
                outline.effectDistance = selected ? new Vector2(3f, -3f) : new Vector2(1.5f, -1.5f);

                var button = tileObject.GetComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => SelectTool(capturedTool));

                AddToolbarText(tileObject.transform, "Icon", RoomDesignerDisplayNames.ForToolIcon(tool), 19, FontStyle.Bold, new Vector2(0f, -7f), new Vector2(tileWidth, 32f), TextAnchor.MiddleCenter, selected);
                AddToolbarText(tileObject.transform, "Name", RoomDesignerDisplayNames.ForTool(tool), selected ? 12 : 11, FontStyle.Normal, new Vector2(0f, -41f), new Vector2(tileWidth, 24f), TextAnchor.MiddleCenter, selected);
            }
        }

        private void AddToolbarText(Transform parent, string name, string value, int fontSize, FontStyle style, Vector2 position, Vector2 size, TextAnchor alignment, bool selected)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = selected ? new Color(0.72f, 1f, 0.78f, 1f) : Color.white;
            text.text = value;
            text.raycastTarget = false;
        }

        private void RefreshHud()
        {
            if (hudText == null)
            {
                return;
            }

            if (Mode != RoomDesignerMode.Editing || currentProject == null)
            {
                if (infoPanel != null)
                {
                    infoPanel.gameObject.SetActive(false);
                }

                if (controlsPanel != null)
                {
                    controlsPanel.gameObject.SetActive(false);
                }

                RefreshToolToolbar();
                RefreshPreviewModeButton();
                RefreshCameraModeButton();
                RefreshZoomButtons();
                return;
            }

            if (infoPanel != null)
            {
                infoPanel.gameObject.SetActive(true);
            }

            if (controlsPanel != null)
            {
                controlsPanel.gameObject.SetActive(true);
            }

            hudText.gameObject.SetActive(true);
            LastValidationReport = RoomDesignerDraftValidator.Validate(currentProject);
            var dimensions = RoomDesignerFootprintUtility.Dimensions(currentProject.footprintPreset);
            var selectedDoor = SelectedDoorSummary();
            var enabledPorts = currentProject.doorPorts.Count(door => door.state != RoomDesignerDoorKinds.Inactive);
            var draftCount = libraryState?.Drafts.Count ?? 0;
            hudText.text =
                $"Room Designer - Macro Authoring\n" +
                $"Drafts: {draftCount} | {currentProject.footprintPreset} | {dimensions.x}x{dimensions.y}m | Ports {enabledPorts}/{currentProject.doorPorts.Count}\n" +
                $"Cursor ({CursorX}, {CursorLayer}, {CursorZ}) | Door {selectedDoor} | Labels {(LabelsVisible ? "Important" : "Off")}\n" +
                $"Validation: {LastValidationReport.Summary()} | E:{LastValidationReport.Errors.Count} W:{LastValidationReport.Warnings.Count}";
            if (controlsText != null)
            {
                controlsText.text =
                    "WASD/Arrows move | Q/E tools | Z/X layer | Space place | Delete erase | F pick | Tab labels | V preview | C camera | -/+ zoom\n" +
                    "P playtest | J export bundle | U export USDA | Esc library\n" +
                    DesignerStatusText();
            }

            RefreshToolToolbar();
            RefreshPreviewModeButton();
            RefreshCameraModeButton();
            RefreshZoomButtons();
        }

        private string DesignerStatusText()
        {
            if (PreviewMode == RoomDesignerPreviewMode.Scene && scenePreviewMissingBindings > 0)
            {
                return $"{status} | Scene preview fallback: {scenePreviewMissingBindings} missing ArtPass binding(s)";
            }

            return status;
        }

        private void EnsurePreviewModeButton()
        {
            if (hudCanvas == null || previewModeButtonPanel != null)
            {
                return;
            }

            var buttonObject = new GameObject("RoomDesignerPreviewModeButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(hudCanvas.transform, false);
            previewModeButtonPanel = (RectTransform)buttonObject.transform;
            previewModeButtonPanel.anchorMin = new Vector2(1f, 1f);
            previewModeButtonPanel.anchorMax = new Vector2(1f, 1f);
            previewModeButtonPanel.pivot = new Vector2(1f, 1f);
            previewModeButtonPanel.anchoredPosition = new Vector2(-24f, -132f);
            previewModeButtonPanel.sizeDelta = new Vector2(220f, 48f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.08f, 0.86f);
            var outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.72f, 1f, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(TogglePreviewMode);

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(previewModeButtonPanel, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
            previewModeButtonText = textObject.GetComponent<Text>();
            previewModeButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            previewModeButtonText.fontSize = 18;
            previewModeButtonText.alignment = TextAnchor.MiddleCenter;
            previewModeButtonText.color = Color.white;
            previewModeButtonText.raycastTarget = false;
            previewModeButtonPanel.gameObject.SetActive(false);
        }

        private void EnsureCameraModeButton()
        {
            if (hudCanvas == null || cameraModeButtonPanel != null)
            {
                return;
            }

            var buttonObject = new GameObject("RoomDesignerCameraModeButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(hudCanvas.transform, false);
            cameraModeButtonPanel = (RectTransform)buttonObject.transform;
            cameraModeButtonPanel.anchorMin = new Vector2(1f, 1f);
            cameraModeButtonPanel.anchorMax = new Vector2(1f, 1f);
            cameraModeButtonPanel.pivot = new Vector2(1f, 1f);
            cameraModeButtonPanel.anchoredPosition = new Vector2(-252f, -132f);
            cameraModeButtonPanel.sizeDelta = new Vector2(220f, 48f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.08f, 0.86f);
            var outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.15f, 1f, 0.48f, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleCameraViewMode);

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(cameraModeButtonPanel, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
            cameraModeButtonText = textObject.GetComponent<Text>();
            cameraModeButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cameraModeButtonText.fontSize = 18;
            cameraModeButtonText.alignment = TextAnchor.MiddleCenter;
            cameraModeButtonText.color = Color.white;
            cameraModeButtonText.raycastTarget = false;
            cameraModeButtonPanel.gameObject.SetActive(false);
        }

        private void EnsureZoomButtons()
        {
            if (hudCanvas == null || zoomOutButtonPanel != null && zoomInButtonPanel != null)
            {
                return;
            }

            zoomOutButtonPanel ??= CreateZoomButton("RoomDesignerZoomOutButton", "Zoom -", new Vector2(-594f, -132f), ZoomOut);
            zoomInButtonPanel ??= CreateZoomButton("RoomDesignerZoomInButton", "Zoom +", new Vector2(-480f, -132f), ZoomIn);
        }

        private RectTransform CreateZoomButton(string name, string label, Vector2 position, Action onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(hudCanvas.transform, false);
            var rectTransform = (RectTransform)buttonObject.transform;
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(106f, 48f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.08f, 0.86f);
            var outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.78f, 0.82f, 0.9f, 0.65f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            var textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(rectTransform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 17;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
            rectTransform.gameObject.SetActive(false);
            return rectTransform;
        }

        private void RefreshPreviewModeButton()
        {
            if (previewModeButtonPanel == null)
            {
                return;
            }

            var visible = Mode == RoomDesignerMode.Editing;
            previewModeButtonPanel.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var image = previewModeButtonPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = PreviewMode == RoomDesignerPreviewMode.Scene
                    ? new Color(0.08f, 0.16f, 0.11f, 0.9f)
                    : new Color(0.06f, 0.07f, 0.08f, 0.86f);
            }

            if (previewModeButtonText != null)
            {
                previewModeButtonText.text = $"Preview: {PreviewMode} (V)";
            }
        }

        private void RefreshCameraModeButton()
        {
            if (cameraModeButtonPanel == null)
            {
                return;
            }

            var visible = Mode == RoomDesignerMode.Editing;
            cameraModeButtonPanel.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var image = cameraModeButtonPanel.GetComponent<Image>();
            if (image != null)
            {
                image.color = CameraViewMode == RoomDesignerCameraViewMode.TopDown
                    ? new Color(0.08f, 0.16f, 0.11f, 0.9f)
                    : new Color(0.06f, 0.07f, 0.08f, 0.86f);
            }

            if (cameraModeButtonText != null)
            {
                cameraModeButtonText.text = CameraViewMode == RoomDesignerCameraViewMode.TopDown
                    ? "Camera: Top (C)"
                    : "Camera: Perspective (C)";
            }
        }

        private void RefreshZoomButtons()
        {
            var visible = Mode == RoomDesignerMode.Editing;
            if (zoomOutButtonPanel != null)
            {
                zoomOutButtonPanel.gameObject.SetActive(visible);
            }

            if (zoomInButtonPanel != null)
            {
                zoomInButtonPanel.gameObject.SetActive(visible);
            }
        }

        private string SelectedDoorSummary()
        {
            var nearest = currentProject.doorPorts
                .OrderBy(door => Vector2.Distance(new Vector2(door.x, door.z), new Vector2(CursorX, CursorZ)))
                .FirstOrDefault();
            return nearest == null
                ? "none"
                : RoomDesignerDisplayNames.ForDoor(nearest);
        }

        private int LibraryOptionCount()
        {
            return Mode switch
            {
                RoomDesignerMode.Library => (libraryState?.CuratedDrafts.Count ?? 0) + (libraryState?.Drafts.Count ?? 0) + 2,
                RoomDesignerMode.CreateTemplate => templatePresets.Length + 1,
                RoomDesignerMode.ConfirmDelete => 2,
                _ => 0
            };
        }

        private bool HasLibraryRows()
        {
            return libraryState != null && (libraryState.CuratedDrafts.Count > 0 || libraryState.Drafts.Count > 0);
        }

        private int DraftIndexFromSelection()
        {
            var curatedCount = libraryState?.CuratedDrafts.Count ?? 0;
            var draftCount = libraryState?.Drafts.Count ?? 0;
            var localIndex = librarySelectedIndex - curatedCount;
            return localIndex >= 0 && localIndex < draftCount
                ? localIndex
                : -1;
        }

        private int CuratedIndexFromSelection()
        {
            var curatedCount = libraryState?.CuratedDrafts.Count ?? 0;
            return librarySelectedIndex >= 0 && librarySelectedIndex < curatedCount
                ? librarySelectedIndex
                : -1;
        }

        private void ClearPreview()
        {
            EnsureRoots();
            lightingController.Apply(RoomDesignerPreviewMode.Graybox);
            if (previewRoot != null)
            {
                ClearChildren(previewRoot);
            }
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
