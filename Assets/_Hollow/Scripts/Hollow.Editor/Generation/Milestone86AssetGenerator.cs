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
    public static class Milestone86AssetGenerator
    {
        public const string AttackDirectory = "Assets/_Hollow/Data/EnemyAttacks/M86";
        public const string ActionDirectory = "Assets/_Hollow/Data/EnemyActions/M86";
        public const string TreeDirectory = "Assets/_Hollow/Data/EnemyBehaviorTrees/M86";
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M86";
        public const string RangedRoomDirectory = "Assets/_Hollow/Data/Rooms/DesignerApproved/M86";
        public const string DocsPath = "Docs/Hollow_M86_Ranged_Firearm_Enemies.md";
        public const string ReportPath = "output/reports/m86_ranged_firearm_enemies.md";
        public const string PdfPath = "output/pdf/Hollow_M86_Ranged_Firearm_Enemies.pdf";
        public const string GeneratorScriptPath = "tools/generate_m86_ranged_firearm_enemies_pdf.py";
        public const string VerifyScriptPath = "tools/verify_m86_ranged_firearm_enemies_pdf.py";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        public static IReadOnlyList<string> SpawnKinds { get; } = new[]
        {
            "spawnEnemyHollowArcher",
            "spawnEnemyPowderGunner",
            "spawnEnemyKnifeThrower",
            "spawnEnemyRepeaterTurret",
            "spawnEnemyClockworkSentry"
        };

        public static IReadOnlyList<string> EncounterIds { get; } = new[]
        {
            "m86_archer_gallery",
            "m86_powder_checkpoint",
            "m86_thrower_alley",
            "m86_repeater_crossfire",
            "m86_clockwork_pattern_hall"
        };

        public static IReadOnlyList<string> RangedRoomIds { get; } = new[]
        {
            "m86_archer_gallery_room",
            "m86_powder_checkpoint_room",
            "m86_thrower_alley_room",
            "m86_repeater_crossfire_room",
            "m86_clockwork_pattern_hall_room"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 86 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(AttackDirectory);
            Directory.CreateDirectory(ActionDirectory);
            Directory.CreateDirectory(TreeDirectory);
            Directory.CreateDirectory(EncounterDirectory);
            Directory.CreateDirectory(RangedRoomDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var attacks = GenerateAttackProfiles();
            var actions = GenerateActionProfiles(attacks);
            var trees = GenerateBehaviorTrees();
            var enemies = GenerateEnemyAssets(attacks, actions, trees);
            RefreshEnemyCatalog(enemies);
            GenerateEncounterRotation();
            GenerateRangedRooms();
            RefreshBranchTemplateCatalog();
            WriteDocs();
            WriteReport();
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 86 ranged and firearm enemy assets.");
        }

        public static IReadOnlyList<RangedEnemySpec> EnemyRows()
        {
            return new[]
            {
                new RangedEnemySpec("Enemy_HollowArcher.asset", "spawnEnemyHollowArcher", "Hollow Archer", EnemyArchetypeId.Normal, EnemyBehaviorId.HollowArcher, 4, 1.35f, 0.31f, 7.5f, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Sentinel, 4f, 7.25f, 8.5f, 135f, 5.4f, PresentationPrefabRole.EnemyHollowArcher, MaterialRole.EnemyHollowArcher, new Color(0.45f, 0.52f, 0.36f, 1f)),
                new RangedEnemySpec("Enemy_PowderGunner.asset", "spawnEnemyPowderGunner", "Powder Gunner", EnemyArchetypeId.Heavy, EnemyBehaviorId.PowderGunner, 5, 1.05f, 0.36f, 8.8f, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, 4.75f, 8.5f, 9f, 115f, 6f, PresentationPrefabRole.EnemyPowderGunner, MaterialRole.EnemyPowderGunner, new Color(0.34f, 0.38f, 0.42f, 1f)),
                new RangedEnemySpec("Enemy_KnifeThrower.asset", "spawnEnemyKnifeThrower", "Knife Thrower", EnemyArchetypeId.Fast, EnemyBehaviorId.KnifeThrower, 4, 1.75f, 0.28f, 5.8f, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Territorial, 2.7f, 5.25f, 8f, 190f, 6.4f, PresentationPrefabRole.EnemyKnifeThrower, MaterialRole.EnemyKnifeThrower, new Color(0.5f, 0.43f, 0.62f, 1f)),
                new RangedEnemySpec("Enemy_RepeaterTurret.asset", "spawnEnemyRepeaterTurret", "Repeater Turret", EnemyArchetypeId.Heavy, EnemyBehaviorId.RepeaterTurret, 6, 0f, 0.42f, 9.25f, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, 6f, 9.25f, 10f, 95f, 3.2f, PresentationPrefabRole.EnemyRepeaterTurret, MaterialRole.EnemyRepeaterTurret, new Color(0.46f, 0.6f, 0.64f, 1f)),
                new RangedEnemySpec("Enemy_ClockworkSentry.asset", "spawnEnemyClockworkSentry", "Clockwork Sentry", EnemyArchetypeId.Heavy, EnemyBehaviorId.ClockworkSentry, 8, 0.65f, 0.44f, 7.8f, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Tactical, EnemyInstinctDisposition.Sentinel, 4.8f, 7.8f, 9f, 220f, 6.5f, PresentationPrefabRole.EnemyClockworkSentry, MaterialRole.EnemyClockworkSentry, new Color(0.62f, 0.56f, 0.42f, 1f))
            };
        }

        private static Dictionary<string, EnemyAttackProfileDefinition> GenerateAttackProfiles()
        {
            var result = new Dictionary<string, EnemyAttackProfileDefinition>();
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => SpawnKinds.Contains(spec.OwnerId)))
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
            foreach (var spec in EnemyActionProfileDefaults.AllEnemySpecs.Where(spec => SpawnKinds.Contains(spec.OwnerId)))
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
            foreach (var spawnKind in SpawnKinds)
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
                    0,
                    1f,
                    spec.RadiusMeters,
                    spec.AttackRangeMeters,
                    1.4f,
                    1,
                    5f,
                    0f,
                    1f,
                    "spawnEnemyNormal",
                    0,
                    spec.BodyClass,
                    spec.Intelligence,
                    spec.Disposition,
                    spec.PreferredRangeMinMeters,
                    spec.PreferredRangeMaxMeters,
                    spec.Color);
                enemy.ConfigureSenseAndLunge(spec.SightRadiusMeters, spec.SightAngleDegrees, spec.HearingRadiusMeters, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
                enemy.ConfigureContactPolicy(EnemyContactDamagePolicy.ActiveOnly, EnemyPassiveContactHazardType.None);
                var execution = EnemyDefinition.DefaultAttackExecutionFor(spec.ArchetypeId, spec.BehaviorId, EnemyMovementMode.Grounded);
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
                SaveEncounter("Encounter_M86_ArcherGallery.asset", "m86_archer_gallery", "M86 Archer Gallery", new[] { new EncounterSpawnEntry("spawnEnemyHollowArcher", 2), new EncounterSpawnEntry("spawnEnemySkeletonSpear", 1) }),
                SaveEncounter("Encounter_M86_PowderCheckpoint.asset", "m86_powder_checkpoint", "M86 Powder Checkpoint", new[] { new EncounterSpawnEntry("spawnEnemyPowderGunner", 1), new EncounterSpawnEntry("spawnEnemySkeletonSword", 1), new EncounterSpawnEntry("spawnEnemyHollowArcher", 1) }),
                SaveEncounter("Encounter_M86_ThrowerAlley.asset", "m86_thrower_alley", "M86 Thrower Alley", new[] { new EncounterSpawnEntry("spawnEnemyKnifeThrower", 2), new EncounterSpawnEntry("spawnEnemyRat", 2) }),
                SaveEncounter("Encounter_M86_RepeaterCrossfire.asset", "m86_repeater_crossfire", "M86 Repeater Crossfire", new[] { new EncounterSpawnEntry("spawnEnemyRepeaterTurret", 2), new EncounterSpawnEntry("spawnEnemyFast", 1) }),
                SaveEncounter("Encounter_M86_ClockworkPatternHall.asset", "m86_clockwork_pattern_hall", "M86 Clockwork Pattern Hall", new[] { new EncounterSpawnEntry("spawnEnemyClockworkSentry", 1), new EncounterSpawnEntry("spawnEnemyRepeaterTurret", 1), new EncounterSpawnEntry("spawnEnemyKnifeThrower", 1) })
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

        private static void GenerateRangedRooms()
        {
            WriteRangedRoom(
                "m86_archer_gallery_room",
                "M86 Archer Gallery Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyHollowArcher, -2, 2), Spawn(RoomDesignerMarkerKinds.EnemyHollowArcher, 2, -2), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSpear, 4, 0) },
                new[] { V(-5, -1), V(-3, 1), V(0, 0), V(3, -1), V(5, 1) });
            WriteRangedRoom(
                "m86_powder_checkpoint_room",
                "M86 Powder Checkpoint Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyPowderGunner, 2, 0), Spawn(RoomDesignerMarkerKinds.EnemyHollowArcher, -1, 2), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSword, 4, -2) },
                new[] { V(-6, -2), V(-4, 2), V(0, -1), V(5, 1), V(7, -2) });
            WriteRangedRoom(
                "m86_thrower_alley_room",
                "M86 Thrower Alley Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyKnifeThrower, -1, -2), Spawn(RoomDesignerMarkerKinds.EnemyKnifeThrower, 2, 2), Spawn(RoomDesignerMarkerKinds.EnemyRat, 4, 0) },
                new[] { V(-7, 1), V(-4, -1), V(-1, 1), V(2, -1), V(5, 2) });
            WriteRangedRoom(
                "m86_repeater_crossfire_room",
                "M86 Repeater Crossfire Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyRepeaterTurret, -1, 2), Spawn(RoomDesignerMarkerKinds.EnemyRepeaterTurret, 3, -2), Spawn(RoomDesignerMarkerKinds.EnemyFast, 5, 1) },
                new[] { V(-6, -2), V(-3, 0), V(0, 2), V(3, 0), V(6, -2) });
            WriteRangedRoom(
                "m86_clockwork_pattern_hall_room",
                "M86 Clockwork Pattern Hall Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyClockworkSentry, 1, 0), Spawn(RoomDesignerMarkerKinds.EnemyRepeaterTurret, 5, 2), Spawn(RoomDesignerMarkerKinds.EnemyKnifeThrower, -2, -2) },
                new[] { V(-7, 2), V(-5, -1), V(-2, 1), V(2, -2), V(5, -1), V(7, 2) });
        }

        private static void WriteRangedRoom(string roomId, string displayName, EnemySpawnMarker[] spawns, Vector2Int[] rocks)
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
                throw new InvalidDataException($"M86 ranged room '{roomId}' is not branch-ready: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = roomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = "m86-ranged-firearm-room";
            manifest.hollowRuntime.prototypeStatus = "m86-curated-ranged-room";
            var path = $"{RangedRoomDirectory}/{roomId}.hollowruntime.json";
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
            builder.AppendLine("# M86: Ranged + Firearm Enemies V1");
            builder.AppendLine();
            builder.AppendLine("M86 adds a ranged enemy family with Dark Souls-inspired commitment: enemies draw, aim, fire during an active point, then recover. Ranged pressure is profile-driven through M76 attack profiles, M81 action metadata, M82 behavior trees, M80 active windows, and M79 harmless ordinary body contact.");
            builder.AppendLine();
            builder.AppendLine("## Roster Cards");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Spawn | HP | Speed | Body | Intelligence | Disposition | Preferred Range | Sight | Hearing | Identity |");
            builder.AppendLine("| --- | --- | ---: | ---: | --- | --- | --- | --- | --- | ---: | --- |");
            foreach (var row in EnemyRows())
            {
                builder.AppendLine($"| {row.DisplayName} | `{row.SpawnKind}` | {row.MaxHealth} | {row.SpeedMetersPerSecond:0.00}m/s | {row.BodyClass} | {row.Intelligence.DisplayLabel()} | {row.Disposition.ToSaveString()} | {row.PreferredRangeMinMeters:0.00}-{row.PreferredRangeMaxMeters:0.00}m | {row.SightRadiusMeters:0.0}m/{row.SightAngleDegrees:0}deg | {row.HearingRadiusMeters:0.0}m | {IdentityFor(row.SpawnKind)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Attack Profiles");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Attack | Runtime | Damage | Force | Range | Count | Speed | Timing | Knockback | Notes |");
            builder.AppendLine("| --- | --- | --- | ---: | --- | ---: | ---: | ---: | --- | ---: | --- |");
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => SpawnKinds.Contains(spec.OwnerId)))
            {
                builder.AppendLine($"| {OwnerLabel(spec.OwnerId)} | `{spec.AttackId}` {spec.DisplayName} | {spec.RuntimeKind} | {spec.Damage} | {spec.ForceClass} | {spec.RangeMeters:0.00}m | {spec.ProjectileCount} | {spec.ProjectileSpeedMetersPerSecond:0.0}m/s | {spec.WindupSeconds:0.00}/{spec.ActiveSeconds:0.00}/{spec.RecoverySeconds:0.00}s | {spec.KnockbackMeters:0.00}m | {spec.Notes} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Runtime Rules");
            builder.AppendLine();
            builder.AppendLine("- `StartRangedAction` can now be profile-specific, so trees can ask whether `arrow_volley`, `scatter_shot`, or `clockwork_radial` is actually in range before committing.");
            builder.AppendLine("- Non-boss ranged attacks fire only at the active transition, then enter recovery. Windup and recovery do not spawn projectiles.");
            builder.AppendLine("- `Projectile`, `FanProjectile`, and `RadialProjectile` are supported for normal enemies. Fan and radial patterns use projectile count, speed, range, force, and knockback from the linked profile.");
            builder.AppendLine("- Ranged and firearm enemies respect the existing ranged/charge attack budget, including the M72 Tactical/Cunning priority tie-break without increasing total pressure.");
            builder.AppendLine("- Ordinary body overlap remains harmless and only disturbs/alerts; no passive contact damage is reintroduced.");
            builder.AppendLine();
            builder.AppendLine("## Encounters And Rooms");
            builder.AppendLine();
            builder.AppendLine($"Encounter ids: {string.Join(", ", EncounterIds.Select(id => $"`{id}`"))}.");
            builder.AppendLine($"Curated rooms: {string.Join(", ", RangedRoomIds.Select(id => $"`{id}`"))}.");
            builder.AppendLine();
            builder.AppendLine("## M87 Bridge");
            builder.AppendLine();
            builder.AppendLine("M86 keeps damage physical and weapon/machine based. The same profile-specific ranged path is ready for M87 magic, ghost, soul, curse, and area-pressure casters without giving those enemies a separate projectile system.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M86 Ranged + Firearm Enemies Report

- Added enemy definitions: {string.Join(", ", EnemyRows().Select(row => row.DisplayName))}.
- Added profile-specific ranged behavior tree checks with `{EnemyBehaviorConditionKind.CanStartRangedAction}`.
- Non-boss ranged runtime now supports single, fan, and radial projectile patterns from attack profiles.
- Encounter ids: {string.Join(", ", EncounterIds)}.
- Curated ranged room ids: {string.Join(", ", RangedRoomIds)}.
- Catalogue Markdown: `{DocsPath}`.
- Catalogue PDF target: `{PdfPath}`.
- Local PDF extraction verification script: `{VerifyScriptPath}`.
- Unity batchmode validation and EditMode results should be recorded under `output/reports/`.
");
        }

        private static void GeneratePdfWithReportLab()
        {
            if (!File.Exists(GeneratorScriptPath))
            {
                Debug.LogWarning($"M86 PDF generator script not found at {GeneratorScriptPath}.");
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
                    Debug.LogWarning("M86 PDF generation did not start.");
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

                Debug.LogWarning($"M86 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M86 PDF generation skipped: {exception.Message}");
            }
        }

        private static string OwnerLabel(string spawnKind)
        {
            return EnemyRows().FirstOrDefault(row => row.SpawnKind == spawnKind).DisplayName ?? spawnKind;
        }

        private static string IdentityFor(string spawnKind)
        {
            return spawnKind switch
            {
                "spawnEnemyHollowArcher" => "bow user, aimed shot and volley",
                "spawnEnemyPowderGunner" => "firearm user, slow heavy aim and scatter",
                "spawnEnemyKnifeThrower" => "thrower skirmisher, quick knives and evasive range",
                "spawnEnemyRepeaterTurret" => "stationary machine turret, burst/fan pressure",
                "spawnEnemyClockworkSentry" => "slow machine, radial and rotating projectile patterns",
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

        public readonly struct RangedEnemySpec
        {
            public RangedEnemySpec(
                string fileName,
                string spawnKind,
                string displayName,
                EnemyArchetypeId archetypeId,
                EnemyBehaviorId behaviorId,
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
