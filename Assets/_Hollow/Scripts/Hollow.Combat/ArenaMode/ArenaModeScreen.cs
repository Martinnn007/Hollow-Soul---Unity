using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Combat
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class ArenaModeScreen : MonoBehaviour
    {
        private const string CuratedRatRoomPresetId = "arena_room_small_ratroom_001";

        private ArenaModeController controller;
        private ArenaModeRuntimeSettings draft;
        private RectTransform rootPanel;
        private RectTransform overlayPanel;
        private Font font;
        private int selectedPresetIndex;
        private int selectedSpawnKindIndex;
        private int selectedManualCount = 3;
        private string[] spawnKinds = Array.Empty<string>();

        public void Bind(ArenaModeController nextController)
        {
            controller = nextController;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ConfigureCanvas();
            spawnKinds = controller != null ? controller.AvailableSpawnKinds().ToArray() : Array.Empty<string>();
        }

        public void ShowSetup(ArenaModeRuntimeSettings settings)
        {
            if (controller == null)
            {
                return;
            }

            draft = (settings ?? controller.CurrentSettings ?? new ArenaModeRuntimeSettings()).Clone();
            selectedPresetIndex = Mathf.Clamp(
                Array.FindIndex(controller.Presets.ToArray(), preset => preset != null && preset.PresetId == draft.PresetId),
                0,
                Mathf.Max(0, controller.Presets.Count - 1));
            RebuildSetup();
        }

        public void ShowCombatOverlay()
        {
            ClearChildren();
            rootPanel = null;
            overlayPanel = CreatePanel("ArenaOverlay", transform, new Vector2(560f, 218f), new Color(0.04f, 0.05f, 0.08f, 0.74f));
            overlayPanel.anchorMin = new Vector2(0f, 1f);
            overlayPanel.anchorMax = new Vector2(0f, 1f);
            overlayPanel.pivot = new Vector2(0f, 1f);
            overlayPanel.anchoredPosition = new Vector2(22f, -22f);
            if (controller != null && controller.IsEditorOrDevelopmentLaunch && controller.CurrentSettings?.CuratedLocked != true)
            {
                AddButton(overlayPanel, "Spawn Selected", new Vector2(-128f, -172f), () => controller.SpawnManualGroup(SelectedSpawnKind, selectedManualCount), new Color(0.35f, 0.42f, 0.72f), new Vector2(220f, 32f));
                AddButton(overlayPanel, "- Count", new Vector2(74f, -172f), () => { selectedManualCount = Mathf.Max(1, selectedManualCount - 1); RefreshOverlay(); }, new Color(0.22f, 0.25f, 0.33f), new Vector2(92f, 32f));
                AddButton(overlayPanel, "+ Count", new Vector2(176f, -172f), () => { selectedManualCount = Mathf.Min(32, selectedManualCount + 1); RefreshOverlay(); }, new Color(0.22f, 0.25f, 0.33f), new Vector2(92f, 32f));
            }

            RefreshOverlay();
        }

        public void ShowArenaComplete()
        {
            ClearChildren();
            rootPanel = CreatePanel("ArenaComplete", transform, new Vector2(620f, 340f), new Color(0.04f, 0.05f, 0.08f, 0.92f));
            AddText(rootPanel, "Arena Complete", 34, FontStyle.Bold, new Vector2(0f, 110f), new Vector2(560f, 48f), new Color(1f, 0.91f, 0.72f));
            AddText(rootPanel, ScoreLine(), 18, FontStyle.Bold, new Vector2(0f, 42f), new Vector2(560f, 42f), new Color(0.88f, 1f, 0.84f));
            AddText(rootPanel, DetailLine(), 14, FontStyle.Normal, new Vector2(0f, -8f), new Vector2(560f, 46f), new Color(0.84f, 0.86f, 0.94f));
            AddButton(rootPanel, "Run Again", new Vector2(-130f, -105f), () => controller.StartArena(controller.CurrentSettings.Clone()), new Color(0.85f, 0.55f, 0.12f), new Vector2(210f, 42f));
            AddButton(rootPanel, "Setup", new Vector2(130f, -105f), controller.StopArenaToSetup, new Color(0.25f, 0.44f, 0.78f), new Vector2(210f, 42f));
            AddButton(rootPanel, "Quit", new Vector2(0f, -160f), controller.QuitArena, new Color(0.52f, 0.18f, 0.18f), new Vector2(210f, 42f));
        }

        public void RefreshOverlay()
        {
            if (overlayPanel == null || controller == null)
            {
                return;
            }

            var label = overlayPanel.Find("ArenaOverlayText")?.GetComponent<Text>();
            if (label == null)
            {
                label = AddText(overlayPanel, string.Empty, 13, FontStyle.Bold, new Vector2(0f, -74f), new Vector2(520f, 148f), new Color(0.92f, 0.94f, 1f));
                label.gameObject.name = "ArenaOverlayText";
            }

            var perfLine = string.Empty;
            if (controller.IsEditorOrDevelopmentLaunch)
            {
                EnemyAiDebugOverlay.ReportRoomEnemyCount(controller.EnemiesRemaining);
                var ai = EnemyAiDebugOverlay.PerformanceStats;
                var nav = EnemyNavigationDebugOverlay.Stats;
                perfLine =
                    $"\nAI {ai.ActiveAiAgents} F/R/B {ai.FullLodAgents}/{ai.ReducedLodAgents}/{ai.BackgroundLodAgents} | brain/s {ai.BrainThinksPerSecond} scorer/s {ai.ScorerCallsPerSecond} UB/s {ai.BehaviorGraphTicksPerSecond}" +
                    $"\nNav agents {nav.ActivePathUsers} pending {nav.PendingPathUsers} stuck {nav.StuckAgents} | pressure {ai.MeleePressure:0.0}/{ai.RangedPressure:0.0}/{ai.AreaPressure:0.0}/{ai.ChargePressure:0.0}";
            }

            label.text =
                $"{controller.CurrentSettings?.DisplayName ?? "Arena"}\n" +
                $"Wave {controller.CurrentWaveNumber} | Enemies {controller.EnemiesRemaining} | Score {controller.ScoreTracker.Score}\n" +
                $"Damage {controller.ScoreTracker.DamageDealt} | Kills {controller.ScoreTracker.Kills} | Time {FormatTime(controller.ScoreTracker.TimeSurvivedSeconds)}" +
                perfLine +
                (controller.IsEditorOrDevelopmentLaunch && controller.CurrentSettings?.CuratedLocked != true ? $"\nManual: {DisplayNameFor(SelectedSpawnKind)} x{selectedManualCount}" : string.Empty);
        }

        private void RebuildSetup()
        {
            ClearChildren();
            rootPanel = CreatePanel("ArenaSetup", transform, new Vector2(860f, 760f), new Color(0.04f, 0.05f, 0.08f, 0.94f));
            AddText(rootPanel, "Arena Mode", 38, FontStyle.Bold, new Vector2(0f, 315f), new Vector2(760f, 56f), new Color(1f, 0.91f, 0.72f));
            AddText(rootPanel, "Pick a preset, tune player stats, add enemy groups, then fight the real runtime encounter.", 14, FontStyle.Normal, new Vector2(0f, 272f), new Vector2(760f, 28f), new Color(0.82f, 0.85f, 0.94f));
            var ratRoomPreset = controller.Presets.FirstOrDefault(preset => preset != null && preset.PresetId == CuratedRatRoomPresetId);
            if (ratRoomPreset != null)
            {
                AddButton(rootPanel, "Play Room_Small_RatRoom_001", new Vector2(0f, 224f), () => controller.StartArena(ratRoomPreset.CreateRuntimeSettings()), new Color(0.74f, 0.31f, 0.24f), new Vector2(330f, 42f));
            }

            AddSection("Preset", -285f);
            AddButton(rootPanel, "<", new Vector2(-355f, -45f), PreviousPreset, new Color(0.22f, 0.25f, 0.33f), new Vector2(44f, 34f));
            AddText(rootPanel, draft.DisplayName, 16, FontStyle.Bold, new Vector2(-185f, -45f), new Vector2(280f, 34f), new Color(0.88f, 1f, 0.84f));
            AddButton(rootPanel, ">", new Vector2(-15f, -45f), NextPreset, new Color(0.22f, 0.25f, 0.33f), new Vector2(44f, 34f));
            if (draft.CuratedLocked)
            {
                AddText(rootPanel, "Curated Survival", 13, FontStyle.Bold, new Vector2(205f, -45f), new Vector2(220f, 34f), new Color(1f, 0.78f, 0.58f));
            }
            else
            {
                AddButton(rootPanel, draft.SurvivalMode ? "Survival: ON" : "Survival: OFF", new Vector2(205f, -45f), ToggleSurvival, new Color(0.55f, 0.24f, 0.62f), new Vector2(220f, 34f));
            }

            AddSection("Room", -225f);
            if (draft.CuratedLocked)
            {
                AddText(rootPanel, "Locked authored room: Room_Small_RatRoom_001. Layout, rocks, doors, and rat waves are fixed.", 12, FontStyle.Normal, new Vector2(-40f, -122f), new Vector2(620f, 40f), new Color(0.84f, 0.86f, 0.94f));
            }
            else
            {
                AddButton(rootPanel, $"Size: {draft.RoomSize}", new Vector2(-275f, -122f), CycleRoomSize, new Color(0.25f, 0.44f, 0.78f), new Vector2(190f, 34f));
                AddButton(rootPanel, $"Layout: {draft.LayoutStyle}", new Vector2(-65f, -122f), CycleLayout, new Color(0.25f, 0.44f, 0.78f), new Vector2(190f, 34f));
                AddButton(rootPanel, $"Obstacles: {draft.ObstaclePreset}", new Vector2(175f, -122f), CycleObstacles, new Color(0.25f, 0.44f, 0.78f), new Vector2(235f, 34f));
            }

            AddSection("Player", -165f);
            AddStepper("HP", draft.PlayerHp.ToString(), new Vector2(-245f, -198f), () => { draft.PlayerHp = Mathf.Max(ArenaModeRuntimeSettings.MinPlayerHp, draft.PlayerHp - 1); RebuildSetup(); }, () => { draft.PlayerHp = Mathf.Min(ArenaModeRuntimeSettings.MaxPlayerHp, draft.PlayerHp + 1); RebuildSetup(); });
            AddStepper("DMG", $"+{draft.PlayerDamageBonus}", new Vector2(0f, -198f), () => { draft.PlayerDamageBonus = Mathf.Max(ArenaModeRuntimeSettings.MinDamageBonus, draft.PlayerDamageBonus - 1); RebuildSetup(); }, () => { draft.PlayerDamageBonus = Mathf.Min(ArenaModeRuntimeSettings.MaxDamageBonus, draft.PlayerDamageBonus + 1); RebuildSetup(); });
            AddStepper("Speed", draft.PlayerSpeedMetersPerSecond.ToString("0.0"), new Vector2(245f, -198f), () => { draft.PlayerSpeedMetersPerSecond = Mathf.Max(ArenaModeRuntimeSettings.MinPlayerSpeed, draft.PlayerSpeedMetersPerSecond - 0.25f); RebuildSetup(); }, () => { draft.PlayerSpeedMetersPerSecond = Mathf.Min(ArenaModeRuntimeSettings.MaxPlayerSpeed, draft.PlayerSpeedMetersPerSecond + 0.25f); RebuildSetup(); });

            AddSection("Enemy Groups", -285f, -255f);
            if (draft.CuratedLocked)
            {
                AddText(rootPanel, "Rats only: 3 rats, 5 rats, 7 rats, then survival scaling continues on the authored spawn anchors.", 12, FontStyle.Normal, new Vector2(55f, -313f), new Vector2(650f, 86f), new Color(0.84f, 0.86f, 0.94f));
            }
            else
            {
                AddText(rootPanel, GroupSummary(), 12, FontStyle.Normal, new Vector2(-215f, -313f), new Vector2(390f, 86f), new Color(0.84f, 0.86f, 0.94f));
                AddButton(rootPanel, "< Enemy", new Vector2(85f, -292f), PreviousEnemyKind, new Color(0.22f, 0.25f, 0.33f), new Vector2(96f, 32f));
                AddText(rootPanel, DisplayNameFor(SelectedSpawnKind), 12, FontStyle.Bold, new Vector2(205f, -292f), new Vector2(130f, 32f), new Color(0.88f, 1f, 0.84f));
                AddButton(rootPanel, "Enemy >", new Vector2(325f, -292f), NextEnemyKind, new Color(0.22f, 0.25f, 0.33f), new Vector2(96f, 32f));
                AddButton(rootPanel, "- Count", new Vector2(85f, -332f), () => { selectedManualCount = Mathf.Max(1, selectedManualCount - 1); RebuildSetup(); }, new Color(0.22f, 0.25f, 0.33f), new Vector2(96f, 32f));
                AddText(rootPanel, selectedManualCount.ToString(), 13, FontStyle.Bold, new Vector2(205f, -332f), new Vector2(130f, 32f), Color.white);
                AddButton(rootPanel, "+ Count", new Vector2(325f, -332f), () => { selectedManualCount = Mathf.Min(32, selectedManualCount + 1); RebuildSetup(); }, new Color(0.22f, 0.25f, 0.33f), new Vector2(96f, 32f));
                AddButton(rootPanel, "Add Group", new Vector2(145f, -375f), AddGroupToDraft, new Color(0.26f, 0.52f, 0.38f), new Vector2(150f, 34f));
                AddButton(rootPanel, "Clear Groups", new Vector2(310f, -375f), ClearDraftGroups, new Color(0.52f, 0.18f, 0.18f), new Vector2(150f, 34f));
            }

            AddButton(rootPanel, draft.CuratedLocked ? "Play Curated Room" : "Start Arena", new Vector2(-135f, -320f), () => controller.StartArena(draft.Clone()), new Color(0.85f, 0.55f, 0.12f), new Vector2(220f, 48f));
            AddButton(rootPanel, "Quit", new Vector2(-135f, -380f), controller.QuitArena, new Color(0.22f, 0.25f, 0.33f), new Vector2(220f, 42f));
        }

        private string SelectedSpawnKind => spawnKinds.Length == 0 ? "spawnEnemyNormal" : spawnKinds[Mathf.Clamp(selectedSpawnKindIndex, 0, spawnKinds.Length - 1)];

        private void PreviousPreset()
        {
            var presets = controller.Presets.ToArray();
            if (presets.Length == 0)
            {
                return;
            }

            selectedPresetIndex = (selectedPresetIndex + presets.Length - 1) % presets.Length;
            draft = presets[selectedPresetIndex].CreateRuntimeSettings();
            RebuildSetup();
        }

        private void NextPreset()
        {
            var presets = controller.Presets.ToArray();
            if (presets.Length == 0)
            {
                return;
            }

            selectedPresetIndex = (selectedPresetIndex + 1) % presets.Length;
            draft = presets[selectedPresetIndex].CreateRuntimeSettings();
            RebuildSetup();
        }

        private void PreviousEnemyKind()
        {
            if (spawnKinds.Length == 0)
            {
                return;
            }

            selectedSpawnKindIndex = (selectedSpawnKindIndex + spawnKinds.Length - 1) % spawnKinds.Length;
            RebuildSetup();
        }

        private void NextEnemyKind()
        {
            if (spawnKinds.Length == 0)
            {
                return;
            }

            selectedSpawnKindIndex = (selectedSpawnKindIndex + 1) % spawnKinds.Length;
            RebuildSetup();
        }

        private void ToggleSurvival()
        {
            if (draft.CuratedLocked)
            {
                return;
            }

            draft.SurvivalMode = !draft.SurvivalMode;
            RebuildSetup();
        }

        private void CycleRoomSize()
        {
            if (draft.CuratedLocked)
            {
                return;
            }

            draft.RoomSize = NextEnum(draft.RoomSize);
            RebuildSetup();
        }

        private void CycleLayout()
        {
            if (draft.CuratedLocked)
            {
                return;
            }

            draft.LayoutStyle = NextEnum(draft.LayoutStyle);
            RebuildSetup();
        }

        private void CycleObstacles()
        {
            if (draft.CuratedLocked)
            {
                return;
            }

            draft.ObstaclePreset = NextEnum(draft.ObstaclePreset);
            RebuildSetup();
        }

        private void AddGroupToDraft()
        {
            if (draft.CuratedLocked)
            {
                return;
            }

            draft.EnsurePlayableDefaults();
            var group = ArenaModeDefaults.CreateGroup(SelectedSpawnKind, selectedManualCount, ArenaSpawnPattern.OuterRing, ArenaGroupingMode.LoosePack);
            var wave = draft.Waves[0];
            var groups = wave.Groups.Select(existing => existing.Clone()).Concat(new[] { group }).ToArray();
            wave.Configure(wave.DisplayName, wave.SpawnDelaySeconds, groups);
            RebuildSetup();
        }

        private void ClearDraftGroups()
        {
            if (draft.CuratedLocked)
            {
                return;
            }

            draft.EnsurePlayableDefaults();
            draft.Waves[0].Configure(draft.Waves[0].DisplayName, draft.Waves[0].SpawnDelaySeconds, Array.Empty<ArenaModeEnemyGroupDefinition>());
            RebuildSetup();
        }

        private string GroupSummary()
        {
            draft.EnsurePlayableDefaults();
            var groups = draft.Waves[0].Groups;
            if (groups.Count == 0)
            {
                return "Wave 1 has no groups. Add at least one group before starting.";
            }

            return string.Join("\n", groups.Take(4).Select(group => $"{DisplayNameFor(group.SpawnKind)} x{group.Count} | {group.SpawnPattern}"));
        }

        private string DisplayNameFor(string spawnKind)
        {
            return controller != null ? controller.DisplayNameForSpawnKind(spawnKind) : spawnKind;
        }

        private static T NextEnum<T>(T value) where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var index = Array.IndexOf(values, value);
            return values[(index + 1) % values.Length];
        }

        private string ScoreLine()
        {
            return controller == null
                ? "Score --"
                : $"Score {controller.ScoreTracker.Score}";
        }

        private string DetailLine()
        {
            if (controller == null)
            {
                return string.Empty;
            }

            return $"Damage {controller.ScoreTracker.DamageDealt} | Kills {controller.ScoreTracker.Kills} | Time {FormatTime(controller.ScoreTracker.TimeSurvivedSeconds)} | Wave Clears {controller.ScoreTracker.WaveClears}";
        }

        private static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private void AddSection(string label, float x, float y = -12f)
        {
            AddText(rootPanel, label, 13, FontStyle.Bold, new Vector2(x, y), new Vector2(180f, 24f), new Color(1f, 0.91f, 0.72f));
        }

        private void AddStepper(string label, string value, Vector2 center, Action minus, Action plus)
        {
            AddText(rootPanel, label, 12, FontStyle.Bold, center + new Vector2(0f, 28f), new Vector2(160f, 22f), new Color(1f, 0.91f, 0.72f));
            AddButton(rootPanel, "-", center + new Vector2(-62f, 0f), minus, new Color(0.22f, 0.25f, 0.33f), new Vector2(42f, 34f));
            AddText(rootPanel, value, 14, FontStyle.Bold, center, new Vector2(78f, 34f));
            AddButton(rootPanel, "+", center + new Vector2(62f, 0f), plus, new Color(0.22f, 0.25f, 0.33f), new Vector2(42f, 34f));
        }

        private void ConfigureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 22;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private Button AddButton(RectTransform parent, string label, Vector2 anchoredPosition, Action onClick, Color color, Vector2? size = null)
        {
            var buttonRoot = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonRoot.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonRoot.transform;
            rect.sizeDelta = size ?? new Vector2(180f, 38f);
            rect.anchoredPosition = anchoredPosition;
            buttonRoot.GetComponent<Image>().color = color;
            var button = buttonRoot.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            AddText(rect, label, 12, FontStyle.Bold, Vector2.zero, rect.sizeDelta);
            return button;
        }

        private Text AddText(RectTransform parent, string text, int size, FontStyle style, Vector2 anchoredPosition, Vector2 sizeDelta, Color? color = null)
        {
            var textRoot = new GameObject(string.IsNullOrEmpty(text) ? "Text" : text, typeof(RectTransform), typeof(Text));
            textRoot.transform.SetParent(parent, false);
            var rect = (RectTransform)textRoot.transform;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            var label = textRoot.GetComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color ?? Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return rect;
        }

        private void ClearChildren()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }

            rootPanel = null;
            overlayPanel = null;
        }
    }
}
