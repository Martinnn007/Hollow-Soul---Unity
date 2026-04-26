using System;
using System.Linq;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Persistence;
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
        private ProfileSlotId slotId;
        private RoomDesignerProject currentProject;
        private Text hudText;
        private float nextMoveTime;
        private int toolIndex;
        private string status = "Ready";

        public RoomDesignerProject CurrentProject => currentProject;

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
            currentProject = store.LoadDrafts(slotId).First();
            EnsureRoots();
            RebuildPreview();
            RefreshHud();
        }

        public void InitializeForTest(RoomDesignerStore nextStore, ProfileSlotId nextSlotId, RoomDesignerProject nextProject)
        {
            store = nextStore;
            slotId = nextSlotId;
            currentProject = nextProject;
            EnsureRoots();
            RebuildPreview();
            RefreshHud();
        }

        private void Update()
        {
            ApplyInput(RoomDesignerInputReader.ReadCurrent(), Time.time);
        }

        public void ApplyInput(RoomDesignerInputSnapshot input, float timeSeconds = 999f)
        {
            var changed = false;
            if ((input.MoveX != 0 || input.MoveZ != 0) && timeSeconds >= nextMoveTime)
            {
                CursorX = Mathf.Clamp(CursorX + input.MoveX, -6, 6);
                CursorZ = Mathf.Clamp(CursorZ + input.MoveZ, -3, 3);
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
                SaveDraft("Erased cell/entity");
                changed = true;
            }
            else if (input.PlacePressed)
            {
                PlaceCurrentTool();
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

        public void PlaytestCurrentDraft()
        {
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
            var projectPath = RoomDesignerJsonExporter.ExportProject(currentProject);
            var runtimePath = RoomDesignerJsonExporter.ExportRuntime(currentProject);
            status = $"Exported JSON: {runtimePath}";
            RefreshHud();
            return projectPath;
        }

        public string ExportCurrentUsda()
        {
            var path = RoomDesignerUsdaExporter.ExportScene(currentProject);
            status = $"Exported USDA: {path}";
            RefreshHud();
            return path;
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
                case RoomDesignerTool.ActiveDoor:
                    SetNearestDoor(RoomDesignerDoorKinds.Door);
                    break;
                case RoomDesignerTool.SecretDoor:
                    SetNearestDoor(RoomDesignerDoorKinds.Secret);
                    break;
            }
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
            if (marker?.kind == RoomDesignerMarkerKinds.Enemy)
            {
                toolIndex = Array.IndexOf(tools, RoomDesignerTool.EnemySpawn);
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
            ApplyColor(cursor, new Color(1f, 0.9f, 0.15f, 1f));
        }

        private void BuildGrid()
        {
            for (var x = -6.5f; x <= 6.51f; x += 1f)
            {
                BuildCube($"grid_x_{x}", new Vector3(x, 0.02f, 0f), new Vector3(0.02f, 0.02f, 7f), new Color(0.85f, 0.9f, 1f, 0.65f));
            }

            for (var z = -3.5f; z <= 3.51f; z += 1f)
            {
                BuildCube($"grid_z_{z}", new Vector3(0f, 0.025f, z), new Vector3(13f, 0.02f, 0.02f), new Color(0.85f, 0.9f, 1f, 0.65f));
            }
        }

        private void BuildCell(RoomDesignerCell cell)
        {
            if (cell.kind == RoomDesignerCellKinds.Ground)
            {
                BuildCube($"tileGround_{cell.x}_{cell.z}", new Vector3(cell.x, -0.5f, cell.z), Vector3.one, new Color(0.23f, 0.32f, 0.38f, 1f));
            }
            else if (cell.kind == RoomDesignerCellKinds.Hole)
            {
                BuildCube($"tileHole_{cell.x}_{cell.z}", new Vector3(cell.x, 0.03f, cell.z), new Vector3(0.86f, 0.06f, 0.86f), Color.black);
            }
            else if (cell.kind == RoomDesignerCellKinds.Rock)
            {
                BuildCube($"rockTile_{cell.x}_{cell.z}_{cell.layer}", new Vector3(cell.x, cell.layer + 0.5f, cell.z), Vector3.one, new Color(0.42f, 0.39f, 0.34f, 1f));
            }

            if (LabelsVisible)
            {
                BuildLabel(cell.kind, new Vector3(cell.x, cell.layer + 1.08f, cell.z));
            }
        }

        private void BuildDoor(RoomDesignerDoorPortState door)
        {
            var color = door.state switch
            {
                RoomDesignerDoorKinds.Door => new Color(0.1f, 0.55f, 1f, 1f),
                RoomDesignerDoorKinds.Secret => new Color(0.9f, 0.25f, 1f, 1f),
                _ => new Color(0.6f, 0.68f, 0.78f, 0.8f)
            };
            BuildCube($"doorAnchor_{door.direction}_{door.state}", new Vector3(door.x, 0.65f, door.z), door.direction is "east" or "west" ? new Vector3(0.18f, 1.3f, 1f) : new Vector3(1f, 1.3f, 0.18f), color);
            if (LabelsVisible)
            {
                BuildLabel($"doorAnchor{door.state}", new Vector3(door.x, 1.5f, door.z));
            }
        }

        private void BuildMarker(RoomDesignerMarker marker)
        {
            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerObject.name = $"{marker.kind}_{marker.id}";
            markerObject.transform.SetParent(previewRoot, false);
            markerObject.transform.localPosition = new Vector3(marker.x, marker.y + 0.2f, marker.z);
            markerObject.transform.localScale = Vector3.one * 0.36f;
            ApplyColor(markerObject, marker.kind == RoomDesignerMarkerKinds.SafeStart ? new Color(0.32f, 1f, 0.56f, 1f) : marker.kind == RoomDesignerMarkerKinds.Enemy ? new Color(1f, 0.2f, 0.18f, 1f) : new Color(1f, 0.82f, 0.18f, 1f));
            if (LabelsVisible)
            {
                BuildLabel(marker.kind, new Vector3(marker.x, marker.y + 0.72f, marker.z));
            }
        }

        private GameObject BuildCube(string name, Vector3 position, Vector3 scale, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(previewRoot, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            ApplyColor(cube, color);
            return cube;
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

            hudText.text =
                $"Room Designer\nTool: {CurrentTool} | Cursor: ({CursorX}, {CursorLayer}, {CursorZ}) | Labels: {(LabelsVisible ? "On" : "Off")}\nWASD/Arrows move | Q/E tool | Z/X layer | Space place | Delete erase\nP playtest | J export JSON | U export USDA | Esc menu\n{status}";
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

        private static void ApplyColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    color = color
                };
            }
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
