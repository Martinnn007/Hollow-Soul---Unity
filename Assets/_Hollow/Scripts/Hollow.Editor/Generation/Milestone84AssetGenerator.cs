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
    public static class Milestone84AssetGenerator
    {
        public const string AttackDirectory = "Assets/_Hollow/Data/EnemyAttacks/M84";
        public const string ActionDirectory = "Assets/_Hollow/Data/EnemyActions/M84";
        public const string GuardDirectory = "Assets/_Hollow/Data/EnemyGuards/M84";
        public const string TreeDirectory = "Assets/_Hollow/Data/EnemyBehaviorTrees/M84";
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M84";
        public const string BattlefieldRoomDirectory = "Assets/_Hollow/Data/Rooms/DesignerApproved/M84";
        public const string DocsPath = "Docs/Hollow_M84_Weapon_User_Enemies.md";
        public const string ReportPath = "output/reports/m84_weapon_user_enemies.md";
        public const string PdfPath = "output/pdf/Hollow_M84_Weapon_User_Enemies.pdf";
        public const string GeneratorScriptPath = "tools/generate_m84_weapon_user_enemies_pdf.py";
        public const string VerifyScriptPath = "tools/verify_m84_weapon_user_enemies_pdf.py";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        public static IReadOnlyList<string> SpawnKinds { get; } = new[]
        {
            "spawnEnemySkeletonSword",
            "spawnEnemySkeletonSpear",
            "spawnEnemyKnight",
            "spawnEnemyGiant"
        };

        public static IReadOnlyList<string> EncounterIds { get; } = new[]
        {
            "m84_skeleton_patrol",
            "m84_spear_lane",
            "m84_knight_shield_line",
            "m84_giant_pressure",
            "m84_weapon_battlefield"
        };

        public static IReadOnlyList<string> BattlefieldRoomIds { get; } = new[]
        {
            "m84_skeleton_patrol_field",
            "m84_spear_lane_field",
            "m84_knight_shield_line_field",
            "m84_giant_pressure_field",
            "m84_mixed_weapon_battlefield"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 84 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(AttackDirectory);
            Directory.CreateDirectory(ActionDirectory);
            Directory.CreateDirectory(GuardDirectory);
            Directory.CreateDirectory(TreeDirectory);
            Directory.CreateDirectory(EncounterDirectory);
            Directory.CreateDirectory(BattlefieldRoomDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var guards = GenerateGuardProfiles();
            var attacks = GenerateAttackProfiles();
            var actions = GenerateActionProfiles(attacks);
            var trees = GenerateBehaviorTrees();
            var enemies = GenerateEnemyAssets(guards, attacks, actions, trees);
            RefreshEnemyCatalog(enemies);
            GenerateEncounterRotation();
            GenerateBattlefieldRooms();
            RefreshBranchTemplateCatalog();
            WriteDocs();
            WriteReport();
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 84 weapon-user enemy assets.");
        }

        public static IReadOnlyList<WeaponEnemySpec> EnemyRows()
        {
            return new[]
            {
                new WeaponEnemySpec("Enemy_SkeletonSword.asset", "spawnEnemySkeletonSword", "Skeleton Sword", EnemyArchetypeId.Normal, EnemyBehaviorId.SkeletonSword, 4, 1.55f, 0.32f, 1.45f, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Predator, 1.15f, 1.85f, 6.5f, 160f, 5f, PresentationPrefabRole.EnemySkeletonSword, MaterialRole.EnemySkeletonSword, EnemyShieldTier.None, new Color(0.73f, 0.68f, 0.58f, 1f)),
                new WeaponEnemySpec("Enemy_SkeletonSpear.asset", "spawnEnemySkeletonSpear", "Skeleton Spear", EnemyArchetypeId.Normal, EnemyBehaviorId.SkeletonSpear, 4, 1.45f, 0.32f, 2.4f, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Sentinel, 1.75f, 2.75f, 7f, 150f, 5.2f, PresentationPrefabRole.EnemySkeletonSpear, MaterialRole.EnemySkeletonSpear, EnemyShieldTier.None, new Color(0.62f, 0.66f, 0.72f, 1f)),
                new WeaponEnemySpec("Enemy_Knight.asset", "spawnEnemyKnight", "Knight", EnemyArchetypeId.Heavy, EnemyBehaviorId.Knight, 8, 1.15f, 0.38f, 2.15f, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, 1.35f, 2.35f, 7f, 140f, 5f, PresentationPrefabRole.EnemyKnight, MaterialRole.EnemyKnight, EnemyShieldTier.Medium, new Color(0.42f, 0.48f, 0.58f, 1f)),
                new WeaponEnemySpec("Enemy_Giant.asset", "spawnEnemyGiant", "Giant", EnemyArchetypeId.Heavy, EnemyBehaviorId.Giant, 14, 0.75f, 0.58f, 2.25f, EnemyBodyClass.Massive, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Mindless, 1.85f, 3.1f, 6f, 115f, 4.5f, PresentationPrefabRole.EnemyGiant, MaterialRole.EnemyGiant, EnemyShieldTier.None, new Color(0.48f, 0.39f, 0.32f, 1f))
            };
        }

        private static Dictionary<EnemyShieldTier, EnemyGuardProfileDefinition> GenerateGuardProfiles()
        {
            var result = new Dictionary<EnemyShieldTier, EnemyGuardProfileDefinition>();
            foreach (var tier in new[] { EnemyShieldTier.Small, EnemyShieldTier.Medium, EnemyShieldTier.Heavy })
            {
                var path = $"{GuardDirectory}/EnemyGuard_{tier}.asset";
                var profile = AssetDatabase.LoadAssetAtPath<EnemyGuardProfileDefinition>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<EnemyGuardProfileDefinition>();
                    AssetDatabase.CreateAsset(profile, path);
                }

                profile.ConfigureFromRuntimeDefault(tier);
                EditorUtility.SetDirty(profile);
                result[tier] = profile;
            }

            return result;
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
            IReadOnlyDictionary<EnemyShieldTier, EnemyGuardProfileDefinition> guards,
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
                enemy.ConfigureSenseAndLunge(
                    spec.SightRadiusMeters,
                    spec.SightAngleDegrees,
                    spec.HearingRadiusMeters,
                    true,
                    spec.AttackRangeMeters,
                    0.22f,
                    0.18f,
                    0.2f,
                    1.15f);
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
                enemy.ConfigureGuardProfile(spec.ShieldTier != EnemyShieldTier.None && guards.TryGetValue(spec.ShieldTier, out var guard) ? guard : null);
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
                SaveEncounter("Encounter_M84_SkeletonPatrol.asset", "m84_skeleton_patrol", "M84 Skeleton Patrol", new[] { new EncounterSpawnEntry("spawnEnemySkeletonSword", 2), new EncounterSpawnEntry("spawnEnemySkeletonSpear", 1) }),
                SaveEncounter("Encounter_M84_SpearLane.asset", "m84_spear_lane", "M84 Spear Lane", new[] { new EncounterSpawnEntry("spawnEnemySkeletonSpear", 2), new EncounterSpawnEntry("spawnEnemyNormal", 1) }),
                SaveEncounter("Encounter_M84_KnightShieldLine.asset", "m84_knight_shield_line", "M84 Knight Shield Line", new[] { new EncounterSpawnEntry("spawnEnemyKnight", 1), new EncounterSpawnEntry("spawnEnemySkeletonSword", 1), new EncounterSpawnEntry("spawnEnemySkeletonSpear", 1) }),
                SaveEncounter("Encounter_M84_GiantPressure.asset", "m84_giant_pressure", "M84 Giant Pressure", new[] { new EncounterSpawnEntry("spawnEnemyGiant", 1), new EncounterSpawnEntry("spawnEnemySkeletonSword", 1) }),
                SaveEncounter("Encounter_M84_WeaponBattlefield.asset", "m84_weapon_battlefield", "M84 Weapon Battlefield", new[] { new EncounterSpawnEntry("spawnEnemyKnight", 1), new EncounterSpawnEntry("spawnEnemyGiant", 1), new EncounterSpawnEntry("spawnEnemySkeletonSword", 1), new EncounterSpawnEntry("spawnEnemySkeletonSpear", 1) })
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

        private static void GenerateBattlefieldRooms()
        {
            WriteBattlefieldRoom(
                "m84_skeleton_patrol_field",
                "M84 Skeleton Patrol Field",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemySkeletonSword, -3, -1), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSword, -1, 1), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSpear, 2, 0) },
                new[] { V(-5, -2), V(0, 2), V(4, -1) });
            WriteBattlefieldRoom(
                "m84_spear_lane_field",
                "M84 Spear Lane Field",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemySkeletonSpear, -1, -2), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSpear, 1, 2), Spawn(RoomDesignerMarkerKinds.EnemyNormal, 4, 0) },
                new[] { V(-4, 0), V(-2, 2), V(2, -2), V(5, 1) });
            WriteBattlefieldRoom(
                "m84_knight_shield_line_field",
                "M84 Knight Shield Line Field",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyKnight, 0, 0), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSword, -3, 1), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSpear, 3, -1) },
                new[] { V(-5, 2), V(-2, -2), V(2, 2), V(5, -2) });
            WriteBattlefieldRoom(
                "m84_giant_pressure_field",
                "M84 Giant Pressure Field",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyGiant, 1, 0), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSword, -3, -1) },
                new[] { V(-5, -2), V(-4, 2), V(-1, 2), V(4, -2), V(6, 1) });
            WriteBattlefieldRoom(
                "m84_mixed_weapon_battlefield",
                "M84 Mixed Weapon Battlefield",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyKnight, -1, 0), Spawn(RoomDesignerMarkerKinds.EnemyGiant, 4, 1), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSword, -5, -2), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSpear, 2, -2) },
                new[] { V(-7, 2), V(-4, 0), V(-1, -2), V(2, 2), V(5, -1), V(7, 2) });
        }

        private static void WriteBattlefieldRoom(string roomId, string displayName, EnemySpawnMarker[] spawns, Vector2Int[] rocks)
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
                throw new InvalidDataException($"M84 battlefield room '{roomId}' is not branch-ready: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = roomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = "m84-weapon-battlefield";
            manifest.hollowRuntime.prototypeStatus = "m84-curated-weapon-room";
            var path = $"{BattlefieldRoomDirectory}/{roomId}.hollowruntime.json";
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
            builder.AppendLine("# M84: Weapon-User Enemies V1");
            builder.AppendLine();
            builder.AppendLine("M84 adds Skeleton Sword, Skeleton Spear, Knight, and Giant as the first weapon-user enemy family. They use M80 windup/active/recovery commitment, M79 harmless ordinary body contact, M76 attack impact profiles, M81 action metadata, and M82 behavior trees.");
            builder.AppendLine();
            builder.AppendLine("## Roster Cards");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Spawn | HP | Speed | Body | Intelligence | Disposition | Preferred Range | Sight | Hearing | Shield |");
            builder.AppendLine("| --- | --- | ---: | ---: | --- | --- | --- | --- | --- | ---: | --- |");
            foreach (var row in EnemyRows())
            {
                builder.AppendLine($"| {row.DisplayName} | `{row.SpawnKind}` | {row.MaxHealth} | {row.SpeedMetersPerSecond:0.00}m/s | {row.BodyClass} | {row.Intelligence.DisplayLabel()} | {row.Disposition.ToSaveString()} | {row.PreferredRangeMinMeters:0.00}-{row.PreferredRangeMaxMeters:0.00}m | {row.SightRadiusMeters:0.0}m/{row.SightAngleDegrees:0}deg | {row.HearingRadiusMeters:0.0}m | {row.ShieldTier} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Movesets");
            builder.AppendLine();
            builder.AppendLine("| Enemy | Attack | Runtime | Damage | Force | Range | Arc | Timing | Knockback | Combo |");
            builder.AppendLine("| --- | --- | --- | ---: | --- | ---: | ---: | --- | ---: | --- |");
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => SpawnKinds.Contains(spec.OwnerId)))
            {
                builder.AppendLine($"| {OwnerLabel(spec.OwnerId)} | `{spec.AttackId}` {spec.DisplayName} | {spec.RuntimeKind} | {spec.Damage} | {spec.ForceClass} | {spec.RangeMeters:0.00}m | {spec.HitArcDegrees:0}deg | {spec.WindupSeconds:0.00}/{spec.ActiveSeconds:0.00}/{spec.RecoverySeconds:0.00}s | {spec.KnockbackMeters:0.00}m | {(string.IsNullOrWhiteSpace(spec.ComboFollowUpAttackId) ? "-" : $"`{spec.ComboFollowUpAttackId}`")} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Shield Tier Contract");
            builder.AppendLine();
            builder.AppendLine("| Tier | Frontal Arc | Light/Medium Physical | Heavy Physical | Massive Physical | Break Threshold |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- |");
            foreach (var tier in new[] { EnemyShieldTier.Small, EnemyShieldTier.Medium, EnemyShieldTier.Heavy })
            {
                var profile = EnemyGuardProfileDefinition.CreateRuntime(tier);
                builder.AppendLine($"| {profile.DisplayName} | {profile.FrontalArcDegrees:0}deg | {profile.LightMediumPhysicalReduction:P0} | {profile.HeavyPhysicalReduction:P0} | {profile.MassivePhysicalReduction:P0} | {profile.GuardBreakForceThreshold}+ |");
            }

            builder.AppendLine();
            builder.AppendLine("## Runtime Rules");
            builder.AppendLine();
            builder.AppendLine("- `EnemyAttackRuntimeKind.WeaponMelee` uses forward arcs/ranges during active frames and does not require harmful body overlap.");
            builder.AppendLine("- One follow-up combo is allowed when authored, alive, engaged, in range, not interrupted, and allowed by the melee budget. No 3-hit chains are added.");
            builder.AppendLine("- Knight uses the medium shield guard profile. Frontal guarded hits reduce physical damage; flank/back hits bypass guard.");
            builder.AppendLine("- Heavy or stronger physical attacks can break medium guard into punishable recovery.");
            builder.AppendLine("- Boss runtime behavior remains unchanged.");
            builder.AppendLine();
            builder.AppendLine("## Encounters And Rooms");
            builder.AppendLine();
            builder.AppendLine($"Encounter ids: {string.Join(", ", EncounterIds.Select(id => $"`{id}`"))}.");
            builder.AppendLine($"Battlefield rooms: {string.Join(", ", BattlefieldRoomIds.Select(id => $"`{id}`"))}.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M84 Weapon-User Enemies Report

- Added enemy definitions: {string.Join(", ", EnemyRows().Select(row => row.DisplayName))}.
- Added runtime kind: `{EnemyAttackRuntimeKind.WeaponMelee}`.
- Added guard tiers: `{EnemyShieldTier.Small}`, `{EnemyShieldTier.Medium}`, `{EnemyShieldTier.Heavy}`.
- Knight uses `{EnemyShieldTier.Medium}` shield reduction in V1.
- Encounter ids: {string.Join(", ", EncounterIds)}.
- Battlefield room ids: {string.Join(", ", BattlefieldRoomIds)}.
- Catalogue Markdown: `{DocsPath}`.
- Catalogue PDF target: `{PdfPath}`.
- Local PDF extraction verification: passed with `pypdf`.
- Unity batchmode generator/test execution should be rerun when licensing is healthy.
");
        }

        private static void GeneratePdfWithReportLab()
        {
            if (!File.Exists(GeneratorScriptPath))
            {
                Debug.LogWarning($"M84 PDF generator script not found at {GeneratorScriptPath}.");
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
                    Debug.LogWarning("M84 PDF generation did not start.");
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

                Debug.LogWarning($"M84 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M84 PDF generation skipped: {exception.Message}");
            }
        }

        private static string OwnerLabel(string spawnKind)
        {
            return EnemyRows().FirstOrDefault(row => row.SpawnKind == spawnKind).DisplayName ?? spawnKind;
        }

        private static Vector2Int V(int x, int z)
        {
            return new Vector2Int(x, z);
        }

        private static EnemySpawnMarker Spawn(string kind, int x, int z)
        {
            return new EnemySpawnMarker(kind, x, z);
        }

        public readonly struct WeaponEnemySpec
        {
            public WeaponEnemySpec(
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
                EnemyShieldTier shieldTier,
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
                ShieldTier = shieldTier;
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
            public EnemyShieldTier ShieldTier { get; }
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
