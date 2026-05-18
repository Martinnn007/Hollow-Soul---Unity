using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Core.App;
using Hollow.Editor.Generation;
using Hollow.Core;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.UI.MainMenu;
using NUnit.Framework;
using Unity.PolySpatial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollow.Tests.EditMode
{
    public sealed class VisionOSMainMenuSetupTests
    {
        private readonly List<GameObject> createdObjects = new();
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_visionos_menu_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in createdObjects.Where(createdObject => createdObject != null))
            {
                UnityEngine.Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
            if (!string.IsNullOrWhiteSpace(tempRoot) && Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void VisionOSMainMenuPrefabUsesGuidedWorldSpaceScreen()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisionOSMainMenuSetup.PrefabPath);

            Assert.IsNotNull(prefab, "MainMenuRoot_VisionOS prefab should exist.");
            Assert.IsNotNull(prefab.GetComponent<VisionOSMainMenuScreen>());
            Assert.IsNull(prefab.GetComponent<MainMenuScreen>(), "VisionOS menu should not use the Windows/shared button layout.");
            Assert.AreEqual(LayerMask.NameToLayer("UI"), prefab.layer);

            var canvas = prefab.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.AreEqual("PolySpatialUI", canvas.sortingLayerName);
            Assert.AreEqual(20, canvas.sortingOrder);

            var controller = prefab.GetComponent<MainMenuController>();
            Assert.AreEqual(HollowPlatformKind.VisionOSBoundedTabletop, controller.DefaultPlatformKind);
            Assert.AreEqual(AppShellRoute.MainMenuVisionOS, controller.DefaultReturnRoute);
        }

        [Test]
        public void BuiltVisionOSMainMenuUsesSpatialSafeRaycastSetup()
        {
            var screen = CreateBuiltScreen();
            var canvas = screen.GetComponent<Canvas>();

            Assert.AreEqual(RenderMode.WorldSpace, canvas.renderMode);
            Assert.AreEqual(Camera.main, canvas.worldCamera);
            Assert.AreEqual(LayerMask.NameToLayer("UI"), screen.gameObject.layer);
            Assert.AreEqual("PolySpatialUI", canvas.sortingLayerName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(screen.CurrentStatusMessage));

            AssertNonButtonImagesDoNotRaycast(screen);
            foreach (var button in screen.GetComponentsInChildren<Button>())
            {
                Assert.IsNotNull(button.GetComponent<VisionOSMenuButtonFeedback>(), $"{button.name} should report visionOS tap feedback.");
            }

            var visibleButtons = screen.GetComponentsInChildren<Button>()
                .Where(button => button.IsInteractable())
                .Select(button => button.gameObject.name)
                .ToArray();
            CollectionAssert.AreEquivalent(new[] { "Empty Slot 1", "Empty Slot 2", "Empty Slot 3" }, visibleButtons);
        }

        [Test]
        public void VisionOSModeButtonsAdvanceThroughFeedbackComponents()
        {
            var screen = CreateBuiltScreen();

            Click(screen, "Empty Slot 1");
            Assert.AreEqual("Mode", screen.CurrentStepName);
            Assert.IsTrue(FindButton(screen, "Normal Run").interactable);
            Assert.IsTrue(FindButton(screen, "Challenges").interactable);
            Assert.IsTrue(FindButton(screen, "Arena").interactable);

            Click(screen, "Normal Run");
            Assert.AreEqual("CharacterForRun", screen.CurrentStepName);
            Assert.IsTrue(screen.GetComponentsInChildren<VisionOSMenuButtonFeedback>().Any(feedback => feedback.DisablesAfterActivation));

            Click(screen, "Back");
            Click(screen, "Arena");
            Assert.AreEqual("CharacterForArena", screen.CurrentStepName);

            Click(screen, "Back");
            Click(screen, "Challenges");
            Assert.AreEqual("Challenge", screen.CurrentStepName);
        }

        [Test]
        public void VisionOSButtonFeedbackPointerClickActivatesOnlyOnce()
        {
            var buttonObject = new GameObject(
                "SpatialButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(VisionOSMenuButtonFeedback));
            createdObjects.Add(buttonObject);
            var feedback = buttonObject.GetComponent<VisionOSMenuButtonFeedback>();
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            createdObjects.Add(eventSystemObject);
            var activationCount = 0;
            feedback.Configure("Mode", "Normal Run", Color.blue, () => activationCount++, _ => { }, disableAfterActivation: false);

            ExecuteEvents.Execute<IPointerClickHandler>(
                buttonObject,
                new PointerEventData(eventSystemObject.GetComponent<EventSystem>()),
                ExecuteEvents.pointerClickHandler);
            buttonObject.GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(1, activationCount);
            Assert.IsTrue(feedback.HasActivated);
        }

        [Test]
        public void VisionOSMainMenuSceneHasGuidedMenuAndBoundedVolumeCamera()
        {
            EditorSceneManager.OpenScene(VisionOSMainMenuSetup.ScenePath);

            Assert.IsNotNull(Object.FindFirstObjectByType<MainMenuController>());
            Assert.IsNotNull(Object.FindFirstObjectByType<VisionOSMainMenuScreen>());
            Assert.IsNull(Object.FindFirstObjectByType<MainMenuScreen>());
            Assert.IsNotNull(Object.FindFirstObjectByType<EventSystem>());

            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>(FindObjectsInactive.Include);
            Assert.IsNotNull(volumeCamera);
            Assert.IsTrue(volumeCamera.OpenWindowOnLoad);
            Assert.AreEqual(VolumeCamera.PolySpatialVolumeCameraMode.Bounded, volumeCamera.WindowConfiguration.Mode);
            AssertVectorApproximately(VisionOSVolumeCameraSetup.BoundedMenuSourceCenter, volumeCamera.transform.localPosition);
        }

        [Test]
        public void ArenaModeSceneHasBoundedVolumeCameraForVisionOSLaunches()
        {
            EditorSceneManager.OpenScene("Assets/_Hollow/Scenes/ArenaMode/ArenaMode.unity");

            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>(FindObjectsInactive.Include);
            Assert.IsNotNull(volumeCamera);
            Assert.AreEqual(VolumeCamera.PolySpatialVolumeCameraMode.Bounded, volumeCamera.WindowConfiguration.Mode);
            AssertVectorApproximately(VisionOSVolumeCameraSetup.BoundedLevelSourceCenter, volumeCamera.transform.localPosition);
        }

        private VisionOSMainMenuScreen CreateBuiltScreen()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            createdObjects.Add(cameraObject);

            var root = new GameObject(
                "VisionOSMainMenuUnderTest",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(VisionOSMainMenuScreen),
                typeof(MainMenuController));
            createdObjects.Add(root);

            var controller = root.GetComponent<MainMenuController>();
            controller.ConfigureDefaults(HollowPlatformKind.VisionOSBoundedTabletop, AppShellRoute.MainMenuVisionOS);
            var viewModel = new MainMenuViewModel(
                new JsonProfileStore(tempRoot),
                new SelectedProfileContext(),
                new AppStateMachine());
            var viewModelSetter = typeof(MainMenuController)
                .GetProperty(nameof(MainMenuController.ViewModel))
                ?.GetSetMethod(nonPublic: true);
            Assert.IsNotNull(viewModelSetter);
            viewModelSetter.Invoke(controller, new object[] { viewModel });

            var screen = root.GetComponent<VisionOSMainMenuScreen>();
            screen.Build(controller);
            return screen;
        }

        private static void Click(VisionOSMainMenuScreen screen, string buttonName)
        {
            FindButton(screen, buttonName).onClick.Invoke();
        }

        private static Button FindButton(VisionOSMainMenuScreen screen, string buttonName)
        {
            return screen.GetComponentsInChildren<Button>()
                .First(button => button.gameObject.name == buttonName && button.IsInteractable());
        }

        private static void AssertNonButtonImagesDoNotRaycast(VisionOSMainMenuScreen screen)
        {
            foreach (var image in screen.GetComponentsInChildren<Image>())
            {
                if (image.GetComponent<Button>() == null)
                {
                    Assert.IsFalse(image.raycastTarget, $"{image.name} should not block spatial UI taps.");
                }
            }
        }

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f);
            Assert.AreEqual(expected.y, actual.y, 0.001f);
            Assert.AreEqual(expected.z, actual.z, 0.001f);
        }
    }
}
