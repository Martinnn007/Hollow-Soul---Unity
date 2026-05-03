using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone77AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M77_Critter_Roster_And_Ballistic_Behaviors.md";
        public const string ReportPath = "output/reports/m77_critter_roster_and_ballistic_behaviors.md";
        public const string PdfPath = "output/pdf/Hollow_M77_Critter_Roster_And_Ballistic_Behaviors.pdf";
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M77";
        public const string ShowcaseRoomDirectory = "Assets/_Hollow/Data/Rooms/DesignerApproved/M77";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        [MenuItem("Hollow/Generation/Generate Milestone 77 Assets")]
        public static void Generate()
        {
            Milestone76AssetGenerator.Generate();
            Directory.CreateDirectory(EncounterDirectory);
            Directory.CreateDirectory(ShowcaseRoomDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var enemies = GenerateEnemyAssets();
            AssignCritterAttackProfiles(enemies);
            RefreshEnemyCatalog(enemies);
            GenerateEncounterRotation();
            GenerateShowcaseRooms();
            AssetDatabase.Refresh();
            RefreshBranchTemplateCatalog();
            WriteDocs();
            WriteReport();
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 77 critter roster and ballistic behavior assets.");
        }

        public static IReadOnlyList<CritterEnemySpec> EnemyRows()
        {
            return new[]
            {
                new CritterEnemySpec("Enemy_SpittingPod.asset", "spawnEnemySpittingPod", "Spitting Pod", EnemyArchetypeId.Heavy, EnemyBehaviorId.SpittingPod, 10, 0f, 0, 1f, 0.44f, 8f, 1f, 1, 5f, 0f, 1f, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Sentinel, 5.5f, 8f, 0f, 0f, 9f, false, 1.4f, new Color(0.38f, 0.78f, 0.42f, 1f)),
                new CritterEnemySpec("Enemy_Rat.asset", "spawnEnemyRat", "Rat", EnemyArchetypeId.Fast, EnemyBehaviorId.Rat, 3, 2.65f, 1, 0.85f, 0.2f, 2.2f, 1f, 0, 5f, 0f, 1f, EnemyBodyClass.Light, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Territorial, 1.2f, 2.2f, 8f, 260f, 7.5f, true, 0.95f, new Color(0.58f, 0.5f, 0.42f, 1f)),
                new CritterEnemySpec("Enemy_Spider.asset", "spawnEnemySpider", "Spider", EnemyArchetypeId.Fast, EnemyBehaviorId.Spider, 2, 2.9f, 1, 0.8f, 0.22f, 2.1f, 1f, 0, 5f, 0f, 1f, EnemyBodyClass.Light, EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Prey, 1f, 1.9f, 8.5f, 300f, 8f, true, 1.15f, new Color(0.16f, 0.12f, 0.2f, 1f))
            };
        }

        public static IReadOnlyList<string> EncounterIds { get; } = new[]
        {
            "m77_pod_warning",
            "m77_rat_scramble",
            "m77_spider_scuttle",
            "m77_critter_mix"
        };

        public static IReadOnlyList<string> ShowcaseRoomIds { get; } = new[]
        {
            "m77_spitting_pod_showcase",
            "m77_rat_showcase",
            "m77_spider_showcase"
        };

        public static IReadOnlyList<string> CuratedEncounterRoomIds { get; } = new[]
        {
            "m77_spider_brood_den_wide",
            "m77_rat_warren_single",
            "m77_rocky_spider_pod_wide",
            "m77_rocky_rat_pod_wide"
        };

        private static readonly CritterRoomSpec[] CuratedEncounterRooms =
        {
            new(
                RoomDesignerFootprintPreset.Wide2x1,
                "m77_spider_brood_den_wide",
                "M77 Spider Brood Den Wide",
                "m77-critter-pack",
                "m77-curated-critter-room",
                V(-11, 0),
                V(11, 0),
                System.Array.Empty<Vector2Int>(),
                new[]
                {
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, -7, -2),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, -6, -1),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, -7, 1),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, 0, -2),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, 1, -1),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, 0, 1),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, 7, -1),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, 8, 1)
                }),
            new(
                RoomDesignerFootprintPreset.Single1x1,
                "m77_rat_warren_single",
                "M77 Rat Warren Single",
                "m77-critter-pack",
                "m77-curated-critter-room",
                V(-5, 0),
                V(5, 2),
                System.Array.Empty<Vector2Int>(),
                new[]
                {
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, -2, -2),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, -1, -1),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, 2, 1),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, 3, 2),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, 2, -1)
                }),
            new(
                RoomDesignerFootprintPreset.Wide2x1,
                "m77_rocky_spider_pod_wide",
                "M77 Rocky Spider Pod Wide",
                "m77-critter-pod-cover",
                "m77-curated-critter-room",
                V(-11, 0),
                V(11, 2),
                new[] { V(-9, -2), V(-7, 1), V(-5, -1), V(-2, 2), V(2, -2), V(5, 1), V(7, -1), V(9, 2), V(-1, -2), V(1, 2) },
                new[]
                {
                    Spawn(RoomDesignerMarkerKinds.EnemySpittingPod, 0, 0),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, -10, -2),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, -8, 2),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, -4, 1),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, 4, -1),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, 8, -2),
                    Spawn(RoomDesignerMarkerKinds.EnemySpider, 10, 1)
                }),
            new(
                RoomDesignerFootprintPreset.Wide2x1,
                "m77_rocky_rat_pod_wide",
                "M77 Rocky Rat Pod Wide",
                "m77-critter-pod-cover",
                "m77-curated-critter-room",
                V(-11, 0),
                V(11, -2),
                new[] { V(-10, 2), V(-8, -1), V(-6, 1), V(-3, -2), V(-1, 2), V(2, -2), V(4, 1), V(6, -1), V(8, 2), V(10, -2) },
                new[]
                {
                    Spawn(RoomDesignerMarkerKinds.EnemySpittingPod, 0, 0),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, -9, -2),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, -7, 2),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, -4, 0),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, 5, -2),
                    Spawn(RoomDesignerMarkerKinds.EnemyRat, 8, 1)
                })
        };

        private static Dictionary<string, EnemyDefinition> GenerateEnemyAssets()
        {
            var result = new Dictionary<string, EnemyDefinition>();
            foreach (var spec in EnemyRows())
            {
                var path = $"Assets/_Hollow/Data/Enemies/{spec.FileName}";
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (enemy == null)
                {
                    enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
                    AssetDatabase.CreateAsset(enemy, path);
                }

                enemy.Configure(
                    spec.SpawnKind,
                    spec.DisplayName,
                    spec.ArchetypeId,
                    spec.BehaviorId,
                    EnemyMovementMode.Grounded,
                    spec.MaxHealth,
                    spec.SpeedMetersPerSecond,
                    spec.ContactDamage,
                    spec.ContactCooldownSeconds,
                    spec.RadiusMeters,
                    spec.AttackRangeMeters,
                    spec.AttackCooldownSeconds,
                    spec.ProjectileDamage,
                    spec.ProjectileSpeedMetersPerSecond,
                    spec.ChargeSpeedMetersPerSecond,
                    spec.ChargeCooldownSeconds,
                    "spawnEnemyNormal",
                    0,
                    spec.BodyClass,
                    spec.Intelligence,
                    spec.Disposition,
                    spec.PreferredRangeMinMeters,
                    spec.PreferredRangeMaxMeters,
                    spec.Color);
                enemy.ConfigureSenseAndLunge(
                    spec.SightRadiusMeters,
                    spec.SightAngleDegrees,
                    spec.HearingRadiusMeters,
                    spec.LungeEnabled,
                    spec.LungeTriggerRangeMeters,
                    spec.BehaviorId == EnemyBehaviorId.Spider ? 0.12f : spec.BehaviorId == EnemyBehaviorId.Rat ? 0.14f : 0.22f,
                    spec.BehaviorId == EnemyBehaviorId.Spider ? 0.16f : spec.BehaviorId == EnemyBehaviorId.Rat ? 0.14f : 0.18f,
                    spec.BehaviorId == EnemyBehaviorId.Spider ? 0.7f : spec.BehaviorId == EnemyBehaviorId.Rat ? 0.55f : 0.75f,
                    spec.BehaviorId == EnemyBehaviorId.Spider ? 0.85f : spec.BehaviorId == EnemyBehaviorId.Rat ? 0.9f : 1.15f);
                EditorUtility.SetDirty(enemy);
                result[spec.SpawnKind] = enemy;
            }

            return result;
        }

        private static void AssignCritterAttackProfiles(IReadOnlyDictionary<string, EnemyDefinition> enemies)
        {
            foreach (var row in enemies)
            {
                var assigned = EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(spec => spec.OwnerId == row.Key)
                    .Select(spec => AssetDatabase.LoadAssetAtPath<EnemyAttackProfileDefinition>($"{Milestone76AssetGenerator.AttackDirectory}/{spec.AssetName}"))
                    .Where(profile => profile != null)
                    .ToArray();
                row.Value.ConfigureAttackProfiles(assigned);
                EditorUtility.SetDirty(row.Value);
            }
        }

        private static void RefreshEnemyCatalog(IReadOnlyDictionary<string, EnemyDefinition> enemies)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);
            if (catalog == null)
            {
                return;
            }

            var definitions = catalog.Definitions
                .Where(definition => definition != null && !enemies.ContainsKey(definition.SpawnKind))
                .Concat(enemies.Values)
                .OrderBy(definition => definition.SpawnKind)
                .ToArray();
            catalog.Configure(definitions, catalog.FallbackDefinition);
            EditorUtility.SetDirty(catalog);
        }

        private static void GenerateEncounterRotation()
        {
            var m48Catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone48AssetGenerator.EncounterCatalogPath);
            if (m48Catalog == null)
            {
                return;
            }

            var encounters = new[]
            {
                SaveEncounter("Encounter_M77_PodWarning.asset", "m77_pod_warning", "M77 Pod Warning", BranchRoomRole.Combat, 0, 99, 1, 99, 3, new[] { new EncounterSpawnEntry("spawnEnemySpittingPod", 1), new EncounterSpawnEntry("spawnEnemyNormal", 1) }),
                SaveEncounter("Encounter_M77_RatScramble.asset", "m77_rat_scramble", "M77 Rat Scramble", BranchRoomRole.Combat, 0, 99, 1, 99, 3, new[] { new EncounterSpawnEntry("spawnEnemyRat", 2), new EncounterSpawnEntry("spawnEnemyNormal", 1) }),
                SaveEncounter("Encounter_M77_SpiderScuttle.asset", "m77_spider_scuttle", "M77 Spider Scuttle", BranchRoomRole.Combat, 0, 99, 1, 99, 3, new[] { new EncounterSpawnEntry("spawnEnemySpider", 2), new EncounterSpawnEntry("spawnEnemyFlying", 1) }),
                SaveEncounter("Encounter_M77_CritterMix.asset", "m77_critter_mix", "M77 Critter Mix", BranchRoomRole.Combat, 1, 99, 1, 99, 2, new[] { new EncounterSpawnEntry("spawnEnemySpittingPod", 1), new EncounterSpawnEntry("spawnEnemyRat", 1), new EncounterSpawnEntry("spawnEnemySpider", 1), new EncounterSpawnEntry("spawnEnemyFast", 1) })
            };

            var combined = m48Catalog.Encounters
                .Concat(encounters)
                .Where(encounter => encounter != null)
                .GroupBy(encounter => encounter.EncounterId)
                .Select(group => group.First())
                .OrderBy(encounter => encounter.EncounterId)
                .ToArray();
            m48Catalog.Configure(m48Catalog.CatalogId, combined, m48Catalog.BossEncounter);
            EditorUtility.SetDirty(m48Catalog);
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

        private static void GenerateShowcaseRooms()
        {
            WriteShowcaseRoom("m77_spitting_pod_showcase", "M77 Spitting Pod Showcase", RoomDesignerMarkerKinds.EnemySpittingPod);
            WriteShowcaseRoom("m77_rat_showcase", "M77 Rat Showcase", RoomDesignerMarkerKinds.EnemyRat);
            WriteShowcaseRoom("m77_spider_showcase", "M77 Spider Showcase", RoomDesignerMarkerKinds.EnemySpider);
            foreach (var spec in CuratedEncounterRooms)
            {
                WriteCritterEncounterRoom(spec);
            }
        }

        private static void WriteShowcaseRoom(string roomId, string displayName, string enemyKind)
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, displayName);
            project.projectId = roomId;
            project.displayName = displayName;
            project.cells.RemoveAll(cell =>
                cell.kind == RoomDesignerCellKinds.Rock ||
                cell.kind == RoomDesignerCellKinds.Hole ||
                cell.kind == RoomDesignerCellKinds.Spike);
            project.markers.Clear();
            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, -4f, 0f, 0f));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_00", enemyKind, 2f, 0f, 0f));
            project.markers.Add(new RoomDesignerMarker("spawn_enemy_01", RoomDesignerMarkerKinds.EnemyNormal, 4f, 0f, -2f));
            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, 5f, 0f, 2f));
            foreach (var door in project.doorPorts)
            {
                door.state = RoomDesignerDoorKinds.Door;
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = roomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = "m77-critter-showcase";
            manifest.hollowRuntime.prototypeStatus = "m77-curated";
            var path = $"{ShowcaseRoomDirectory}/{roomId}.hollowruntime.json";
            File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void WriteCritterEncounterRoom(CritterRoomSpec spec)
        {
            var project = RoomDesignerProject.CreateDefault(spec.Preset, spec.DisplayName);
            project.projectId = spec.RoomId;
            project.displayName = spec.DisplayName;
            project.cells.RemoveAll(cell =>
                cell.kind == RoomDesignerCellKinds.Rock ||
                cell.kind == RoomDesignerCellKinds.Hole ||
                cell.kind == RoomDesignerCellKinds.Spike);
            project.markers.Clear();

            foreach (var rock in spec.Rocks)
            {
                project.cells.Add(new RoomDesignerCell(rock.x, rock.y, 0, RoomDesignerCellKinds.Rock));
            }

            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, spec.SafeStart.x, 0f, spec.SafeStart.y));
            for (var index = 0; index < spec.Enemies.Length; index++)
            {
                var spawn = spec.Enemies[index];
                project.markers.Add(new RoomDesignerMarker($"spawn_enemy_{index:00}", spawn.Kind, spawn.X, 0f, spawn.Z));
            }

            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, spec.Reward.x, 0f, spec.Reward.y));
            foreach (var door in project.doorPorts)
            {
                door.state = RoomDesignerDoorKinds.Door;
            }

            var validation = RoomDesignerDraftValidator.Validate(project);
            if (!validation.IsValid)
            {
                throw new InvalidDataException($"M77 curated room '{spec.RoomId}' is not branch-ready: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = spec.RoomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = spec.RewardType;
            manifest.hollowRuntime.prototypeStatus = spec.PrototypeStatus;
            var path = $"{ShowcaseRoomDirectory}/{spec.RoomId}.hollowruntime.json";
            File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static BranchRoomTemplateCatalogDefinition RefreshBranchTemplateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                throw new FileNotFoundException($"Missing branch room template catalog at {Milestone14AssetGenerator.CatalogPath}.");
            }

            catalog.Configure(
                catalog.Single1x1,
                catalog.Wide2x1,
                catalog.Tall1x2,
                catalog.Block2x2,
                catalog.L3Cell,
                catalog.DefaultSeed,
                Milestone16AssetGenerator.LoadApprovedTemplates());
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WriteDocs()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M77: Critter Roster + Ballistic Creature Behaviors V1");
            builder.AppendLine();
            builder.AppendLine("M77 adds Spitting Pod, Rat, and Spider as early mixed enemies. They use the existing intelligence, senses, movement intent, and M76 attack profile systems, plus a small shared critter behavior layer for readable chaotic movement.");
            builder.AppendLine();
            builder.AppendLine("## Enemy Stat Cards");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Spawn Kind | Behavior | HP | Speed | Radius | Intelligence | Disposition | Sight | Hearing | Preferred Range | Attacks |");
            builder.AppendLine("| --- | --- | --- | ---: | ---: | ---: | --- | --- | --- | ---: | --- | --- |");
            foreach (var row in EnemyRows())
            {
                var attacks = string.Join(", ", EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => spec.OwnerId == row.SpawnKind).Select(spec => spec.AttackId));
                builder.AppendLine($"| {row.DisplayName} | `{row.SpawnKind}` | {row.BehaviorId} | {row.MaxHealth} | {row.SpeedMetersPerSecond:0.00}m/s | {row.RadiusMeters:0.00}m | {row.Intelligence.DisplayLabel()} | {row.Disposition.ToSaveString()} | {row.SightRadiusMeters:0.0}m/{row.SightAngleDegrees:0}deg | {row.HearingRadiusMeters:0.0}m | {row.PreferredRangeMinMeters:0.0}-{row.PreferredRangeMaxMeters:0.0}m | {attacks} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Runtime Behavior Contract");
            builder.AppendLine();
            builder.AppendLine("- Spitting Pod is stationary, blind, hearing-driven, and fires visible ballistic lob projectiles with a small non-lingering splash.");
            builder.AppendLine("- Rat uses territorial awareness: it roams chaotically, warns/pressures before biting, and retreats readily after damage.");
            builder.AppendLine("- Spider uses readable chaotic fight-or-flight decisions, with fast retreat bursts and quick hop/bite attacks.");
            builder.AppendLine("- New attacks remain Physical/Natural metadata; no poison, acid, elemental resistance, pathfinding, obstacle LOS, or squad behavior is added.");
            builder.AppendLine();
            builder.AppendLine("## Encounter And Room Coverage");
            builder.AppendLine();
            builder.AppendLine("- Early mixed encounter rotation adds `m77_pod_warning`, `m77_rat_scramble`, `m77_spider_scuttle`, and `m77_critter_mix`.");
            builder.AppendLine("- Curated showcase rooms are generated under `Assets/_Hollow/Data/Rooms/DesignerApproved/M77/`.");
            builder.AppendLine($"- Bespoke critter encounter rooms: {string.Join(", ", CuratedEncounterRoomIds.Select(id => $"`{id}`"))}.");
            builder.AppendLine("- Presentation roles and material roles are added for art-pass-ready placeholder replacement.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M77 Critter Roster And Ballistic Behaviors Report

- Added enemy definitions: {string.Join(", ", EnemyRows().Select(row => row.DisplayName))}.
- Added disposition: `{EnemyInstinctDisposition.Territorial.ToSaveString()}`.
- Added early encounter ids: {string.Join(", ", EncounterIds)}.
- Added showcase room ids: {string.Join(", ", ShowcaseRoomIds)}.
- Added bespoke critter room ids: {string.Join(", ", CuratedEncounterRoomIds)}.
- Catalogue Markdown: `{DocsPath}`.
- Catalogue PDF target: `{PdfPath}`.
");
        }

        private static void GeneratePdfWithReportLab()
        {
            const string scriptPath = "tools/generate_m77_critter_roster_pdf.py";
            if (!File.Exists(scriptPath))
            {
                Debug.LogWarning($"M77 PDF generator script not found at {scriptPath}.");
                return;
            }

            try
            {
                var startInfo = new DiagnosticsProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = scriptPath,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = DiagnosticsProcess.Start(startInfo);
                if (process == null)
                {
                    Debug.LogWarning("M77 PDF generation did not start.");
                    return;
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Debug.Log(string.IsNullOrWhiteSpace(output) ? $"Generated {PdfPath}." : output.Trim());
                    return;
                }

                Debug.LogWarning($"M77 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M77 PDF generation skipped: {exception.Message}");
            }
        }

        public readonly struct CritterEnemySpec
        {
            public CritterEnemySpec(
                string fileName,
                string spawnKind,
                string displayName,
                EnemyArchetypeId archetypeId,
                EnemyBehaviorId behaviorId,
                int maxHealth,
                float speedMetersPerSecond,
                int contactDamage,
                float contactCooldownSeconds,
                float radiusMeters,
                float attackRangeMeters,
                float attackCooldownSeconds,
                int projectileDamage,
                float projectileSpeedMetersPerSecond,
                float chargeSpeedMetersPerSecond,
                float chargeCooldownSeconds,
                EnemyBodyClass bodyClass,
                EnemyIntelligenceLevel intelligence,
                EnemyInstinctDisposition disposition,
                float preferredRangeMinMeters,
                float preferredRangeMaxMeters,
                float sightRadiusMeters,
                float sightAngleDegrees,
                float hearingRadiusMeters,
                bool lungeEnabled,
                float lungeTriggerRangeMeters,
                Color color)
            {
                FileName = fileName;
                SpawnKind = spawnKind;
                DisplayName = displayName;
                ArchetypeId = archetypeId;
                BehaviorId = behaviorId;
                MaxHealth = maxHealth;
                SpeedMetersPerSecond = speedMetersPerSecond;
                ContactDamage = contactDamage;
                ContactCooldownSeconds = contactCooldownSeconds;
                RadiusMeters = radiusMeters;
                AttackRangeMeters = attackRangeMeters;
                AttackCooldownSeconds = attackCooldownSeconds;
                ProjectileDamage = projectileDamage;
                ProjectileSpeedMetersPerSecond = projectileSpeedMetersPerSecond;
                ChargeSpeedMetersPerSecond = chargeSpeedMetersPerSecond;
                ChargeCooldownSeconds = chargeCooldownSeconds;
                BodyClass = bodyClass;
                Intelligence = intelligence;
                Disposition = disposition;
                PreferredRangeMinMeters = preferredRangeMinMeters;
                PreferredRangeMaxMeters = preferredRangeMaxMeters;
                SightRadiusMeters = sightRadiusMeters;
                SightAngleDegrees = sightAngleDegrees;
                HearingRadiusMeters = hearingRadiusMeters;
                LungeEnabled = lungeEnabled;
                LungeTriggerRangeMeters = lungeTriggerRangeMeters;
                Color = color;
            }

            public string FileName { get; }
            public string SpawnKind { get; }
            public string DisplayName { get; }
            public EnemyArchetypeId ArchetypeId { get; }
            public EnemyBehaviorId BehaviorId { get; }
            public int MaxHealth { get; }
            public float SpeedMetersPerSecond { get; }
            public int ContactDamage { get; }
            public float ContactCooldownSeconds { get; }
            public float RadiusMeters { get; }
            public float AttackRangeMeters { get; }
            public float AttackCooldownSeconds { get; }
            public int ProjectileDamage { get; }
            public float ProjectileSpeedMetersPerSecond { get; }
            public float ChargeSpeedMetersPerSecond { get; }
            public float ChargeCooldownSeconds { get; }
            public EnemyBodyClass BodyClass { get; }
            public EnemyIntelligenceLevel Intelligence { get; }
            public EnemyInstinctDisposition Disposition { get; }
            public float PreferredRangeMinMeters { get; }
            public float PreferredRangeMaxMeters { get; }
            public float SightRadiusMeters { get; }
            public float SightAngleDegrees { get; }
            public float HearingRadiusMeters { get; }
            public bool LungeEnabled { get; }
            public float LungeTriggerRangeMeters { get; }
            public Color Color { get; }
        }

        private static Vector2Int V(int x, int z)
        {
            return new Vector2Int(x, z);
        }

        private static RoomEnemySpawnSpec Spawn(string kind, int x, int z)
        {
            return new RoomEnemySpawnSpec(kind, x, z);
        }

        private readonly struct CritterRoomSpec
        {
            public CritterRoomSpec(
                RoomDesignerFootprintPreset preset,
                string roomId,
                string displayName,
                string rewardType,
                string prototypeStatus,
                Vector2Int safeStart,
                Vector2Int reward,
                IReadOnlyList<Vector2Int> rocks,
                IReadOnlyList<RoomEnemySpawnSpec> enemies)
            {
                Preset = preset;
                RoomId = roomId;
                DisplayName = displayName;
                RewardType = rewardType;
                PrototypeStatus = prototypeStatus;
                SafeStart = safeStart;
                Reward = reward;
                Rocks = rocks?.ToArray() ?? System.Array.Empty<Vector2Int>();
                Enemies = enemies?.ToArray() ?? System.Array.Empty<RoomEnemySpawnSpec>();
            }

            public RoomDesignerFootprintPreset Preset { get; }
            public string RoomId { get; }
            public string DisplayName { get; }
            public string RewardType { get; }
            public string PrototypeStatus { get; }
            public Vector2Int SafeStart { get; }
            public Vector2Int Reward { get; }
            public Vector2Int[] Rocks { get; }
            public RoomEnemySpawnSpec[] Enemies { get; }
        }

        private readonly struct RoomEnemySpawnSpec
        {
            public RoomEnemySpawnSpec(string kind, int x, int z)
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
