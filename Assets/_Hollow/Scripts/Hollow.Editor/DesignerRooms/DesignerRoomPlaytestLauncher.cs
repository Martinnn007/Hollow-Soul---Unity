using System;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.DesignerRooms
{
    [InitializeOnLoad]
    public static class DesignerRoomPlaytestLauncher
    {
        public const string GameWindowsScenePath = "Assets/_Hollow/Scenes/Game_Windows.unity";

        private const string SelectedCharacterKey = "Hollow.DesignerRooms.Playtest.SelectedCharacterId";
        private const string ReturnScenePathKey = "Hollow.DesignerRooms.Playtest.ReturnScenePath";

        public static readonly string[] CharacterIds = { "balanced", "heavy" };

        public static readonly string[] CharacterLabels =
        {
            "Balanced - steady form",
            "Heavy - slower, tougher"
        };

        static DesignerRoomPlaytestLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static string SelectedCharacterId
        {
            get => SanitizeCharacterId(EditorPrefs.GetString(SelectedCharacterKey, "balanced"));
            set => EditorPrefs.SetString(SelectedCharacterKey, SanitizeCharacterId(value));
        }

        [MenuItem("Hollow/Designer Rooms/Play This Room")]
        public static void PlayThisRoom()
        {
            PlayActiveDesignerRoom(SelectedCharacterId);
        }

        [MenuItem("Hollow/Designer Rooms/Play This Room", true)]
        private static bool CanPlayThisRoom()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   DesignerRoomSceneAuthoringUtility.FindRoomRoot(SceneManager.GetActiveScene()) != null;
        }

        public static string BuildRuntimeJsonForScene(Scene scene)
        {
            var project = DesignerRoomSceneAuthoringUtility.BuildRoomDesignerProject(scene);
            var report = RoomDesignerDraftValidator.Validate(project);
            if (!report.IsValid)
            {
                throw new InvalidOperationException($"Room playtest blocked: {report.Summary()} - {string.Join("; ", report.Errors)}");
            }

            return RoomDesignerCompiler.ExportRuntimeJson(project, prettyPrint: false);
        }

        public static void PrimeHandoffForScene(Scene scene, string selectedCharacterId)
        {
            var runtimeJson = BuildRuntimeJsonForScene(scene);
            RoomPlaytestHandoff.Set(
                runtimeJson,
                RuntimeSessionMode.TransientRoomDesignerPlaytest,
                AppShellRoute.MainMenu,
                SanitizeCharacterId(selectedCharacterId));
        }

        public static void PlayActiveDesignerRoom(string selectedCharacterId)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Room Playtest Launcher", "Stop Play Mode before launching a room playtest.", "OK");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var root = DesignerRoomSceneAuthoringUtility.FindRoomRoot(scene);
            if (root == null)
            {
                EditorUtility.DisplayDialog("Room Playtest Launcher", "Active scene is not a Designer Room scene.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(scene.path))
            {
                EditorUtility.DisplayDialog("Room Playtest Launcher", "Save this Designer Room scene before launching a playtest.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                SelectedCharacterId = selectedCharacterId;
                PrimeHandoffForScene(scene, SelectedCharacterId);
                SessionState.SetString(ReturnScenePathKey, scene.path);
                EditorSceneManager.OpenScene(GameWindowsScenePath, OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                EditorUtility.DisplayDialog("Room Playtest Launcher Failed", exception.Message, "OK");
            }
        }

        private static string SanitizeCharacterId(string characterId)
        {
            return Array.IndexOf(CharacterIds, characterId) >= 0 ? characterId : "balanced";
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            var returnScenePath = SessionState.GetString(ReturnScenePathKey, string.Empty);
            SessionState.SetString(ReturnScenePathKey, string.Empty);
            if (string.IsNullOrWhiteSpace(returnScenePath) || !System.IO.File.Exists(returnScenePath))
            {
                return;
            }

            if (SceneManager.GetActiveScene().path == returnScenePath)
            {
                return;
            }

            EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
        }
    }
}
