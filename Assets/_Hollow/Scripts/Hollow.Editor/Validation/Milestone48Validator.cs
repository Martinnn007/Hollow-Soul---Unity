using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone48Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone48AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone48Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone48ContentExpansionLockTests.cs",
            "Docs/Milestone48ContentExpansionLockV1.md",
            Milestone48AssetGenerator.EncounterCatalogPath,
            Milestone48AssetGenerator.ReportJsonPath,
            Milestone48AssetGenerator.ReportMarkdownPath,
            Milestone48AssetGenerator.PdfPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        private static readonly HashSet<string> AllowedSpawnKinds = new(StringComparer.Ordinal)
        {
            "spawnEnemyNormal",
            "spawnEnemyFlying",
            "spawnEnemyFast",
            "spawnEnemyHeavy",
            "spawnEnemyCharger",
            "spawnEnemyTurret",
            "spawnEnemySplitter",
            "spawnEnemySpittingPod",
            "spawnEnemyRat",
            "spawnEnemySpider",
            "spawnEnemyBoss"
        };

        private static readonly Dictionary<string, RoomFootprintShape> ExpectedRoomShapes = new()
        {
            ["approved_cover_arena_single_1x1"] = RoomFootprintShape.Single1x1,
            ["approved_pressure_lane_wide_2x1"] = RoomFootprintShape.Wide2x1,
            ["approved_turret_spire_tall_1x2"] = RoomFootprintShape.Tall1x2,
            ["approved_hazard_quadrant_block_2x2"] = RoomFootprintShape.Block2x2,
            ["approved_ambush_l_3cell"] = RoomFootprintShape.L3Cell
        };

        [MenuItem("Hollow/Validation/Run Milestone 48 Validation")]
        public static bool Validate()
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M48 file: {file}");
                }
            }

            ValidateRooms(failures);
            ValidateCuratedDrafts(failures);
            ValidateEncounterCatalog(failures);
            ValidateBranchCoverage(failures);
            ValidateSceneWiring(failures);

            if (failures.Count == 0)
            {
                Debug.Log("Milestone 48 validation passed.");
                return true;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            return false;
        }

        private static void ValidateRooms(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                failures.Add("M48 requires the branch room template catalog.");
                return;
            }

            var approvedPaths = catalog.AdditionalTemplates.Select(AssetDatabase.GetAssetPath).ToHashSet();
            foreach (var roomId in Milestone48AssetGenerator.ApprovedRoomIds)
            {
                var expectedPath = $"{Milestone16AssetGenerator.ApprovedRoomDirectory}/{roomId}.hollowruntime.json";
                if (!File.Exists(expectedPath))
                {
                    failures.Add($"Missing M48 approved room JSON: {expectedPath}");
                    continue;
                }

                if (!approvedPaths.Contains(expectedPath))
                {
                    failures.Add($"M48 approved room '{roomId}' is missing from the branch room catalog.");
                }

                if (!HollowRuntimeV2Importer.TryImport(File.ReadAllText(expectedPath), out var room, out var error))
                {
                    failures.Add($"M48 room '{roomId}' failed HollowRuntime V2 import: {error}");
                    continue;
                }

                if (!ExpectedRoomShapes.TryGetValue(roomId, out var expectedShape) ||
                    RoomFootprintShapeUtility.Classify(room.Footprint) != expectedShape)
                {
                    failures.Add($"M48 room '{roomId}' has the wrong footprint shape.");
                }

                if (room.SafeStart?.position == null || room.DoorPorts.Count == 0 || room.ItemSpawns.Count == 0)
                {
                    failures.Add($"M48 room '{roomId}' must preserve safe start, door ports, and reward marker.");
                }

                if (room.EnemySpawns.Count < 4)
                {
                    failures.Add($"M48 room '{roomId}' must contain at least four enemy anchors.");
                }

                if (room.Hazards.Count + room.InteractiveObjects.Count == 0)
                {
                    failures.Add($"M48 room '{roomId}' must contain authored hazards or interactive objects.");
                }
            }

            var importReport = ApprovedDesignerRoomImporter.ImportApprovedRooms(catalog.AdditionalTemplates);
            foreach (var error in importReport.Errors.Where(error => Milestone48AssetGenerator.ApprovedRoomIds.Any(error.Contains)))
            {
                failures.Add($"M48 approved-room import error: {error}");
            }
        }

        private static void ValidateCuratedDrafts(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomDesignerCuratedDraftCatalogDefinition>(CuratedRoomDesignerDraftGenerator.CuratedDraftCatalogPath);
            if (catalog == null)
            {
                failures.Add("M48 requires the curated Room Designer draft catalog.");
                return;
            }

            var draftIds = catalog.CuratedDrafts
                .Where(asset => asset != null)
                .Select(asset => JsonUtility.FromJson<RoomDesignerProject>(asset.text))
                .Where(project => project != null)
                .Select(project => project.projectId)
                .ToHashSet();

            foreach (var roomId in Milestone48AssetGenerator.ApprovedRoomIds)
            {
                var runtimePath = $"{Milestone16AssetGenerator.ApprovedRoomDirectory}/{roomId}.hollowruntime.json";
                if (!File.Exists(runtimePath))
                {
                    continue;
                }

                var expectedProjectId = CuratedRoomDesignerDraftGenerator.CuratedProjectIdForRuntimeRoom(File.ReadAllText(runtimePath));
                var expectedPath = $"{CuratedRoomDesignerDraftGenerator.CuratedDraftDirectory}/{expectedProjectId}.roomdesigner.json";
                if (!File.Exists(expectedPath) || !draftIds.Contains(expectedProjectId))
                {
                    failures.Add($"M48 curated Room Designer draft is missing for '{roomId}'.");
                    continue;
                }

                var project = JsonUtility.FromJson<RoomDesignerProject>(File.ReadAllText(expectedPath));
                var report = RoomDesignerDraftValidator.Validate(project);
                foreach (var error in report.Errors)
                {
                    failures.Add($"M48 curated draft '{expectedProjectId}' validation error: {error}");
                }

                if (!HollowRuntimeV2Importer.TryImport(RoomDesignerCompiler.ExportRuntimeJson(project), out _, out var importError))
                {
                    failures.Add($"M48 curated draft '{expectedProjectId}' failed runtime roundtrip: {importError}");
                }
            }
        }

        private static void ValidateEncounterCatalog(List<string> failures)
        {
            var m46Catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone46AssetGenerator.EncounterCatalogPath);
            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone48AssetGenerator.EncounterCatalogPath);
            if (m46Catalog == null || catalog == null)
            {
                failures.Add("M48 requires both M46 and M48 encounter catalogs.");
                return;
            }

            if (catalog.CatalogId != Milestone48AssetGenerator.CatalogId)
            {
                failures.Add("M48 encounter catalog must use the M48 successor catalog id.");
            }

            foreach (var encounterId in Milestone46AssetGenerator.EncounterIds.Concat(Milestone48AssetGenerator.EncounterIds))
            {
                if (catalog.Encounters.All(encounter => encounter == null || encounter.EncounterId != encounterId))
                {
                    failures.Add($"M48 encounter catalog is missing '{encounterId}'.");
                }
            }

            if (catalog.BossEncounter == null || catalog.BossEncounter.EncounterId != "stone_warden_boss")
            {
                failures.Add("M48 boss encounter must remain Stone Warden.");
            }

            foreach (var encounter in catalog.Encounters.Where(encounter => encounter != null))
            {
                var expanded = encounter.ExpandSpawnKinds();
                foreach (var kind in expanded)
                {
                    if (!AllowedSpawnKinds.Contains(kind))
                    {
                        failures.Add($"M48 encounter '{encounter.EncounterId}' uses unknown spawn kind '{kind}'.");
                    }
                }

                if (encounter.RoomRole != BranchRoomRole.Boss && expanded.Count > 6)
                {
                    failures.Add($"M48 non-boss encounter '{encounter.EncounterId}' exceeds the six-enemy cap.");
                }
            }
        }

        private static void ValidateBranchCoverage(List<string> failures)
        {
            var roomCatalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone48AssetGenerator.EncounterCatalogPath);
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath)
                          ?? EncounterDirectorProfileDefinition.CreateRuntimeDefault();
            if (roomCatalog == null || settings == null || encounterCatalog == null)
            {
                failures.Add("M48 branch coverage requires room catalog, branch settings, and encounter catalog.");
                return;
            }

            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "sample room text asset is missing";
            if (sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out importError))
            {
                failures.Add($"M48 could not import legacy sample room: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, roomCatalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M48 branch content import error: {contentError}");
                return;
            }

            if (!Milestone48AssetGenerator.ApprovedRoomIds.All(id => content.ApprovedRoomPool.ContainsKey(id)))
            {
                failures.Add("M48 rooms must enter BranchSessionContent.ApprovedRoomPool.");
            }

            var selectedM48Rooms = new HashSet<string>();
            var selectedM48Encounters = new HashSet<string>();
            for (var seed = 48001; seed < 48060; seed++)
            {
                var world = 1 + (seed % 3);
                var graph = BranchGenerator.CreateDirectedEncounterBranch(content, settings, profile, world, seed);
                if (!BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError))
                {
                    failures.Add($"M48 generated branch special-room topology failed for seed {seed}: {topologyError}");
                    return;
                }

                foreach (var room in graph.Rooms.Where(room => Milestone48AssetGenerator.ApprovedRoomIds.Contains(room.RuntimeRoomAssetId)))
                {
                    selectedM48Rooms.Add(room.RuntimeRoomAssetId);
                }

                var plan = EncounterResolver.CreateDirectedSeededPlan(graph, encounterCatalog, graph.Seed, world, profile);
                foreach (var assignment in plan.Assignments.Where(assignment => Milestone48AssetGenerator.EncounterIds.Contains(assignment.EncounterId)))
                {
                    selectedM48Encounters.Add(assignment.EncounterId);
                }
            }

            if (selectedM48Rooms.Count < 3)
            {
                failures.Add($"M48 seed sweep should select several new rooms; selected {selectedM48Rooms.Count}.");
            }

            if (selectedM48Encounters.Count < 4)
            {
                failures.Add($"M48 seed sweep should select several new encounters; selected {selectedM48Encounters.Count}.");
            }
        }

        private static void ValidateSceneWiring(List<string> failures)
        {
            var roomCatalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone48AssetGenerator.EncounterCatalogPath);
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindAnyObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (branch.BranchRoomTemplateCatalog != roomCatalog || branch.EncounterCatalog != encounterCatalog)
                {
                    failures.Add($"{scenePath} BranchSessionController must reference the M48 room pool and encounter catalog.");
                }

                if (profile != null && branch.EncounterDirectorProfile != profile)
                {
                    failures.Add($"{scenePath} BranchSessionController must preserve the M46 director profile.");
                }
            }
        }
    }
}
