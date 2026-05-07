using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.RoomDesigner;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone115AssetGenerator
    {
        public const string AttackDirectory = "Assets/_Hollow/Data/EnemyAttacks/M115";
        public const string ActionDirectory = "Assets/_Hollow/Data/EnemyActions/M115";
        public const string TreeDirectory = "Assets/_Hollow/Data/EnemyBehaviorTrees/M115";
        public const string EncounterDirectory = "Assets/_Hollow/Data/Encounters/M115";
        public const string MechanicalRoomDirectory = "Assets/_Hollow/Data/Rooms/DesignerApproved/M115";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        public static IReadOnlyList<string> SpawnKinds { get; } = new[]
        {
            "spawnEnemyStarforgedOctantSentry",
            "spawnEnemyCrimsonRailSpider",
            "spawnEnemyAzureMinigunTurret"
        };

        public static IReadOnlyList<string> EncounterIds { get; } = new[]
        {
            "m115_starforged_octant_sentry",
            "m115_crimson_rail_spider",
            "m115_azure_minigun_turret"
        };

        public static IReadOnlyList<string> MechanicalRoomIds { get; } = new[]
        {
            "m115_starforged_octant_sentry_room",
            "m115_crimson_rail_spider_room",
            "m115_azure_minigun_turret_room"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 115 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(AttackDirectory);
            Directory.CreateDirectory(ActionDirectory);
            Directory.CreateDirectory(TreeDirectory);
            Directory.CreateDirectory(EncounterDirectory);
            Directory.CreateDirectory(MechanicalRoomDirectory);
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassRoot);
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassMaterialDirectory);

            var visuals = GenerateMechanicalVisuals();
            var attacks = GenerateAttackProfiles();
            var actions = GenerateActionProfiles(attacks);
            var trees = GenerateBehaviorTrees();
            var enemies = GenerateEnemyAssets(attacks, actions, trees);
            RefreshEnemyCatalog(enemies);
            RefreshPresentationCatalog(visuals);
            GenerateEncounterRotation();
            GenerateMechanicalRooms();
            RefreshBranchTemplateCatalog();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 115 Meshy mechanical enemies.");
        }

        public static IReadOnlyList<MechanicalEnemySpec> EnemyRows()
        {
            return new[]
            {
                new MechanicalEnemySpec(
                    "Enemy_StarforgedOctantSentry.asset",
                    "spawnEnemyStarforgedOctantSentry",
                    "Starforged Octant Sentry",
                    EnemyArchetypeId.Heavy,
                    EnemyBehaviorId.OctantSentry,
                    20,
                    0f,
                    0.46f,
                    7.8f,
                    1,
                    5.8f,
                    EnemyBodyClass.Heavy,
                    EnemyIntelligenceLevel.Trained,
                    EnemyInstinctDisposition.Sentinel,
                    5.2f,
                    7.8f,
                    9.5f,
                    360f,
                    5.5f,
                    PresentationPrefabRole.EnemyStarforgedOctantSentry,
                    MaterialRole.EnemyStarforgedOctantSentry,
                    new Color(0.72f, 0.62f, 0.44f, 1f),
                    new Color(1.25f, 0.9f, 0.38f, 1f),
                    new Vector3(0.92f, 0.86f, 0.92f),
                    "Assets/MeshyImports/Meshy_Model_20260507_013338/Meshy_AI_Starforged_Sentinel_0507003311_texture.fbx"),
                new MechanicalEnemySpec(
                    "Enemy_CrimsonRailSpider.asset",
                    "spawnEnemyCrimsonRailSpider",
                    "Crimson Rail Spider",
                    EnemyArchetypeId.Fast,
                    EnemyBehaviorId.RailSpider,
                    10,
                    1.25f,
                    0.36f,
                    9f,
                    3,
                    0.1f,
                    EnemyBodyClass.Medium,
                    EnemyIntelligenceLevel.Tactical,
                    EnemyInstinctDisposition.Territorial,
                    5.2f,
                    8.4f,
                    10f,
                    170f,
                    6.5f,
                    PresentationPrefabRole.EnemyCrimsonRailSpider,
                    MaterialRole.EnemyCrimsonRailSpider,
                    new Color(0.62f, 0.22f, 0.2f, 1f),
                    new Color(1.35f, 0.28f, 0.12f, 1f),
                    new Vector3(0.9f, 0.72f, 1.05f),
                    "Assets/MeshyImports/Meshy_Model_20260507_013400/Meshy_AI_Crimson_Sentinel_0507003350_texture.fbx"),
                new MechanicalEnemySpec(
                    "Enemy_AzureMinigunTurret.asset",
                    "spawnEnemyAzureMinigunTurret",
                    "Azure Minigun Turret",
                    EnemyArchetypeId.Heavy,
                    EnemyBehaviorId.MinigunTurret,
                    20,
                    0f,
                    0.46f,
                    8.5f,
                    1,
                    9.5f,
                    EnemyBodyClass.Heavy,
                    EnemyIntelligenceLevel.Trained,
                    EnemyInstinctDisposition.Sentinel,
                    5.8f,
                    8.5f,
                    9.5f,
                    300f,
                    5.5f,
                    PresentationPrefabRole.EnemyAzureMinigunTurret,
                    MaterialRole.EnemyAzureMinigunTurret,
                    new Color(0.24f, 0.66f, 0.9f, 1f),
                    new Color(0.2f, 1.15f, 1.5f, 1f),
                    new Vector3(0.94f, 0.78f, 0.94f),
                    "Assets/MeshyImports/Meshy_Model_20260507_013436/Meshy_AI_Azure_Ion_Turret_0507003424_texture.fbx")
            };
        }

        private static Dictionary<PresentationPrefabRole, VisualBinding> GenerateMechanicalVisuals()
        {
            var result = new Dictionary<PresentationPrefabRole, VisualBinding>();
            foreach (var spec in EnemyRows())
            {
                var material = CreateOrUpdateMeshyMaterial(spec);
                var prefab = CreateOrUpdateMeshyPrefab(spec, material);
                result[spec.PrefabRole] = new VisualBinding(spec.PrefabRole, spec.MaterialRole, material, prefab, spec.Color);
            }

            return result
                .Where(pair => pair.Value.Prefab != null || pair.Value.Material != null)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private static Material CreateOrUpdateMeshyMaterial(MechanicalEnemySpec spec)
        {
            ConfigureTextureImporter(spec.NormalPath, TextureImporterType.NormalMap);
            ConfigureLinearTextureImporter(spec.MetallicPath);
            ConfigureLinearTextureImporter(spec.RoughnessPath);

            var path = $"{Milestone23AssetGenerator.ArtPassMaterialDirectory}/AP_M_{spec.MaterialRole}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Universal Render Pipeline/Lit") ?? material.shader;
            SetColor(material, "_BaseColor", Color.white);
            SetColor(material, "_Color", Color.white);
            SetTexture(material, "_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AlbedoPath));
            SetTexture(material, "_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AlbedoPath));
            SetTexture(material, "_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.NormalPath));
            SetTexture(material, "_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.MetallicPath));
            SetFloat(material, "_Metallic", 0.72f);
            SetFloat(material, "_Smoothness", 0.58f);
            SetTexture(material, "_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(spec.EmissionPath));
            SetColor(material, "_EmissionColor", spec.EmissionColor);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateOrUpdateMeshyPrefab(MechanicalEnemySpec spec, Material material)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(spec.FbxPath);
            if (source == null)
            {
                Debug.LogWarning($"M115 Meshy source missing for {spec.DisplayName}: {spec.FbxPath}");
                return null;
            }

            var root = new GameObject($"AP_{spec.PrefabRole}");
            try
            {
                root.AddComponent<PresentationVisualMarker>().Configure(spec.PrefabRole, isFallback: false);
                var model = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (model == null)
                {
                    model = Object.Instantiate(source);
                }

                model.name = "MeshyMechanicalModel";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                AssignMaterialToRenderers(model, material);
                StripGameplayComponents(root);
                PresentationVisualBoundsFitter.FitToTargetBounds(model.transform, spec.TargetBounds, 0f);

                var path = $"{Milestone23AssetGenerator.ArtPassRoot}/AP_{spec.PrefabRole}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
                    spec.ProjectileDamage,
                    spec.ProjectileSpeedMetersPerSecond,
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
                enemy.ConfigureAttackExecutionModifiers(1f, 1f, 1f, 0f, 1);
                enemy.ConfigureAttackProfiles(EnemyAttackProfileDefaults.AllEnemySpecs
                    .Where(row => row.OwnerId == spec.SpawnKind)
                    .Select(row => attacks.TryGetValue($"{row.OwnerId}:{row.AttackId}", out var profile) ? profile : null)
                    .Where(profile => profile != null));
                enemy.ConfigureActionProfiles(EnemyActionProfileDefaults.AllEnemySpecs
                    .Where(row => row.OwnerId == spec.SpawnKind)
                    .Select(row => actions.TryGetValue($"{row.OwnerId}:{row.ActionId}", out var profile) ? profile : null)
                    .Where(profile => profile != null));
                enemy.ConfigureBehaviorTree(trees.TryGetValue(spec.SpawnKind, out var tree) ? tree : null);
                enemy.ConfigurePresentationRoles(true, spec.PrefabRole, false, default, false, default, false, default, false, default);
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

        private static void RefreshPresentationCatalog(IReadOnlyDictionary<PresentationPrefabRole, VisualBinding> visuals)
        {
            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(Milestone23AssetGenerator.ArtPassPalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MaterialPaletteDefinition>();
                AssetDatabase.CreateAsset(palette, Milestone23AssetGenerator.ArtPassPalettePath);
            }

            var newMaterialRoles = new HashSet<MaterialRole>(EnemyRows().Select(row => row.MaterialRole))
            {
                MaterialRole.CombatTelegraphTracking,
                MaterialRole.CombatTelegraphLocked
            };
            var materialBindings = palette.Bindings
                .Where(binding => !newMaterialRoles.Contains(binding.Role))
                .ToList();
            foreach (var row in EnemyRows())
            {
                if (visuals.TryGetValue(row.PrefabRole, out var visual) && visual.Material != null)
                {
                    materialBindings.Add(new MaterialRoleBinding(row.MaterialRole, visual.Material, row.Color));
                }
            }

            materialBindings.Add(new MaterialRoleBinding(
                MaterialRole.CombatTelegraphTracking,
                CreateOrUpdateSolidMaterial(MaterialRole.CombatTelegraphTracking, MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphTracking)),
                MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphTracking)));
            materialBindings.Add(new MaterialRoleBinding(
                MaterialRole.CombatTelegraphLocked,
                CreateOrUpdateSolidMaterial(MaterialRole.CombatTelegraphLocked, MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphLocked)),
                MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphLocked)));
            palette.Configure(materialBindings.OrderBy(binding => binding.Role.ToString(), StringComparer.Ordinal).ToArray());
            EditorUtility.SetDirty(palette);

            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var newPrefabRoles = new HashSet<PresentationPrefabRole>(EnemyRows().Select(row => row.PrefabRole));
            var prefabBindings = catalog.PrefabBindings
                .Where(binding => !newPrefabRoles.Contains(binding.Role))
                .ToList();
            foreach (var visual in visuals.Values)
            {
                if (visual.Prefab != null)
                {
                    prefabBindings.Add(new PresentationPrefabBinding(visual.PrefabRole, visual.Prefab));
                }
            }

            catalog.Configure(palette, catalog.VfxCues, catalog.AudioCues, prefabBindings.OrderBy(binding => binding.Role.ToString(), StringComparer.Ordinal).ToArray());
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
                SaveEncounter("Encounter_M115_StarforgedOctantSentry.asset", "m115_starforged_octant_sentry", "M115 Starforged Octant Sentry", new[] { new EncounterSpawnEntry("spawnEnemyStarforgedOctantSentry", 1) }),
                SaveEncounter("Encounter_M115_CrimsonRailSpider.asset", "m115_crimson_rail_spider", "M115 Crimson Rail Spider", new[] { new EncounterSpawnEntry("spawnEnemyCrimsonRailSpider", 1) }),
                SaveEncounter("Encounter_M115_AzureMinigunTurret.asset", "m115_azure_minigun_turret", "M115 Azure Minigun Turret", new[] { new EncounterSpawnEntry("spawnEnemyAzureMinigunTurret", 1) })
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

            encounter.Configure(encounterId, displayName, BranchRoomRole.Combat, 0, 99, 1, 99, 1, spawns);
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static void GenerateMechanicalRooms()
        {
            WriteMechanicalRoom(
                "m115_starforged_octant_sentry_room",
                "M115 Starforged Octant Sentry Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyStarforgedOctantSentry, 1, 0) },
                new[] { V(-5, -2), V(-3, 2), V(3, -2), V(5, 2) });
            WriteMechanicalRoom(
                "m115_crimson_rail_spider_room",
                "M115 Crimson Rail Spider Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyCrimsonRailSpider, 3, 0) },
                new[] { V(-6, 2), V(-3, -2), V(0, 2), V(6, -2) });
            WriteMechanicalRoom(
                "m115_azure_minigun_turret_room",
                "M115 Azure Minigun Turret Room",
                new[] { Spawn(RoomDesignerMarkerKinds.EnemyAzureMinigunTurret, 2, 0) },
                new[] { V(-5, 1), V(-2, -2), V(4, 2), V(6, -1) });
        }

        private static void WriteMechanicalRoom(string roomId, string displayName, EnemySpawnMarker[] spawns, Vector2Int[] rocks)
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
                throw new InvalidDataException($"M115 mechanical room '{roomId}' is not branch-ready: {string.Join("; ", validation.Errors)}");
            }

            var manifest = RoomDesignerCompiler.BuildManifest(project);
            manifest.hollowRuntime.canonicalRoomId = roomId;
            manifest.hollowRuntime.roomType = "combat";
            manifest.hollowRuntime.rewardType = "m115-mechanical-room";
            manifest.hollowRuntime.prototypeStatus = "m115-curated-mechanical-room";
            var path = $"{MechanicalRoomDirectory}/{roomId}.hollowruntime.json";
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

        private static void AssignMaterialToRenderers(GameObject root, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (var index = 0; index < slots.Length; index++)
                {
                    slots[index] = material;
                }

                renderer.sharedMaterials = slots;
            }
        }

        private static void StripGameplayComponents(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                Object.DestroyImmediate(rigidbody);
            }
        }

        private static Material CreateOrUpdateSolidMaterial(MaterialRole role, Color color)
        {
            var path = $"{Milestone23AssetGenerator.ArtPassMaterialDirectory}/AP_M_{role}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                AssetDatabase.CreateAsset(material, path);
            }

            SetColor(material, "_BaseColor", color);
            SetColor(material, "_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTextureImporter(string path, TextureImporterType type)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == type)
            {
                return;
            }

            importer.textureType = type;
            importer.SaveAndReimport();
        }

        private static void ConfigureLinearTextureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !importer.sRGBTexture)
            {
                return;
            }

            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        private static void SetTexture(Material material, string propertyName, Texture texture)
        {
            if (material != null && texture != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void SetColor(Material material, string propertyName, Color color)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static EnemySpawnMarker Spawn(string kind, float x, float z)
        {
            return new EnemySpawnMarker(kind, x, z);
        }

        private static Vector2Int V(int x, int z)
        {
            return new Vector2Int(x, z);
        }

        public readonly struct MechanicalEnemySpec
        {
            public MechanicalEnemySpec(
                string fileName,
                string spawnKind,
                string displayName,
                EnemyArchetypeId archetypeId,
                EnemyBehaviorId behaviorId,
                int maxHealth,
                float speedMetersPerSecond,
                float radiusMeters,
                float attackRangeMeters,
                int projectileDamage,
                float projectileSpeedMetersPerSecond,
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
                Color color,
                Color emissionColor,
                Vector3 targetBounds,
                string fbxPath)
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
                ProjectileDamage = projectileDamage;
                ProjectileSpeedMetersPerSecond = projectileSpeedMetersPerSecond;
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
                EmissionColor = emissionColor;
                TargetBounds = targetBounds;
                FbxPath = fbxPath;
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
            public int ProjectileDamage { get; }
            public float ProjectileSpeedMetersPerSecond { get; }
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
            public Color EmissionColor { get; }
            public Vector3 TargetBounds { get; }
            public string FbxPath { get; }
            public string TexturePrefix => FbxPath.EndsWith(".fbx", StringComparison.Ordinal) ? FbxPath.Substring(0, FbxPath.Length - 4) : FbxPath;
            public string AlbedoPath => $"{TexturePrefix}.png";
            public string EmissionPath => $"{TexturePrefix}_emission.png";
            public string MetallicPath => $"{TexturePrefix}_metallic.png";
            public string NormalPath => $"{TexturePrefix}_normal.png";
            public string RoughnessPath => $"{TexturePrefix}_roughness.png";
        }

        public readonly struct VisualBinding
        {
            public VisualBinding(PresentationPrefabRole prefabRole, MaterialRole materialRole, Material material, GameObject prefab, Color fallbackColor)
            {
                PrefabRole = prefabRole;
                MaterialRole = materialRole;
                Material = material;
                Prefab = prefab;
                FallbackColor = fallbackColor;
            }

            public PresentationPrefabRole PrefabRole { get; }
            public MaterialRole MaterialRole { get; }
            public Material Material { get; }
            public GameObject Prefab { get; }
            public Color FallbackColor { get; }
        }

        private readonly struct EnemySpawnMarker
        {
            public EnemySpawnMarker(string kind, float x, float z)
            {
                Kind = kind;
                X = x;
                Z = z;
            }

            public string Kind { get; }
            public float X { get; }
            public float Z { get; }
        }
    }
}
