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
    public static class Milestone48AssetGenerator
    {
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M48";
        public const string EncounterCatalogPath = EncounterDirectory + "/EncounterCatalog_M48.asset";
        public const string CatalogId = "m48_content_lock_encounter_catalog_v1";
        public const string ReportJsonPath = "output/reports/m48_content_expansion_lock_v1.json";
        public const string ReportMarkdownPath = "output/reports/m48_content_expansion_lock_v1.md";
        public const string PdfPath = "output/pdf/Hollow_M48_Content_Expansion_Lock_V1.pdf";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        private static readonly ApprovedRoomSpec[] ApprovedRoomSpecs =
        {
            new(
                RoomDesignerFootprintPreset.Single1x1,
                "approved_cover_arena_single_1x1",
                "Approved Cover Arena 1x1",
                new[] { V(-4, -2), V(4, -2), V(-4, 2), V(4, 2), V(0, -2), V(0, 2) },
                Array.Empty<Vector2Int>(),
                new[] { V(-2, 0), V(2, 0) },
                new[] { Marker(RoomDesignerMarkerKinds.StandardBarrel, -5, 0), Marker(RoomDesignerMarkerKinds.StandardBarrel, 5, 0) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyNormal, -5, -2), Spawn(RoomDesignerMarkerKinds.EnemyFast, 5, -2), Spawn(RoomDesignerMarkerKinds.EnemyFlying, -5, 2), Spawn(RoomDesignerMarkerKinds.EnemyHeavy, 5, 2) },
                V(0, 0),
                V(0, 3)),
            new(
                RoomDesignerFootprintPreset.Wide2x1,
                "approved_pressure_lane_wide_2x1",
                "Approved Pressure Lane 2x1",
                new[] { V(-9, -2), V(-6, 2), V(-2, -1), V(2, 1), V(6, -2), V(9, 2) },
                new[] { V(-4, 0), V(4, 0) },
                new[] { V(-7, 0), V(7, 0) },
                new[] { Marker(RoomDesignerMarkerKinds.StandardBarrel, -10, 0), Marker(RoomDesignerMarkerKinds.ExplosiveBarrel, 0, 0) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyCharger, -10, -2), Spawn(RoomDesignerMarkerKinds.EnemyCharger, 8, 2), Spawn(RoomDesignerMarkerKinds.EnemyTurret, -2, 2), Spawn(RoomDesignerMarkerKinds.EnemyFast, 4, -2), Spawn(RoomDesignerMarkerKinds.EnemyFlying, 10, 2) },
                V(-8, 0),
                V(9, 1)),
            new(
                RoomDesignerFootprintPreset.Tall1x2,
                "approved_turret_spire_tall_1x2",
                "Approved Turret Spire 1x2",
                new[] { V(-4, -5), V(4, -5), V(-2, -1), V(2, 1), V(-4, 5), V(4, 5) },
                new[] { V(-1, 0), V(1, 0), V(0, 2) },
                new[] { V(0, -3), V(0, 4) },
                new[] { Marker(RoomDesignerMarkerKinds.StandardBarrel, -5, 0), Marker(RoomDesignerMarkerKinds.StandardBarrel, 5, 0) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyTurret, -5, -4), Spawn(RoomDesignerMarkerKinds.EnemyTurret, 5, 4), Spawn(RoomDesignerMarkerKinds.EnemyFlying, 5, -3), Spawn(RoomDesignerMarkerKinds.EnemySplitter, -4, 3), Spawn(RoomDesignerMarkerKinds.EnemyFast, 3, 5) },
                V(0, -5),
                V(0, 5)),
            new(
                RoomDesignerFootprintPreset.Block2x2,
                "approved_hazard_quadrant_block_2x2",
                "Approved Hazard Quadrant 2x2",
                new[] { V(-8, -5), V(-2, -5), V(4, -4), V(9, -1), V(-9, 2), V(-4, 5), V(3, 3), V(8, 5) },
                new[] { V(-1, -1), V(1, -1), V(-1, 1), V(1, 1) },
                new[] { V(-6, -1), V(6, 1), V(0, -5), V(0, 5) },
                new[] { Marker(RoomDesignerMarkerKinds.StandardBarrel, -10, 0), Marker(RoomDesignerMarkerKinds.StandardBarrel, 10, 0), Marker(RoomDesignerMarkerKinds.ExplosiveBarrel, -6, 4), Marker(RoomDesignerMarkerKinds.ExplosiveBarrel, 6, -4) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyHeavy, -10, -5), Spawn(RoomDesignerMarkerKinds.EnemyCharger, 8, -4), Spawn(RoomDesignerMarkerKinds.EnemyTurret, -8, 4), Spawn(RoomDesignerMarkerKinds.EnemySplitter, 7, 4), Spawn(RoomDesignerMarkerKinds.EnemyFlying, 0, -3), Spawn(RoomDesignerMarkerKinds.EnemyNormal, 0, 3) },
                V(-4, -3),
                V(5, 4)),
            new(
                RoomDesignerFootprintPreset.L3Cell,
                "approved_ambush_l_3cell",
                "Approved Ambush L 3-Cell",
                new[] { V(-10, -5), V(-5, -2), V(0, -5), V(6, -3), V(-10, 2), V(-4, 4), V(-1, 0) },
                new[] { V(-7, 0), V(-6, 0) },
                new[] { V(-11, 1), V(-2, -4), V(7, -2) },
                new[] { Marker(RoomDesignerMarkerKinds.StandardBarrel, -8, 4), Marker(RoomDesignerMarkerKinds.ExplosiveBarrel, 4, -4) },
                new[] { Spawn(RoomDesignerMarkerKinds.EnemySplitter, -11, -4), Spawn(RoomDesignerMarkerKinds.EnemyTurret, 8, -4), Spawn(RoomDesignerMarkerKinds.EnemyCharger, -9, 4), Spawn(RoomDesignerMarkerKinds.EnemyFlying, -2, -5), Spawn(RoomDesignerMarkerKinds.EnemyNormal, -3, 3) },
                V(-6, -2),
                V(-9, 5))
        };

        public static IReadOnlyList<string> ApprovedRoomIds => ApprovedRoomSpecs.Select(spec => spec.RoomId).ToArray();

        public static IReadOnlyList<string> EncounterIds => new[]
        {
            "m48_cover_scramble",
            "m48_lane_pursuit",
            "m48_turret_spire",
            "m48_splitter_pit",
            "m48_barrel_chain",
            "m48_reward_hazard_guard",
            "m48_world2_pressure_mix",
            "m48_world3_hazard_macro"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 48 Assets")]
        public static void Generate()
        {
            Milestone47AssetGenerator.Generate();
            Directory.CreateDirectory(Milestone16AssetGenerator.ApprovedRoomDirectory);
            Directory.CreateDirectory(EncounterDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportJsonPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            foreach (var spec in ApprovedRoomSpecs)
            {
                WriteApprovedRoom(spec);
            }

            AssetDatabase.Refresh();
            var roomCatalog = RefreshBranchTemplateCatalog();
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var directorProfile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath)
                                  ?? EncounterDirectorProfileDefinition.CreateRuntimeDefault();
            var encounterCatalog = GenerateEncounterCatalog();
            AssignToGameScenes(roomCatalog, settings, encounterCatalog, directorProfile);
            CuratedRoomDesignerDraftGenerator.Generate();
            WriteReports(encounterCatalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 48 content lock: {ApprovedRoomSpecs.Length} approved rooms and {EncounterIds.Count} new encounter templates.");
        }

        private static void WriteApprovedRoom(ApprovedRoomSpec spec)
        {
            var project = RoomDesignerProject.CreateDefault(spec.Preset, spec.DisplayName);
            project.projectId = spec.RoomId;
            project.displayName = spec.DisplayName;
            project.cells.RemoveAll(cell =>
                cell.kind == RoomDesignerCellKinds.Rock ||
                cell.kind == RoomDesignerCellKinds.Hole ||
                cell.kind == RoomDesignerCellKinds.Spike);
            project.markers.Clear();

            foreach (var hole in spec.Holes)
            {
                AddCell(project, hole, RoomDesignerCellKinds.Hole);
            }

            foreach (var spike in spec.Spikes)
            {
                AddCell(project, spike, RoomDesignerCellKinds.Spike);
            }

            foreach (var rock in spec.Rocks)
            {
                AddCell(project, rock, RoomDesignerCellKinds.Rock);
            }

            var safeStart = ClampToRoom(spec.Preset, spec.SafeStart);
            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, safeStart.x, 0f, safeStart.y));

            for (var index = 0; index < spec.InteractiveObjects.Length; index++)
            {
                var marker = spec.InteractiveObjects[index];
                var position = ClampToRoom(spec.Preset, new Vector2Int(marker.X, marker.Z));
                project.markers.Add(new RoomDesignerMarker($"interactive_{index:00}_{marker.Kind}", marker.Kind, position.x, 0f, position.y));
            }

            for (var index = 0; index < spec.Enemies.Length; index++)
            {
                var spawn = spec.Enemies[index];
                var position = ClampToRoom(spec.Preset, new Vector2Int(spawn.X, spawn.Z));
                project.markers.Add(new RoomDesignerMarker($"spawn_enemy_{index:00}", spawn.Kind, position.x, 0f, position.y));
            }

            var reward = ClampToRoom(spec.Preset, spec.Reward);
            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, reward.x, 0f, reward.y));

            foreach (var door in project.doorPorts)
            {
                door.state = RoomDesignerDoorKinds.Door;
            }

            var validation = RoomDesignerDraftValidator.Validate(project);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"M48 room '{spec.RoomId}' is not branch-ready: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = spec.RoomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = "m48-content-lock";
            manifest.hollowRuntime.prototypeStatus = "m48-approved";
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
            var m46Catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone46AssetGenerator.EncounterCatalogPath);
            if (m46Catalog == null)
            {
                throw new FileNotFoundException($"Missing M46 encounter catalog at {Milestone46AssetGenerator.EncounterCatalogPath}.");
            }

            var newEncounters = new[]
            {
                SaveEncounter("Encounter_M48_CoverScramble.asset", "m48_cover_scramble", "M48 Cover Scramble", BranchRoomRole.Combat, 1, 99, 1, 2, 3, new[]
                {
                    new EncounterSpawnEntry("spawnEnemyNormal", 2),
                    new EncounterSpawnEntry("spawnEnemyFast", 1),
                    new EncounterSpawnEntry("spawnEnemyFlying", 1)
                }),
                SaveEncounter("Encounter_M48_LanePursuit.asset", "m48_lane_pursuit", "M48 Lane Pursuit", BranchRoomRole.Combat, 1, 99, 2, 99, 3, new[]
                {
                    new EncounterSpawnEntry("spawnEnemyCharger", 2),
                    new EncounterSpawnEntry("spawnEnemyFast", 2),
                    new EncounterSpawnEntry("spawnEnemyNormal", 1)
                }),
                SaveEncounter("Encounter_M48_TurretSpire.asset", "m48_turret_spire", "M48 Turret Spire", BranchRoomRole.Combat, 2, 99, 2, 99, 3, new[]
                {
                    new EncounterSpawnEntry("spawnEnemyTurret", 2),
                    new EncounterSpawnEntry("spawnEnemyFlying", 2),
                    new EncounterSpawnEntry("spawnEnemyNormal", 1)
                }),
                SaveEncounter("Encounter_M48_SplitterPit.asset", "m48_splitter_pit", "M48 Splitter Pit", BranchRoomRole.Combat, 2, 99, 1, 99, 2, new[]
                {
                    new EncounterSpawnEntry("spawnEnemySplitter", 2),
                    new EncounterSpawnEntry("spawnEnemyFlying", 1),
                    new EncounterSpawnEntry("spawnEnemyFast", 1)
                }),
                SaveEncounter("Encounter_M48_BarrelChain.asset", "m48_barrel_chain", "M48 Barrel Chain", BranchRoomRole.Combat, 1, 99, 3, 99, 2, new[]
                {
                    new EncounterSpawnEntry("spawnEnemyHeavy", 1),
                    new EncounterSpawnEntry("spawnEnemyCharger", 1),
                    new EncounterSpawnEntry("spawnEnemyTurret", 1),
                    new EncounterSpawnEntry("spawnEnemyNormal", 2)
                }),
                SaveEncounter("Encounter_M48_RewardHazardGuard.asset", "m48_reward_hazard_guard", "M48 Reward Hazard Guard", BranchRoomRole.Reward, 1, 99, 1, 99, 3, new[]
                {
                    new EncounterSpawnEntry("spawnEnemyTurret", 1),
                    new EncounterSpawnEntry("spawnEnemyCharger", 1),
                    new EncounterSpawnEntry("spawnEnemyFlying", 1),
                    new EncounterSpawnEntry("spawnEnemyNormal", 1)
                }),
                SaveEncounter("Encounter_M48_World2PressureMix.asset", "m48_world2_pressure_mix", "M48 World 2 Pressure Mix", BranchRoomRole.Combat, 2, 99, 1, 99, 2, new[]
                {
                    new EncounterSpawnEntry("spawnEnemyCharger", 1),
                    new EncounterSpawnEntry("spawnEnemyTurret", 1),
                    new EncounterSpawnEntry("spawnEnemySplitter", 1),
                    new EncounterSpawnEntry("spawnEnemyFast", 2)
                }),
                SaveEncounter("Encounter_M48_World3HazardMacro.asset", "m48_world3_hazard_macro", "M48 World 3 Hazard Macro", BranchRoomRole.Combat, 3, 99, 3, 99, 3, new[]
                {
                    new EncounterSpawnEntry("spawnEnemyHeavy", 1),
                    new EncounterSpawnEntry("spawnEnemyCharger", 2),
                    new EncounterSpawnEntry("spawnEnemyTurret", 1),
                    new EncounterSpawnEntry("spawnEnemySplitter", 1),
                    new EncounterSpawnEntry("spawnEnemyFlying", 1)
                })
            };

            var combined = m46Catalog.Encounters
                .Concat(newEncounters)
                .Where(encounter => encounter != null)
                .GroupBy(encounter => encounter.EncounterId)
                .Select(group => group.First())
                .OrderBy(encounter => encounter.EncounterId)
                .ToArray();

            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(EncounterCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EncounterCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, EncounterCatalogPath);
            }

            catalog.Configure(CatalogId, combined, m46Catalog.BossEncounter);
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

        private static void AssignToGameScenes(
            BranchRoomTemplateCatalogDefinition roomCatalog,
            BranchGenerationSettingsDefinition settings,
            EncounterCatalogDefinition encounterCatalog,
            EncounterDirectorProfileDefinition directorProfile)
        {
            if (roomCatalog == null || encounterCatalog == null || directorProfile == null)
            {
                throw new InvalidOperationException("M48 scene wiring requires room catalog, encounter catalog, and director profile.");
            }

            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureTemplateCatalog(roomCatalog, settings != null ? settings.DefaultSeed : roomCatalog.DefaultSeed);
                if (settings != null)
                {
                    branch.ConfigureGenerationSettings(settings);
                }

                branch.ConfigureEncounterCatalog(encounterCatalog);
                branch.ConfigureEncounterDirectorProfile(directorProfile);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteReports(EncounterCatalogDefinition encounterCatalog)
        {
            var markdown =
                "# M48 Content Expansion Lock V1\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                $"- Approved rooms added: `{ApprovedRoomIds.Count}`.\n" +
                $"- Encounter catalog: `{encounterCatalog.CatalogId}` with `{encounterCatalog.Encounters.Count}` total templates.\n" +
                $"- New M48 encounter templates: `{EncounterIds.Count}`.\n" +
                "- Normal runs and M47 challenges are wired through the M48 room pool and encounter catalog.\n" +
                "- Starter/origin/prologue room safety remains unchanged; new hazard rooms are additive branch candidates only.\n" +
                $"- PDF handoff target: `{PdfPath}`.\n";
            File.WriteAllText(ReportMarkdownPath, markdown);

            var report = new ContentExpansionLockReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                catalogId = encounterCatalog.CatalogId,
                approvedRoomIds = ApprovedRoomIds.ToArray(),
                newEncounterIds = EncounterIds.ToArray(),
                totalEncounterCount = encounterCatalog.Encounters.Count,
                pdfPath = PdfPath
            };
            File.WriteAllText(ReportJsonPath, JsonUtility.ToJson(report, prettyPrint: true));
        }

        private static void AddCell(RoomDesignerProject project, Vector2Int position, string kind)
        {
            var clamped = ClampToRoom(project.footprintPreset, position);
            project.cells.RemoveAll(cell => cell.x == clamped.x && cell.z == clamped.y && cell.layer == 0 &&
                                            (cell.kind == RoomDesignerCellKinds.Rock ||
                                             cell.kind == RoomDesignerCellKinds.Hole ||
                                             cell.kind == RoomDesignerCellKinds.Spike));
            project.cells.Add(new RoomDesignerCell(clamped.x, clamped.y, 0, kind));
        }

        private static Vector2Int ClampToRoom(RoomDesignerFootprintPreset preset, Vector2Int position)
        {
            return RoomDesignerFootprintUtility.ContainsTile(preset, position.x, position.y)
                ? position
                : RoomDesignerFootprintUtility.NearestContainedTile(preset, position.x, position.y);
        }

        private static Vector2Int V(int x, int z)
        {
            return new Vector2Int(x, z);
        }

        private static SpawnSpec Spawn(string kind, int x, int z)
        {
            return new SpawnSpec(kind, x, z);
        }

        private static MarkerSpec Marker(string kind, int x, int z)
        {
            return new MarkerSpec(kind, x, z);
        }

        [Serializable]
        private sealed class ContentExpansionLockReport
        {
            public string generatedAtUtc;
            public string catalogId;
            public string[] approvedRoomIds;
            public string[] newEncounterIds;
            public int totalEncounterCount;
            public string pdfPath;
        }

        private readonly struct ApprovedRoomSpec
        {
            public ApprovedRoomSpec(
                RoomDesignerFootprintPreset preset,
                string roomId,
                string displayName,
                IReadOnlyList<Vector2Int> rocks,
                IReadOnlyList<Vector2Int> holes,
                IReadOnlyList<Vector2Int> spikes,
                IReadOnlyList<MarkerSpec> interactiveObjects,
                IReadOnlyList<SpawnSpec> enemies,
                Vector2Int safeStart,
                Vector2Int reward)
            {
                Preset = preset;
                RoomId = roomId;
                DisplayName = displayName;
                Rocks = rocks?.ToArray() ?? Array.Empty<Vector2Int>();
                Holes = holes?.ToArray() ?? Array.Empty<Vector2Int>();
                Spikes = spikes?.ToArray() ?? Array.Empty<Vector2Int>();
                InteractiveObjects = interactiveObjects?.ToArray() ?? Array.Empty<MarkerSpec>();
                Enemies = enemies?.ToArray() ?? Array.Empty<SpawnSpec>();
                SafeStart = safeStart;
                Reward = reward;
            }

            public RoomDesignerFootprintPreset Preset { get; }

            public string RoomId { get; }

            public string DisplayName { get; }

            public Vector2Int[] Rocks { get; }

            public Vector2Int[] Holes { get; }

            public Vector2Int[] Spikes { get; }

            public MarkerSpec[] InteractiveObjects { get; }

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

        private readonly struct MarkerSpec
        {
            public MarkerSpec(string kind, int x, int z)
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
