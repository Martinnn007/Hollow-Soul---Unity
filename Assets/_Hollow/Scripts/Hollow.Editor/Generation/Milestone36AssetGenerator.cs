using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone36AssetGenerator
    {
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M36";
        public const string EncounterCatalogPath = EncounterDirectory + "/EncounterCatalog_M36.asset";
        public const string BaselineReportPath = "output/reports/m36_room_encounter_content_expansion.md";

        private static readonly ApprovedRoomSpec[] ApprovedRoomSpecs =
        {
            new(
                RoomDesignerFootprintPreset.Single1x1,
                "approved_crossroads_single_1x1",
                "Approved Crossroads 1x1",
                new[] { new Vector2Int(-3, -2), new Vector2Int(3, -2), new Vector2Int(-3, 2), new Vector2Int(3, 2), new Vector2Int(0, -1), new Vector2Int(0, 1) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyNormal, -5, -2), Spawn(RoomDesignerMarkerKinds.EnemyFast, 5, -2), Spawn(RoomDesignerMarkerKinds.EnemyFlying, -5, 2), Spawn(RoomDesignerMarkerKinds.EnemyHeavy, 5, 2) },
                new Vector2Int(0, 0),
                new Vector2Int(0, 3)),
            new(
                RoomDesignerFootprintPreset.Wide2x1,
                "approved_lane_wide_2x1",
                "Approved Twin Lane 2x1",
                new[] { new Vector2Int(-8, -2), new Vector2Int(-6, 1), new Vector2Int(-3, -1), new Vector2Int(2, 1), new Vector2Int(5, -2), new Vector2Int(8, 2) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyCharger, -10, -2), Spawn(RoomDesignerMarkerKinds.EnemyTurret, -2, 2), Spawn(RoomDesignerMarkerKinds.EnemyFast, 4, -2), Spawn(RoomDesignerMarkerKinds.EnemyFlying, 10, 2), Spawn(RoomDesignerMarkerKinds.EnemyNormal, 7, 0) },
                new Vector2Int(-9, 0),
                new Vector2Int(9, 0)),
            new(
                RoomDesignerFootprintPreset.Tall1x2,
                "approved_watchtower_tall_1x2",
                "Approved Watchtower 1x2",
                new[] { new Vector2Int(-4, -5), new Vector2Int(4, -5), new Vector2Int(-2, -1), new Vector2Int(2, 1), new Vector2Int(-4, 5), new Vector2Int(4, 5) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyTurret, -5, -4), Spawn(RoomDesignerMarkerKinds.EnemyFlying, 5, -3), Spawn(RoomDesignerMarkerKinds.EnemySplitter, -4, 3), Spawn(RoomDesignerMarkerKinds.EnemyFast, 4, 5), Spawn(RoomDesignerMarkerKinds.EnemyNormal, 0, 1) },
                new Vector2Int(0, -4),
                new Vector2Int(0, 5)),
            new(
                RoomDesignerFootprintPreset.Block2x2,
                "approved_quadrant_block_2x2",
                "Approved Quadrant Block 2x2",
                new[] { new Vector2Int(-8, -5), new Vector2Int(-2, -5), new Vector2Int(4, -4), new Vector2Int(9, -1), new Vector2Int(-9, 2), new Vector2Int(-4, 5), new Vector2Int(3, 3), new Vector2Int(8, 5) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyHeavy, -10, -5), Spawn(RoomDesignerMarkerKinds.EnemyCharger, 8, -4), Spawn(RoomDesignerMarkerKinds.EnemyTurret, -8, 4), Spawn(RoomDesignerMarkerKinds.EnemySplitter, 7, 4), Spawn(RoomDesignerMarkerKinds.EnemyFlying, 0, -1), Spawn(RoomDesignerMarkerKinds.EnemyNormal, 0, 3) },
                new Vector2Int(-4, -3),
                new Vector2Int(5, 4)),
            new(
                RoomDesignerFootprintPreset.L3Cell,
                "approved_broken_l_3cell",
                "Approved Broken L 3-Cell",
                new[] { new Vector2Int(-10, -5), new Vector2Int(-5, -2), new Vector2Int(0, -5), new Vector2Int(6, -3), new Vector2Int(-10, 2), new Vector2Int(-4, 4), new Vector2Int(-1, 0) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemySplitter, -11, -4), Spawn(RoomDesignerMarkerKinds.EnemyTurret, 8, -4), Spawn(RoomDesignerMarkerKinds.EnemyCharger, -9, 4), Spawn(RoomDesignerMarkerKinds.EnemyFlying, -2, -5), Spawn(RoomDesignerMarkerKinds.EnemyNormal, -3, 3) },
                new Vector2Int(-6, -2),
                new Vector2Int(-9, 5))
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 36 Assets")]
        public static void Generate()
        {
            Milestone35AssetGenerator.Generate();
            Directory.CreateDirectory(Milestone16AssetGenerator.ApprovedRoomDirectory);
            Directory.CreateDirectory(EncounterDirectory);

            foreach (var spec in ApprovedRoomSpecs)
            {
                WriteApprovedRoom(spec);
            }

            AssetDatabase.Refresh();
            var catalog = RefreshBranchTemplateCatalog();
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var encounterCatalog = GenerateEncounterCatalog();
            AssignToGameScenes(catalog, settings, encounterCatalog);
            WriteReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 36 content expansion: {ApprovedRoomSpecs.Length} approved rooms and {encounterCatalog.Encounters.Count} encounter definitions.");
        }

        public static IReadOnlyList<string> ApprovedRoomIds => ApprovedRoomSpecs.Select(spec => spec.RoomId).ToArray();

        public static IReadOnlyList<string> EncounterIds => new[]
        {
            "origin_intro",
            "origin_crossfire",
            "skirmish_chasers",
            "lane_chargers",
            "turret_crossfire",
            "splitter_brood",
            "macro_mixup",
            "reward_guard",
            "reward_watchers",
            "reward_brood_guard",
            "stone_warden_boss"
        };

        private static void WriteApprovedRoom(ApprovedRoomSpec spec)
        {
            var project = RoomDesignerProject.CreateDefault(spec.Preset, spec.DisplayName);
            project.projectId = spec.RoomId;
            project.displayName = spec.DisplayName;
            project.cells.RemoveAll(cell => cell.kind == RoomDesignerCellKinds.Rock || cell.kind == RoomDesignerCellKinds.Hole);
            project.markers.Clear();

            foreach (var rock in spec.Rocks)
            {
                AddRock(project, rock);
            }

            var safeStart = ClampToRoom(spec.Preset, spec.SafeStart);
            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, safeStart.x, 0f, safeStart.y));

            for (var index = 0; index < spec.Enemies.Length; index++)
            {
                var spawn = spec.Enemies[index];
                var position = ClampToRoom(spec.Preset, new Vector2Int(spawn.X, spawn.Z));
                project.markers.Add(new RoomDesignerMarker($"spawn_enemy_{index}", spawn.Kind, position.x, 0f, position.y));
            }

            var reward = ClampToRoom(spec.Preset, spec.Reward);
            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, reward.x, 0f, reward.y));

            foreach (var door in project.doorPorts)
            {
                door.state = RoomDesignerDoorKinds.Door;
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = spec.RoomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = "approved-content";
            manifest.hollowRuntime.prototypeStatus = "m36-approved";
            var path = $"{Milestone16AssetGenerator.ApprovedRoomDirectory}/{spec.RoomId}.hollowruntime.json";
            File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static BranchRoomTemplateCatalogDefinition RefreshBranchTemplateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                throw new FileNotFoundException($"Missing branch template catalog at {Milestone14AssetGenerator.CatalogPath}.");
            }

            var approvedTemplates = Milestone16AssetGenerator.LoadApprovedTemplates();
            catalog.Configure(
                catalog.Single1x1,
                catalog.Wide2x1,
                catalog.Tall1x2,
                catalog.Block2x2,
                catalog.L3Cell,
                catalog.DefaultSeed,
                approvedTemplates);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static EncounterCatalogDefinition GenerateEncounterCatalog()
        {
            var originIntro = SaveEncounter("Encounter_OriginIntro.asset", "origin_intro", "Origin Intro", BranchRoomRole.Origin, 0, 99, 1, 1, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyNormal", 1),
                new EncounterSpawnEntry("spawnEnemyFast", 1)
            });
            var originCrossfire = SaveEncounter("Encounter_OriginCrossfire.asset", "origin_crossfire", "Origin Crossfire", BranchRoomRole.Origin, 0, 99, 2, 99, 1, new[]
            {
                new EncounterSpawnEntry("spawnEnemyNormal", 2),
                new EncounterSpawnEntry("spawnEnemyFlying", 1),
                new EncounterSpawnEntry("spawnEnemyTurret", 1)
            });
            var skirmish = SaveEncounter("Encounter_SkirmishChasers.asset", "skirmish_chasers", "Skirmish Chasers", BranchRoomRole.Combat, 1, 99, 1, 2, 3, new[]
            {
                new EncounterSpawnEntry("spawnEnemyNormal", 2),
                new EncounterSpawnEntry("spawnEnemyFast", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var laneChargers = SaveEncounter("Encounter_LaneChargers.asset", "lane_chargers", "Lane Chargers", BranchRoomRole.Combat, 1, 99, 2, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyCharger", 2),
                new EncounterSpawnEntry("spawnEnemyNormal", 2),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var turretCrossfire = SaveEncounter("Encounter_TurretCrossfire.asset", "turret_crossfire", "Turret Crossfire", BranchRoomRole.Combat, 1, 99, 2, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 2),
                new EncounterSpawnEntry("spawnEnemyFast", 2),
                new EncounterSpawnEntry("spawnEnemyNormal", 1)
            });
            var splitterBrood = SaveEncounter("Encounter_SplitterBrood.asset", "splitter_brood", "Splitter Brood", BranchRoomRole.Combat, 2, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemySplitter", 2),
                new EncounterSpawnEntry("spawnEnemyNormal", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var macroMixup = SaveEncounter("Encounter_MacroMixup.asset", "macro_mixup", "Macro Mixup", BranchRoomRole.Combat, 2, 99, 3, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyHeavy", 1),
                new EncounterSpawnEntry("spawnEnemyCharger", 1),
                new EncounterSpawnEntry("spawnEnemyTurret", 1),
                new EncounterSpawnEntry("spawnEnemySplitter", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var rewardGuard = SaveEncounter("Encounter_RewardGuard.asset", "reward_guard", "Reward Guard", BranchRoomRole.Reward, 1, 99, 1, 99, 3, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 1),
                new EncounterSpawnEntry("spawnEnemyCharger", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var rewardWatchers = SaveEncounter("Encounter_RewardWatchers.asset", "reward_watchers", "Reward Watchers", BranchRoomRole.Reward, 1, 99, 2, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemyTurret", 2),
                new EncounterSpawnEntry("spawnEnemyNormal", 1),
                new EncounterSpawnEntry("spawnEnemyFast", 1)
            });
            var rewardBrood = SaveEncounter("Encounter_RewardBroodGuard.asset", "reward_brood_guard", "Reward Brood Guard", BranchRoomRole.Reward, 2, 99, 1, 99, 2, new[]
            {
                new EncounterSpawnEntry("spawnEnemySplitter", 1),
                new EncounterSpawnEntry("spawnEnemyHeavy", 1),
                new EncounterSpawnEntry("spawnEnemyFlying", 1)
            });
            var boss = SaveEncounter("Encounter_StoneWarden.asset", "stone_warden_boss", "Stone Warden", BranchRoomRole.Boss, 1, 99, 1, 99, 1, new[]
            {
                new EncounterSpawnEntry("spawnEnemyBoss", 1)
            });

            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(EncounterCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EncounterCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, EncounterCatalogPath);
            }

            catalog.Configure("m36_encounter_catalog_v1", new[] { originIntro, originCrossfire, skirmish, laneChargers, turretCrossfire, splitterBrood, macroMixup, rewardGuard, rewardWatchers, rewardBrood, boss }, boss);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static EncounterDefinition SaveEncounter(
            string fileName,
            string encounterId,
            string displayName,
            BranchRoomRole role,
            int minDifficultyBand,
            int maxDifficultyBand,
            int minFootprintCells,
            int maxFootprintCells,
            int weight,
            IEnumerable<EncounterSpawnEntry> spawns)
        {
            var path = $"{EncounterDirectory}/{fileName}";
            var encounter = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(path);
            if (encounter == null)
            {
                encounter = ScriptableObject.CreateInstance<EncounterDefinition>();
                AssetDatabase.CreateAsset(encounter, path);
            }

            encounter.Configure(encounterId, displayName, role, minDifficultyBand, maxDifficultyBand, minFootprintCells, maxFootprintCells, weight, spawns);
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static void AssignToGameScenes(BranchRoomTemplateCatalogDefinition catalog, BranchGenerationSettingsDefinition settings, EncounterCatalogDefinition encounterCatalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureTemplateCatalog(catalog, settings != null ? settings.DefaultSeed : catalog.DefaultSeed);
                if (settings != null)
                {
                    branch.ConfigureGenerationSettings(settings);
                }

                branch.ConfigureEncounterCatalog(encounterCatalog);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteReport()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BaselineReportPath) ?? "output/reports");
            File.WriteAllText(
                BaselineReportPath,
                "# M36 Room and Encounter Content Expansion\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                "- Scope: five approved branch-ready room templates, expanded encounter catalog, scene wiring, and validation hooks.\n" +
                "- Approved rooms: Crossroads 1x1, Twin Lane 2x1, Watchtower 1x2, Quadrant Block 2x2, Broken L 3-cell.\n" +
                "- Encounter catalog: M36 successor catalog with origin, combat, reward, macro-footprint, and boss encounters.\n" +
                "- Verification: run Milestone36Validator and the M32 QA gate.\n");
        }

        private static void AddRock(RoomDesignerProject project, Vector2Int position)
        {
            var clamped = ClampToRoom(project.footprintPreset, position);
            if (project.markers.Any(marker => Mathf.RoundToInt(marker.x) == clamped.x && Mathf.RoundToInt(marker.z) == clamped.y))
            {
                return;
            }

            project.cells.Add(new RoomDesignerCell(clamped.x, clamped.y, 0, RoomDesignerCellKinds.Rock));
        }

        private static Vector2Int ClampToRoom(RoomDesignerFootprintPreset preset, Vector2Int position)
        {
            return RoomDesignerFootprintUtility.ContainsTile(preset, position.x, position.y)
                ? position
                : RoomDesignerFootprintUtility.NearestContainedTile(preset, position.x, position.y);
        }

        private static SpawnSpec Spawn(string kind, int x, int z)
        {
            return new SpawnSpec(kind, x, z);
        }

        private readonly struct ApprovedRoomSpec
        {
            public ApprovedRoomSpec(
                RoomDesignerFootprintPreset preset,
                string roomId,
                string displayName,
                IReadOnlyList<Vector2Int> rocks,
                IReadOnlyList<SpawnSpec> enemies,
                Vector2Int safeStart,
                Vector2Int reward)
            {
                Preset = preset;
                RoomId = roomId;
                DisplayName = displayName;
                Rocks = rocks?.ToArray() ?? Array.Empty<Vector2Int>();
                Enemies = enemies?.ToArray() ?? Array.Empty<SpawnSpec>();
                SafeStart = safeStart;
                Reward = reward;
            }

            public RoomDesignerFootprintPreset Preset { get; }

            public string RoomId { get; }

            public string DisplayName { get; }

            public Vector2Int[] Rocks { get; }

            public SpawnSpec[] Enemies { get; }

            public Vector2Int SafeStart { get; }

            public Vector2Int Reward { get; }
        }

        private readonly struct SpawnSpec
        {
            public SpawnSpec(string kind, int x, int z)
            {
                Kind = kind;
                X = x;
                Z = z;
            }

            public string Kind { get; }

            public int X { get; }

            public int Z { get; }
        }
    }
}
