using System.IO;
using Hollow.UI.Shell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone42AssetGenerator
    {
        public const string ReportPath = "output/reports/m42_player_build_ux_pickup_clarity.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 42 Assets")]
        public static void Generate()
        {
            Milestone41AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            AssignHudControllersToGameScenes();
            WriteReport();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 42 player-build UX and pickup clarity wiring.");
        }

        private static void AssignHudControllersToGameScenes()
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var shellCanvas = GameObject.Find("PlatformShellCanvas");
                if (shellCanvas == null)
                {
                    throw new MissingReferenceException($"{scenePath} is missing PlatformShellCanvas.");
                }

                if (shellCanvas.GetComponent<PlayerBuildHudController>() == null)
                {
                    shellCanvas.AddComponent<PlayerBuildHudController>();
                }

                if (shellCanvas.GetComponent<PickupRevealController>() == null)
                {
                    shellCanvas.AddComponent<PickupRevealController>();
                }

                EditorUtility.SetDirty(shellCanvas);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteReport()
        {
            File.WriteAllText(
                ReportPath,
                "# M42 Player Build UX + Pickup Clarity\n\n" +
                "- Adds `PlayerBuildHudController` as a left-side always-visible build sidebar.\n" +
                "- Adds `PickupRevealController` as a center-right pickup card/toast layer.\n" +
                "- Keeps minimap and economy/status separated from detailed player-build state.\n" +
                "- Adds saved replacement pickups for weapon, armor, active item, and consumable card swap-back.\n" +
                "- Uses generated glyphs and rarity colors; no final icon art required.\n");
        }
    }
}
