using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.UI.Shell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone39AssetGenerator
    {
        public const string WorldIdentityDirectory = "Assets/_Hollow/Data/Worlds/M39";
        public const string FramingDirectory = WorldIdentityDirectory + "/Framing";
        public const string RunFramingCatalogPath = WorldIdentityDirectory + "/RunFramingCatalog_M39.asset";
        public const string ReportPath = "output/reports/m39_story_world_identity_run_framing.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 39 Assets")]
        public static void Generate()
        {
            Milestone38AssetGenerator.Generate();
            EnsureDirectories();
            var definitions = CreateWorldDefinitions();
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(RunFramingCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RunFramingCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, RunFramingCatalogPath);
            }

            catalog.Configure("m39_run_framing_catalog_v1", definitions);
            EditorUtility.SetDirty(catalog);
            WriteReport(definitions);
            AssignToGameScenes(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 39 run framing catalog with {definitions.Count} worlds.");
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(WorldIdentityDirectory);
            Directory.CreateDirectory(FramingDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
        }

        private static IReadOnlyList<RunFramingDefinition> CreateWorldDefinitions()
        {
            return new[]
            {
                SaveWorld(
                    1,
                    "The Hollow Threshold",
                    "A room-made wound where lost souls first learn the rules.",
                    "The first branch opens before the hub remembers you.",
                    "This branch tests whether your build can survive its own shape.",
                    "The threshold hub offers trade, breath, and three worse decisions.",
                    "The Stone Warden guards the first locked seam.",
                    "The threshold loosens. What you carry can become memory."),
                SaveWorld(
                    2,
                    "The Ashen Toyworks",
                    "A deeper floor of broken mechanisms, soot, and patient little traps.",
                    "The toyworks prologue starts the machine again.",
                    "Rooms click together like gears around your current build.",
                    "The toyworks hub smells of coins, old varnish, and warm dust.",
                    "A heavier warden keeps the factory heart turning.",
                    "Ash settles on the run. Keep going if the build still has teeth."),
                SaveWorld(
                    3,
                    "The Quiet Reliquary",
                    "A final prototype world for things the Hollow wanted to keep.",
                    "The reliquary opens softly, which somehow feels worse.",
                    "Each branch here feels less like a place and more like a memory sorting you.",
                    "The reliquary hub waits without pretending to be safe.",
                    "The last warden stands where extraction should be.",
                    "Extraction is possible. The Hollow will remember what you bank.")
            };
        }

        private static RunFramingDefinition SaveWorld(
            int worldIndex,
            string displayName,
            string subtitle,
            string prologueLine,
            string branchLine,
            string hubLine,
            string bossLine,
            string extractionLine)
        {
            var path = $"{FramingDirectory}/RunFraming_World{worldIndex}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<RunFramingDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RunFramingDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(worldIndex, displayName, subtitle, prologueLine, branchLine, hubLine, bossLine, extractionLine);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void AssignToGameScenes(RunFramingCatalogDefinition catalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureRunFramingCatalog(catalog);
                EditorUtility.SetDirty(branch);

                var shellCanvas = GameObject.Find("PlatformShellCanvas");
                if (shellCanvas == null)
                {
                    throw new MissingReferenceException($"{scenePath} is missing PlatformShellCanvas.");
                }

                var framingHud = shellCanvas.GetComponent<RunFramingHudController>();
                if (framingHud == null)
                {
                    framingHud = shellCanvas.AddComponent<RunFramingHudController>();
                }

                framingHud.Configure(catalog);
                EditorUtility.SetDirty(framingHud);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteReport(IReadOnlyList<RunFramingDefinition> definitions)
        {
            File.WriteAllText(
                ReportPath,
                "# M39 Story, World Identity, And Run Framing V1\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                $"- Catalog: `{RunFramingCatalogPath}`\n" +
                "- Scope: data-driven world names, short branch/hub/boss/extraction lines, and a top-center run-framing HUD panel.\n" +
                "- Non-goals: no new story progression saves, no combat balance changes, no branch-generation changes, and no ArtPass authority changes.\n\n" +
                "## Worlds\n\n" +
                string.Join("\n", definitions
                    .Where(definition => definition != null)
                    .Select(definition => $"- World {definition.WorldIndex}: {definition.DisplayName} - {definition.Subtitle}")) +
                "\n\n## Runtime Presentation\n\n" +
                "- `BranchSessionController.CreateRunFramingSnapshot()` resolves the current world/phase/seed text.\n" +
                "- `RunFramingHudController` renders the snapshot on `PlatformShellCanvas`, outside `WorldPresentationRoot`.\n" +
                "- The panel is intentionally compact so it adds context without stealing the minimap or combat HUD's job.\n");
        }
    }
}
