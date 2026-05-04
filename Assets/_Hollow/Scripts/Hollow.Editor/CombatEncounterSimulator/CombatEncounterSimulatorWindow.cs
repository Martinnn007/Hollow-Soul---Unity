using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.CombatEncounterSimulator
{
    public sealed class CombatEncounterSimulatorWindow : EditorWindow
    {
        private static readonly string[] LoadoutIds = { "balanced", "heavy" };
        private static readonly string[] LoadoutLabels = { "Balanced - fast baseline", "Heavy - tougher, slower" };

        private readonly CombatEncounterScenario scenario = new();
        private EnemyCatalog catalog;
        private EnemyDefinition[] enemies = Array.Empty<EnemyDefinition>();
        private CombatEncounterSimulationResult lastResult;
        private CombatEncounterBatchResult lastBatch;
        private Vector2 setupScroll;
        private Vector2 resultScroll;
        private Vector2 sandboxScroll;
        private Vector2 batchScroll;
        private Vector2 recommendationScroll;
        private int selectedTab;
        private int playbackFrameIndex;
        private bool playbackRunning;
        private double lastPlaybackTime;
        private bool showPaths = true;
        private bool showAttackFlash = true;
        private bool showLabels = true;
        private bool autoRunAfterScenarioChange;

        [MenuItem("Hollow/Combat Tools/Combat Encounter Simulator")]
        public static void Open()
        {
            GetWindow<CombatEncounterSimulatorWindow>("Encounter Simulator");
        }

        public static void OpenWithEnemy(EnemyDefinition enemy)
        {
            var window = GetWindow<CombatEncounterSimulatorWindow>("Encounter Simulator");
            window.RefreshCatalog();
            window.scenario.enemyGroups.Clear();
            window.scenario.enemyGroups.Add(new CombatEncounterEnemyGroup
            {
                spawnKind = enemy != null ? enemy.SpawnKind : "spawnEnemyNormal",
                count = 4,
                spawnPattern = CombatEncounterSpawnPattern.SpreadPatrol,
                aiEnabled = true
            });
            window.Focus();
        }

        private void OnEnable()
        {
            RefreshCatalog();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnGUI()
        {
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(420f)))
                {
                    setupScroll = EditorGUILayout.BeginScrollView(setupScroll);
                    DrawScenarioSetup();
                    EditorGUILayout.EndScrollView();
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    selectedTab = GUILayout.Toolbar(
                        selectedTab,
                        new[] { "Dashboard", "Visible Sandbox", "Batch Balancing", "Recommendations", "Export" },
                        GUILayout.Height(28f));
                    switch (selectedTab)
                    {
                        case 0:
                            DrawDashboard();
                            break;
                        case 1:
                            DrawSandbox();
                            break;
                        case 2:
                            DrawBatch();
                            break;
                        case 3:
                            DrawRecommendations();
                            break;
                        case 4:
                            DrawExport();
                            break;
                    }
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Combat Encounter Simulator", EditorStyles.boldLabel, GUILayout.Width(230f));
                if (GUILayout.Button("Run Seed", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                {
                    RunSingle();
                }

                if (GUILayout.Button("Run 10", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    RunBatch(10);
                }

                if (GUILayout.Button("Run 100", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                {
                    RunBatch(100);
                }

                if (GUILayout.Button("Load Active Designer Room", EditorStyles.toolbarButton, GUILayout.Width(178f)))
                {
                    LoadActiveDesignerRoom();
                }

                GUILayout.FlexibleSpace();
                autoRunAfterScenarioChange = GUILayout.Toggle(autoRunAfterScenarioChange, "Auto-run", EditorStyles.toolbarButton, GUILayout.Width(76f));
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(74f)))
                {
                    RefreshCatalog();
                }
            }
        }

        private void DrawScenarioSetup()
        {
            EditorGUILayout.LabelField("Scenario Setup", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            scenario.scenarioName = EditorGUILayout.TextField("Name", scenario.scenarioName);
            scenario.roomPreset = (CombatEncounterRoomPreset)EditorGUILayout.EnumPopup("Room", scenario.roomPreset);
            if (scenario.roomPreset is CombatEncounterRoomPreset.Custom or CombatEncounterRoomPreset.ActiveDesignerRoom)
            {
                scenario.customRoomSizeMeters = EditorGUILayout.Vector2Field("Room Size Meters", scenario.customRoomSizeMeters);
            }

            var loadoutIndex = Mathf.Max(0, Array.IndexOf(LoadoutIds, scenario.playerLoadoutId));
            loadoutIndex = EditorGUILayout.Popup("Player Loadout", loadoutIndex, LoadoutLabels);
            scenario.playerLoadoutId = LoadoutIds[Mathf.Clamp(loadoutIndex, 0, LoadoutIds.Length - 1)];
            scenario.difficulty = (CombatEncounterDifficulty)EditorGUILayout.EnumPopup("Difficulty", scenario.difficulty);
            scenario.durationSeconds = EditorGUILayout.Slider("Duration", scenario.durationSeconds, 5f, 120f);
            scenario.seed = EditorGUILayout.IntField("Seed", scenario.seed);
            scenario.tickSeconds = EditorGUILayout.Slider("Tick", scenario.tickSeconds, 0.1f, 0.5f);
            scenario.usePathfinding = EditorGUILayout.Toggle("Use Pathfinding", scenario.usePathfinding);
            scenario.obstacleDensity = EditorGUILayout.Slider("Obstacle Density", scenario.obstacleDensity, 0f, 1f);
            scenario.includeRuntimePressureBudgets = EditorGUILayout.Toggle("Pressure Budgets", scenario.includeRuntimePressureBudgets);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enemy Groups", EditorStyles.boldLabel);
            if (scenario.enemyGroups.Count == 0)
            {
                scenario.enemyGroups.Add(new CombatEncounterEnemyGroup());
            }

            for (var index = 0; index < scenario.enemyGroups.Count; index++)
            {
                DrawGroup(index, scenario.enemyGroups[index]);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Add Group"))
                {
                    scenario.enemyGroups.Add(new CombatEncounterEnemyGroup
                    {
                        spawnKind = enemies.FirstOrDefault()?.SpawnKind ?? "spawnEnemyNormal",
                        count = 2,
                        spawnPattern = CombatEncounterSpawnPattern.SpreadPatrol
                    });
                }

                if (GUILayout.Button("Small Monsters Preset"))
                {
                    scenario.scenarioName = "Small Monsters Swarm";
                    scenario.roomPreset = CombatEncounterRoomPreset.Medium;
                    scenario.enemyGroups.Clear();
                    scenario.enemyGroups.Add(new CombatEncounterEnemyGroup { spawnKind = "spawnEnemyRat", count = 6, spawnPattern = CombatEncounterSpawnPattern.SpreadPatrol });
                    scenario.enemyGroups.Add(new CombatEncounterEnemyGroup { spawnKind = "spawnEnemySpider", count = 8, spawnPattern = CombatEncounterSpawnPattern.ClusteredGroup });
                }
            }

            if (EditorGUI.EndChangeCheck() && autoRunAfterScenarioChange)
            {
                RunSingle();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "V1 uses deterministic fast metrics from current enemy definitions, attacks, intelligence, movement, and action ranges. V2 shows one seed as a visible sandbox. V3 batches seeds. V4 generates tuning suggestions.",
                MessageType.Info);
        }

        private void DrawGroup(int index, CombatEncounterEnemyGroup group)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Group {index + 1}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Duplicate", GUILayout.Width(80f)))
                    {
                        scenario.enemyGroups.Insert(index + 1, group.Clone());
                        GUIUtility.ExitGUI();
                    }

                    using (new EditorGUI.DisabledScope(scenario.enemyGroups.Count <= 1))
                    {
                        if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                        {
                            scenario.enemyGroups.RemoveAt(index);
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                DrawEnemyPopup(group);
                group.count = Mathf.Clamp(EditorGUILayout.IntField("Count", group.count), 0, 80);
                group.spawnPattern = (CombatEncounterSpawnPattern)EditorGUILayout.EnumPopup("Spawn Pattern", group.spawnPattern);
                group.aiEnabled = EditorGUILayout.Toggle("AI Enabled", group.aiEnabled);
                group.notes = EditorGUILayout.TextField("Notes", group.notes);
                var enemy = ResolveEnemy(group.spawnKind);
                if (enemy != null)
                {
                    EditorGUILayout.LabelField(
                        "Identity",
                        $"{enemy.DisplayName} | {enemy.BehaviorId} | {enemy.Intelligence} | {enemy.Disposition}");
                    var attack = enemy.AttackProfiles.FirstOrDefault(profile => profile != null && profile.Damage > 0);
                    if (attack != null)
                    {
                        EditorGUILayout.LabelField(
                            "Primary Data",
                            $"{attack.DisplayName}: dmg {attack.Damage}, {attack.RuntimeKind}, cd {attack.CooldownSeconds:0.00}s, range {attack.RangeMeters:0.0}m");
                    }
                }
            }
        }

        private void DrawEnemyPopup(CombatEncounterEnemyGroup group)
        {
            if (enemies.Length == 0)
            {
                group.spawnKind = EditorGUILayout.TextField("Spawn Kind", group.spawnKind);
                return;
            }

            var labels = enemies.Select(enemy => $"{enemy.DisplayName} [{enemy.SpawnKind}]").ToArray();
            var current = Mathf.Max(0, Array.FindIndex(enemies, enemy => enemy.SpawnKind == group.spawnKind));
            var next = EditorGUILayout.Popup("Enemy", current, labels);
            group.spawnKind = enemies[Mathf.Clamp(next, 0, enemies.Length - 1)].SpawnKind;
        }

        private void DrawDashboard()
        {
            resultScroll = EditorGUILayout.BeginScrollView(resultScroll);
            if (lastResult == null)
            {
                EditorGUILayout.HelpBox("Run a seed to see pressure, deaths, attack frequency, stuck enemies, and pathfinding load.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawSummaryCards(lastResult);
            DrawPressureTimeline(lastResult);
            DrawEnemyMetricTable(lastResult.enemyMetrics, lastResult.durationSeconds);
            EditorGUILayout.EndScrollView();
        }

        private void DrawSummaryCards(CombatEncounterSimulationResult result)
        {
            EditorGUILayout.LabelField("Results Dashboard", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawCard("Survival", result.playerSurvived ? "Survived" : "Died", result.playerSurvived ? new Color(0.2f, 0.45f, 0.24f) : new Color(0.55f, 0.18f, 0.18f));
                DrawCard("Final HP", $"{result.playerFinalHealth:0.0}", new Color(0.28f, 0.32f, 0.42f));
                DrawCard("Enemy Deaths", $"{result.enemyDeaths}/{result.totalEnemies}", new Color(0.24f, 0.31f, 0.36f));
                DrawCard("Attacks/sec", $"{result.totalAttackStarts / Mathf.Max(result.durationSeconds, 0.01f):0.00}", new Color(0.34f, 0.26f, 0.38f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawCard("Peak Pressure", $"{result.peakPressure:0.00}", new Color(0.42f, 0.28f, 0.22f));
                DrawCard("Path req/sec", $"{result.pathRequestsPerSecond:0.0}", new Color(0.18f, 0.36f, 0.4f));
                DrawCard("Avg solve ms", $"{result.averagePathSolveMs:0.000}", new Color(0.18f, 0.36f, 0.4f));
                DrawCard("Stuck sec", $"{result.stuckSeconds:0.0}", new Color(0.42f, 0.26f, 0.22f));
            }
        }

        private static void DrawCard(string title, string value, Color color)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(62f)))
            {
                var rect = GUILayoutUtility.GetRect(1f, 1f);
                rect.height = 3f;
                EditorGUI.DrawRect(rect, color);
                EditorGUILayout.LabelField(title, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            }
        }

        private void DrawPressureTimeline(CombatEncounterSimulationResult result)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pressure Timeline", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(10f, 96f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.11f, 0.11f, 0.12f));
            if (result.frames.Count < 2)
            {
                return;
            }

            Handles.BeginGUI();
            DrawLane(result, rect, frame => frame.meleePressure, new Color(0.95f, 0.42f, 0.35f), result.peakPressure);
            DrawLane(result, rect, frame => frame.rangedPressure, new Color(0.35f, 0.62f, 0.95f), result.peakPressure);
            DrawLane(result, rect, frame => frame.areaPressure, new Color(0.72f, 0.42f, 0.9f), result.peakPressure);
            DrawLane(result, rect, frame => frame.chargePressure, new Color(0.95f, 0.72f, 0.28f), result.peakPressure);
            Handles.EndGUI();
        }

        private static void DrawLane(CombatEncounterSimulationResult result, Rect rect, Func<CombatEncounterFrame, float> value, Color color, float max)
        {
            var points = result.frames;
            if (points.Count < 2)
            {
                return;
            }

            Handles.color = color;
            for (var index = 1; index < points.Count; index++)
            {
                var a = points[index - 1];
                var b = points[index];
                var ax = rect.x + (index - 1) / (float)(points.Count - 1) * rect.width;
                var bx = rect.x + index / (float)(points.Count - 1) * rect.width;
                var ay = rect.yMax - Mathf.Clamp01(value(a) / Mathf.Max(max, 0.1f)) * rect.height;
                var by = rect.yMax - Mathf.Clamp01(value(b) / Mathf.Max(max, 0.1f)) * rect.height;
                Handles.DrawLine(new Vector3(ax, ay), new Vector3(bx, by));
            }
        }

        private static void DrawEnemyMetricTable(IReadOnlyList<CombatEncounterEnemyTypeMetrics> metrics, float durationSeconds)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enemy Table", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Enemy", EditorStyles.boldLabel, GUILayout.Width(170f));
                GUILayout.Label("Count", EditorStyles.boldLabel, GUILayout.Width(46f));
                GUILayout.Label("Deaths", EditorStyles.boldLabel, GUILayout.Width(52f));
                GUILayout.Label("Atk/s", EditorStyles.boldLabel, GUILayout.Width(54f));
                GUILayout.Label("Hit %", EditorStyles.boldLabel, GUILayout.Width(54f));
                GUILayout.Label("Dmg", EditorStyles.boldLabel, GUILayout.Width(44f));
                GUILayout.Label("Stuck", EditorStyles.boldLabel, GUILayout.Width(58f));
                GUILayout.Label("Path", EditorStyles.boldLabel, GUILayout.Width(58f));
                GUILayout.Label("ms", EditorStyles.boldLabel, GUILayout.Width(58f));
            }

            foreach (var metric in metrics)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(metric.displayName, GUILayout.Width(170f));
                    GUILayout.Label(metric.count.ToString(), GUILayout.Width(46f));
                    GUILayout.Label(metric.deaths.ToString(), GUILayout.Width(52f));
                    GUILayout.Label(metric.AttacksPerSecond(durationSeconds).ToString("0.00"), GUILayout.Width(54f));
                    GUILayout.Label(metric.HitRate.ToString("P0"), GUILayout.Width(54f));
                    GUILayout.Label(metric.damageDealt.ToString(), GUILayout.Width(44f));
                    GUILayout.Label(metric.stuckSeconds.ToString("0.0"), GUILayout.Width(58f));
                    GUILayout.Label(metric.pathRequests.ToString(), GUILayout.Width(58f));
                    GUILayout.Label(metric.estimatedPathSolveMs.ToString("0.00"), GUILayout.Width(58f));
                }
            }
        }

        private void DrawSandbox()
        {
            sandboxScroll = EditorGUILayout.BeginScrollView(sandboxScroll);
            if (lastResult == null)
            {
                EditorGUILayout.HelpBox("Run a seed first. The sandbox plays back the deterministic simulation with player, enemy, attack, stuck, and path overlays.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(playbackRunning ? "Pause" : "Play", GUILayout.Width(90f)))
                {
                    playbackRunning = !playbackRunning;
                    lastPlaybackTime = EditorApplication.timeSinceStartup;
                }

                if (GUILayout.Button("Step", GUILayout.Width(70f)))
                {
                    playbackRunning = false;
                    playbackFrameIndex = Mathf.Min(lastResult.frames.Count - 1, playbackFrameIndex + 1);
                }

                if (GUILayout.Button("Reset", GUILayout.Width(70f)))
                {
                    playbackFrameIndex = 0;
                    playbackRunning = false;
                }

                playbackFrameIndex = EditorGUILayout.IntSlider(playbackFrameIndex, 0, Mathf.Max(0, lastResult.frames.Count - 1));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                showPaths = EditorGUILayout.ToggleLeft("Path hints", showPaths, GUILayout.Width(100f));
                showAttackFlash = EditorGUILayout.ToggleLeft("Attack flash", showAttackFlash, GUILayout.Width(112f));
                showLabels = EditorGUILayout.ToggleLeft("Labels", showLabels, GUILayout.Width(80f));
            }

            var frame = lastResult.frames[Mathf.Clamp(playbackFrameIndex, 0, lastResult.frames.Count - 1)];
            EditorGUILayout.LabelField($"t={frame.timeSeconds:0.00}s | HP={frame.playerHealth:0.0} | Alive={frame.aliveEnemies} | Path req={frame.pathRequests} | Stuck={frame.stuckEnemies}");
            DrawSandboxCanvas(lastResult, frame);
            EditorGUILayout.EndScrollView();
        }

        private void DrawSandboxCanvas(CombatEncounterSimulationResult result, CombatEncounterFrame frame)
        {
            var rect = GUILayoutUtility.GetRect(10f, 430f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.095f, 0.1f, 0.105f));
            var room = result.scenario.RoomSizeMeters;
            var roomRect = new Rect(rect.x + 18f, rect.y + 18f, rect.width - 36f, rect.height - 36f);
            EditorGUI.DrawRect(roomRect, new Color(0.145f, 0.15f, 0.145f));
            Handles.BeginGUI();
            Handles.color = new Color(0.36f, 0.42f, 0.45f, 0.5f);
            for (var x = -Mathf.FloorToInt(room.x / 2f); x <= Mathf.CeilToInt(room.x / 2f); x++)
            {
                var px = MapPoint(new Vector2(x, 0f), room, roomRect).x;
                Handles.DrawLine(new Vector3(px, roomRect.y), new Vector3(px, roomRect.yMax));
            }

            for (var y = -Mathf.FloorToInt(room.y / 2f); y <= Mathf.CeilToInt(room.y / 2f); y++)
            {
                var py = MapPoint(new Vector2(0f, y), room, roomRect).y;
                Handles.DrawLine(new Vector3(roomRect.x, py), new Vector3(roomRect.xMax, py));
            }

            var player = MapPoint(frame.playerPosition, room, roomRect);
            Handles.color = Color.white;
            Handles.DrawSolidDisc(player, Vector3.forward, 6f);
            GUI.Label(new Rect(player.x + 7f, player.y - 10f, 80f, 20f), "Player", EditorStyles.miniLabel);

            foreach (var entity in frame.entities)
            {
                if (!entity.alive)
                {
                    continue;
                }

                var position = MapPoint(entity.position, room, roomRect);
                var color = entity.lane switch
                {
                    CombatEncounterPressureLane.Ranged => new Color(0.35f, 0.62f, 0.95f),
                    CombatEncounterPressureLane.Area => new Color(0.72f, 0.42f, 0.9f),
                    CombatEncounterPressureLane.Charge => new Color(0.95f, 0.72f, 0.28f),
                    _ => new Color(0.95f, 0.42f, 0.35f)
                };
                if (entity.stuck)
                {
                    color = Color.yellow;
                }

                if (showPaths)
                {
                    Handles.color = new Color(color.r, color.g, color.b, 0.35f);
                    Handles.DrawLine(position, player);
                }

                Handles.color = color;
                Handles.DrawSolidDisc(position, Vector3.forward, entity.attacking && showAttackFlash ? 7.5f : 5f);
                if (entity.attacking && showAttackFlash)
                {
                    Handles.DrawWireDisc(position, Vector3.forward, 13f);
                }

                if (showLabels)
                {
                    GUI.Label(new Rect(position.x + 6f, position.y - 10f, 160f, 20f), entity.displayName, EditorStyles.miniLabel);
                }
            }

            Handles.EndGUI();
        }

        private static Vector2 MapPoint(Vector2 point, Vector2 roomSize, Rect rect)
        {
            var normalized = new Vector2(
                Mathf.InverseLerp(-roomSize.x * 0.5f, roomSize.x * 0.5f, point.x),
                Mathf.InverseLerp(-roomSize.y * 0.5f, roomSize.y * 0.5f, point.y));
            return new Vector2(rect.x + normalized.x * rect.width, rect.yMax - normalized.y * rect.height);
        }

        private void DrawBatch()
        {
            batchScroll = EditorGUILayout.BeginScrollView(batchScroll);
            EditorGUILayout.LabelField("Batch Balancing", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run 10 Seeds"))
                {
                    RunBatch(10);
                }

                if (GUILayout.Button("Run 100 Seeds"))
                {
                    RunBatch(100);
                }
            }

            if (lastBatch == null)
            {
                EditorGUILayout.HelpBox("Batch mode compares seed variance for swarm balance and ranged pressure.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Runs: {lastBatch.Runs}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Survival Rate", lastBatch.SurvivalRate.ToString("P0"));
                EditorGUILayout.LabelField("Avg Final HP", lastBatch.AverageFinalHealth.ToString("0.00"));
                EditorGUILayout.LabelField("Avg Enemy Deaths", lastBatch.AverageEnemyDeaths.ToString("0.00"));
                EditorGUILayout.LabelField("Avg Attacks/sec", lastBatch.AverageAttacksPerSecond.ToString("0.00"));
                EditorGUILayout.LabelField("Avg Damage Taken", lastBatch.AverageDamageTaken.ToString("0.00"));
                EditorGUILayout.LabelField("Avg Peak Pressure", lastBatch.AveragePeakPressure.ToString("0.00"));
                EditorGUILayout.LabelField("Avg Path req/sec", lastBatch.AveragePathRequestsPerSecond.ToString("0.00"));
                EditorGUILayout.LabelField("Avg Path solve ms", lastBatch.AveragePathSolveMs.ToString("0.000"));
                EditorGUILayout.LabelField("Avg Stuck sec", lastBatch.AverageStuckSeconds.ToString("0.00"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seed Rows", EditorStyles.boldLabel);
            foreach (var result in lastBatch.results.Take(100))
            {
                EditorGUILayout.LabelField(
                    $"Seed {result.seed}",
                    $"{(result.playerSurvived ? "Survived" : "Died")} | HP {result.playerFinalHealth:0.0} | deaths {result.enemyDeaths}/{result.totalEnemies} | attacks {result.totalAttackStarts} | path {result.pathRequestsPerSecond:0.0}/s");
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRecommendations()
        {
            recommendationScroll = EditorGUILayout.BeginScrollView(recommendationScroll);
            EditorGUILayout.LabelField("Recommendation Engine", EditorStyles.boldLabel);
            if (lastResult == null && lastBatch == null)
            {
                EditorGUILayout.HelpBox("Run a single seed or batch to generate recommendations.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (lastBatch != null)
            {
                EditorGUILayout.LabelField("Batch Recommendations", EditorStyles.boldLabel);
                foreach (var recommendation in lastBatch.recommendations)
                {
                    EditorGUILayout.HelpBox(recommendation, MessageType.Info);
                }
            }

            if (lastResult != null)
            {
                EditorGUILayout.LabelField("Single-Seed Recommendations", EditorStyles.boldLabel);
                foreach (var recommendation in lastResult.recommendations)
                {
                    EditorGUILayout.HelpBox(recommendation, MessageType.Info);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Warnings", EditorStyles.boldLabel);
                foreach (var warning in lastResult.warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawExport()
        {
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(lastResult == null))
            {
                if (GUILayout.Button("Export Last Seed Markdown Report"))
                {
                    var path = CombatEncounterSimulatorEngine.ExportMarkdownReport(lastResult);
                    AssetDatabase.Refresh();
                    EditorUtility.RevealInFinder(System.IO.Path.GetFullPath(path));
                }
            }

            using (new EditorGUI.DisabledScope(lastBatch == null))
            {
                if (GUILayout.Button("Export Batch CSV"))
                {
                    var path = CombatEncounterSimulatorEngine.ExportBatchCsv(lastBatch);
                    AssetDatabase.Refresh();
                    EditorUtility.RevealInFinder(System.IO.Path.GetFullPath(path));
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Reports are written to `{CombatEncounterSimulatorEngine.ReportDirectory}`.", MessageType.None);
            if (lastResult != null)
            {
                EditorGUILayout.TextArea(CombatEncounterSimulatorEngine.BuildMarkdownReport(lastResult), GUILayout.MinHeight(360f));
            }
        }

        private void RunSingle()
        {
            lastResult = CombatEncounterSimulatorEngine.Run(scenario, catalog);
            lastBatch = null;
            playbackFrameIndex = 0;
            playbackRunning = false;
            selectedTab = 0;
        }

        private void RunBatch(int count)
        {
            lastBatch = CombatEncounterSimulatorEngine.RunBatch(scenario, count, catalog);
            lastResult = lastBatch.results.FirstOrDefault();
            playbackFrameIndex = 0;
            playbackRunning = false;
            selectedTab = 2;
        }

        private void LoadActiveDesignerRoom()
        {
            try
            {
                var loaded = CombatEncounterSimulatorEngine.ScenarioFromActiveDesignerRoom(scenario);
                scenario.scenarioName = loaded.scenarioName;
                scenario.roomPreset = loaded.roomPreset;
                scenario.customRoomSizeMeters = loaded.customRoomSizeMeters;
                scenario.obstacleDensity = loaded.obstacleDensity;
                scenario.enemyGroups = loaded.enemyGroups;
                if (autoRunAfterScenarioChange)
                {
                    RunSingle();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Encounter Simulator", exception.Message, "OK");
            }
        }

        private void RefreshCatalog()
        {
            catalog = CombatEncounterSimulatorEngine.ResolveCatalog();
            enemies = catalog.Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .OrderBy(enemy => enemy.DisplayName)
                .ToArray();
        }

        private EnemyDefinition ResolveEnemy(string spawnKind)
        {
            return catalog != null ? catalog.Resolve(spawnKind) : null;
        }

        private void OnEditorUpdate()
        {
            if (!playbackRunning || lastResult == null || lastResult.frames.Count == 0)
            {
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            if (now - lastPlaybackTime < 0.18)
            {
                return;
            }

            lastPlaybackTime = now;
            playbackFrameIndex++;
            if (playbackFrameIndex >= lastResult.frames.Count)
            {
                playbackFrameIndex = 0;
            }

            Repaint();
        }
    }
}
