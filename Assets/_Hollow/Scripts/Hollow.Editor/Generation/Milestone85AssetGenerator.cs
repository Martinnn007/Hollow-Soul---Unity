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
using Debug = UnityEngine.Debug;

namespace Hollow.Editor.Generation
{
    public static class Milestone85AssetGenerator
    {
        public const string AttackDirectory = "Assets/_Hollow/Data/EnemyAttacks/M85";
        public const string ActionDirectory = "Assets/_Hollow/Data/EnemyActions/M85";
        public const string TreeDirectory = "Assets/_Hollow/Data/EnemyBehaviorTrees/M85";
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M85";
        public const string CreatureRoomDirectory = "Assets/_Hollow/Data/Rooms/DesignerApproved/M85";
        public const string DocsPath = "Docs/Hollow_M85_Creature_Action_Expansion.md";
        public const string ReportPath = "output/reports/m85_creature_action_expansion.md";
        public const string PdfPath = "output/pdf/Hollow_M85_Creature_Action_Expansion.pdf";
        public const string GeneratorScriptPath = "tools/generate_m85_creature_action_expansion_pdf.py";
        public const string VerifyScriptPath = "tools/verify_m85_creature_action_expansion_pdf.py";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        public static IReadOnlyList<string> NewSpawnKinds { get; } = new[]
        {
            "spawnEnemyHollowBird",
            "spawnEnemyHollowBeast"
        };

        public static IReadOnlyList<string> BodyCreatureSpawnKinds { get; } = new[]
        {
            "spawnEnemyNormal",
            "spawnEnemyFlying",
            "spawnEnemyFast",
            "spawnEnemyHeavy",
            "spawnEnemyCharger",
            "spawnEnemySplitter",
            "spawnEnemyRat",
            "spawnEnemySpider",
            "spawnEnemyHollowBird",
            "spawnEnemyHollowBeast"
        };

        public static IReadOnlyList<string> EncounterIds { get; } = new[]
        {
            "m85_hollow_bird_perch",
            "m85_hollow_beast_den",
            "m85_rat_spider_signal",
            "m85_mixed_creature_scramble"
        };

        public static IReadOnlyList<string> CreatureRoomIds { get; } = new[]
        {
            "m85_hollow_bird_perch_room",
            "m85_hollow_beast_den",
            "m85_rat_spider_signal_room",
            "m85_mixed_body_creature_scramble"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 85 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(AttackDirectory);
            Directory.CreateDirectory(ActionDirectory);
            Directory.CreateDirectory(TreeDirectory);
            Directory.CreateDirectory(EncounterDirectory);
            Directory.CreateDirectory(CreatureRoomDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var attacks = GenerateAttackProfiles();
            var actions = GenerateActionProfiles(attacks);
            var trees = GenerateBehaviorTrees();
            var enemies = GenerateEnemyAssets(attacks, actions, trees);
            RefreshEnemyCatalog(enemies);
            GenerateEncounterRotation();
            GenerateCreatureRooms();
            RefreshBranchTemplateCatalog();
            WriteDocs();
            WriteReport();
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 85 creature action expansion assets.");
        }

        public static IReadOnlyList<CreatureEnemySpec> NewCreatureRows()
        {
            return new[]
            {
                new CreatureEnemySpec("Enemy_HollowBird.asset", "spawnEnemyHollowBird", "Hollow Bird", EnemyArchetypeId.Flying, EnemyBehaviorId.HollowBird, EnemyMovementMode.Flying, 3, 2.25f, 0.24f, 1.55f, EnemyBodyClass.Light, EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Predator, 1.8f, 3.6f, 8.2f, 235f, 6.4f, PresentationPrefabRole.EnemyHollowBird, MaterialRole.EnemyHollowBird, new Color(0.36f, 0.42f, 0.56f, 1f)),
                new CreatureEnemySpec("Enemy_HollowBeast.asset", "spawnEnemyHollowBeast", "Hollow Beast", EnemyArchetypeId.Normal, EnemyBehaviorId.HollowBeast, EnemyMovementMode.Grounded, 5, 1.9f, 0.34f, 1.65f, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Predator, 1.15f, 2.1f, 7.2f, 165f, 6.2f, PresentationPrefabRole.EnemyHollowBeast, MaterialRole.EnemyHollowBeast, new Color(0.28f, 0.24f, 0.2f, 1f))
            };
        }

        public static IReadOnlyList<string> PromotedCreatureActionIds { get; } = new[]
        {
            "short_backstep",
            "warning_feint",
            "fly_strafe",
            "dive_feint",
            "evasive_skitter",
            "snap_combo",
            "guarded_shove",
            "slow_overhead_slam",
            "short_recover_hop",
            "shoulder_check",
            "splitter_backstep",
            "cleave_feint",
            "skitter_retreat",
            "panic_pounce",
            "alarm_squeal",
            "panic_flee",
            "web_feint",
            "swoop_peck",
            "claw_dive",
            "wing_retreat",
            "caw_signal",
            "leap_bite",
            "body_check",
            "leap_back",
            "howl_signal"
        };

        private static Dictionary<string, EnemyAttackProfileDefinition> GenerateAttackProfiles()
        {
            var result = new Dictionary<string, EnemyAttackProfileDefinition>();
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => BodyCreatureSpawnKinds.Contains(spec.OwnerId)))
            {
                var path = $"{AttackDirectory}/{spec.AssetName}";
                var profile = AssetDatabase.LoadAssetAtPath<EnemyAttackProfileDefinition>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<EnemyAttackProfileDefinition>();
                    AssetDatabase.CreateAsset(profile, path);
                }

