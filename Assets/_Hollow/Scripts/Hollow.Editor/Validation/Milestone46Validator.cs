using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone46Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/EncounterDirectorProfileDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/EncounterDirectorContext.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone46AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone46Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone46EncounterDirectorDifficultyCurveTests.cs",
            "Docs/Milestone46EncounterDirectorDifficultyCurve.md",
            Milestone46AssetGenerator.EncounterCatalogPath,
            Milestone46AssetGenerator.DirectorProfilePath,
            Milestone46AssetGenerator.ReportPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 46 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M46 file: {file}");
                }
            }

            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath)
                          ?? EncounterDirectorProfileDefinition.CreateRuntimeDefault();
            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone46AssetGenerator.EncounterCatalogPath);
            var roomCatalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            ValidateProfile(profile, failures);
            ValidateEncounterCatalog(catalog, failures);
            ValidateDirectedGraphs(roomCatalog, settings, catalog, profile, failures);
            ValidateSceneWiring(catalog, profile, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 46 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateProfile(EncounterDirectorProfileDefinition profile, List<string> failures)
        {
            if (profile == null)
            {
                failures.Add("M46 requires an encounter director profile.");
                return;
            }

            var expected = new Dictionary<int, int> { [1] = 8, [2] = 10, [3] = 12 };
            foreach (var pair in expected)
            {
                if (profile.WorldConfigFor(pair.Key).TargetRoomCount != pair.Value)
                {
                    failures.Add($"M46 world {pair.Key} must target {pair.Value} rooms.");
                }
            }

            if (profile.MaxNonBossEnemySpawns != 6)
            {
                failures.Add("M46 non-boss spawn cap must be 6.");
            }
        }

        private static void ValidateEncounterCatalog(EncounterCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("Missing M46 encounter catalog.");
                return;
            }

            foreach (var encounterId in Milestone46AssetGenerator.EncounterIds)
            {
                if (catalog.Encounters.All(encounter => encounter == null || encounter.EncounterId != encounterId))
                {
                    failures.Add($"M46 encounter catalog is missing '{encounterId}'.");
                }
            }

            if (catalog.BossEncounter == null || catalog.BossEncounter.EncounterId != "stone_warden_boss")
            {
                failures.Add("M46 boss encounter must remain Stone Warden.");
            }
        }

        private static void ValidateDirectedGraphs(
            BranchRoomTemplateCatalogDefinition roomCatalog,
            BranchGenerationSettingsDefinition settings,
            EncounterCatalogDefinition encounterCatalog,
            EncounterDirectorProfileDefinition profile,
            List<string> failures)
        {
            if (roomCatalog == null || settings == null || encounterCatalog == null)
            {
                failures.Add("M46 requires room catalog, branch settings, and encounter catalog for graph validation.");
                return;
            }

            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "sample room text asset is missing";
            if (sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out importError))
            {
                failures.Add($"M46 could not import sample room: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, roomCatalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M46 branch content import error: {contentError}");
                return;
            }

            var signatures = new HashSet<string>();
            for (var world = 1; world <= 3; world++)
            {
                var seed = RunSeedDeriver.PrologueBranchSeed(15001, world);
                var graph = BranchGenerator.CreateDirectedEncounterBranch(content, settings, profile, world, seed);
                if (graph.BranchId != BranchGenerator.DirectedEncounterBranchId)
                {
                    failures.Add("M46 generated graph must use the directed branch ID.");
                }

                if (graph.Rooms.Count != profile.WorldConfigFor(world).TargetRoomCount)
                {
                    failures.Add($"M46 world {world} graph has {graph.Rooms.Count} rooms; expected {profile.WorldConfigFor(world).TargetRoomCount}.");
                }

                var origin = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Origin);
                if (origin == null || origin.RuntimeRoomAssetId != "combat_macro_single_1x1")
                {
                    failures.Add("M46 origin must use the safe single-room fixture.");
                }

                var first = EncounterResolver.CreateDirectedSeededPlan(graph, encounterCatalog, graph.Seed, world, profile);
                var second = EncounterResolver.CreateDirectedSeededPlan(graph, encounterCatalog, graph.Seed, world, profile);
                if (Signature(first) != Signature(second))
                {
                    failures.Add("M46 directed encounter plan must be deterministic.");
                }

                foreach (var assignment in first.Assignments)
                {
                    var room = graph.Rooms.FirstOrDefault(candidate => candidate.Id.Value == assignment.RoomId);
                    if (room == null)
                    {
                        failures.Add($"M46 encounter assignment references missing room '{assignment.RoomId}'.");
                        continue;
                    }

                    if (room.Role is BranchRoomRole.Origin or BranchRoomRole.Treasure or BranchRoomRole.Secret)
                    {
                        failures.Add($"M46 no-combat room '{assignment.RoomId}' should not receive an encounter.");
                    }

                    if (room.Role != BranchRoomRole.Boss && assignment.EnemySpawnKinds.Count > 6)
                    {
                        failures.Add($"M46 room '{assignment.RoomId}' exceeds the non-boss spawn cap.");
                    }

                    if (room.Role == BranchRoomRole.Boss && assignment.EncounterId != "stone_warden_boss")
                    {
                        failures.Add("M46 boss room must use Stone Warden.");
                    }
                }

                signatures.Add(Signature(first));
            }

            if (signatures.Count < 2)
            {
                failures.Add("M46 worlds should produce distinct directed encounter signatures.");
            }
        }

        private static void ValidateSceneWiring(EncounterCatalogDefinition catalog, EncounterDirectorProfileDefinition profile, List<string> failures)
        {
            if (catalog == null || profile == null)
            {
                return;
            }

            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (!IsM46OrSuccessorEncounterCatalog(branch.EncounterCatalog, catalog))
                {
                    failures.Add($"{scenePath} BranchSessionController must reference the M46 encounter catalog or an accepted successor.");
                }

                if (branch.EncounterDirectorProfile != profile)
                {
                    failures.Add($"{scenePath} BranchSessionController must reference the M46 director profile.");
                }
            }
        }

        private static bool IsM46OrSuccessorEncounterCatalog(EncounterCatalogDefinition actual, EncounterCatalogDefinition m46Catalog)
        {
            if (actual == null)
            {
                return false;
            }

            return actual == m46Catalog || actual.CatalogId == Milestone48AssetGenerator.CatalogId;
        }

        private static string Signature(EncounterPlan plan)
        {
            return string.Join("|", plan.Assignments
                .OrderBy(assignment => assignment.RoomId)
                .Select(assignment => $"{assignment.RoomId}:{assignment.WorldIndex}:{assignment.DifficultyBand}:{assignment.EncounterId}:{string.Join(",", assignment.EnemySpawnKinds)}"));
        }
    }
}
