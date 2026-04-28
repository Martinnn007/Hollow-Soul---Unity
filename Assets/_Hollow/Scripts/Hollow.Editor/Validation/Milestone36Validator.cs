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
    public static class Milestone36Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone36AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone36Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone36RoomEncounterContentExpansionTests.cs",
            "Docs/Milestone36RoomEncounterContentExpansion.md",
            Milestone36AssetGenerator.EncounterCatalogPath,
            Milestone36AssetGenerator.BaselineReportPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 36 Validation")]
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
                    failures.Add($"Missing M36 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone36AssetGenerator.EncounterCatalogPath);

            ValidateApprovedRooms(catalog, settings, failures);
            ValidateEncounterCatalog(encounterCatalog, catalog, settings, failures);
            ValidateSceneWiring(catalog, settings, encounterCatalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 36 validation passed.");
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

        private static void ValidateApprovedRooms(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("M36 requires the M14/M16 branch room template catalog.");
                return;
            }

            var approvedPaths = catalog.AdditionalTemplates.Select(AssetDatabase.GetAssetPath).ToArray();
            foreach (var roomId in Milestone36AssetGenerator.ApprovedRoomIds)
            {
                var expectedPath = $"{Milestone16AssetGenerator.ApprovedRoomDirectory}/{roomId}.hollowruntime.json";
                if (!approvedPaths.Contains(expectedPath))
                {
                    failures.Add($"M36 approved room '{roomId}' is missing from the branch room catalog.");
                }
            }

            var report = ApprovedDesignerRoomImporter.ImportApprovedRooms(catalog.AdditionalTemplates);
            foreach (var error in report.Errors)
            {
                failures.Add($"M36 approved room import error: {error}");
            }

            var m36Rooms = report.ValidRooms
                .Where(room => Milestone36AssetGenerator.ApprovedRoomIds.Contains(room.Id))
                .ToArray();
            if (m36Rooms.Length != Milestone36AssetGenerator.ApprovedRoomIds.Count)
            {
                failures.Add("M36 must import all five generated approved rooms.");
            }

            var footprintShapes = m36Rooms
                .Select(room => RoomFootprintShapeUtility.Classify(room.Footprint))
                .Distinct()
                .ToHashSet();
            foreach (var shape in new[] { RoomFootprintShape.Single1x1, RoomFootprintShape.Wide2x1, RoomFootprintShape.Tall1x2, RoomFootprintShape.Block2x2, RoomFootprintShape.L3Cell })
            {
                if (!footprintShapes.Contains(shape))
                {
                    failures.Add($"M36 approved rooms must include footprint {shape}.");
                }
            }

            if (m36Rooms.Any(room => room.EnemySpawns.Count < 4 || room.DoorPorts.Count == 0 || room.Layout.Obstacles.Count < 4))
            {
                failures.Add("M36 approved rooms must include at least four enemy anchors, enabled doors, and obstacle structure.");
            }

            if (settings != null)
            {
                ValidateBranchCanSelectApprovedRooms(catalog, settings, failures);
            }
        }

        private static void ValidateBranchCanSelectApprovedRooms(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "missing legacy sample room";
            if (sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out importError))
            {
                failures.Add($"M36 could not import legacy sample room: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M36 branch content import error: {contentError}");
                return;
            }

            if (!Milestone36AssetGenerator.ApprovedRoomIds.All(id => content.ApprovedRoomPool.ContainsKey(id)))
            {
                failures.Add("M36 approved rooms must enter BranchSessionContent.ApprovedRoomPool.");
            }

            var selectedApproved = false;
            for (var seed = 36001; seed < 36025; seed++)
            {
                var graph = BranchGenerator.CreateSeededBranchFeatures(content, settings, seed);
                if (!BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError))
                {
                    failures.Add($"M36 generated branch special-room topology failed for seed {seed}: {topologyError}");
                    return;
                }

                if (graph.Rooms.Any(room => Milestone36AssetGenerator.ApprovedRoomIds.Contains(room.RuntimeRoomAssetId)))
                {
                    selectedApproved = true;
                    break;
                }
            }

            if (!selectedApproved)
            {
                failures.Add("M36 seeded generation did not select any approved room across validation seeds.");
            }
        }

        private static void ValidateEncounterCatalog(EncounterCatalogDefinition encounterCatalog, BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, List<string> failures)
        {
            if (encounterCatalog == null)
            {
                failures.Add("Missing M36 encounter catalog.");
                return;
            }

            if (encounterCatalog.Encounters.Count < 10 || encounterCatalog.BossEncounter == null)
            {
                failures.Add("M36 encounter catalog must expand origin, combat, reward, macro, and boss encounters.");
            }

            foreach (var encounterId in Milestone36AssetGenerator.EncounterIds)
            {
                if (encounterCatalog.Encounters.All(encounter => encounter == null || encounter.EncounterId != encounterId))
                {
                    failures.Add($"M36 encounter catalog is missing encounter '{encounterId}'.");
                }
            }

            if (catalog == null || settings == null)
            {
                return;
            }

            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            if (sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out _))
            {
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                return;
            }

            var signatures = new HashSet<string>();
            var encounterIds = new HashSet<string>();
            for (var seed = 36001; seed < 36008; seed++)
            {
                var graph = BranchGenerator.CreateSeededBranchFeatures(content, settings, seed);
                var first = EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed);
                var second = EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed);
                if (Signature(first) != Signature(second))
                {
                    failures.Add("M36 encounter plan must be deterministic for the same graph, seed, and catalog.");
                    return;
                }

                signatures.Add(Signature(first));
                foreach (var assignment in first.Assignments)
                {
                    encounterIds.Add(assignment.EncounterId);
                }
            }

            if (signatures.Count < 2 || encounterIds.Count < 4)
            {
                failures.Add("M36 encounter catalog should produce varied seeded encounter assignments across validation seeds.");
            }
        }

        private static void ValidateSceneWiring(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, EncounterCatalogDefinition encounterCatalog, List<string> failures)
        {
            if (catalog == null || encounterCatalog == null)
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

                if (branch.BranchRoomTemplateCatalog != catalog || branch.BranchGenerationSettings != settings)
                {
                    failures.Add($"{scenePath} BranchSessionController must reference the M36-updated room catalog/settings.");
                }

                if (branch.EncounterCatalog != encounterCatalog)
                {
                    failures.Add($"{scenePath} BranchSessionController must reference the M36 encounter catalog.");
                }
            }
        }

        private static string Signature(EncounterPlan plan)
        {
            return string.Join("|", plan.Assignments
                .OrderBy(assignment => assignment.RoomId)
                .Select(assignment => $"{assignment.RoomId}:{assignment.EncounterId}:{string.Join(",", assignment.EnemySpawnKinds)}"));
        }
    }
}
