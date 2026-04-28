using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone19Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Combat/EnemyBehaviorId.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/EnemyProjectileController.cs",
            "Assets/_Hollow/Scripts/Hollow.Combat/RoomCombatEncounterContext.cs",
            "Assets/_Hollow/Scripts/Hollow.Branches/EncounterResolver.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone19AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone19Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone19EnemyEncounterContentTests.cs",
            "Docs/Milestone19EnemyEncounterContent.md",
            Milestone19AssetGenerator.EncounterCatalogPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 19 Validation")]
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
                    failures.Add($"Missing M19 file: {file}");
                }
            }

            var enemyCatalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>("Assets/_Hollow/Data/Enemies/EnemyCatalog.asset");
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);
            ValidateEnemies(enemyCatalog, failures);
            ValidateEncounterPlan(encounterCatalog, failures);
            ValidateScenes(encounterCatalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 19 validation passed.");
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

        private static void ValidateEnemies(EnemyCatalog catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("M19 enemy catalog is missing.");
                return;
            }

            ValidateEnemy(catalog, "spawnEnemyNormal", EnemyBehaviorId.Chaser, failures);
            ValidateEnemy(catalog, "spawnEnemyFlying", EnemyBehaviorId.FlyingChaser, failures);
            ValidateEnemy(catalog, "spawnEnemyCharger", EnemyBehaviorId.Charger, failures);
            ValidateEnemy(catalog, "spawnEnemyTurret", EnemyBehaviorId.TurretShooter, failures);
            ValidateEnemy(catalog, "spawnEnemySplitter", EnemyBehaviorId.Splitter, failures);
            ValidateEnemy(catalog, "spawnEnemyBoss", EnemyBehaviorId.BossWarden, failures);
        }

        private static void ValidateEnemy(EnemyCatalog catalog, string spawnKind, EnemyBehaviorId expectedBehavior, List<string> failures)
        {
            var enemy = catalog.Resolve(spawnKind);
            if (enemy == null || enemy.SpawnKind != spawnKind || enemy.BehaviorId != expectedBehavior)
            {
                failures.Add($"M19 enemy catalog must resolve {spawnKind} as {expectedBehavior}.");
            }
        }

        private static void ValidateEncounterPlan(EncounterCatalogDefinition encounterCatalog, List<string> failures)
        {
            if (encounterCatalog == null || encounterCatalog.Encounters.Count < 5 || encounterCatalog.BossEncounter == null)
            {
                failures.Add("M19 encounter catalog must contain standard, reward, origin, and boss encounters.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json");
            var importError = "missing sample room";
            if (catalog == null || settings == null || sample == null || !HollowRuntimeV2Importer.TryImport(sample.text, out var sampleRoom, out importError))
            {
                failures.Add($"M19 could not import branch content: {importError}");
                return;
            }

            var content = BranchSessionContent.Create(sampleRoom, catalog, settings.DefaultSeed, out var contentError);
            if (!string.IsNullOrWhiteSpace(contentError))
            {
                failures.Add($"M19 branch content import error: {contentError}");
                return;
            }

            var graph = BranchGenerator.CreateSeededEncounterBranch(content, settings, settings.DefaultSeed);
            if (graph.BranchId != BranchGenerator.EnemyEncounterBranchId)
            {
                failures.Add("M19 fresh graph must use the M19 branch id.");
            }

            var first = EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed);
            var second = EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed);
            if (Signature(first) != Signature(second))
            {
                failures.Add("M19 encounter plan must be deterministic for the same seed and catalog.");
            }

            var treasure = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Treasure);
            if (treasure != null && first.TryResolve(treasure.Id.Value, out _))
            {
                failures.Add("M19 treasure rooms must not receive combat encounters.");
            }

            var boss = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.Boss);
            if (boss == null || !first.TryResolve(boss.Id.Value, out var bossAssignment) || !bossAssignment.EnemySpawnKinds.Contains("spawnEnemyBoss"))
            {
                failures.Add("M19 boss room must resolve to the Stone Warden encounter.");
            }

            if (first.TryResolve(BranchRoomId.Origin.Value, out _))
            {
                failures.Add("Origin rooms must remain safe starter rooms without combat encounters.");
            }
        }

        private static void ValidateScenes(EncounterCatalogDefinition encounterCatalog, List<string> failures)
        {
            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (branch.EncounterCatalog == null ||
                    (branch.EncounterCatalog != encounterCatalog && !IsM19CompatibleSuccessorCatalog(branch.EncounterCatalog)))
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to the M19 encounter catalog or a compatible successor catalog.");
                }
            }
        }

        private static bool IsM19CompatibleSuccessorCatalog(EncounterCatalogDefinition catalog)
        {
            if (catalog == null || catalog.BossEncounter == null)
            {
                return false;
            }

            var ids = catalog.Encounters
                .Where(encounter => encounter != null)
                .Select(encounter => encounter.EncounterId)
                .ToHashSet();
            return ids.Contains("origin_intro") &&
                   ids.Contains("reward_guard") &&
                   ids.Contains("stone_warden_boss") &&
                   catalog.Encounters.Count >= 5;
        }

        private static string Signature(EncounterPlan plan)
        {
            return string.Join("|", plan.Assignments
                .OrderBy(assignment => assignment.RoomId)
                .Select(assignment => $"{assignment.RoomId}:{assignment.EncounterId}:{string.Join(",", assignment.EnemySpawnKinds)}"));
        }
    }
}
