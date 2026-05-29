using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using Hollow.UI.Shell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    [Serializable]
    public sealed class Milestone132BiomeWorldSelectionLockReport
    {
        public string lockId;
        public string title;
        public string generatedAtUtc;
        public bool passed;
        public string result;
        public int totalChecks;
        public int passedChecks;
        public string[] evidencePaths;
        public string[] failures;
        public Milestone132BiomeWorldSelectionLockCheck[] checks;
    }

    [Serializable]
    public sealed class Milestone132BiomeWorldSelectionLockCheck
    {
        public string id;
        public string category;
        public bool passed;
        public string detail;
    }

    public static class Milestone132BiomeWorldSelectionLockAssetGenerator
    {
        public const string LockId = "m132_biome_world_selection_lock_v1";
        public const string Title = "M132 Biome + World Selection Lock + Beta Art Pack";
        public const string CatalogId = "m132_beta_world_selection_catalog_v1";
        public const int TextureSize = 1024;
        public const string DocsPath = "Docs/Milestone132BiomeWorldSelectionLock.md";
        public const string M131ReportPath = "output/reports/m131_room_type_expansion_lock.md";
        public const string TextureRoot = "Assets/_Hollow/Art/Textures/M132";
        public const string BiomeRoomDirectory = "Assets/_Hollow/Data/Rooms/Biomes/M132";
        public const string BiomePrefabDirectory = "Assets/_Hollow/Prefabs/ArtPass/Biomes/M132";
        public const string BiomeResourceDirectory = "Assets/_Hollow/Resources/Hollow/Biomes";
        public const string WorldIdentityDirectory = "Assets/_Hollow/Data/Worlds/M132";
        public const string FramingDirectory = WorldIdentityDirectory + "/Framing";
        public const string RunFramingCatalogPath = WorldIdentityDirectory + "/RunFramingCatalog_M132.asset";
        public const string BiomeCatalogPath = BiomeResourceDirectory + "/RoomBiomeCatalog.asset";
        public const string ReportMarkdownPath = "output/reports/m132_biome_world_selection_lock.md";
        public const string ReportJsonPath = "output/reports/m132_biome_world_selection_lock.json";
        public const string M132TestsPath = "Assets/_Hollow/Tests/EditMode/Milestone132BiomeWorldSelectionLockTests.cs";

        private const string GeneratorPath = "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone132BiomeWorldSelectionLockAssetGenerator.cs";
        private const string ValidatorPath = "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone132BiomeWorldSelectionLockValidator.cs";
        private const string RoomBiomeIdsPath = "Assets/_Hollow/Scripts/Hollow.Data/Definitions/RoomBiomeIds.cs";
        private const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        private const string NextBranchPortalPath = "Assets/_Hollow/Scripts/Hollow.Branches/NextBranchPortal.cs";
        private const string PresentationResolverPath = "Assets/_Hollow/Scripts/Hollow.Presentation/RoomBiomePresentationResolver.cs";

        public static readonly string[] RequiredTextureFamilies =
        {
            "Floor",
            "Wall",
            "Rock",
            "Door",
            "OrganicDecor",
            "AccentTrim"
        };

        public static readonly string[] RequiredTextureMaps =
        {
            "BaseColor",
            "Normal",
            "Mask"
        };

        public static readonly MaterialRole[] RequiredMaterialRoles =
        {
            MaterialRole.RoomFloor,
            MaterialRole.RoomWall,
            MaterialRole.RoomWallTransparent,
            MaterialRole.RoomObstacleRock,
            MaterialRole.DoorActive,
            MaterialRole.DoorCleared,
            MaterialRole.DoorLocked,
            MaterialRole.DoorUnavailable,
            MaterialRole.DecorGrassTuft,
            MaterialRole.DecorCrystalCluster,
            MaterialRole.DecorSmallTree,
            MaterialRole.DecorStoneRuin,
            MaterialRole.NextBranchPortal
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        private static readonly RoomShapeSpec[] RoomShapes =
        {
            new("combat_macro_single_1x1", "single_1x1", "Single 1x1"),
            new("combat_macro_wide_2x1", "wide_2x1", "Wide 2x1"),
            new("combat_macro_tall_1x2", "tall_1x2", "Tall 1x2"),
            new("combat_macro_block_2x2", "block_2x2", "Block 2x2"),
            new("combat_macro_l_3cell", "l_3cell", "L 3-cell")
        };

        private static readonly WorldSpec[] WorldSpecs =
        {
            new(
                RoomBiomeIds.BeforeTeeth,
                "BeforeTeeth",
                1,
                "Before Teeth",
                "Prehistoric hunger preserved before language learned mercy.",
                new[] { WorldBiomeTag.Prehistoric, WorldBiomeTag.ShatteredTimeline },
                "bone white, wet fern, tar black, volcanic ember",
                "low amber stormlight with deep leaf shadow",
                "fossil ribs, cracked clay, fern mats, obsidian teeth",
                "The old world breathes under your feet, huge and unfinished.",
                "Every branch is a jaw waiting to become history.",
                "The anchor smells of rain before extinction.",
                "Something ancient stands where fear first got a name.",
                "The beast-era sinks back under black gravity.",
                new[] { "Rib Orchard", "Tar Choir", "Fern Grave", "First Maw" },
                new Color(0.78f, 0.68f, 0.5f, 1f),
                new Color(0.96f, 0.42f, 0.18f, 1f)),
            new(
                RoomBiomeIds.SunkenCartouche,
                "SunkenCartouche",
                2,
                "The Sunken Cartouche",
                "A drowned royal afterlife, rewritten by impossible tides.",
                new[] { WorldBiomeTag.AncientEgypt, WorldBiomeTag.Ritual, WorldBiomeTag.Memory },
                "lapis, sand-gold, oxidized teal, black river silt",
                "sun shafts through dust and water that should not be there",
                "carved stone, wet papyrus, cracked gold leaf, ritual ink",
                "A name is sealed in every wall, and none of them are yours.",
                "Branches open like tomb texts read in the wrong century.",
                "The anchor is a dry island in a drowned burial spell.",
                "The warden kneels before a crown the Star has already eaten.",
                "The cartouche closes. The name inside keeps scratching.",
                new[] { "Silt Throne", "Lapis Teeth", "False Nile", "Mummy Sun" },
                new Color(0.72f, 0.58f, 0.28f, 1f),
                new Color(0.16f, 0.72f, 0.9f, 1f)),
            new(
                RoomBiomeIds.RustChoir,
                "RustChoir",
                3,
                "The Rust Choir",
                "A fallen future still singing through broken machines.",
                new[] { WorldBiomeTag.FallenFuture, WorldBiomeTag.EndTimes },
                "rust orange, oil black, warning yellow, dead cyan",
                "failing neon under dust-heavy industrial haze",
                "corroded alloy, cracked screens, cable nests, coolant stains",
                "The future has died already. Its alarms keep practicing.",
                "Branches route power through rooms that forgot their purpose.",
                "The anchor hums like a generator under a battlefield.",
                "The warden is old code wearing stone like a uniform.",
                "The choir cuts out. Something beneath it keeps tone.",
                new[] { "Static Nave", "Oil Psalm", "Battery Grave", "Redline Atrium" },
                new Color(0.72f, 0.34f, 0.16f, 1f),
                new Color(1f, 0.72f, 0.12f, 1f))
        };

        public static IReadOnlyList<string> BetaBiomeIds => WorldSpecs.Select(spec => spec.BiomeId).ToArray();

        public static IReadOnlyList<string> BetaWorldDisplayNames => WorldSpecs.Select(spec => spec.DisplayName).ToArray();

        public static IEnumerable<string> RequiredTexturePaths =>
            from world in WorldSpecs
            from family in RequiredTextureFamilies
            from map in RequiredTextureMaps
            select TexturePath(world, family, map);

        public static IEnumerable<string> RequiredBiomePaths => WorldSpecs.Select(BiomePath);

        public static IEnumerable<string> RequiredRoomPaths =>
            from world in WorldSpecs
            from shape in RoomShapes
            select RoomPath(world, shape);

        public static IEnumerable<string> RequiredMaterialPaths =>
            from world in WorldSpecs
            from role in RequiredMaterialRoles
            select MaterialPath(world, role);

        [MenuItem("Hollow/Generation/Generate Milestone 132 Biome World Selection Lock")]
        public static void Generate()
        {
            GenerateAssets(assignScenes: true);
        }

        public static void GenerateBatch()
        {
            try
            {
                GenerateAssets(assignScenes: true);
                Debug.Log("Milestone 132 Biome + World Selection Lock generation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void GenerateAssets(bool assignScenes = true, bool saveAssets = true, bool refresh = true)
        {
            EnsureDirectories();
            ConfigureSourceTextures();
            var materials = GenerateMaterials();
            var prefabs = GenerateDecorPrefabs(materials);
            GenerateRoomTemplates();
            GenerateBiomeCatalog(materials, prefabs);
            var framingCatalog = GenerateRunFramingCatalog();
            if (assignScenes)
            {
                AssignToGameScenes(framingCatalog);
            }

            File.WriteAllText(DocsPath, BuildDocsMarkdown());
            var report = BuildReport();
            File.WriteAllText(ReportJsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(ReportMarkdownPath, ToMarkdown(report));

            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
            }

            if (refresh)
            {
                AssetDatabase.Refresh();
            }
        }

        public static Milestone132BiomeWorldSelectionLockReport BuildReport()
        {
            var checks = new List<Milestone132BiomeWorldSelectionLockCheck>();
            foreach (var path in RequiredEvidencePaths())
            {
                AddCheck(
                    checks,
                    $"evidence:{Path.GetFileName(path)}",
                    "Evidence",
                    File.Exists(path),
                    File.Exists(path) ? $"Found `{path}`." : $"Missing `{path}`.");
            }

            AddWorldOrderChecks(checks);
            AddTextureChecks(checks);
            AddBiomeAndMaterialChecks(checks);
            AddRuntimeChecks(checks);
            AddDocsChecks(checks);
            AddDependencyChecks(checks);

            var failures = checks
                .Where(check => !check.passed)
                .Select(check => $"{check.id}: {check.detail}")
                .ToArray();

            return new Milestone132BiomeWorldSelectionLockReport
            {
                lockId = LockId,
                title = Title,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                passed = failures.Length == 0,
                result = failures.Length == 0 ? "PASSED" : "FAILED",
                totalChecks = checks.Count,
                passedChecks = checks.Count(check => check.passed),
                evidencePaths = RequiredEvidencePaths().ToArray(),
                failures = failures,
                checks = checks.ToArray()
            };
        }

        public static string ToMarkdown(Milestone132BiomeWorldSelectionLockReport report)
        {
            var builder = new StringBuilder(8192);
            builder.AppendLine("# M132 Biome + World Selection Lock Report");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Lock id: `{report.lockId}`");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passing");
            builder.AppendLine("- Beta world order: `Before Teeth` -> `The Sunken Cartouche` -> `The Rust Choir`.");
            builder.AppendLine("- Prologue/fallback: `The Hollow Threshold` remains available but is not one of the three beta worlds.");
            builder.AppendLine("- Art policy: global chest silhouettes stay readable; room, door, decor, rock, and portal trim carry biome identity.");
            builder.AppendLine();
            builder.AppendLine("## Evidence");
            foreach (var path in report.evidencePaths ?? Array.Empty<string>())
            {
                builder.AppendLine($"- `{path}`");
            }

            builder.AppendLine();
            builder.AppendLine("## Checks");
            foreach (var check in report.checks ?? Array.Empty<Milestone132BiomeWorldSelectionLockCheck>())
            {
                builder.AppendLine($"- [{(check.passed ? "PASS" : "FAIL")}] `{check.id}` ({check.category}) - {check.detail}");
            }

            builder.AppendLine();
            builder.AppendLine("## Failures");
            if (report.failures == null || report.failures.Length == 0)
            {
                builder.AppendLine("None.");
            }
            else
            {
                foreach (var failure in report.failures)
                {
                    builder.AppendLine($"- {failure}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("## Next Gate");
            builder.AppendLine("M133 may build on this beta world order and biome art lock after M132 is reviewed and accepted.");
            return builder.ToString();
        }

        public static string TexturePath(string biomeId, string family, string map)
        {
            var world = WorldSpecs.First(spec => RoomBiomeIds.Matches(spec.BiomeId, biomeId));
            return TexturePath(world, family, map);
        }

        public static string BiomePath(string biomeId)
        {
            var world = WorldSpecs.First(spec => RoomBiomeIds.Matches(spec.BiomeId, biomeId));
            return BiomePath(world);
        }

        public static string RoomPath(string biomeId, string shapeSuffix)
        {
            var world = WorldSpecs.First(spec => RoomBiomeIds.Matches(spec.BiomeId, biomeId));
            var shape = RoomShapes.First(candidate => candidate.TargetSuffix == shapeSuffix);
            return RoomPath(world, shape);
        }

        public static string MaterialPath(string biomeId, MaterialRole role)
        {
            var world = WorldSpecs.First(spec => RoomBiomeIds.Matches(spec.BiomeId, biomeId));
            return MaterialPath(world, role);
        }

        private static IEnumerable<string> RequiredEvidencePaths()
        {
            return new[]
                {
                    DocsPath,
                    M131ReportPath,
                    GeneratorPath,
                    ValidatorPath,
                    M132TestsPath,
                    RoomBiomeIdsPath,
                    BranchSessionControllerPath,
                    NextBranchPortalPath,
                    PresentationResolverPath,
                    RunFramingCatalogPath,
                    BiomeCatalogPath
                }
                .Concat(RequiredTexturePaths)
                .Concat(RequiredBiomePaths)
                .Concat(RequiredRoomPaths)
                .Concat(RequiredMaterialPaths);
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(TextureRoot);
            Directory.CreateDirectory(BiomeRoomDirectory);
            Directory.CreateDirectory(BiomePrefabDirectory);
            Directory.CreateDirectory(BiomeResourceDirectory);
            Directory.CreateDirectory(WorldIdentityDirectory);
            Directory.CreateDirectory(FramingDirectory);
            Directory.CreateDirectory(Milestone23AssetGenerator.ArtPassMaterialDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportMarkdownPath) ?? "output/reports");
            foreach (var world in WorldSpecs)
            {
                Directory.CreateDirectory($"{BiomeRoomDirectory}/{world.AssetPrefix}");
                Directory.CreateDirectory($"{BiomePrefabDirectory}/{world.AssetPrefix}");
            }
        }

        private static void ConfigureSourceTextures()
        {
            foreach (var world in WorldSpecs)
            {
                foreach (var family in RequiredTextureFamilies)
                {
                    ConfigureSourceTexture(TexturePath(world, family, "BaseColor"), TextureImporterType.Default, srgb: true, alpha: false);
                    ConfigureSourceTexture(TexturePath(world, family, "Normal"), TextureImporterType.NormalMap, srgb: false, alpha: false);
                    ConfigureSourceTexture(TexturePath(world, family, "Mask"), TextureImporterType.Default, srgb: false, alpha: true);
                }
            }
        }

        private static void ConfigureSourceTexture(string path, TextureImporterType type, bool srgb, bool alpha)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Missing M132 texture source: {path}");
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                throw new InvalidOperationException($"M132 texture source is not importable: {path}");
            }

            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = TextureSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static Dictionary<string, Dictionary<MaterialRole, Material>> GenerateMaterials()
        {
            var byBiome = new Dictionary<string, Dictionary<MaterialRole, Material>>();
            foreach (var world in WorldSpecs)
            {
                var materials = new Dictionary<MaterialRole, Material>();
                foreach (var spec in MaterialSpecsFor(world))
                {
                    materials[spec.Role] = CreateOrUpdateLitMaterial(spec);
                }

                byBiome[world.BiomeId] = materials;
            }

            return byBiome;
        }

        private static Dictionary<string, Dictionary<PresentationPrefabRole, GameObject>> GenerateDecorPrefabs(
            IReadOnlyDictionary<string, Dictionary<MaterialRole, Material>> materials)
        {
            var prefabs = new Dictionary<string, Dictionary<PresentationPrefabRole, GameObject>>();
            foreach (var world in WorldSpecs)
            {
                var biomeMaterials = materials[world.BiomeId];
                prefabs[world.BiomeId] = new Dictionary<PresentationPrefabRole, GameObject>
                {
                    [PresentationPrefabRole.DecorGrassTuft] = CreateLowDecorPrefab(world, PresentationPrefabRole.DecorGrassTuft, "OrganicPatch", biomeMaterials[MaterialRole.DecorGrassTuft]),
                    [PresentationPrefabRole.DecorCrystalCluster] = CreateShardPrefab(world, biomeMaterials[MaterialRole.DecorCrystalCluster]),
                    [PresentationPrefabRole.DecorSmallTree] = CreatePillarPrefab(world, biomeMaterials[MaterialRole.DecorSmallTree]),
                    [PresentationPrefabRole.DecorStoneRuin] = CreateBrokenStonePrefab(world, biomeMaterials[MaterialRole.DecorStoneRuin])
                };
            }

            return prefabs;
        }

        private static void GenerateRoomTemplates()
        {
            foreach (var world in WorldSpecs)
            {
                foreach (var shape in RoomShapes)
                {
                    var sourcePath = $"{Milestone13AssetGenerator.MacroFixtureDirectory}/{shape.SourceRoomId}.hollowruntime.json";
                    var targetPath = RoomPath(world, shape);
                    var sourceJson = File.ReadAllText(sourcePath);
                    var manifest = JsonUtility.FromJson<ImportedHollowRoomManifest>(sourceJson);
                    if (manifest?.hollowRuntime == null)
                    {
                        throw new InvalidOperationException($"Cannot duplicate M132 room template because {sourcePath} is not a HollowRuntime manifest.");
                    }

                    var roomId = RoomId(world, shape);
                    var runtime = manifest.hollowRuntime;
                    runtime.sourceProjectId = roomId;
                    runtime.canonicalRoomId = roomId;
                    runtime.displayName = $"{world.DisplayName} {shape.DisplayName}";
                    runtime.biomeId = world.BiomeId;
                    runtime.decor = CreateBiomeDecor(world, runtime);
                    File.WriteAllText(targetPath, JsonUtility.ToJson(manifest, prettyPrint: true));
                    AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        private static void GenerateBiomeCatalog(
            IReadOnlyDictionary<string, Dictionary<MaterialRole, Material>> materials,
            IReadOnlyDictionary<string, Dictionary<PresentationPrefabRole, GameObject>> prefabs)
        {
            var generatedBiomes = new List<RoomBiomeDefinition>();
            foreach (var world in WorldSpecs)
            {
                var biome = LoadOrCreate<RoomBiomeDefinition>(BiomePath(world));
                biome.Configure(
                    world.BiomeId,
                    world.DisplayName,
                    world.BiomeTags,
                    RoomShapes
                        .Select(shape => AssetDatabase.LoadAssetAtPath<TextAsset>(RoomPath(world, shape)))
                        .Where(asset => asset != null),
                    materials[world.BiomeId].Select(pair => new RoomBiomeMaterialOverride(pair.Key, pair.Value)),
                    prefabs[world.BiomeId].Select(pair => new RoomBiomePrefabOverride(pair.Key, pair.Value)),
                    RoomBiomeCatalogDefinition.DefaultDecorBindings());
                EditorUtility.SetDirty(biome);
                generatedBiomes.Add(biome);
            }

            var catalog = LoadOrCreate<RoomBiomeCatalogDefinition>(BiomeCatalogPath);
            var biomes = catalog.Biomes
                .Where(existing => existing != null && !WorldSpecs.Any(world => RoomBiomeIds.Matches(existing.BiomeId, world.BiomeId)))
                .Concat(generatedBiomes)
                .ToArray();
            catalog.Configure(RoomBiomeIds.HollowThreshold, biomes);
            EditorUtility.SetDirty(catalog);
        }

        private static RunFramingCatalogDefinition GenerateRunFramingCatalog()
        {
            var worlds = new List<RunFramingDefinition>();
            foreach (var spec in WorldSpecs)
            {
                var definition = LoadOrCreate<RunFramingDefinition>(FramingPath(spec));
                definition.Configure(
                    spec.BiomeId,
                    spec.WorldIndex,
                    spec.DisplayName,
                    spec.Subtitle,
                    spec.BiomeTags,
                    spec.PaletteHint,
                    spec.LightingHint,
                    spec.MaterialNotes,
                    spec.PrologueLine,
                    spec.BranchLine,
                    spec.HubLine,
                    spec.BossLine,
                    spec.ExtractionLine,
                    spec.BranchEchoNames);
                definition.ConfigureBiome(spec.BiomeId);
                EditorUtility.SetDirty(definition);
                worlds.Add(definition);
            }

            var catalog = LoadOrCreate<RunFramingCatalogDefinition>(RunFramingCatalogPath);
            catalog.Configure(CatalogId, worlds);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AssignToGameScenes(RunFramingCatalogDefinition catalog)
        {
            foreach (var scenePath in GameScenes)
            {
                if (!File.Exists(scenePath))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch != null)
                {
                    branch.ConfigureRunFramingCatalog(catalog);
                    EditorUtility.SetDirty(branch);
                }

                var framingHud = Object.FindFirstObjectByType<RunFramingHudController>();
                if (framingHud != null)
                {
                    framingHud.Configure(catalog);
                    EditorUtility.SetDirty(framingHud);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static Material CreateOrUpdateLitMaterial(MaterialSpec spec)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = AssetDatabase.LoadAssetAtPath<Material>(spec.Path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, spec.Path);
            }

            material.name = spec.Name;
            if (shader != null)
            {
                material.shader = shader;
            }

            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(spec.World, spec.Family, "BaseColor"));
            var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(spec.World, spec.Family, "Normal"));
            var maskMap = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(spec.World, spec.Family, "Mask"));
            material.color = spec.Tint;
            SetTexture(material, "_BaseMap", baseMap, spec.TextureScale);
            SetTexture(material, "_MainTex", baseMap, spec.TextureScale);
            SetTexture(material, "_BumpMap", normalMap, spec.TextureScale);
            SetTexture(material, "_MetallicGlossMap", maskMap, spec.TextureScale);
            SetTexture(material, "_OcclusionMap", maskMap, spec.TextureScale);
            SetColor(material, "_BaseColor", spec.Tint);
            SetColor(material, "_Color", spec.Tint);
            SetFloat(material, "_Metallic", spec.Metallic);
            SetFloat(material, "_Smoothness", spec.Smoothness);
            SetFloat(material, "_Glossiness", spec.Smoothness);
            SetFloat(material, "_BumpScale", 0.62f);
            SetFloat(material, "_OcclusionStrength", 0.86f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            ConfigureSurface(material, spec.Transparent, spec.DoubleSided);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static List<ImportedRoomDecor> CreateBiomeDecor(WorldSpec world, ImportedHollowRuntime runtime)
        {
            var existing = runtime.decor?
                .Where(decor => decor != null && !(decor.id ?? string.Empty).StartsWith($"m132_{world.BiomeId}_decor_", StringComparison.Ordinal))
                .ToList() ?? new List<ImportedRoomDecor>();

            existing.Add(new ImportedRoomDecor
            {
                id = $"m132_{world.BiomeId}_decor_grass_tuft_01",
                kind = RoomBiomeDecorKinds.GrassTuft,
                center = DecorPosition(runtime, 0.18f, 0.78f),
                size = Vec(0.65f, 0.35f, 0.65f)
            });
            existing.Add(new ImportedRoomDecor
            {
                id = $"m132_{world.BiomeId}_decor_crystal_cluster_01",
                kind = RoomBiomeDecorKinds.CrystalCluster,
                center = DecorPosition(runtime, 0.82f, 0.25f),
                size = Vec(0.58f, 0.72f, 0.58f)
            });
            existing.Add(new ImportedRoomDecor
            {
                id = $"m132_{world.BiomeId}_decor_small_tree_01",
                kind = RoomBiomeDecorKinds.SmallTree,
                center = DecorPosition(runtime, 0.08f, 0.18f),
                size = Vec(0.85f, 1.35f, 0.85f)
            });
            existing.Add(new ImportedRoomDecor
            {
                id = $"m132_{world.BiomeId}_decor_stone_ruin_01",
                kind = RoomBiomeDecorKinds.StoneRuin,
                center = DecorPosition(runtime, 0.9f, 0.82f),
                size = Vec(0.9f, 0.65f, 0.52f)
            });
            return existing;
        }

        private static GameObject CreateLowDecorPrefab(WorldSpec world, PresentationPrefabRole role, string nameSuffix, Material material)
        {
            return CreatePrefab(world, role, $"AP_M132_{world.AssetPrefix}_{nameSuffix}", root =>
            {
                for (var index = 0; index < 5; index++)
                {
                    var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    piece.name = $"Patch_{index:00}";
                    piece.transform.SetParent(root.transform, false);
                    piece.transform.localPosition = new Vector3((index - 2) * 0.12f, 0.055f, Mathf.Sin(index * 1.7f) * 0.1f);
                    piece.transform.localRotation = Quaternion.Euler(0f, index * 31f, 0f);
                    piece.transform.localScale = new Vector3(0.26f, 0.11f, 0.16f + index * 0.018f);
                    AssignMaterialAndStrip(piece, material);
                }
            });
        }

        private static GameObject CreateShardPrefab(WorldSpec world, Material material)
        {
            return CreatePrefab(world, PresentationPrefabRole.DecorCrystalCluster, $"AP_M132_{world.AssetPrefix}_AccentShard", root =>
            {
                for (var index = 0; index < 4; index++)
                {
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = $"Shard_{index:00}";
                    shard.transform.SetParent(root.transform, false);
                    shard.transform.localPosition = new Vector3(Mathf.Cos(index * 1.55f) * 0.14f, 0.18f + index * 0.04f, Mathf.Sin(index * 1.55f) * 0.12f);
                    shard.transform.localRotation = Quaternion.Euler(0f, 28f + index * 37f, 8f - index * 3f);
                    shard.transform.localScale = new Vector3(0.075f, 0.36f + index * 0.05f, 0.05f);
                    AssignMaterialAndStrip(shard, material);
                }
            });
        }

        private static GameObject CreatePillarPrefab(WorldSpec world, Material material)
        {
            return CreatePrefab(world, PresentationPrefabRole.DecorSmallTree, $"AP_M132_{world.AssetPrefix}_VerticalRelic", root =>
            {
                var baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseBlock.name = "Base";
                baseBlock.transform.SetParent(root.transform, false);
                baseBlock.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                baseBlock.transform.localScale = new Vector3(0.42f, 0.16f, 0.42f);
                AssignMaterialAndStrip(baseBlock, material);

                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Pillar";
                pillar.transform.SetParent(root.transform, false);
                pillar.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                pillar.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                pillar.transform.localScale = new Vector3(0.22f, 0.82f, 0.22f);
                AssignMaterialAndStrip(pillar, material);
            });
        }

        private static GameObject CreateBrokenStonePrefab(WorldSpec world, Material material)
        {
            return CreatePrefab(world, PresentationPrefabRole.DecorStoneRuin, $"AP_M132_{world.AssetPrefix}_BrokenStones", root =>
            {
                for (var index = 0; index < 4; index++)
                {
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = $"Stone_{index:00}";
                    block.transform.SetParent(root.transform, false);
                    block.transform.localPosition = new Vector3((index - 1.5f) * 0.16f, 0.08f + index * 0.035f, Mathf.Sin(index * 1.2f) * 0.12f);
                    block.transform.localRotation = Quaternion.Euler(0f, index * 17f - 12f, index * 3f);
                    block.transform.localScale = new Vector3(0.3f + index * 0.05f, 0.16f + index * 0.06f, 0.22f + index * 0.035f);
                    AssignMaterialAndStrip(block, material);
                }
            });
        }

        private static GameObject CreatePrefab(WorldSpec world, PresentationPrefabRole role, string prefabName, Action<GameObject> build)
        {
            var root = new GameObject(prefabName);
            try
            {
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                root.AddComponent<PresentationVisualMarker>().Configure(role, isFallback: false);
                build(root);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{BiomePrefabDirectory}/{world.AssetPrefix}/{prefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssignMaterialAndStrip(GameObject target, Material material)
        {
            if (target.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.sharedMaterial = material;
            }

            foreach (var collider in target.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static MaterialSpec[] MaterialSpecsFor(WorldSpec world)
        {
            return new[]
            {
                new MaterialSpec(world, MaterialRole.RoomFloor, "Floor", new Vector2(7f, 5f), 0.24f, world.BaseTint),
                new MaterialSpec(world, MaterialRole.RoomWall, "Wall", new Vector2(4f, 4f), 0.18f, Color.white, doubleSided: true),
                new MaterialSpec(world, MaterialRole.RoomWallTransparent, "Wall", new Vector2(4f, 4f), 0.18f, new Color(1f, 1f, 1f, RoomWallVisibilityController.TransparentAlpha), transparent: true, doubleSided: true),
                new MaterialSpec(world, MaterialRole.RoomObstacleRock, "Rock", new Vector2(2.4f, 2.4f), 0.18f, Color.white),
                new MaterialSpec(world, MaterialRole.DoorActive, "Door", new Vector2(2f, 2f), 0.32f, Color.white),
                new MaterialSpec(world, MaterialRole.DoorCleared, "AccentTrim", new Vector2(1.6f, 1.6f), 0.46f, world.AccentTint),
                new MaterialSpec(world, MaterialRole.DoorLocked, "Door", new Vector2(2f, 2f), 0.18f, new Color(0.72f, 0.68f, 0.6f, 1f)),
                new MaterialSpec(world, MaterialRole.DoorUnavailable, "Wall", new Vector2(2f, 2f), 0.12f, new Color(0.52f, 0.5f, 0.46f, 0.92f)),
                new MaterialSpec(world, MaterialRole.DecorGrassTuft, "OrganicDecor", Vector2.one, 0.28f, Color.white),
                new MaterialSpec(world, MaterialRole.DecorCrystalCluster, "AccentTrim", Vector2.one, 0.58f, world.AccentTint),
                new MaterialSpec(world, MaterialRole.DecorSmallTree, "OrganicDecor", new Vector2(1.2f, 1.2f), 0.26f, Color.white),
                new MaterialSpec(world, MaterialRole.DecorStoneRuin, "Rock", new Vector2(1.4f, 1.4f), 0.18f, Color.white),
                new MaterialSpec(world, MaterialRole.NextBranchPortal, "AccentTrim", new Vector2(1.1f, 1.1f), 0.52f, world.AccentTint)
            };
        }

        private static void ConfigureSurface(Material material, bool transparent, bool doubleSided)
        {
            SetFloat(material, "_Cull", doubleSided ? 0f : 2f);
            if (transparent)
            {
                SetFloat(material, "_Surface", 1f);
                SetFloat(material, "_Blend", 0f);
                SetFloat(material, "_AlphaClip", 0f);
                SetFloat(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                SetFloat(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                SetFloat(material, "_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                return;
            }

            SetFloat(material, "_Surface", 0f);
            SetFloat(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            SetFloat(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            SetFloat(material, "_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        private static void AddWorldOrderChecks(List<Milestone132BiomeWorldSelectionLockCheck> checks)
        {
            AddCheck(
                checks,
                "world-order:beta-itinerary",
                "Worlds",
                BetaWorldDisplayNames.SequenceEqual(new[] { "Before Teeth", "The Sunken Cartouche", "The Rust Choir" }),
                "Beta world order is Before Teeth -> The Sunken Cartouche -> The Rust Choir.");
            AddCheck(
                checks,
                "world-order:hollow-threshold-prologue",
                "Worlds",
                !BetaBiomeIds.Contains(RoomBiomeIds.HollowThreshold),
                "The Hollow Threshold remains outside the selected three-world beta itinerary.");
        }

        private static void AddTextureChecks(List<Milestone132BiomeWorldSelectionLockCheck> checks)
        {
            foreach (var world in WorldSpecs)
            {
                foreach (var family in RequiredTextureFamilies)
                {
                    foreach (var map in RequiredTextureMaps)
                    {
                        var path = TexturePath(world, family, map);
                        AddCheck(checks, $"texture:{world.BiomeId}:{family}:{map}", "Textures", File.Exists(path), File.Exists(path) ? $"Found {TextureSize} source map `{path}`." : $"Missing `{path}`.");
                    }
                }
            }
        }

        private static void AddBiomeAndMaterialChecks(List<Milestone132BiomeWorldSelectionLockCheck> checks)
        {
            foreach (var world in WorldSpecs)
            {
                foreach (var shape in RoomShapes)
                {
                    var path = RoomPath(world, shape);
                    var text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
                    AddCheck(
                        checks,
                        $"room:{world.BiomeId}:{shape.TargetSuffix}",
                        "Rooms",
                        File.Exists(path) && ContainsOrdinal(text, $"\"biomeId\": \"{world.BiomeId}\""),
                        $"Room variant `{path}` uses biome `{world.BiomeId}`.");
                }

                foreach (var role in RequiredMaterialRoles)
                {
                    AddCheck(checks, $"material:{world.BiomeId}:{role}", "Materials", File.Exists(MaterialPath(world, role)), $"Material override exists for `{world.BiomeId}` / `{role}`.");
                }
            }
        }

        private static void AddRuntimeChecks(List<Milestone132BiomeWorldSelectionLockCheck> checks)
        {
            var ids = Read(RoomBiomeIdsPath);
            RequireAll(checks, "runtime:biome-id-constants", "Runtime", ids, new[]
            {
                "BeforeTeeth = \"before_teeth\"",
                "SunkenCartouche = \"sunken_cartouche\"",
                "RustChoir = \"rust_choir\""
            });

            var portal = Read(NextBranchPortalPath) + Read(BranchSessionControllerPath) + Read(PresentationResolverPath);
            RequireAll(checks, "runtime:biome-portal-trim", "Runtime", portal, new[]
            {
                "BranchBiomeId",
                "BiomeIdForHubChoice",
                "RoomBiomePresentationResolver.InstantiateVisual(BranchBiomeId",
                "PresentationPrefabRole.NextBranchPortal",
                "MaterialRole.NextBranchPortal"
            });

            var scenes = GameScenes.Select(File.Exists).All(exists => exists) &&
                GameScenes.All(path => ContainsOrdinal(File.ReadAllText(path), "13200000000000000000000000000050"));
            AddCheck(checks, "runtime:game-scenes-use-m132-catalog", "Runtime", scenes, "Game scenes reference the M132 beta run-framing catalog.");
        }

        private static void AddDocsChecks(List<Milestone132BiomeWorldSelectionLockCheck> checks)
        {
            var docs = Read(DocsPath);
            RequireAll(checks, "docs:m132-decisions", "Documentation", docs, new[]
            {
                "Before Teeth",
                "The Sunken Cartouche",
                "The Rust Choir",
                "The Hollow Threshold",
                "1024",
                "BaseColor",
                "Normal",
                "Mask",
                "global chest silhouettes",
                "Enemy color and silhouette work is documented only"
            });
        }

        private static void AddDependencyChecks(List<Milestone132BiomeWorldSelectionLockCheck> checks)
        {
            var m131 = Read(M131ReportPath);
            AddCheck(
                checks,
                "dependency:m131-passing-report",
                "Dependency",
                ContainsOrdinal(m131, "- Result: PASSED") &&
                ContainsOrdinal(m131, Milestone131RoomTypeExpansionLockAssetGenerator.LockId),
                "M131 passing report exists and includes the M131 lock id.");
        }

        private static string BuildDocsMarkdown()
        {
            var builder = new StringBuilder(8192);
            builder.AppendLine("# M132: Biome + World Selection Lock + Beta Art Pack");
            builder.AppendLine();
            builder.AppendLine("## Summary");
            builder.AppendLine("- M132 locks the beta world order as `Before Teeth` -> `The Sunken Cartouche` -> `The Rust Choir`.");
            builder.AppendLine("- `The Hollow Threshold` remains the prologue/fallback framing and is not one of the three beta worlds.");
            builder.AppendLine("- Each selected world receives a readable 1024 PBR art pack with six source texture families: floor, wall, rock, door, organic/decor, and accent/trim.");
            builder.AppendLine("- Chests remain global gameplay affordances; biome identity comes from rooms, doors, rocks, decor, and branch-portal trim.");
            builder.AppendLine();
            builder.AppendLine("## World Order");
            foreach (var world in WorldSpecs)
            {
                builder.AppendLine($"- World {world.WorldIndex}: `{world.BiomeId}` - {world.DisplayName}. {world.Subtitle}");
            }

            builder.AppendLine();
            builder.AppendLine("## PBR Source Maps");
            builder.AppendLine("- Every family has separate `BaseColor`, `Normal`, and packed `Mask` PNGs at 1024x1024.");
            builder.AppendLine("- Mask channels are `R = metallic`, `G = occlusion`, `B = reserved`, `A = smoothness`.");
            builder.AppendLine("- Base maps import as sRGB; normal maps import as normal maps; masks import as linear data.");
            builder.AppendLine("- Materials use URP/Lit and wire `_BaseMap`, `_BumpMap`, `_MetallicGlossMap`, and `_OcclusionMap`.");
            builder.AppendLine();
            builder.AppendLine("## Room And Runtime Policy");
            builder.AppendLine("- M132 creates 1x1, 2x1, 1x2, 2x2, and L-shape biome variants from existing macro fixture shapes only.");
            builder.AppendLine("- Normal branch rooms use the active world biome when that biome has complete room-template coverage.");
            builder.AppendLine("- Corrupted chest rooms remain on `corrupted_ashen_shrine`.");
            builder.AppendLine("- Wave Rooms inherit the active branch biome.");
            builder.AppendLine("- Normal, Golden, and Corrupted Chest silhouettes/material identity stay global for readability.");
            builder.AppendLine();
            builder.AppendLine("## Enemy Visual Deferral");
            builder.AppendLine("- Enemy color and silhouette work is documented only in M132.");
            builder.AppendLine("- Future enemy art passes should preserve clear silhouettes: bone/fern reads for Before Teeth, lapis/gold reads for The Sunken Cartouche, and rust/neon reads for The Rust Choir.");
            builder.AppendLine("- M132 does not add runtime enemy material swapping.");
            builder.AppendLine();
            builder.AppendLine("## Non-Goals");
            builder.AppendLine("- No save schema, reward schema, economy schema, chest-kind, room-role, or branch-generation rule changes.");
            builder.AppendLine("- No new room layouts beyond macro fixture variants.");
            return builder.ToString();
        }

        private static void RequireAll(List<Milestone132BiomeWorldSelectionLockCheck> checks, string id, string category, string haystack, IEnumerable<string> needles)
        {
            var missing = needles.Where(needle => !ContainsOrdinal(haystack, needle)).ToArray();
            AddCheck(
                checks,
                id,
                category,
                missing.Length == 0,
                missing.Length == 0 ? $"All required markers found for `{id}`." : $"Missing markers: {string.Join(", ", missing)}.");
        }

        private static void AddCheck(List<Milestone132BiomeWorldSelectionLockCheck> checks, string id, string category, bool passed, string detail)
        {
            checks.Add(new Milestone132BiomeWorldSelectionLockCheck
            {
                id = id,
                category = category,
                passed = passed,
                detail = detail
            });
        }

        private static string Read(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }

        private static bool ContainsOrdinal(string haystack, string needle)
        {
            return (haystack ?? string.Empty).IndexOf(needle ?? string.Empty, StringComparison.Ordinal) >= 0;
        }

        private static ImportedVector3 DecorPosition(ImportedHollowRuntime runtime, float xFraction, float zFraction)
        {
            var bounds = runtime.dimensions?.bounds;
            var targetX = bounds != null ? Mathf.Lerp(bounds.minX + 1f, bounds.maxX - 1f, xFraction) : Mathf.Lerp(-5.5f, 5.5f, xFraction);
            var targetZ = bounds != null ? Mathf.Lerp(bounds.minZ + 1f, bounds.maxZ - 1f, zFraction) : Mathf.Lerp(-2.5f, 2.5f, zFraction);
            var tile = (runtime.walkableTiles ?? new List<ImportedGridPosition>())
                .OrderBy(candidate => Mathf.Pow(candidate.x - targetX, 2f) + Mathf.Pow(candidate.z - targetZ, 2f))
                .FirstOrDefault();
            return tile != null ? Vec(tile.x, 0f, tile.z) : Vec(targetX, 0f, targetZ);
        }

        private static ImportedVector3 Vec(float x, float y, float z)
        {
            return new ImportedVector3 { x = x, y = y, z = z };
        }

        private static void SetTexture(Material material, string propertyName, Texture texture, Vector2 scale)
        {
            if (!material.HasProperty(propertyName))
            {
                return;
            }

            material.SetTexture(propertyName, texture);
            material.SetTextureScale(propertyName, scale);
            material.SetTextureOffset(propertyName, Vector2.zero);
        }

        private static void SetColor(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static string TexturePath(WorldSpec world, string family, string map)
        {
            return $"{TextureRoot}/{world.AssetPrefix}/T_{world.AssetPrefix}_{family}_{map}.png";
        }

        private static string BiomePath(WorldSpec world)
        {
            return $"{BiomeResourceDirectory}/Biome_{world.AssetPrefix}.asset";
        }

        private static string RoomPath(WorldSpec world, RoomShapeSpec shape)
        {
            return $"{BiomeRoomDirectory}/{world.AssetPrefix}/{RoomId(world, shape)}.hollowruntime.json";
        }

        private static string RoomId(WorldSpec world, RoomShapeSpec shape)
        {
            return $"{world.BiomeId}_macro_{shape.TargetSuffix}";
        }

        private static string MaterialPath(WorldSpec world, MaterialRole role)
        {
            return $"{Milestone23AssetGenerator.ArtPassMaterialDirectory}/AP_M132_{world.AssetPrefix}_{role}.mat";
        }

        private static string FramingPath(WorldSpec world)
        {
            return $"{FramingDirectory}/RunFraming_{world.BiomeId}.asset";
        }

        private readonly struct RoomShapeSpec
        {
            public RoomShapeSpec(string sourceRoomId, string targetSuffix, string displayName)
            {
                SourceRoomId = sourceRoomId;
                TargetSuffix = targetSuffix;
                DisplayName = displayName;
            }

            public string SourceRoomId { get; }

            public string TargetSuffix { get; }

            public string DisplayName { get; }
        }

        private readonly struct MaterialSpec
        {
            public MaterialSpec(WorldSpec world, MaterialRole role, string family, Vector2 textureScale, float smoothness, Color tint, bool transparent = false, bool doubleSided = false)
            {
                World = world;
                Role = role;
                Family = family;
                TextureScale = textureScale;
                Smoothness = smoothness;
                Tint = tint;
                Transparent = transparent;
                DoubleSided = doubleSided;
            }

            public WorldSpec World { get; }

            public MaterialRole Role { get; }

            public string Family { get; }

            public Vector2 TextureScale { get; }

            public float Smoothness { get; }

            public float Metallic => Family == "Door" || Family == "AccentTrim" ? 0.28f : 0f;

            public Color Tint { get; }

            public bool Transparent { get; }

            public bool DoubleSided { get; }

            public string Name => $"AP_M132_{World.AssetPrefix}_{Role}";

            public string Path => MaterialPath(World, Role);
        }

        private readonly struct WorldSpec
        {
            public WorldSpec(
                string biomeId,
                string assetPrefix,
                int worldIndex,
                string displayName,
                string subtitle,
                IReadOnlyList<WorldBiomeTag> biomeTags,
                string paletteHint,
                string lightingHint,
                string materialNotes,
                string prologueLine,
                string branchLine,
                string hubLine,
                string bossLine,
                string extractionLine,
                IReadOnlyList<string> branchEchoNames,
                Color baseTint,
                Color accentTint)
            {
                BiomeId = biomeId;
                AssetPrefix = assetPrefix;
                WorldIndex = worldIndex;
                DisplayName = displayName;
                Subtitle = subtitle;
                BiomeTags = biomeTags;
                PaletteHint = paletteHint;
                LightingHint = lightingHint;
                MaterialNotes = materialNotes;
                PrologueLine = prologueLine;
                BranchLine = branchLine;
                HubLine = hubLine;
                BossLine = bossLine;
                ExtractionLine = extractionLine;
                BranchEchoNames = branchEchoNames;
                BaseTint = baseTint;
                AccentTint = accentTint;
            }

            public string BiomeId { get; }

            public string AssetPrefix { get; }

            public int WorldIndex { get; }

            public string DisplayName { get; }

            public string Subtitle { get; }

            public IReadOnlyList<WorldBiomeTag> BiomeTags { get; }

            public string PaletteHint { get; }

            public string LightingHint { get; }

            public string MaterialNotes { get; }

            public string PrologueLine { get; }

            public string BranchLine { get; }

            public string HubLine { get; }

            public string BossLine { get; }

            public string ExtractionLine { get; }

            public IReadOnlyList<string> BranchEchoNames { get; }

            public Color BaseTint { get; }

            public Color AccentTint { get; }
        }
    }
}
