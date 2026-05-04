using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Core.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.Editor.ArenaMode
{
    public sealed class ArenaStudioWindow : EditorWindow
    {
        private ArenaModePresetDefinition selectedPreset;
        private UnityEditor.Editor presetEditor;
        private Vector2 scroll;
        private string validationSummary = "Generate or select a preset to validate.";

        [MenuItem("Hollow/Arena Mode/Arena Studio")]
        public static void Open()
        {
            GetWindow<ArenaStudioWindow>("Arena Studio");
        }

        private void OnEnable()
        {
            selectedPreset = LoadPresets().FirstOrDefault();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Arena Mode", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Playable combat arena using real Hollow runtime room, combat, enemies, assets, lighting, and scoring.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Starter Presets + Scene", GUILayout.Height(32f)))
                {
                    ArenaModeAssetGenerator.GenerateAll();
                    selectedPreset = LoadPresets().FirstOrDefault();
                    ValidateSelected();
                }

                if (GUILayout.Button("Open Arena Scene", GUILayout.Height(32f)))
                {
                    OpenArenaScene();
                }

                using (new EditorGUI.DisabledScope(selectedPreset == null))
                {
                    if (GUILayout.Button("Play Selected Arena", GUILayout.Height(32f)))
                    {
                        PlaySelectedArena();
                    }
                }
            }

            EditorGUILayout.Space(8f);
            DrawPresetPicker();
            DrawValidation();
            DrawPresetEditor();
        }

        private void DrawPresetPicker()
        {
            var presets = LoadPresets();
            using (new EditorGUILayout.HorizontalScope())
            {
                selectedPreset = (ArenaModePresetDefinition)EditorGUILayout.ObjectField("Selected Preset", selectedPreset, typeof(ArenaModePresetDefinition), false);
                if (GUILayout.Button("Validate", GUILayout.Width(100f)))
                {
                    ValidateSelected();
                }
            }

            if (presets.Count == 0)
            {
                EditorGUILayout.HelpBox("No Arena presets found. Use Generate Starter Presets + Scene.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var preset in presets)
                {
                    if (GUILayout.Toggle(selectedPreset == preset, preset.DisplayName, "Button"))
                    {
                        selectedPreset = preset;
                    }
                }
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(validationSummary, validationSummary.StartsWith("OK", System.StringComparison.Ordinal) ? MessageType.Info : MessageType.Warning);
        }

        private void DrawPresetEditor()
        {
            if (selectedPreset == null)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            UnityEditor.Editor.CreateCachedEditor(selectedPreset, null, ref presetEditor);
            presetEditor?.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void ValidateSelected()
        {
            if (selectedPreset == null)
            {
                validationSummary = "Select an Arena preset first.";
                return;
            }

            var errors = selectedPreset.ValidateForArena();
            validationSummary = errors.Count == 0
                ? $"OK: {selectedPreset.DisplayName} is playable. Waves: {selectedPreset.Waves.Count}, Survival: {selectedPreset.SurvivalMode}."
                : string.Join("\n", errors);
        }

        private void OpenArenaScene()
        {
            if (!File.Exists(ArenaModeAssetGenerator.ArenaScenePath))
            {
                ArenaModeAssetGenerator.GenerateAll();
            }

            EditorSceneManager.OpenScene(ArenaModeAssetGenerator.ArenaScenePath, OpenSceneMode.Single);
        }

        private void PlaySelectedArena()
        {
            if (selectedPreset == null)
            {
                return;
            }

            ValidateSelected();
            if (!validationSummary.StartsWith("OK", System.StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("Arena Studio", validationSummary, "OK");
                return;
            }

            if (!File.Exists(ArenaModeAssetGenerator.ArenaScenePath))
            {
                ArenaModeAssetGenerator.GenerateAll();
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ArenaModeHandoff.Set(selectedPreset.PresetId, nextAutoStart: true, AppShellRoute.MainMenu);
            EditorSceneManager.OpenScene(ArenaModeAssetGenerator.ArenaScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static System.Collections.Generic.IReadOnlyList<ArenaModePresetDefinition> LoadPresets()
        {
            if (!Directory.Exists(ArenaModeAssetGenerator.PresetFolder))
            {
                return System.Array.Empty<ArenaModePresetDefinition>();
            }

            return AssetDatabase.FindAssets("t:ArenaModePresetDefinition", new[] { ArenaModeAssetGenerator.PresetFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ArenaModePresetDefinition>)
                .Where(preset => preset != null)
                .OrderBy(preset => preset.DisplayName)
                .ToArray();
        }
    }
}
