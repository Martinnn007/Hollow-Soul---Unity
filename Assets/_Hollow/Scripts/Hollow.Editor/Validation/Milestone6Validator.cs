using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rewards;
using Hollow.Rooms;
using Hollow.UI.Shell;
using Hollow.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone6Validator
    {
        private const string SampleRoomRuntimePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Branches/Hollow.Branches.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/BranchGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/BranchMiniMapModel.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/Hollow.Rewards.asmdef",
            "Assets/_Hollow/Scripts/Hollow.Rewards/RoomRewardPickup.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/RuntimeRewardCounter.cs",
            "Assets/_Hollow/Scripts/Hollow.Rewards/HubReturnPortal.cs",
            "Assets/_Hollow/Scripts/Hollow.UI/Shell/BranchMiniMapController.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone6AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone6Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone6BranchTraversalRewardTests.cs",
            "Assets/_Hollow/Prefabs/Rewards/RoomRewardPickup.prefab",
            "Assets/_Hollow/Prefabs/Rewards/HubReturnPortal.prefab",
            "Docs/Milestone6BranchTraversalRewards.md"
        };

        private static readonly (string ScenePath, HollowPlatformKind PlatformKind)[] GameScenes =
        {
            ("Assets/_Hollow/Scenes/Game_Windows.unity", HollowPlatformKind.WindowsStandard3D),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", HollowPlatformKind.VisionOSBoundedTabletop),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive)
        };

        [MenuItem("Hollow/Validation/Run Milestone 6 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();

            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M6 file: {file}");
                }
            }

            ValidateGraph(failures);
            ValidatePrefabs(failures);
            foreach (var (scenePath, platformKind) in GameScenes)
            {
                ValidateScene(scenePath, platformKind, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 6 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateGraph(List<string> failures)
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(SampleRoomRuntimePath);
            var error = "missing sample room JSON";
            if (json == null || !HollowRuntimeV2Importer.TryImport(json.text, out var asset, out error))
            {
                failures.Add($"M6 could not import sample room for graph validation: {error}");
                return;
            }

            var graph = BranchGenerator.CreateFiveRoomCross(asset);
            if (graph.RoomCount != 5 || graph.Connections.Count != 8)
            {
                failures.Add("M6 five-room cross must contain five rooms and eight directional connections.");
            }

            foreach (var direction in new[] { "north", "south", "east", "west" })
            {
                if (!graph.TryGetConnection(BranchRoomId.Origin, direction, out _))
                {
                    failures.Add($"M6 origin is missing {direction} connection.");
                }
            }
        }

        private static void ValidatePrefabs(List<string> failures)
        {
            var reward = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hollow/Prefabs/Rewards/RoomRewardPickup.prefab");
            if (reward == null || reward.GetComponent<RoomRewardPickup>() == null)
            {
                failures.Add("RoomRewardPickup.prefab must include RoomRewardPickup.");
            }

            var portal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hollow/Prefabs/Rewards/HubReturnPortal.prefab");
            if (portal == null || portal.GetComponent<HubReturnPortal>() == null)
            {
                failures.Add("HubReturnPortal.prefab must include HubReturnPortal.");
            }
        }

        private static void ValidateScene(string scenePath, HollowPlatformKind expectedPlatformKind, List<string> failures)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Missing M6 game scene: {scenePath}");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);

            var session = Object.FindFirstObjectByType<GameSessionController>();
            if (session == null || session.SampleRoomRuntimeJson == null)
            {
                failures.Add($"{scenePath} is missing GameSessionController sample room JSON.");
            }

            var branch = Object.FindFirstObjectByType<BranchSessionController>();
            if (branch == null)
            {
                failures.Add($"{scenePath} is missing BranchSessionController.");
            }
            else
            {
                if (branch.RewardPickupPrefab == null)
                {
                    failures.Add($"{scenePath} BranchSessionController has no reward pickup prefab.");
                }

                if (branch.HubReturnPortalPrefab == null)
                {
                    failures.Add($"{scenePath} BranchSessionController has no hub return portal prefab.");
                }
            }

            var presentationRoot = Object.FindFirstObjectByType<PlatformPresentationRoot>();
            if (presentationRoot == null || Mathf.Abs(presentationRoot.WorldScale - PresentationScalePolicy.WorldScaleFor(expectedPlatformKind)) > 0.0001f)
            {
                failures.Add($"{scenePath} has invalid presentation scaling.");
            }

            var shellCanvas = GameObject.Find("PlatformShellCanvas");
            if (shellCanvas == null || shellCanvas.GetComponent<BranchMiniMapController>() == null)
            {
                failures.Add($"{scenePath} PlatformShellCanvas must include BranchMiniMapController.");
            }
            else if (presentationRoot != null && shellCanvas.transform.IsChildOf(presentationRoot.transform))
            {
                failures.Add($"{scenePath} PlatformShellCanvas must remain outside WorldPresentationRoot.");
            }
        }
    }
}
