using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone53AssetGenerator
    {
        public const string BossDirectory = "Assets/_Hollow/Data/Bosses/M53";
        public const string BossCatalogPath = BossDirectory + "/BossCatalog_M53.asset";
        public const string DocsPath = "Docs/Milestone53BossRosterFramework.md";
        public const string ReportPath = "output/reports/m53_boss_roster_framework.md";
        public const string PdfPath = "output/pdf/Hollow_M53_Boss_Roster_Framework.pdf";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 53 Assets")]
        public static void Generate()
        {
            Milestone52AssetGenerator.Generate();
            Directory.CreateDirectory(BossDirectory);
            Directory.CreateDirectory(Milestone16AssetGenerator.ApprovedRoomDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var bosses = GenerateBossCatalog();
            foreach (var boss in bosses)
            {
                WriteBossArenaRoom(boss);
            }

            AssetDatabase.Refresh();
            var roomCatalog = RefreshBranchTemplateCatalog();
            AssignToGameScenes(roomCatalog, AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(BossCatalogPath));
            CuratedRoomDesignerDraftGenerator.Generate();
            WriteDocs(bosses);
            WriteReport(bosses);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 53 boss roster and arenas.");
        }

        public static IReadOnlyList<string> ApprovedBossArenaIds => BossCatalogDefinition
            .CreateRuntimeRoster()
            .Select(boss => boss.Arena.arenaId)
            .ToArray();

        private static BossDefinition[] GenerateBossCatalog()
        {
            var roster = BossCatalogDefinition.CreateRuntimeRoster();
            var savedBosses = new List<BossDefinition>();
            foreach (var runtimeBoss in roster)
            {
                var path = $"{BossDirectory}/Boss_{runtimeBoss.BossId}.asset";
                var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>(path);
                if (boss == null)
                {
                    boss = ScriptableObject.CreateInstance<BossDefinition>();
                    AssetDatabase.CreateAsset(boss, path);
                }

                boss.Configure(
                    runtimeBoss.BossId,
                    runtimeBoss.DisplayName,
                    runtimeBoss.WorldBand,
                    runtimeBoss.BehaviorId,
                    runtimeBoss.MaxHealth,
                    runtimeBoss.SpeedMetersPerSecond,
                    runtimeBoss.ContactDamage,
                    runtimeBoss.ContactCooldownSeconds,
                    runtimeBoss.RadiusMeters,
                    runtimeBoss.ProjectileSpeedMetersPerSecond,
                    runtimeBoss.VisualScale,
                    runtimeBoss.DebugColor,
                    new BossArenaDefinition(runtimeBoss.Arena.arenaId, runtimeBoss.Arena.displayName),
                    runtimeBoss.Phases,
                    runtimeBoss.Attacks);
                EditorUtility.SetDirty(boss);
                savedBosses.Add(boss);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BossCatalogDefinition>(BossCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BossCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, BossCatalogPath);
            }

            catalog.Configure(BossCatalogDefinition.DefaultCatalogId, savedBosses, savedBosses.FirstOrDefault(boss => boss.BossId == "stone_warden"));
            EditorUtility.SetDirty(catalog);
            return savedBosses.ToArray();
        }

        private static void WriteBossArenaRoom(BossDefinition boss)
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, boss.Arena.displayName);
            project.projectId = boss.Arena.arenaId;
            project.displayName = boss.Arena.displayName;
            project.cells.RemoveAll(cell =>
                cell.kind == RoomDesignerCellKinds.Rock ||
                cell.kind == RoomDesignerCellKinds.Hole ||
                cell.kind == RoomDesignerCellKinds.Spike);
            project.markers.Clear();

            var seed = StableHash(boss.BossId);
            AddSafeCellPattern(project, seed);
            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, 0f, 0f, 2f));
            project.markers.Add(new RoomDesignerMarker("spawn_boss_anchor_0", RoomDesignerMarkerKinds.EnemyHeavy, 0f, 0f, -1.5f));
            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, 0f, 0f, 2.5f));
            foreach (var door in project.doorPorts)
            {
                door.state = RoomDesignerDoorKinds.Door;
            }

            var validation = RoomDesignerDraftValidator.Validate(project);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"M53 boss arena '{boss.Arena.arenaId}' is not Room Designer-compatible: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = boss.Arena.arenaId;
            manifest.hollowRuntime.displayName = boss.Arena.displayName;
            manifest.hollowRuntime.roomType = "boss";
            manifest.hollowRuntime.rewardType = "boss-arena";
            manifest.hollowRuntime.prototypeStatus = "m53-approved-boss-arena";
            var path = $"{Milestone16AssetGenerator.ApprovedRoomDirectory}/{boss.Arena.arenaId}.hollowruntime.json";
            File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void AddSafeCellPattern(RoomDesignerProject project, int seed)
        {
            var rockPatterns = new[]
            {
                new[] { V(-4, -2), V(4, -2), V(-4, 2), V(4, 2) },
                new[] { V(-5, 0), V(5, 0), V(-1, -2), V(1, 2) },
                new[] { V(-3, -2), V(3, -2), V(-3, 2), V(3, 2) },
                new[] { V(-5, -2), V(5, 2), V(0, -3), V(0, 3) }
            };
            var spikePatterns = new[]
            {
                Array.Empty<Vector2Int>(),
                new[] { V(-2, 0), V(2, 0) },
                new[] { V(-1, -2), V(1, -2), V(-1, 2), V(1, 2) },
                new[] { V(-5, 1), V(5, -1) }
            };
            foreach (var rock in rockPatterns[Math.Abs(seed) % rockPatterns.Length])
            {
                project.cells.Add(new RoomDesignerCell(rock.x, rock.y, 0, RoomDesignerCellKinds.Rock));
            }

            foreach (var spike in spikePatterns[Math.Abs(seed / 7) % spikePatterns.Length])
            {
                project.cells.Add(new RoomDesignerCell(spike.x, spike.y, 0, RoomDesignerCellKinds.Spike));
            }
        }

        private static BranchRoomTemplateCatalogDefinition RefreshBranchTemplateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                throw new FileNotFoundException($"Missing branch room template catalog at {Milestone14AssetGenerator.CatalogPath}.");
            }

            var approvedTemplates = Milestone16AssetGenerator.LoadApprovedTemplates();
            catalog.Configure(catalog.Single1x1, catalog.Wide2x1, catalog.Tall1x2, catalog.Block2x2, catalog.L3Cell, catalog.DefaultSeed, approvedTemplates);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AssignToGameScenes(BranchRoomTemplateCatalogDefinition roomCatalog, BossCatalogDefinition bossCatalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureTemplateCatalog(roomCatalog, roomCatalog.DefaultSeed);
                branch.ConfigureBossCatalog(bossCatalog);
                var combat = Object.FindFirstObjectByType<RoomCombatController>();
                if (combat != null)
                {
                    combat.ConfigureBossCatalog(bossCatalog);
                    EditorUtility.SetDirty(combat);
                }

                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteDocs(IEnumerable<BossDefinition> bosses)
        {
            File.WriteAllText(DocsPath, @"# M53: Boss Roster + Boss Framework V1

M53 adds a data-driven boss roster and top-center boss HUD.

- Bosses use fixed HP from 20-50 with no hidden world scaling.
- Boss selection is deterministic from run/challenge seed and world band.
- Each boss owns a Room Designer-compatible approved arena.
- Boss rewards remain on the existing boss reward path.
- Boss projectiles are capped at 24 active boss-owned projectiles.
- Boss-summoned minions use existing enemy kinds and count for room clear.
- Boss Lab is represented by the generated boss catalog and validator routes; any boss can be launched by selecting its `bossId` and arena.

Roster:
" + string.Join("\n", bosses.Select(boss => $"- W{(int)boss.WorldBand}: {boss.DisplayName} (`{boss.BossId}`), HP {boss.MaxHealth}, arena `{boss.Arena.arenaId}`.")) + "\n");
        }

        private static void WriteReport(IEnumerable<BossDefinition> bosses)
        {
            File.WriteAllText(ReportPath, @"# M53 Boss Roster Framework Report

" + string.Join("\n", bosses.Select(boss => $"- `{boss.BossId}`: {boss.DisplayName}, W{(int)boss.WorldBand}, HP {boss.MaxHealth}, behavior `{boss.BehaviorId}`, arena `{boss.Arena.arenaId}`.")) + $@"

Boss catalog: `{BossCatalogPath}`
Boss arena source: `{Milestone16AssetGenerator.ApprovedRoomDirectory}`
PDF checklist: `{PdfPath}`
");
        }

        private static Vector2Int V(int x, int z)
        {
            return new Vector2Int(x, z);
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
