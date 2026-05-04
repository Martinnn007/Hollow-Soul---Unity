using System;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.EnemyAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.EnemyPreviewLab
{
    public sealed class EnemyPreviewLabWindow : EditorWindow
    {
        private string selectedSpawnKind = EnemyPreviewLabController.DefaultSelectedSpawnKind;
        private string search = string.Empty;
        private Vector2 scroll;

        [MenuItem("Hollow/Enemy Authoring/Enemy Preview Lab")]
        public static void Open()
        {
            GetWindow<EnemyPreviewLabWindow>("Enemy Preview Lab");
        }

        public static void OpenWithEnemy(EnemyDefinition enemy)
        {
            var window = GetWindow<EnemyPreviewLabWindow>("Enemy Preview Lab");
            window.selectedSpawnKind = enemy != null ? enemy.SpawnKind : EnemyPreviewLabController.DefaultSelectedSpawnKind;
            window.Focus();
            EnemyPreviewLabSceneBuilder.OpenWithEnemy(enemy);
        }

        private void OnGUI()
        {
            DrawToolbar();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSceneControls();
            DrawTargetControls();
            DrawRuntimeControls();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Enemy Preview Lab", EditorStyles.boldLabel, GUILayout.Width(160f));
                GUILayout.Label("Search", GUILayout.Width(46f));
                search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    search = string.Empty;
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open Scene", EditorStyles.toolbarButton, GUILayout.Width(94f)))
                {
                    EnemyPreviewLabSceneBuilder.OpenWithSpawnKind(selectedSpawnKind);
                }
            }
        }

        private void DrawSceneControls()
        {
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create / Refresh Lab Scene"))
                {
                    EnemyPreviewLabSceneBuilder.CreateOrRefreshScene();
                }

                if (GUILayout.Button("Open With Selected Enemy"))
                {
                    EnemyPreviewLabSceneBuilder.OpenWithSpawnKind(selectedSpawnKind);
                }
            }

            EditorGUILayout.HelpBox(
                "Open the scene, press Play, and the lab spawns the selected enemy into a lit preview room with ranges, path tracing, AI blackboard, and a moving dummy player.",
                MessageType.Info);
        }

        private void DrawTargetControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enemy Target", EditorStyles.boldLabel);
            var enemies = ResolveCatalogForDisplay()
                .Definitions
                .Where(enemy => enemy != null && enemy.SpawnKind != "spawnEnemyBoss")
                .Where(MatchesSearch)
                .OrderBy(enemy => enemy.DisplayName)
                .ToArray();

            var labels = enemies.Select(enemy => $"{enemy.DisplayName} [{enemy.SpawnKind}]").ToArray();
            var index = Mathf.Max(0, Array.FindIndex(enemies, enemy => enemy.SpawnKind == selectedSpawnKind));
            if (labels.Length > 0)
            {
                var next = EditorGUILayout.Popup("Enemy", index, labels);
                selectedSpawnKind = enemies[Mathf.Clamp(next, 0, enemies.Length - 1)].SpawnKind;
            }

            var controller = ActiveController();
            using (new EditorGUI.DisabledScope(controller == null))
            {
                if (GUILayout.Button("Send Enemy To Active Lab"))
                {
                    controller.SetSelectedSpawnKind(selectedSpawnKind, respawnIfPlaying: true);
                    controller.RebuildPreviewRoom();
                    if (Application.isPlaying)
                    {
                        controller.RespawnPreviewEnemy();
                    }

                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
            }
        }

        private void DrawRuntimeControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active Lab Controls", EditorStyles.boldLabel);
            var controller = ActiveController();
            if (controller == null)
            {
                EditorGUILayout.HelpBox("No EnemyPreviewLabController is active in the current scene.", MessageType.Warning);
                return;
            }

            var serialized = new SerializedObject(controller);
            Draw(serialized, "playerPattern");
            Draw(serialized, "playerPatternRadiusMeters");
            Draw(serialized, "playerPatternSpeed");
            Draw(serialized, "freezeEnemyInspectionMode");
            Draw(serialized, "showRangeOverlays");
            Draw(serialized, "showGridOverlay");
            Draw(serialized, "showPathTracing");
            Draw(serialized, "showAiBlackboard");
            Draw(serialized, "showRuntimeStats");
            if (serialized.ApplyModifiedProperties())
            {
                controller.SetOverlayToggles(
                    controller.ShowRangeOverlays,
                    controller.ShowGridOverlay,
                    controller.ShowPathTracing,
                    controller.ShowAiBlackboard,
                    controller.ShowRuntimeStats);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Room"))
                {
                    controller.RebuildPreviewRoom();
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }

                if (GUILayout.Button("Respawn Enemy"))
                {
                    controller.RespawnPreviewEnemy();
                }
            }

            if (controller.ActiveEnemy != null)
            {
                var enemy = controller.ActiveEnemy;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Trace", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Enemy", enemy.Definition != null ? $"{enemy.Definition.DisplayName} [{enemy.Definition.SpawnKind}]" : "No definition");
                EditorGUILayout.LabelField("State", $"{enemy.ReadabilityState} | {enemy.AwarenessState} | {enemy.AiBlackboard.LodTier}");
                EditorGUILayout.LabelField("AI", enemy.AiBlackboard.Summary);
                EditorGUILayout.LabelField("Path", $"{enemy.LastNavigationBackend} / {enemy.LastNavigationPathStatus} / {enemy.LastNavigationFallbackReason}");
                EditorGUILayout.LabelField("Navigation Stats", EnemyNavigationDebugOverlay.DiagnosticsSummary);
                EditorGUILayout.LabelField("AI Stats", EnemyAiDebugOverlay.DiagnosticsSummary);
            }
        }

        private bool MatchesSearch(EnemyDefinition enemy)
        {
            if (enemy == null || string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            var needle = search.Trim();
            return enemy.DisplayName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   enemy.SpawnKind.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   enemy.BehaviorId.ToString().IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   enemy.Disposition.ToString().IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static EnemyPreviewLabController ActiveController()
        {
            return FindObjectsByType<EnemyPreviewLabController>(FindObjectsInactive.Include).FirstOrDefault();
        }

        private static EnemyCatalog ResolveCatalogForDisplay()
        {
            var assetCatalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyPreviewLabController.DefaultEnemyCatalogPath);
            return assetCatalog != null ? assetCatalog : EnemyCatalog.CreateRuntimeDefault();
        }

        private static void Draw(SerializedObject serialized, string propertyName)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, includeChildren: true);
            }
        }
    }
}
