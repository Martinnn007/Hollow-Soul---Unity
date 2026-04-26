using System;
using System.IO;
using Hollow.Core.App;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hollow.Editor.Generation
{
    public static class Milestone8AssetGenerator
    {
        private const string Root = "Assets/_Hollow";

        [MenuItem("Hollow/Generation/Generate Milestone 8 Assets")]
        public static void Generate()
        {
            Milestone7AssetGenerator.Generate();
            Directory.CreateDirectory($"{Root}/Prefabs/Designer");
            SavePrefab(CreateRoomDesignerRoot(), $"{Root}/Prefabs/Designer/RoomDesignerRoot.prefab");
            GenerateRoomDesignerScene();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 8 room designer scene, prefab, and build settings.");
        }

        private static GameObject CreateRoomDesignerRoot()
        {
            var root = new GameObject("RoomDesignerRoot");
            root.AddComponent<RoomDesignerController>();
            var preview = new GameObject("RoomDesignerPreviewRoot");
            preview.transform.SetParent(root.transform, false);
            preview.transform.localPosition = Vector3.zero;
            return root;
        }

        private static void GenerateRoomDesignerScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RoomDesigner";
            InstantiatePrefab($"{Root}/Prefabs/Core/AppRoot.prefab");
            CreateDesignerCamera();
            InstantiatePrefab($"{Root}/Prefabs/Designer/RoomDesignerRoot.prefab");
            CreateEventSystem();
            CreateDirectionalLight();
            EditorSceneManager.SaveScene(scene, $"{Root}/Scenes/RoomDesigner.unity");
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                BuildScene($"{Root}/Scenes/Boot.unity"),
                BuildScene($"{Root}/Scenes/MainMenu.unity"),
                BuildScene($"{Root}/Scenes/Game_Windows.unity"),
                BuildScene($"{Root}/Scenes/Game_VisionOS_Bounded.unity"),
                BuildScene($"{Root}/Scenes/Game_VisionOS_Immersive.unity"),
                BuildScene($"{Root}/Scenes/RoomDesigner.unity")
            };
        }

        private static EditorBuildSettingsScene BuildScene(string path)
        {
            return new EditorBuildSettingsScene(path, enabled: true);
        }

        private static void CreateDesignerCamera()
        {
            var rig = new GameObject("RoomDesignerCameraRig");
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(rig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 10f, -10f);
            cameraObject.transform.localRotation = Quaternion.Euler(48f, 0f, 0f);
            cameraObject.tag = "MainCamera";
        }

        private static GameObject InstantiatePrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new FileNotFoundException($"Missing prefab at {path}");
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null)
            {
                eventSystem.AddComponent(inputSystemModule);
            }
            else
            {
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
