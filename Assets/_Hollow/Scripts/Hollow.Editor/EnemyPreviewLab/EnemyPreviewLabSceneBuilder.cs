using System.IO;
using Hollow.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hollow.Editor.EnemyPreviewLab
{
    public static class EnemyPreviewLabSceneBuilder
    {
        public const string SceneDirectory = "Assets/_Hollow/Scenes/EnemyPreviewLab";
        public const string ScenePath = EnemyPreviewLabController.DefaultScenePath;

        [MenuItem("Hollow/Enemy Authoring/Open Enemy Preview Lab")]
        public static void OpenLab()
        {
            OpenWithSpawnKind(EnemyPreviewLabController.DefaultSelectedSpawnKind);
        }

        [MenuItem("Hollow/Enemy Authoring/Create Or Refresh Enemy Preview Lab Scene")]
        public static void CreateOrRefreshScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            CreateSceneAsset(openAfterCreate: true);
        }

        public static EnemyPreviewLabController OpenWithEnemy(EnemyDefinition enemy)
        {
            return OpenWithSpawnKind(enemy != null ? enemy.SpawnKind : EnemyPreviewLabController.DefaultSelectedSpawnKind);
        }

        public static EnemyPreviewLabController OpenWithSpawnKind(string spawnKind)
        {
            if (!File.Exists(ScenePath))
            {
                CreateSceneAsset(openAfterCreate: false);
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return null;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = FindOrCreateController(scene);
            controller.SetSelectedSpawnKind(spawnKind, respawnIfPlaying: false);
            controller.RebuildPreviewRoom();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = controller;
            SceneView.lastActiveSceneView?.FrameSelected();
            return controller;
        }

        public static string CreateSceneAsset(bool openAfterCreate)
        {
            Directory.CreateDirectory(SceneDirectory);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controller = BuildSceneObjects();
            controller.RebuildPreviewRoom();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            if (openAfterCreate)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            return ScenePath;
        }

        public static EnemyPreviewLabController FindOrCreateController(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var existing = root.GetComponentInChildren<EnemyPreviewLabController>(true);
                if (existing != null)
                {
                    return existing;
                }
            }

            return BuildSceneObjects();
        }

        private static EnemyPreviewLabController BuildSceneObjects()
        {
            var root = new GameObject("EnemyPreviewLab");
            return root.AddComponent<EnemyPreviewLabController>();
        }
    }
}
