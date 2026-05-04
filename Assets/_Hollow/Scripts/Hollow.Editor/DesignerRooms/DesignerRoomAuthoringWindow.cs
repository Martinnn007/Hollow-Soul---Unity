using System;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.DesignerRooms
{
    public sealed class DesignerRoomAuthoringWindow : EditorWindow
    {
        private int selectedPanel;
        private DesignerRoomSceneMarkerKind paletteKind = DesignerRoomSceneMarkerKind.EnemySpawn;
        private string paletteRuntimeKind = RoomDesignerMarkerKinds.EnemyNormal;
        private bool placementArmed;
        private bool showGrid = true;
        private bool showWalkability;
        private bool showSelectedEnemyRange = true;
        private bool previewLightingEnabled = true;
        private bool previewCameraEnabled = true;
        private Vector2 scrollPosition;
        private DesignerRoomSceneValidationResult lastValidation;
        private string lastExportPath = string.Empty;
        private string lastDiff = string.Empty;

        [MenuItem("Hollow/Designer Rooms/Room Authoring")]
        public static void Open()
        {
            GetWindow<DesignerRoomAuthoringWindow>("Room Authoring");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var panels = DesignerRoomAuthoringLocalization.PanelLabels;
                selectedPanel = Mathf.Clamp(selectedPanel, 0, panels.Length - 1);
                selectedPanel = GUILayout.Toolbar(selectedPanel, panels, EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                showGrid = GUILayout.Toggle(showGrid, T("Grid", "Siatka"), EditorStyles.toolbarButton);
                showWalkability = GUILayout.Toggle(showWalkability, T("Walkability", "Przejścia"), EditorStyles.toolbarButton);
                showSelectedEnemyRange = GUILayout.Toggle(showSelectedEnemyRange, T("Enemy Range", "Zasięg wroga"), EditorStyles.toolbarButton);
                var language = GUILayout.Toolbar(
                    (int)DesignerRoomAuthoringLocalization.CurrentLanguage,
                    new[] { "EN", "PL" },
                    EditorStyles.toolbarButton,
                    GUILayout.Width(64f));
                if (language != (int)DesignerRoomAuthoringLocalization.CurrentLanguage)
                {
                    DesignerRoomAuthoringLocalization.CurrentLanguage = (DesignerRoomAuthoringLanguage)language;
                    SceneView.RepaintAll();
                    Repaint();
                }
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            switch (selectedPanel)
            {
                case 0:
                    DrawPalettePanel();
                    break;
                case 1:
                    DrawSelectionPanel();
                    break;
                case 2:
                    DrawValidationPanel();
                    break;
                case 3:
                    DrawExportPanel();
                    break;
                case 4:
                    DrawPreviewPanel();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPalettePanel()
        {
            EditorGUILayout.LabelField(T("Palette", "Paleta"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(T(
                "Pick a marker type, then arm placement and click in the Scene View. New objects are selectable 3D scene markers and export to manual runtime JSON drafts.",
                "Wybierz typ znacznika, uzbrój dodawanie i kliknij w Scene View. Nowe obiekty są wybieralnymi znacznikami 3D i eksportują się do roboczych plików runtime JSON."), MessageType.Info);

            var nextKind = DrawMarkerKindPopup(T("Marker Type", "Typ znacznika"), paletteKind);
            if (nextKind != paletteKind)
            {
                paletteKind = nextKind;
                paletteRuntimeKind = DesignerRoomSceneAuthoringUtility.DefaultRuntimeKind(paletteKind);
            }

            DrawRuntimeKindPopup(T("Runtime Kind", "Typ runtime"), paletteKind, ref paletteRuntimeKind);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(placementArmed ? T("Click Scene To Place", "Kliknij scenę, aby dodać") : T("Arm Placement", "Uzbrój dodawanie"), GUILayout.Height(28f)))
                {
                    placementArmed = true;
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button(T("Cancel", "Anuluj"), GUILayout.Height(28f)))
                {
                    placementArmed = false;
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(T("Quick Enemies", "Szybkie wstawianie wrogów"), EditorStyles.boldLabel);
            var enemyKinds = RoomDesignerMarkerKinds.EnemyKinds
                .Where(kind => kind != RoomDesignerMarkerKinds.Enemy)
                .ToArray();
            for (var index = 0; index < enemyKinds.Length; index++)
            {
                if (GUILayout.Button(DesignerRoomAuthoringLocalization.DisplayNameForRuntimeKind(enemyKinds[index])))
                {
                    paletteKind = DesignerRoomSceneMarkerKind.EnemySpawn;
                    paletteRuntimeKind = enemyKinds[index];
                    placementArmed = true;
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(T("Room Editing Helpers", "Pomoc przy edycji pokoju"), EditorStyles.boldLabel);
            if (GUILayout.Button(T("Top-Down Fit Active Room", "Dopasuj widok z góry")))
            {
                FitActiveRoom();
            }

            if (GUILayout.Button(T("Snap Selected", "Przyciągnij zaznaczone")))
            {
                SnapSelected();
            }

            if (GUILayout.Button(T("Snap All In Scene", "Przyciągnij wszystko w scenie")))
            {
                DesignerRoomSceneAuthoringUtility.SnapAllInScene(SceneManager.GetActiveScene());
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                SceneView.RepaintAll();
            }
        }

        private void DrawSelectionPanel()
        {
            EditorGUILayout.LabelField(T("Selection", "Zaznaczenie"), EditorStyles.boldLabel);
            var marker = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<DesignerRoomSceneMarker>()
                : null;
            if (marker == null)
            {
                EditorGUILayout.HelpBox(T(
                    "Select a DesignerRoomSceneMarker in the Hierarchy or Scene View.",
                    "Zaznacz DesignerRoomSceneMarker w Hierarchy albo Scene View."), MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            var markerId = EditorGUILayout.TextField(T("Marker Id", "Id znacznika"), marker.MarkerId);
            var markerKind = DrawMarkerKindPopup(T("Marker Kind", "Typ znacznika"), marker.MarkerKind);
            var runtimeKind = marker.RuntimeKind;
            DrawRuntimeKindPopup(T("Runtime Kind", "Typ runtime"), markerKind, ref runtimeKind);
            var displayName = EditorGUILayout.TextField(T("Display Name Override", "Własna nazwa wyświetlana"), marker.DisplayName);
            var showLabel = EditorGUILayout.Toggle(T("Show Scene Label", "Pokaż etykietę w scenie"), marker.ShowLabel);
            var editable = EditorGUILayout.Toggle(T("Editable By Designer", "Edytowalne dla designera"), marker.EditableByDesigner);
            var locked = EditorGUILayout.Toggle(T("Lock Layer", "Zablokuj warstwe"), marker.LockedLayer);
            var previewRadius = EditorGUILayout.FloatField(T("Preview Radius", "Promien podgladu"), marker.PreviewRadiusMeters);
            var notes = EditorGUILayout.TextField(T("Notes", "Notatki"), marker.Notes);

            var doorDirection = marker.DoorDirection;
            var doorLane = marker.DoorLaneIndex;
            var doorState = marker.DoorState;
            var hostX = marker.HostCellX;
            var hostZ = marker.HostCellZ;
            if (markerKind == DesignerRoomSceneMarkerKind.DoorPort)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(T("Door", "Drzwi"), EditorStyles.boldLabel);
                doorDirection = DrawStringPopup(T("Direction", "Kierunek"), doorDirection, new[] { "north", "south", "east", "west" });
                doorState = DrawStringPopup(T("State", "Stan"), doorState, DesignerRoomSceneAuthoringUtility.RuntimeKindsFor(DesignerRoomSceneMarkerKind.DoorPort));
                doorLane = Mathf.Max(0, EditorGUILayout.IntField(T("Lane Index", "Numer wejscia"), doorLane));
                hostX = EditorGUILayout.IntField(T("Host Cell X", "Komórka hosta X"), hostX);
                hostZ = EditorGUILayout.IntField(T("Host Cell Z", "Komórka hosta Z"), hostZ);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(marker, "Edit Designer Room Marker");
                marker.ConfigureAuthoring(
                    markerId,
                    markerKind,
                    runtimeKind,
                    marker.SourceRoomId,
                    marker.SourceRuntimePath,
                    notes,
                    editable,
                    displayName,
                    showLabel,
                    locked,
                    previewRadius,
                    doorDirection,
                    doorLane,
                    hostX,
                    hostZ,
                    doorState);
                EditorUtility.SetDirty(marker);
                EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
                SceneView.RepaintAll();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(T("Snap Selected", "Przyciągnij zaznaczone")))
                {
                    DesignerRoomSceneAuthoringUtility.SnapMarker(marker);
                    EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
                }

                if (GUILayout.Button(T("Frame", "Pokaż w kadrze")))
                {
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
            }

            DrawEnemyPreview(marker);
            DrawSourcePreview(marker);
        }

        private void DrawValidationPanel()
        {
            EditorGUILayout.LabelField(T("Validation", "Walidacja"), EditorStyles.boldLabel);
            if (GUILayout.Button(T("Validate Active DesignerRoom Scene", "Sprawdź aktywną scenę DesignerRoom"), GUILayout.Height(28f)))
            {
                lastValidation = DesignerRoomSceneAuthoringUtility.ValidateScene(SceneManager.GetActiveScene());
            }

            if (GUILayout.Button(T("Batch QA All DesignerRooms", "QA wszystkich DesignerRooms")))
            {
                BatchValidateAllDesignerRooms();
            }

            if (lastValidation == null)
            {
                EditorGUILayout.HelpBox(T("No validation has been run yet.", "Walidacja nie byla jeszcze uruchomiona."), MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(lastValidation.Summary(), lastValidation.IsValid ? MessageType.Info : MessageType.Error);
            foreach (var error in lastValidation.Errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            foreach (var warning in lastValidation.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        private void DrawExportPanel()
        {
            EditorGUILayout.LabelField(T("Export", "Eksport"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(T(
                $"Exports write new drafts under {DesignerRoomSceneAuthoringUtility.ManualExportDirectory}. Approved source JSON is never overwritten.",
                $"Eksport zapisuje nowe wersje robocze w {DesignerRoomSceneAuthoringUtility.ManualExportDirectory}. Zatwierdzone źródłowe JSON-y nigdy nie są nadpisywane."), MessageType.Info);

            if (GUILayout.Button(T("Export Active DesignerRoom Scene", "Eksportuj aktywną scenę DesignerRoom"), GUILayout.Height(28f)))
            {
                ExportActiveScene();
            }

            if (GUILayout.Button(T("Export All DesignerRooms", "Eksportuj wszystkie DesignerRooms")))
            {
                ExportAllDesignerRoomScenes();
            }

            if (!string.IsNullOrWhiteSpace(lastExportPath))
            {
                EditorGUILayout.SelectableLabel(lastExportPath, GUILayout.Height(20f));
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(T("Diff Against Source", "Porównaj ze źródłem")))
            {
                lastDiff = DesignerRoomSceneAuthoringUtility.DiffAgainstSource(SceneManager.GetActiveScene());
            }

            if (!string.IsNullOrWhiteSpace(lastDiff))
            {
                EditorGUILayout.TextArea(lastDiff, GUILayout.MinHeight(120f));
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(T("Refresh Scene From Source JSON", "Odśwież scenę ze źródłowego JSON")))
            {
                RefreshActiveSceneFromSource();
            }
        }

        private void DrawPreviewPanel()
        {
            EditorGUILayout.LabelField(T("Visual Preview", "Podgląd wizualny"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(T(
                "Creates a temporary non-exported runtime-style preview using art-pass prefabs or fallback materials. Markers remain the only source of exported room data.",
                "Tworzy tymczasowy, nieeksportowany podgląd w stylu runtime, używając prefabów art-pass albo materiałów fallback. Znaczniki pozostają jedynym źródłem danych eksportu."), MessageType.Info);

            previewLightingEnabled = EditorGUILayout.Toggle(T("Preview Lighting", "Oświetlenie podglądu"), previewLightingEnabled);
            previewCameraEnabled = EditorGUILayout.Toggle(T("Preview Camera", "Kamera podglądu"), previewCameraEnabled);

            var scene = SceneManager.GetActiveScene();
            var hasPreview = DesignerRoomSceneVisualPreviewBuilder.HasPreview(scene);
            var toggleLabel = hasPreview ? T("Visual Preview: ON", "Podgląd wizualny: WŁ.") : T("Visual Preview: OFF", "Podgląd wizualny: WYŁ.");
            if (GUILayout.Button(toggleLabel, GUILayout.Height(32f)))
            {
                if (hasPreview)
                {
                    DesignerRoomSceneVisualPreviewBuilder.ClearPreview(scene);
                }
                else
                {
                    BuildVisualPreview(scene);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!hasPreview))
                {
                    if (GUILayout.Button(T("Refresh Preview", "Odśwież podgląd")))
                    {
                        BuildVisualPreview(scene);
                    }
                }

                using (new EditorGUI.DisabledScope(!hasPreview))
                {
                    if (GUILayout.Button(T("Clear Preview", "Wyczyść podgląd")))
                    {
                        DesignerRoomSceneVisualPreviewBuilder.ClearPreview(scene);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene View", EditorStyles.boldLabel);
            if (GUILayout.Button(T("Top-Down Fit Active Room", "Dopasuj widok z góry")))
            {
                FitActiveRoom();
            }

            EditorGUILayout.HelpBox(T(
                "Use the Scene View shading dropdown for Lit/Textured mode. The preview lights are scene lights, so they work in Lit mode without entering Play Mode.",
                "Użyj menu cieniowania w Scene View i wybierz tryb Lit/Textured. Lampy podglądu są światłami sceny, więc działają bez włączania Play Mode."), MessageType.None);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            var scene = SceneManager.GetActiveScene();
            if (showGrid)
            {
                DrawGrid(scene);
            }

            if (showWalkability)
            {
                DrawWalkability(scene);
            }

            DrawLabels(scene);
            DrawSelectedEnemyRange();
            HandlePalettePlacement(sceneView);
        }

        private void DrawRuntimeKindPopup(string label, DesignerRoomSceneMarkerKind markerKind, ref string runtimeKind)
        {
            var options = DesignerRoomSceneAuthoringUtility.RuntimeKindsFor(markerKind);
            if (options.Length == 0)
            {
                EditorGUILayout.TextField(label, runtimeKind);
                return;
            }

            if (string.IsNullOrWhiteSpace(runtimeKind) || Array.IndexOf(options, runtimeKind) < 0)
            {
                runtimeKind = options[0];
            }

            var labels = options.Select(DesignerRoomAuthoringLocalization.DisplayNameForRuntimeKind).ToArray();
            var selected = Mathf.Max(0, Array.IndexOf(options, runtimeKind));
            runtimeKind = options[EditorGUILayout.Popup(label, selected, labels)];
        }

        private static DesignerRoomSceneMarkerKind DrawMarkerKindPopup(string label, DesignerRoomSceneMarkerKind value)
        {
            var values = (DesignerRoomSceneMarkerKind[])Enum.GetValues(typeof(DesignerRoomSceneMarkerKind));
            var selected = Mathf.Max(0, Array.IndexOf(values, value));
            var labels = values.Select(DesignerRoomAuthoringLocalization.MarkerKindLabel).ToArray();
            return values[EditorGUILayout.Popup(label, selected, labels)];
        }

        private static string DrawStringPopup(string label, string value, string[] options)
        {
            var selected = Mathf.Max(0, Array.IndexOf(options, value));
            var labels = options.Select(DesignerRoomAuthoringLocalization.OptionLabel).ToArray();
            return options[EditorGUILayout.Popup(label, selected, labels)];
        }

        private static void DrawEnemyPreview(DesignerRoomSceneMarker marker)
        {
            if (marker.MarkerKind != DesignerRoomSceneMarkerKind.EnemySpawn)
            {
                return;
            }

            var enemy = EnemyCatalog.CreateRuntimeDefault().Resolve(marker.RuntimeKind);
            if (enemy == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(T("Enemy Preview", "Podgląd wroga"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(T("Display", "Nazwa"), enemy.DisplayName);
            EditorGUILayout.LabelField("HP", enemy.MaxHealth.ToString());
            EditorGUILayout.LabelField(T("Intelligence", "Inteligencja"), enemy.Intelligence.DisplayLabel());
            EditorGUILayout.LabelField(T("Disposition", "Nastawienie"), enemy.Disposition.ToSaveString());
            EditorGUILayout.LabelField(T("Senses", "Zmysly"), T(
                $"Sight {enemy.SightRadiusMeters:0.#}m / {enemy.SightAngleDegrees:0} deg, Hearing {enemy.HearingRadiusMeters:0.#}m",
                $"Wzrok {enemy.SightRadiusMeters:0.#}m / {enemy.SightAngleDegrees:0} st., słuch {enemy.HearingRadiusMeters:0.#}m"));
            EditorGUILayout.LabelField(T("Spacing", "Dystans"), $"{enemy.PreferredRangeMinMeters:0.##}-{enemy.PreferredRangeMaxMeters:0.##}m");
            var attacks = enemy.AttackProfiles != null && enemy.AttackProfiles.Count > 0
                ? string.Join(", ", enemy.AttackProfiles.Take(4).Select(profile => profile.DisplayName))
                : T("No profile", "Brak profilu");
            EditorGUILayout.LabelField(T("Attacks", "Ataki"), attacks);
        }

        private static void DrawSourcePreview(DesignerRoomSceneMarker marker)
        {
            if (string.IsNullOrWhiteSpace(marker.SourceRuntimePath))
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(T("Source", "Źródło"), EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(marker.SourceRuntimePath, GUILayout.Height(20f));
            if (GUILayout.Button(T("Ping Source JSON", "Pokaż źródłowy JSON")) && File.Exists(marker.SourceRuntimePath))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(marker.SourceRuntimePath);
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void HandlePalettePlacement(SceneView sceneView)
        {
            if (!placementArmed)
            {
                return;
            }

            var root = DesignerRoomSceneAuthoringUtility.FindRoomRoot(SceneManager.GetActiveScene());
            if (root == null)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(12f, 12f, 360f, 42f), EditorStyles.helpBox);
                GUILayout.Label(T(
                    "Active scene needs a DesignerRoom root marker before placement.",
                    "Aktywna scena wymaga znacznika korzenia DesignerRoom przed dodawaniem."));
                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            var evt = Event.current;
            if (evt == null || evt.alt)
            {
                return;
            }

            var ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            var plane = new Plane(Vector3.up, root.transform.position);
            if (!plane.Raycast(ray, out var enter))
            {
                return;
            }

            var world = ray.GetPoint(enter);
            var local = root.transform.InverseTransformPoint(world);
            local.x = Mathf.Round(local.x);
            local.z = Mathf.Round(local.z);
            local.y = 0f;
            var preview = root.transform.TransformPoint(local);
            Handles.color = DesignerRoomSceneMarker.ColorFor(paletteKind);
            Handles.DrawWireDisc(preview, Vector3.up, paletteKind == DesignerRoomSceneMarkerKind.EnemySpawn ? 0.5f : 0.35f);
            Handles.Label(preview + Vector3.up * 0.35f, DesignerRoomAuthoringLocalization.DisplayNameForRuntimeKind(paletteRuntimeKind));

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                var marker = DesignerRoomSceneAuthoringUtility.CreateMarker(root, paletteKind, paletteRuntimeKind, local);
                Selection.activeGameObject = marker.gameObject;
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
                evt.Use();
                SceneView.RepaintAll();
            }
        }

        private static void DrawLabels(Scene scene)
        {
            foreach (var marker in DesignerRoomSceneAuthoringUtility.MarkersInScene(scene))
            {
                if (!marker.ShowLabel || marker.MarkerKind == DesignerRoomSceneMarkerKind.Folder)
                {
                    continue;
                }

                Handles.color = DesignerRoomSceneMarker.ColorFor(marker.MarkerKind);
                Handles.Label(marker.transform.position + Vector3.up * 0.55f, DesignerRoomAuthoringLocalization.MarkerLabel(marker));
            }
        }

        private static void DrawGrid(Scene scene)
        {
            DesignerRoomSceneAuthoringUtility.TryResolveBounds(scene, out var bounds);
            Handles.color = new Color(0.35f, 0.42f, 0.5f, 0.28f);
            for (var x = Mathf.CeilToInt(bounds.xMin); x <= Mathf.FloorToInt(bounds.xMax); x++)
            {
                Handles.DrawLine(new Vector3(x, 0.015f, bounds.yMin), new Vector3(x, 0.015f, bounds.yMax));
            }

            for (var z = Mathf.CeilToInt(bounds.yMin); z <= Mathf.FloorToInt(bounds.yMax); z++)
            {
                Handles.DrawLine(new Vector3(bounds.xMin, 0.015f, z), new Vector3(bounds.xMax, 0.015f, z));
            }

            Handles.color = new Color(0.2f, 0.55f, 1f, 0.8f);
            Handles.DrawAAPolyLine(
                new Vector3(bounds.xMin, 0.02f, bounds.yMin),
                new Vector3(bounds.xMax, 0.02f, bounds.yMin),
                new Vector3(bounds.xMax, 0.02f, bounds.yMax),
                new Vector3(bounds.xMin, 0.02f, bounds.yMax),
                new Vector3(bounds.xMin, 0.02f, bounds.yMin));
        }

        private static void DrawWalkability(Scene scene)
        {
            try
            {
                var project = DesignerRoomSceneAuthoringUtility.BuildRoomDesignerProject(scene);
                foreach (var cell in project.cells)
                {
                    if (cell.kind == RoomDesignerCellKinds.Ground)
                    {
                        continue;
                    }

                    var color = cell.kind switch
                    {
                        RoomDesignerCellKinds.Rock => new Color(0.45f, 0.45f, 0.5f, 0.18f),
                        RoomDesignerCellKinds.Hole => new Color(0.05f, 0.05f, 0.08f, 0.32f),
                        RoomDesignerCellKinds.Spike => new Color(1f, 0.2f, 0f, 0.22f),
                        _ => new Color(1f, 1f, 1f, 0.1f)
                    };
                    DrawCell(cell.x, cell.z, color);
                }
            }
            catch
            {
                // Scene View drawing should never block editing if a draft is temporarily invalid.
            }
        }

        private static void DrawCell(int x, int z, Color color)
        {
            var points = new[]
            {
                new Vector3(x - 0.5f, 0.035f, z - 0.5f),
                new Vector3(x + 0.5f, 0.035f, z - 0.5f),
                new Vector3(x + 0.5f, 0.035f, z + 0.5f),
                new Vector3(x - 0.5f, 0.035f, z + 0.5f)
            };
            Handles.DrawSolidRectangleWithOutline(points, color, new Color(color.r, color.g, color.b, 0.65f));
        }

        private void DrawSelectedEnemyRange()
        {
            if (!showSelectedEnemyRange || Selection.activeGameObject == null)
            {
                return;
            }

            var marker = Selection.activeGameObject.GetComponent<DesignerRoomSceneMarker>();
            if (marker == null || marker.MarkerKind != DesignerRoomSceneMarkerKind.EnemySpawn)
            {
                return;
            }

            var enemy = EnemyCatalog.CreateRuntimeDefault().Resolve(marker.RuntimeKind);
            if (enemy == null)
            {
                return;
            }

            Handles.color = new Color(1f, 0.85f, 0.2f, 0.5f);
            Handles.DrawWireDisc(marker.transform.position, Vector3.up, enemy.SightRadiusMeters);
            Handles.color = new Color(0.2f, 0.65f, 1f, 0.55f);
            Handles.DrawWireDisc(marker.transform.position, Vector3.up, enemy.HearingRadiusMeters);
            Handles.color = new Color(1f, 0.25f, 0.25f, 0.7f);
            Handles.DrawWireDisc(marker.transform.position, Vector3.up, enemy.PreferredRangeMaxMeters);
        }

        private static void FitActiveRoom()
        {
            if (SceneView.lastActiveSceneView == null)
            {
                return;
            }

            DesignerRoomSceneAuthoringUtility.TryResolveBounds(SceneManager.GetActiveScene(), out var bounds);
            var center = new Vector3(bounds.center.x, 0f, bounds.center.y);
            var size = Mathf.Max(bounds.width, bounds.height) * 0.65f;
            SceneView.lastActiveSceneView.LookAt(center, Quaternion.Euler(90f, 0f, 0f), size, true, false);
        }

        private static void SnapSelected()
        {
            foreach (var gameObject in Selection.gameObjects)
            {
                var marker = gameObject.GetComponent<DesignerRoomSceneMarker>();
                if (marker != null)
                {
                    DesignerRoomSceneAuthoringUtility.SnapMarker(marker);
                    EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
                }
            }

            SceneView.RepaintAll();
        }

        private void ExportActiveScene()
        {
            try
            {
                lastExportPath = DesignerRoomSceneAuthoringUtility.ExportScene(SceneManager.GetActiveScene());
                AssetDatabase.Refresh();
                Debug.Log($"Exported DesignerRoom scene draft to {lastExportPath}");
            }
            catch (Exception exception)
            {
                lastExportPath = string.Empty;
                Debug.LogError(exception.Message);
                EditorUtility.DisplayDialog(T("DesignerRoom Export Failed", "Eksport DesignerRoom nie powiódł się"), exception.Message, "OK");
            }
        }

        private void BuildVisualPreview(Scene scene)
        {
            try
            {
                DesignerRoomSceneVisualPreviewBuilder.BuildPreview(scene, previewLightingEnabled, previewCameraEnabled);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
                EditorUtility.DisplayDialog(T("DesignerRoom Preview Failed", "Podgląd DesignerRoom nie powiódł się"), exception.Message, "OK");
            }
        }

        private void ExportAllDesignerRoomScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var activePath = SceneManager.GetActiveScene().path;
            var exported = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { DesignerRoomSceneAuthoringUtility.DesignerRoomsDirectory }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path);
                DesignerRoomSceneAuthoringUtility.ExportScene(scene);
                exported++;
            }

            if (!string.IsNullOrWhiteSpace(activePath))
            {
                EditorSceneManager.OpenScene(activePath);
            }

            AssetDatabase.Refresh();
            lastExportPath = $"Exported {exported} DesignerRoom scene(s).";
            Debug.Log(lastExportPath);
        }

        private void BatchValidateAllDesignerRooms()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var activePath = SceneManager.GetActiveScene().path;
            var lines = new System.Collections.Generic.List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { DesignerRoomSceneAuthoringUtility.DesignerRoomsDirectory }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path);
                var result = DesignerRoomSceneAuthoringUtility.ValidateScene(scene);
                lines.Add($"{Path.GetFileNameWithoutExtension(path)}: {result.Summary()}");
                lines.AddRange(result.Errors.Select(error => $"  ERROR {error}"));
                lines.AddRange(result.Warnings.Select(warning => $"  WARN {warning}"));
            }

            if (!string.IsNullOrWhiteSpace(activePath))
            {
                EditorSceneManager.OpenScene(activePath);
            }

            Directory.CreateDirectory("output/reports");
            var reportPath = "output/reports/designer_room_scene_authoring_qa.md";
            File.WriteAllText(reportPath, "# Designer Room Scene Authoring QA\n\n" + string.Join("\n", lines));
            AssetDatabase.Refresh();
            Debug.Log($"Wrote DesignerRoom QA dashboard report to {reportPath}");
        }

        private static void RefreshActiveSceneFromSource()
        {
            var root = DesignerRoomSceneAuthoringUtility.FindRoomRoot(SceneManager.GetActiveScene());
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    T("Refresh Failed", "Odświeżenie nie powiodło się"),
                    T("Active scene is missing a DesignerRoom root marker.", "Aktywna scena nie ma znacznika korzenia DesignerRoom."),
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    T("Refresh Scene From Source JSON", "Odśwież scenę ze źródłowego JSON"),
                    T(
                        "This will remove current editable markers and regenerate them from the source runtime JSON. Continue?",
                        "To usunie obecne edytowalne znaczniki i odtworzy je ze źródłowego runtime JSON. Kontynuować?"),
                    T("Refresh", "Odśwież"),
                    T("Cancel", "Anuluj")))
            {
                return;
            }

            try
            {
                DesignerRoomSceneAuthoringUtility.RefreshSceneFromSource(root);
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
                EditorUtility.DisplayDialog(T("Refresh Failed", "Odświeżenie nie powiodło się"), exception.Message, "OK");
            }
        }

        private static string T(string english, string polish)
        {
            return DesignerRoomAuthoringLocalization.T(english, polish);
        }
    }
}