                profile.Configure(spec);
                EditorUtility.SetDirty(profile);
                result[$"{spec.OwnerId}:{spec.AttackId}"] = profile;
            }

            return result;
        }

        private static Dictionary<string, EnemyActionProfileDefinition> GenerateActionProfiles(IReadOnlyDictionary<string, EnemyAttackProfileDefinition> attacks)
        {
            var result = new Dictionary<string, EnemyActionProfileDefinition>();
            foreach (var spec in EnemyActionProfileDefaults.AllEnemySpecs.Where(spec => BodyCreatureSpawnKinds.Contains(spec.OwnerId)))
            {
                var path = $"{ActionDirectory}/{spec.AssetName}";
                var profile = AssetDatabase.LoadAssetAtPath<EnemyActionProfileDefinition>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<EnemyActionProfileDefinition>();
                    AssetDatabase.CreateAsset(profile, path);
                }

                var linked = !string.IsNullOrWhiteSpace(spec.LinkedAttackId) &&
                             attacks.TryGetValue($"{spec.OwnerId}:{spec.LinkedAttackId}", out var attack)
                    ? attack
                    : null;
                profile.Configure(spec, linked);
                EditorUtility.SetDirty(profile);
                result[$"{spec.OwnerId}:{spec.ActionId}"] = profile;
            }

            return result;
        }

        private static Dictionary<string, EnemyBehaviorTreeDefinition> GenerateBehaviorTrees()
        {
            var result = new Dictionary<string, EnemyBehaviorTreeDefinition>();
            foreach (var spawnKind in BodyCreatureSpawnKinds)
            {
                var tree = EnemyBehaviorTreeDefaults.CreateEnemyTree(spawnKind);
                var path = $"{TreeDirectory}/{EnemyBehaviorTreeDefaults.AssetNameForEnemy(spawnKind)}";
                if (AssetDatabase.LoadAssetAtPath<EnemyBehaviorTreeDefinition>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }

                AssetDatabase.CreateAsset(tree, path);
                foreach (var node in tree.Nodes.Where(node => node != null))
                {
                    AssetDatabase.AddObjectToAsset(node, tree);
                }

                EditorUtility.SetDirty(tree);
                result[spawnKind] = tree;
            }

            return result;
        }

        private static Dictionary<string, EnemyDefinition> GenerateEnemyAssets(
            IReadOnlyDictionary<string, EnemyAttackProfileDefinition> attacks,
            IReadOnlyDictionary<string, EnemyActionProfileDefinition> actions,
            IReadOnlyDictionary<string, EnemyBehaviorTreeDefinition> trees)
        {
            var result = new Dictionary<string, EnemyDefinition>();
            foreach (var spec in NewCreatureRows())
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
                    spec.MovementMode,
                    spec.MaxHealth,
                    spec.SpeedMetersPerSecond,
                    1,
                    1f,
                    spec.RadiusMeters,
                    spec.AttackRangeMeters,
                    1.4f,
                    0,
                    5f,
                    0f,
                    2f,
                    "spawnEnemyNormal",
                    0,
                    spec.BodyClass,
                    spec.Intelligence,
                    spec.Disposition,
                    spec.PreferredRangeMinMeters,
                    spec.PreferredRangeMaxMeters,
                    spec.Color);
                var lunge = EnemyDefinition.DefaultLungeFor(spec.ArchetypeId, spec.BehaviorId, spec.MovementMode);
                enemy.ConfigureSenseAndLunge(spec.SightRadiusMeters, spec.SightAngleDegrees, spec.HearingRadiusMeters, lunge.enabled, lunge.triggerRange, lunge.windup, lunge.active, lunge.distance, lunge.cooldown);
                enemy.ConfigureContactPolicy(EnemyContactDamagePolicy.ActiveOnly, EnemyPassiveContactHazardType.None);
                var execution = EnemyDefinition.DefaultAttackExecutionFor(spec.ArchetypeId, spec.BehaviorId, spec.MovementMode);
                enemy.ConfigureAttackExecutionModifiers(execution.windupScale, execution.activeScale, execution.recoveryScale, execution.hitArcDegreesBonus, execution.poiseBreakThresholdOffset);
                enemy.ConfigureAttackProfiles(EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(row => row.OwnerId == spec.SpawnKind)
                    .Select(row => attacks.TryGetValue($"{row.OwnerId}:{row.AttackId}", out var profile) ? profile : null)
                    .Where(profile => profile != null));
                enemy.ConfigureActionProfiles(EnemyActionProfileDefaults.AllEnemySpecs
                    .Where(row => row.OwnerId == spec.SpawnKind)
                    .Select(row => actions.TryGetValue($"{row.OwnerId}:{row.ActionId}", out var profile) ? profile : null)
                    .Where(profile => profile != null));
                enemy.ConfigureBehaviorTree(trees.TryGetValue(spec.SpawnKind, out var tree) ? tree : null);
                EditorUtility.SetDirty(enemy);
                result[spec.SpawnKind] = enemy;
            }

            return result;
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
            var catalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone48AssetGenerator.EncounterCatalogPath);
            if (catalog == null)
            {
                return;
            }

            var encounters = new[]
            {
                SaveEncounter("Encounter_M85_HollowBirdPerch.asset", "m85_hollow_bird_perch", "M85 Hollow Bird Perch", new[] { new EncounterSpawnEntry("spawnEnemyHollowBird", 2), new EncounterSpawnEntry("spawnEnemySpider", 1) }),
                SaveEncounter("Encounter_M85_HollowBeastDen.asset", "m85_hollow_beast_den", "M85 Hollow Beast Den", new[] { new EncounterSpawnEntry("spawnEnemyHollowBeast", 1), new EncounterSpawnEntry("spawnEnemyRat", 2) }),
                SaveEncounter("Encounter_M85_RatSpiderSignal.asset", "m85_rat_spider_signal", "M85 Rat Spider Signal", new[] { new EncounterSpawnEntry("spawnEnemyRat", 3), new EncounterSpawnEntry("spawnEnemySpider", 3) }),
                SaveEncounter("Encounter_M85_MixedCreatureScramble.asset", "m85_mixed_creature_scramble", "M85 Mixed Creature Scramble", new[] { new EncounterSpawnEntry("spawnEnemyHollowBird", 1), new EncounterSpawnEntry("spawnEnemyHollowBeast", 1), new EncounterSpawnEntry("spawnEnemyRat", 2), new EncounterSpawnEntry("spawnEnemySpider", 2), new EncounterSpawnEntry("spawnEnemyNormal", 1) })
            };

            var combined = catalog.Encounters
                .Concat(encounters)
                .Where(encounter => encounter != null)
                .GroupBy(encounter => encounter.EncounterId)
                .Select(group => group.First())
                .OrderBy(encounter => encounter.EncounterId)
                .ToArray();
            catalog.Configure(catalog.CatalogId, combined, catalog.BossEncounter);
            EditorUtility.SetDirty(catalog);
        }

        private static EncounterDefinition SaveEncounter(string fileName, string encounterId, string displayName, IEnumerable<EncounterSpawnEntry> spawns)
        {
            var path = $"{EncounterDirectory}/{fileName}";
            var encounter = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(path);
            if (encounter == null)
            {
                encounter = ScriptableObject.CreateInstance<EncounterDefinition>();
                AssetDatabase.CreateAsset(encounter, path);
            }

            encounter.Configure(encounterId, displayName, BranchRoomRole.Combat, 0, 99, 1, 99, 2, spawns);
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static void GenerateCreatureRooms()
        {
            WriteCreatureRoom(
                "m85_hollow_bird_perch_room",
                "M85 Hollow Bird Perch Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyHollowBird, -3, 2), Spawn(RoomDesignerMarkerKinds.EnemyHollowBird, 1, -2), Spawn(RoomDesignerMarkerKinds.EnemySpider, 4, 1) },
                new[] { V(-6, -2), V(-2, 0), V(3, 2), V(6, -1) });
            WriteCreatureRoom(
                "m85_hollow_beast_den",
                "M85 Hollow Beast Den",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyHollowBeast, 1, 0), Spawn(RoomDesignerMarkerKinds.EnemyRat, -3, 1), Spawn(RoomDesignerMarkerKinds.EnemyRat, -2, -2) },
                new[] { V(-5, -1), V(-1, 2), V(3, -2), V(6, 1) });
            WriteCreatureRoom(
                "m85_rat_spider_signal_room",
                "M85 Rat Spider Signal Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyRat, -4, -1), Spawn(RoomDesignerMarkerKinds.EnemyRat, -3, 1), Spawn(RoomDesignerMarkerKinds.EnemySpider, 2, -2), Spawn(RoomDesignerMarkerKinds.EnemySpider, 4, 1) },
                new[] { V(-6, 2), V(-1, -2), V(1, 2), V(6, -1) });
            WriteCreatureRoom(
                "m85_mixed_body_creature_scramble",
                "M85 Mixed Body Creature Scramble",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyHollowBird, -4, 2), Spawn(RoomDesignerMarkerKinds.EnemyHollowBeast, 3, -1), Spawn(RoomDesignerMarkerKinds.EnemyRat, -2, -2), Spawn(RoomDesignerMarkerKinds.EnemySpider, 1, 2), Spawn(RoomDesignerMarkerKinds.EnemyFast, 5, 1) },
                new[] { V(-7, -2), V(-5, 1), V(-1, 0), V(2, -2), V(5, -1), V(7, 2) });
        }

        private static void WriteCreatureRoom(string roomId, string displayName, EnemySpawnMarker[] spawns, Vector2Int[] rocks)
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Wide2x1, displayName);
            project.projectId = roomId;
            project.displayName = displayName;
            project.cells.RemoveAll(cell => cell.kind == RoomDesignerCellKinds.Rock || cell.kind == RoomDesignerCellKinds.Hole || cell.kind == RoomDesignerCellKinds.Spike);
            project.markers.Clear();
            foreach (var rock in rocks)
            {
                project.cells.Add(new RoomDesignerCell(rock.x, rock.y, 0, RoomDesignerCellKinds.Rock));
            }

            project.markers.Add(new RoomDesignerMarker("spawn_safeStart", RoomDesignerMarkerKinds.SafeStart, -10f, 0f, 0f));
            for (var index = 0; index < spawns.Length; index++)
            {
                project.markers.Add(new RoomDesignerMarker($"spawn_enemy_{index:00}", spawns[index].Kind, spawns[index].X, 0f, spawns[index].Z));
            }

            project.markers.Add(new RoomDesignerMarker("spawn_reward_0", RoomDesignerMarkerKinds.RoomReward, 10f, 0f, 0f));
            foreach (var door in project.doorPorts)
            {
                door.state = RoomDesignerDoorKinds.Door;
            }

            var validation = RoomDesignerDraftValidator.Validate(project);
            if (!validation.IsValid)
            {
                throw new InvalidDataException($"M85 creature room '{roomId}' is not branch-ready: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = roomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = "m85-creature-action-expansion";
            manifest.hollowRuntime.prototypeStatus = "m85-curated-creature-room";
            var path = $"{CreatureRoomDirectory}/{roomId}.hollowruntime.json";
            File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static BranchRoomTemplateCatalogDefinition RefreshBranchTemplateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                return null;
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
            builder.AppendLine("# M85: Creature Action Expansion V1");
            builder.AppendLine();
            builder.AppendLine("M85 expands body-only creature combat toward Souls-lite readable commitment. Damage remains physical and active-window-only, and every damaging creature move still lands through an explicit active window. Ordinary body overlap stays harmless from M79. Movement actions are local bursts only and same-family signals affect only matching living non-boss enemies.");
            builder.AppendLine();
            builder.AppendLine("## New Creature Roster");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Spawn | HP | Speed | Body | Intelligence | Disposition | Preferred Range | Senses | Core Actions |");
            builder.AppendLine("| --- | --- | ---: | ---: | --- | --- | --- | --- | --- | --- |");
            foreach (var row in NewCreatureRows())
            {
                var attacks = EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(spec => spec.OwnerId == row.SpawnKind)
                    .Select(spec => $"`{spec.AttackId}`");
                builder.AppendLine($"| {row.DisplayName} | `{row.SpawnKind}` | {row.MaxHealth} | {row.SpeedMetersPerSecond:0.00}m/s | {row.BodyClass} | {row.Intelligence.DisplayLabel()} | {row.Disposition.ToSaveString()} | {row.PreferredRangeMinMeters:0.00}-{row.PreferredRangeMaxMeters:0.00}m | {row.SightRadiusMeters:0.0}m/{row.SightAngleDegrees:0}deg, hearing {row.HearingRadiusMeters:0.0}m | {string.Join(", ", attacks)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Creature Action Cards");
            builder.AppendLine();
            builder.AppendLine("| Owner | Action | Runtime | Damage | Force | Range | Timing | Move | Signal/Notes |");
            builder.AppendLine("| --- | --- | --- | ---: | --- | ---: | --- | ---: | --- |");
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => BodyCreatureSpawnKinds.Contains(spec.OwnerId) && PromotedCreatureActionIds.Contains(spec.AttackId)))
            {
                builder.AppendLine($"| {OwnerLabel(spec.OwnerId)} | `{spec.AttackId}` {spec.DisplayName} | {spec.RuntimeKind} | {spec.Damage} | {spec.ForceClass} | {spec.RangeMeters:0.00}m | {spec.WindupSeconds:0.00}/{spec.ActiveSeconds:0.00}/{spec.RecoverySeconds:0.00}s | {(spec.ActiveMovementDistanceMeters >= 0f ? $"{spec.ActiveMovementDistanceMeters:0.00}m" : "-")} | {spec.Notes} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Signal Rules");
            builder.AppendLine();
            builder.AppendLine("- `alarm_squeal`, `caw_signal`, and `howl_signal` emit `EnemyStimulusKind.CreatureSignal` only to nearby living non-boss enemies with the same creature family.");
            builder.AppendLine("- Signals do not wake bosses, unrelated enemies, or the entire room. They bias awareness/action choice but do not increase melee or ranged attack budget pressure.");
            builder.AppendLine("- Web feint and warning feint are non-damaging readable tells. They add no poison, bleed, web slow, or status effects.");
            builder.AppendLine();
            builder.AppendLine("## Local Burst Rules");
            builder.AppendLine();
            builder.AppendLine("- Swoop, strafe, skitter, leap-back, circle, and hop-back actions run through windup, active, and recovery states.");
            builder.AppendLine("- Movement bursts never deal damage by themselves. Only linked melee/area active windows can damage.");
            builder.AppendLine("- No pathfinding, obstacle LOS, squad navigation, passive creature body damage, boss runtime changes, or save schema changes are added.");
            builder.AppendLine();
            builder.AppendLine("## Existing Body-Only Upgrades");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Upgrade Actions |");
            builder.AppendLine("| --- | --- |");
            foreach (var owner in BodyCreatureSpawnKinds.Where(owner => !NewSpawnKinds.Contains(owner)))
            {
                var actions = EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(spec => spec.OwnerId == owner && PromotedCreatureActionIds.Contains(spec.AttackId))
                    .Select(spec => $"`{spec.AttackId}`");
                builder.AppendLine($"| {OwnerLabel(owner)} | {string.Join(", ", actions)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Curated Rooms");
            builder.AppendLine();
            foreach (var roomId in CreatureRoomIds)
            {
                builder.AppendLine($"- `{roomId}`");
            }

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M85 Creature Action Expansion Report

- Added creature enemies: {string.Join(", ", NewCreatureRows().Select(row => row.DisplayName))}.
- Body-creature runtime owners: {string.Join(", ", BodyCreatureSpawnKinds)}.
- Promoted creature actions: `{string.Join("`, `", PromotedCreatureActionIds)}`.
- Same-family signal stimulus: `{EnemyStimulusKind.CreatureSignal}`.
- New runtime kinds: `{EnemyAttackRuntimeKind.CreatureMove}`, `{EnemyAttackRuntimeKind.CreatureSignal}`.
- Encounter ids: {string.Join(", ", EncounterIds)}.
- Curated room ids: {string.Join(", ", CreatureRoomIds)}.
- Catalogue Markdown: `{DocsPath}`.
- Catalogue PDF target: `{PdfPath}`.
- Unity batchmode generator/test execution should be rerun when licensing is healthy.
");
        }

        private static void GeneratePdfWithReportLab()
        {
            if (!File.Exists(GeneratorScriptPath))
            {
                Debug.LogWarning($"M85 PDF generator script not found at {GeneratorScriptPath}.");
                return;
            }

            try
            {
                var startInfo = new DiagnosticsProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = GeneratorScriptPath,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var process = DiagnosticsProcess.Start(startInfo);
                if (process == null)
                {
                    Debug.LogWarning("M85 PDF generation did not start.");
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

                Debug.LogWarning($"M85 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M85 PDF generation skipped: {exception.Message}");
            }
        }

        private static string OwnerLabel(string spawnKind)
        {
            return spawnKind switch
            {
                "spawnEnemyNormal" => "Normal Chaser",
                "spawnEnemyFlying" => "Flying Chaser",
                "spawnEnemyFast" => "Fast Chaser",
                "spawnEnemyHeavy" => "Heavy Chaser",
                "spawnEnemyCharger" => "Ash Charger",
                "spawnEnemySplitter" => "Husk Splitter",
                "spawnEnemyRat" => "Rat",
                "spawnEnemySpider" => "Spider",
                "spawnEnemyHollowBird" => "Hollow Bird",
                "spawnEnemyHollowBeast" => "Hollow Beast",
                _ => spawnKind
            };
        }

        private static Vector2Int V(int x, int z)
        {
            return new Vector2Int(x, z);
        }

        private static EnemySpawnMarker Spawn(string kind, int x, int z)
        {
            return new EnemySpawnMarker(kind, x, z);
        }

        public readonly struct CreatureEnemySpec
        {
            public CreatureEnemySpec(
                string fileName,
                string spawnKind,
                string displayName,
                EnemyArchetypeId archetypeId,
                EnemyBehaviorId behaviorId,
                EnemyMovementMode movementMode,
                int maxHealth,
                float speedMetersPerSecond,
                float radiusMeters,
                float attackRangeMeters,
                EnemyBodyClass bodyClass,
                EnemyIntelligenceLevel intelligence,
                EnemyInstinctDisposition disposition,
                float preferredRangeMinMeters,
                float preferredRangeMaxMeters,
                float sightRadiusMeters,
                float sightAngleDegrees,
                float hearingRadiusMeters,
                PresentationPrefabRole prefabRole,
                MaterialRole materialRole,
                Color color)
            {
                FileName = fileName;
                SpawnKind = spawnKind;
                DisplayName = displayName;
                ArchetypeId = archetypeId;
                BehaviorId = behaviorId;
                MovementMode = movementMode;
                MaxHealth = maxHealth;
                SpeedMetersPerSecond = speedMetersPerSecond;
                RadiusMeters = radiusMeters;
                AttackRangeMeters = attackRangeMeters;
                BodyClass = bodyClass;
                Intelligence = intelligence;
                Disposition = disposition;
                PreferredRangeMinMeters = preferredRangeMinMeters;
                PreferredRangeMaxMeters = preferredRangeMaxMeters;
                SightRadiusMeters = sightRadiusMeters;
                SightAngleDegrees = sightAngleDegrees;
                HearingRadiusMeters = hearingRadiusMeters;
                PrefabRole = prefabRole;
                MaterialRole = materialRole;
                Color = color;
            }

            public string FileName { get; }
            public string SpawnKind { get; }
            public string DisplayName { get; }
            public EnemyArchetypeId ArchetypeId { get; }
            public EnemyBehaviorId BehaviorId { get; }
            public EnemyMovementMode MovementMode { get; }
            public int MaxHealth { get; }
            public float SpeedMetersPerSecond { get; }
            public float RadiusMeters { get; }
            public float AttackRangeMeters { get; }
            public EnemyBodyClass BodyClass { get; }
            public EnemyIntelligenceLevel Intelligence { get; }
            public EnemyInstinctDisposition Disposition { get; }
            public float PreferredRangeMinMeters { get; }
            public float PreferredRangeMaxMeters { get; }
            public float SightRadiusMeters { get; }
            public float SightAngleDegrees { get; }
            public float HearingRadiusMeters { get; }
            public PresentationPrefabRole PrefabRole { get; }
            public MaterialRole MaterialRole { get; }
            public Color Color { get; }
        }

        private readonly struct EnemySpawnMarker
        {
            public EnemySpawnMarker(string kind, int x, int z)
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
