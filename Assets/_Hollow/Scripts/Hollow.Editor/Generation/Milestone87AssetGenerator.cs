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
    public static class Milestone87AssetGenerator
    {
        public const string AttackDirectory = "Assets/_Hollow/Data/EnemyAttacks/M87";
        public const string ActionDirectory = "Assets/_Hollow/Data/EnemyActions/M87";
        public const string TreeDirectory = "Assets/_Hollow/Data/EnemyBehaviorTrees/M87";
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M87";
        public const string MagicRoomDirectory = "Assets/_Hollow/Data/Rooms/DesignerApproved/M87";
        public const string DocsPath = "Docs/Hollow_M87_Magic_Ghost_Soul_Enemies.md";
        public const string ReportPath = "output/reports/m87_magic_ghost_soul_enemies.md";
        public const string PdfPath = "output/pdf/Hollow_M87_Magic_Ghost_Soul_Enemies.pdf";
        public const string GeneratorScriptPath = "tools/generate_m87_magic_ghost_soul_enemies_pdf.py";
        public const string VerifyScriptPath = "tools/verify_m87_magic_ghost_soul_enemies_pdf.py";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        public static IReadOnlyList<string> SpawnKinds { get; } = new[]
        {
            "spawnEnemyHollowAcolyte",
            "spawnEnemyWraith",
            "spawnEnemySoulEater",
            "spawnEnemyCurseBinder",
            "spawnEnemyGraveLantern"
        };

        public static IReadOnlyList<string> EncounterIds { get; } = new[]
        {
            "m87_acolyte_rite",
            "m87_wraith_crossing",
            "m87_soul_eater_chapel",
            "m87_curse_binder_sigil",
            "m87_grave_lantern_pattern"
        };

        public static IReadOnlyList<string> MagicRoomIds { get; } = new[]
        {
            "m87_acolyte_rite_room",
            "m87_wraith_crossing_room",
            "m87_soul_eater_chapel_room",
            "m87_curse_binder_sigil_room",
            "m87_grave_lantern_pattern_room"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 87 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(AttackDirectory);
            Directory.CreateDirectory(ActionDirectory);
            Directory.CreateDirectory(TreeDirectory);
            Directory.CreateDirectory(EncounterDirectory);
            Directory.CreateDirectory(MagicRoomDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");

            var attacks = GenerateAttackProfiles();
            var actions = GenerateActionProfiles(attacks);
            var trees = GenerateBehaviorTrees();
            var enemies = GenerateEnemyAssets(attacks, actions, trees);
            RefreshEnemyCatalog(enemies);
            GenerateEncounterRotation();
            GenerateMagicRooms();
            RefreshBranchTemplateCatalog();
            WriteDocs();
            WriteReport();
            GeneratePdfWithReportLab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 87 magic, ghost, and soul enemy assets.");
        }

        public static IReadOnlyList<MagicEnemySpec> EnemyRows()
        {
            return new[]
            {
                new MagicEnemySpec("Enemy_HollowAcolyte.asset", "spawnEnemyHollowAcolyte", "Hollow Acolyte", EnemyArchetypeId.Normal, EnemyBehaviorId.HollowAcolyte, EnemyMovementMode.Grounded, 4, 1.05f, 0.31f, 7.2f, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, 3.8f, 6.8f, 8.4f, 180f, 6.2f, PresentationPrefabRole.EnemyHollowAcolyte, MaterialRole.EnemyHollowAcolyte, new Color(0.36f, 0.32f, 0.72f, 1f)),
                new MagicEnemySpec("Enemy_Wraith.asset", "spawnEnemyWraith", "Wraith", EnemyArchetypeId.Flying, EnemyBehaviorId.Wraith, EnemyMovementMode.Flying, 3, 1.75f, 0.28f, 6.4f, EnemyBodyClass.Light, EnemyIntelligenceLevel.Tactical, EnemyInstinctDisposition.Predator, 2.2f, 5.2f, 8.8f, 300f, 7f, PresentationPrefabRole.EnemyWraith, MaterialRole.EnemyWraith, new Color(0.66f, 0.88f, 1f, 0.92f)),
                new MagicEnemySpec("Enemy_SoulEater.asset", "spawnEnemySoulEater", "Soul Eater", EnemyArchetypeId.Heavy, EnemyBehaviorId.SoulEater, EnemyMovementMode.Grounded, 7, 1.2f, 0.38f, 5.8f, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Predator, 2.4f, 4.8f, 7.6f, 170f, 6f, PresentationPrefabRole.EnemySoulEater, MaterialRole.EnemySoulEater, new Color(0.12f, 0.34f, 0.38f, 1f)),
                new MagicEnemySpec("Enemy_CurseBinder.asset", "spawnEnemyCurseBinder", "Curse Binder", EnemyArchetypeId.Normal, EnemyBehaviorId.CurseBinder, EnemyMovementMode.Grounded, 5, 0.85f, 0.34f, 7.4f, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Tactical, EnemyInstinctDisposition.Territorial, 4f, 7f, 8.2f, 150f, 5.8f, PresentationPrefabRole.EnemyCurseBinder, MaterialRole.EnemyCurseBinder, new Color(0.56f, 0.34f, 0.64f, 1f)),
                new MagicEnemySpec("Enemy_GraveLantern.asset", "spawnEnemyGraveLantern", "Grave Lantern", EnemyArchetypeId.Heavy, EnemyBehaviorId.GraveLantern, EnemyMovementMode.Grounded, 6, 0f, 0.42f, 8.5f, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Sentinel, 5.5f, 8.5f, 9.2f, 240f, 7.2f, PresentationPrefabRole.EnemyGraveLantern, MaterialRole.EnemyGraveLantern, new Color(0.28f, 0.58f, 0.78f, 1f))
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
                    spec.MovementMode,
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
                SaveEncounter("Encounter_M87_AcolyteRite.asset", "m87_acolyte_rite", "M87 Acolyte Rite", new[] { new EncounterSpawnEntry("spawnEnemyHollowAcolyte", 2), new EncounterSpawnEntry("spawnEnemySkeletonSword", 1) }),
                SaveEncounter("Encounter_M87_WraithCrossing.asset", "m87_wraith_crossing", "M87 Wraith Crossing", new[] { new EncounterSpawnEntry("spawnEnemyWraith", 2), new EncounterSpawnEntry("spawnEnemyHollowBird", 1) }),
                SaveEncounter("Encounter_M87_SoulEaterChapel.asset", "m87_soul_eater_chapel", "M87 Soul Eater Chapel", new[] { new EncounterSpawnEntry("spawnEnemySoulEater", 1), new EncounterSpawnEntry("spawnEnemyHollowAcolyte", 1), new EncounterSpawnEntry("spawnEnemyRat", 2) }),
                SaveEncounter("Encounter_M87_CurseBinderSigil.asset", "m87_curse_binder_sigil", "M87 Curse Binder Sigil", new[] { new EncounterSpawnEntry("spawnEnemyCurseBinder", 1), new EncounterSpawnEntry("spawnEnemyKnifeThrower", 1), new EncounterSpawnEntry("spawnEnemySpider", 2) }),
                SaveEncounter("Encounter_M87_GraveLanternPattern.asset", "m87_grave_lantern_pattern", "M87 Grave Lantern Pattern", new[] { new EncounterSpawnEntry("spawnEnemyGraveLantern", 1), new EncounterSpawnEntry("spawnEnemyWraith", 1), new EncounterSpawnEntry("spawnEnemyClockworkSentry", 1) })
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

        private static void GenerateMagicRooms()
        {
            WriteMagicRoom(
                "m87_acolyte_rite_room",
                "M87 Acolyte Rite Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyHollowAcolyte, -1, 2), Spawn(RoomDesignerMarkerKinds.EnemyHollowAcolyte, 3, -2), Spawn(RoomDesignerMarkerKinds.EnemySkeletonSword, 5, 0) },
                new[] { V(-5, -2), V(-3, 1), V(0, 0), V(3, 1), V(6, -1) });
            WriteMagicRoom(
                "m87_wraith_crossing_room",
                "M87 Wraith Crossing Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyWraith, -2, 2), Spawn(RoomDesignerMarkerKinds.EnemyWraith, 3, -1), Spawn(RoomDesignerMarkerKinds.EnemyHollowBird, 5, 2) },
                new[] { V(-6, 1), V(-4, -2), V(-1, 0), V(2, 2), V(6, -2) });
            WriteMagicRoom(
                "m87_soul_eater_chapel_room",
                "M87 Soul Eater Chapel Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemySoulEater, 2, 0), Spawn(RoomDesignerMarkerKinds.EnemyHollowAcolyte, -2, 2), Spawn(RoomDesignerMarkerKinds.EnemyRat, 5, -2) },
                new[] { V(-6, -1), V(-4, 2), V(0, -2), V(4, 1), V(7, -1) });
            WriteMagicRoom(
                "m87_curse_binder_sigil_room",
                "M87 Curse Binder Sigil Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyCurseBinder, 2, 0), Spawn(RoomDesignerMarkerKinds.EnemySpider, -2, 2), Spawn(RoomDesignerMarkerKinds.EnemyKnifeThrower, 5, -2) },
                new[] { V(-6, 2), V(-3, -1), V(0, 2), V(3, -2), V(6, 1) });
            WriteMagicRoom(
                "m87_grave_lantern_pattern_room",
                "M87 Grave Lantern Pattern Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyGraveLantern, 1, 0), Spawn(RoomDesignerMarkerKinds.EnemyWraith, -3, 2), Spawn(RoomDesignerMarkerKinds.EnemyClockworkSentry, 5, -2) },
                new[] { V(-7, -2), V(-5, 1), V(-2, -1), V(3, 2), V(6, 0), V(8, -2) });
        }

        private static void WriteMagicRoom(string roomId, string displayName, EnemySpawnMarker[] spawns, Vector2Int[] rocks)
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
                throw new InvalidDataException($"M87 magic room '{roomId}' is not branch-ready: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = roomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = "m87-magic-ghost-soul-room";
            manifest.hollowRuntime.prototypeStatus = "m87-curated-magic-room";
            var path = $"{MagicRoomDirectory}/{roomId}.hollowruntime.json";
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
            builder.AppendLine("# M87: Magic/Ghost/Soul Enemies V1");
            builder.AppendLine();
            builder.AppendLine("M87 adds caster, ghost, soul-drain, curse, and magical pattern enemies while preserving M79 harmless ordinary body contact, M80 active windows, M82 idle-gated behavior trees, and M86 budgeted projectile pressure. The feel target is Dark Souls-style readable commitment in a faster top-down room.");
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
            builder.AppendLine("| Enemy | Attack | Runtime | Channel | Element | Damage | Force | Range | Count | Timing | Knockback | Notes |");
            builder.AppendLine("| --- | --- | --- | --- | --- | ---: | --- | ---: | ---: | --- | ---: | --- |");
            foreach (var spec in EnemyAttackProfileDefaults.AllEnemySpecs.Where(spec => SpawnKinds.Contains(spec.OwnerId)))
            {
                builder.AppendLine($"| {OwnerLabel(spec.OwnerId)} | `{spec.AttackId}` {spec.DisplayName} | {spec.RuntimeKind} | {spec.DamageChannel}/{spec.DamageDelivery} | {spec.DamageElement} | {spec.Damage} | {spec.ForceClass} | {spec.RangeMeters:0.00}m | {spec.ProjectileCount} | {spec.WindupSeconds:0.00}/{spec.ActiveSeconds:0.00}/{spec.RecoverySeconds:0.00}s | {spec.KnockbackMeters:0.00}m | {spec.Notes} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Runtime Rules");
            builder.AppendLine();
            builder.AppendLine("- `Beam` is a profile runtime kind for committed magical lane damage. It resolves damage once at the active transition using authored range, facing arc, force, knockback, and elemental classification.");
            builder.AppendLine("- `PhaseMove` is a non-damaging reposition action. It uses local burst movement, can ignore obstacles inside the same room bounds, and still has windup/active/recovery.");
            builder.AppendLine("- Soul and curse attacks are elemental metadata now. M87 does not add resistances, status buildup, stealth UI, pathfinding, LOS, alert sharing, or boss runtime changes.");
            builder.AppendLine("- Magic projectiles use the M86 projectile/fan/radial budget. Curse fields use the melee/area budget. Tactical/Cunning priority only breaks ties and does not increase total pressure.");
            builder.AppendLine("- Ordinary body overlap remains harmless and disturbing; Wraith and Soul Eater damage only through explicit active attacks.");
            builder.AppendLine();
            builder.AppendLine("## Encounters And Rooms");
            builder.AppendLine();
            builder.AppendLine($"Encounter ids: {string.Join(", ", EncounterIds.Select(id => $"`{id}`"))}.");
            builder.AppendLine($"Curated rooms: {string.Join(", ", MagicRoomIds.Select(id => $"`{id}`"))}.");
            builder.AppendLine();
            builder.AppendLine("## M88 Bridge");
            builder.AppendLine();
            builder.AppendLine("M87 deliberately keeps movement local. M88 should wrap pathfinding/local navigation behind an adapter so future casters can choose destinations, retreat points, and obstacle-aware lanes without replacing the combat action system.");

            File.WriteAllText(DocsPath, builder.ToString());
        }

        private static void WriteReport()
        {
            File.WriteAllText(ReportPath, $@"# M87 Magic/Ghost/Soul Enemies Report

- Added enemy definitions: {string.Join(", ", EnemyRows().Select(row => row.DisplayName))}.
- Added runtime kinds: `{EnemyAttackRuntimeKind.Beam}` and `{EnemyAttackRuntimeKind.PhaseMove}`.
- Added elemental Soul/Cursed projectile, beam, area, and phase action profiles.
- Encounter ids: {string.Join(", ", EncounterIds)}.
- Curated magic room ids: {string.Join(", ", MagicRoomIds)}.
- Catalogue Markdown: `{DocsPath}`.
- Catalogue PDF target: `{PdfPath}`.
- Local PDF extraction verification script: `{VerifyScriptPath}`.
- M88 should build navigation adapters around these profile/tree commands rather than adding one-off caster movement code.
");
        }

        private static void GeneratePdfWithReportLab()
        {
            if (!File.Exists(GeneratorScriptPath))
            {
                Debug.LogWarning($"M87 PDF generator script not found at {GeneratorScriptPath}.");
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
                    Debug.LogWarning("M87 PDF generation did not start.");
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

                Debug.LogWarning($"M87 PDF generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M87 PDF generation skipped: {exception.Message}");
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
                "spawnEnemyHollowAcolyte" => "caster, slow soul orb and radial rune pressure",
                "spawnEnemyWraith" => "ghost, phase movement, soul bolt, curse touch",
                "spawnEnemySoulEater" => "drain predator, beam lane and soul burst",
                "spawnEnemyCurseBinder" => "territorial curse caster, sigil fan and curse field",
                "spawnEnemyGraveLantern" => "stationary magical pattern turret",
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

        public readonly struct MagicEnemySpec
        {
            public MagicEnemySpec(
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
