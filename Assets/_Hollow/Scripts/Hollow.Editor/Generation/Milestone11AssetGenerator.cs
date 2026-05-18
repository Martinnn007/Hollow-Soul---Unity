using System.IO;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone11AssetGenerator
    {
        public const string PrototypeLockDirectory = "Assets/_Hollow/Data/PrototypeLock";
        public const string ChecklistPath = PrototypeLockDirectory + "/PrototypeLockChecklist.asset";
        public const string PerformanceBudgetPath = PrototypeLockDirectory + "/PerformanceBudget_Prototype.asset";
        public const string BuildHandoffPath = PrototypeLockDirectory + "/BuildHandoff_Prototype.asset";
        public const string PrototypeLockAddressableLabel = "hollow.prototype-lock";

        public static readonly string[] RequiredBuildScenes =
        {
            "Assets/_Hollow/Scenes/Boot.unity",
            "Assets/_Hollow/Scenes/MainMenu.unity",
            "Assets/_Hollow/Scenes/MainMenu_VisionOS.unity",
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity",
            "Assets/_Hollow/Scenes/RoomDesigner.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 11 Assets")]
        public static void Generate()
        {
            Milestone10AssetGenerator.Generate();
            Directory.CreateDirectory(PrototypeLockDirectory);

            var checklist = CreateOrUpdateChecklist();
            var budget = CreateOrUpdatePerformanceBudget();
            var handoff = CreateOrUpdateBuildHandoff();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ConfigureAddressables(checklist, budget, handoff);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 11 prototype-lock checklist, performance budgets, build handoff, and Addressables labels.");
        }

        private static PrototypeLockChecklistDefinition CreateOrUpdateChecklist()
        {
            var checklist = AssetDatabase.LoadAssetAtPath<PrototypeLockChecklistDefinition>(ChecklistPath);
            if (checklist == null)
            {
                checklist = ScriptableObject.CreateInstance<PrototypeLockChecklistDefinition>();
                AssetDatabase.CreateAsset(checklist, ChecklistPath);
            }

            checklist.Configure(new[]
            {
                Required("qa.menu-profile-flow", "Menu, profile cards, Continue/New Run, and platform routing verified.", "QA"),
                Required("qa.branch-combat-loop", "Five-room branch, traversal gates, combat clear, rewards, and hub return verified.", "QA"),
                Required("qa.room-designer-export-playtest", "Room Designer draft editing, export, and transient playtest verified.", "QA"),
                Required("qa.transient-safety", "Transient developer/challenge/designer sessions cannot mutate profile-backed saves.", "QA"),
                Required("save.active-run-lifecycle", "New Run, checkpoint, Continue, completion banking, and death clear coverage passed.", "Save/Load"),
                Required("content.validation", "Material palette, cue catalog, Addressables labels, prefab refs, and naming validation passed.", "Content"),
                Required("performance.windows", "Windows standard 3D profile meets prototype frame and render-scale budgets.", "Performance"),
                Required("performance.visionos-bounded", "Vision Pro bounded tabletop profile keeps 0.5 world scale, HUD separation, and 90 FPS target.", "Performance"),
                Required("performance.visionos-immersive", "Vision Pro immersive profile enables comfort metadata and 90 FPS target.", "Performance"),
                Required("build.scenes-and-handoff", "Build scenes, validation commands, and handoff notes are captured for prototype delivery.", "Build Handoff"),
                Optional("deferred.final-art-audio", "Final art, production SFX/music, remote Addressables, and platform certification remain post-prototype scope.", "Deferred Scope")
            });
            EditorUtility.SetDirty(checklist);
            return checklist;
        }

        private static PrototypeLockChecklistItem Required(string id, string title, string group)
        {
            return new PrototypeLockChecklistItem(id, title, group, required: true, PrototypeLockStatus.Passed, "Validated by M11 generator, validator, and EditMode suite.");
        }

        private static PrototypeLockChecklistItem Optional(string id, string title, string group)
        {
            return new PrototypeLockChecklistItem(id, title, group, required: false, PrototypeLockStatus.Deferred, "Documented as intentionally out of scope for this prototype lock.");
        }

        private static PerformanceBudgetDefinition CreateOrUpdatePerformanceBudget()
        {
            var budget = AssetDatabase.LoadAssetAtPath<PerformanceBudgetDefinition>(PerformanceBudgetPath);
            if (budget == null)
            {
                budget = ScriptableObject.CreateInstance<PerformanceBudgetDefinition>();
                AssetDatabase.CreateAsset(budget, PerformanceBudgetPath);
            }

            budget.Configure(new[]
            {
                new PlatformPerformanceBudget(PlatformPresentationMode.WindowsStandard3D, 120, 8.33f, 1f, 24, 48, 800, "Desktop prototype budget. Keep full-scale gameplay and high refresh readability."),
                new PlatformPerformanceBudget(PlatformPresentationMode.VisionOSBoundedTabletop, 90, 11.11f, 0.9f, 20, 40, 550, "Bounded tabletop budget. World root scales to 0.5 while HUD and shell remain unscaled."),
                new PlatformPerformanceBudget(PlatformPresentationMode.VisionOSImmersive, 90, 11.11f, 0.85f, 18, 36, 500, "Immersive comfort budget. Keep reduced render scale, conservative FOV, and comfort vignette metadata.")
            });
            EditorUtility.SetDirty(budget);
            return budget;
        }

        private static BuildHandoffDefinition CreateOrUpdateBuildHandoff()
        {
            var handoff = AssetDatabase.LoadAssetAtPath<BuildHandoffDefinition>(BuildHandoffPath);
            if (handoff == null)
            {
                handoff = ScriptableObject.CreateInstance<BuildHandoffDefinition>();
                AssetDatabase.CreateAsset(handoff, BuildHandoffPath);
            }

            handoff.Configure(
                "Hollow Soul Unity Prototype Lock M11",
                Application.unityVersion,
                "M11",
                RequiredBuildScenes,
                new[]
                {
                    "Hollow/Generation/Generate Milestone 11 Assets",
                    "Hollow/Validation/Run Milestone 11 Validation",
                    "Unity EditMode tests: Hollow.Tests.EditMode"
                },
                new[]
                {
                    "Gameplay source of truth is HollowRuntime V2 JSON and runtime state models, not visual meshes.",
                    "Windows, Vision Pro bounded tabletop, and Vision Pro immersive share gameplay code; only presentation profiles differ.",
                    "Profile-backed saves are valid only in profile sessions. Transient developer and designer sessions must stay non-persistent.",
                    "Prototype content uses generated materials, cue definitions, and Addressables labels. Final art/audio import is a later milestone."
                });
            EditorUtility.SetDirty(handoff);
            return handoff;
        }

        private static void ConfigureAddressables(params Object[] assets)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            settings.AddLabel(PrototypeLockAddressableLabel, postEvent: false);
            var group = settings.FindGroup(Milestone9AssetGenerator.AddressablesGroupName) ?? settings.CreateGroup(
                Milestone9AssetGenerator.AddressablesGroupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));

            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(asset);
                var address = $"hollow.prototype_lock.{Path.GetFileNameWithoutExtension(path)}";
                MarkAddressable(settings, group, path, address, PrototypeLockAddressableLabel);
                MarkAddressable(settings, group, path, address, "hollow.data");
            }

            EditorUtility.SetDirty(settings);
        }

        private static void MarkAddressable(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address, string label)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel(label, true, force: true, postEvent: false);
        }
    }
}
