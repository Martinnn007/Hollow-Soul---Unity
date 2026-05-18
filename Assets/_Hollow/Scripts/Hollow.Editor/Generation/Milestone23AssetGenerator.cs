using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone23AssetGenerator
    {
        public const string ArtPassRoot = "Assets/_Hollow/Prefabs/ArtPass";
        public const string ArtPassVfxDirectory = ArtPassRoot + "/VFX";
        public const string ArtPassModelSourceDirectory = "Assets/_Hollow/Art/Models/ArtPass";
        public const string ArtPassMaterialDirectory = "Assets/_Hollow/Art/Materials/ArtPass";
        public const string ArtPassAudioDirectory = "Assets/_Hollow/Audio/SFX/ArtPass";
        public const string ArtPassPalettePath = Milestone9AssetGenerator.PresentationDataDirectory + "/MaterialPalette_ArtPass.asset";
        public const string AddressablesGroupName = "Hollow ArtPass Content";

        public static readonly string[] RequiredAddressableLabels =
        {
            "hollow.artpass",
            "hollow.artpass.prefabs",
            "hollow.artpass.materials",
            "hollow.artpass.vfx",
            "hollow.artpass.audio"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 23 Assets")]
        public static void Generate()
        {
            PresentationContentProvider.Configure(null);
            Milestone22AssetGenerator.Generate();
            PresentationContentProvider.Configure(null);
            EnsureDirectories();

            var palette = GenerateArtPassPalette();
            PresentationContentProvider.Configure(LoadCatalogWithPalette(palette));
            var prefabBindings = GenerateArtPassPrefabs();
            var vfxCues = GenerateArtPassVfxCues();
            var audioCues = GenerateArtPassAudioCues();
            var catalog = LoadCatalogWithPalette(palette);
            catalog.Configure(palette, vfxCues, audioCues, prefabBindings);
            EditorUtility.SetDirty(catalog);
            PresentationContentProvider.Configure(catalog);
            MeshyEnvironmentPropAssetGenerator.GenerateAssets(saveAssets: false, refresh: false);
            SampleEnvironmentTextureSetGenerator.GenerateAssets(saveAssets: false, refresh: false);

            ConfigureAddressables(palette, prefabBindings, vfxCues, audioCues, catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 23 ArtPass content replacement pipeline.");
        }

        private static void EnsureDirectories()
        {
            foreach (var path in new[]
            {
                ArtPassRoot,
                ArtPassVfxDirectory,
                ArtPassModelSourceDirectory,
                ArtPassMaterialDirectory,
                ArtPassAudioDirectory,
                Milestone9AssetGenerator.PresentationDataDirectory,
                Milestone9AssetGenerator.VfxCueDirectory,
                Milestone9AssetGenerator.AudioCueDirectory,
                Milestone9AssetGenerator.ResourcesPresentationDirectory
            })
            {
                Directory.CreateDirectory(path);
            }
        }

        private static MaterialPaletteDefinition GenerateArtPassPalette()
        {
            var bindings = new List<MaterialRoleBinding>();
            foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
            {
                var color = ArtPassColorFor(role);
                var material = CreateOrUpdateMaterial(role, color);
                bindings.Add(new MaterialRoleBinding(role, material, color));
            }

            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(ArtPassPalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MaterialPaletteDefinition>();
                AssetDatabase.CreateAsset(palette, ArtPassPalettePath);
            }

            palette.Configure(bindings.ToArray());
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static PresentationContentCatalog LoadCatalogWithPalette(MaterialPaletteDefinition palette)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            catalog.Configure(palette, catalog.VfxCues, catalog.AudioCues, catalog.PrefabBindings);
            EditorUtility.SetDirty(catalog);

            return catalog;
        }

        private static Material CreateOrUpdateMaterial(MaterialRole role, Color color)
        {
            var path = $"{ArtPassMaterialDirectory}/AP_M_{role}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = MaterialResolver.CreateRuntimeMaterial(color);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = $"AP_M_{role}";
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static PresentationPrefabBinding[] GenerateArtPassPrefabs()
        {
            var bindings = new List<PresentationPrefabBinding>();
            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                var prefab = role.ToString().StartsWith("Vfx", StringComparison.Ordinal)
                    ? CreateVfxPrefab(role)
                    : CreateRolePrefab(role);
                bindings.Add(new PresentationPrefabBinding(role, prefab));
            }

            return bindings.ToArray();
        }

        private static GameObject CreateRolePrefab(PresentationPrefabRole role)
        {
            var root = new GameObject($"AP_{role}");
            root.AddComponent<PresentationVisualMarker>().Configure(role, isFallback: false);
            AddRoleGeometry(root.transform, role);
            return SavePrefab(root, $"{ArtPassRoot}/AP_{role}.prefab");
        }

        private static GameObject CreateVfxPrefab(PresentationPrefabRole role)
        {
            var root = new GameObject($"VFX_{role}");
            root.AddComponent<PresentationVisualMarker>().Configure(role, isFallback: false);
            AddPrimitive(root.transform, PrimitiveType.Sphere, "spark_core", Vector3.zero, Vector3.one * 0.22f, role, alpha: 0.82f);
            AddPrimitive(root.transform, PrimitiveType.Cube, "spark_bar", Vector3.zero, new Vector3(0.52f, 0.04f, 0.04f), role, alpha: 0.58f);
            return SavePrefab(root, $"{ArtPassVfxDirectory}/VFX_{role}.prefab");
        }

        private static void AddRoleGeometry(Transform parent, PresentationPrefabRole role)
        {
            switch (role)
            {
                case PresentationPrefabRole.Player:
                    AddPrimitive(parent, PrimitiveType.Capsule, "body", new Vector3(0f, 0.45f, 0f), new Vector3(0.34f, 0.86f, 0.34f), role);
                    AddPrimitive(parent, PrimitiveType.Sphere, "soul_glow", new Vector3(0f, 0.95f, 0f), Vector3.one * 0.22f, role, alpha: 0.75f);
                    break;
                case PresentationPrefabRole.EnemyBoss:
                    AddPrimitive(parent, PrimitiveType.Cube, "warden_core", new Vector3(0f, 0.55f, 0f), new Vector3(0.72f, 0.9f, 0.72f), role);
                    AddPrimitive(parent, PrimitiveType.Cylinder, "warden_crown", new Vector3(0f, 1.1f, 0f), new Vector3(0.62f, 0.16f, 0.62f), role, alpha: 0.86f);
                    break;
                case PresentationPrefabRole.EnemyTurret:
                    AddPrimitive(parent, PrimitiveType.Cylinder, "turret_base", new Vector3(0f, 0.2f, 0f), new Vector3(0.48f, 0.18f, 0.48f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "turret_eye", new Vector3(0f, 0.46f, 0.22f), new Vector3(0.38f, 0.22f, 0.18f), role, alpha: 0.95f);
                    break;
                case PresentationPrefabRole.Projectile:
                case PresentationPrefabRole.EnemyProjectile:
                case PresentationPrefabRole.RewardPickup:
                case PresentationPrefabRole.BossKeyPickup:
                    AddPrimitive(parent, PrimitiveType.Sphere, "orb", new Vector3(0f, 0.08f, 0f), Vector3.one * 0.42f, role);
                    AddPrimitive(parent, PrimitiveType.Cube, "facet", new Vector3(0f, 0.09f, 0f), Vector3.one * 0.24f, role, alpha: 0.78f);
                    break;
                case PresentationPrefabRole.HubReturnPortal:
                case PresentationPrefabRole.NextBranchPortal:
                    AddPrimitive(parent, PrimitiveType.Cylinder, "portal_ring", Vector3.zero, new Vector3(0.86f, 0.08f, 0.86f), role, alpha: 0.78f);
                    AddPrimitive(parent, PrimitiveType.Cylinder, "portal_core", new Vector3(0f, 0.04f, 0f), new Vector3(0.54f, 0.06f, 0.54f), role, alpha: 0.52f);
                    break;
                case PresentationPrefabRole.RoomFloor:
                    AddPrimitive(parent, PrimitiveType.Cube, "floor_plate", Vector3.zero, new Vector3(1f, 0.08f, 1f), role, alpha: 0.82f);
                    AddPrimitive(parent, PrimitiveType.Cube, "floor_inset", new Vector3(0f, 0.052f, 0f), new Vector3(0.84f, 0.02f, 0.84f), role, alpha: 0.5f);
                    break;
                case PresentationPrefabRole.RoomObstacleRock:
                    AddPrimitive(parent, PrimitiveType.Cube, "rock_block", new Vector3(0f, 0.04f, 0f), new Vector3(0.88f, 0.92f, 0.88f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "rock_cap", new Vector3(0.08f, 0.5f, -0.08f), new Vector3(0.64f, 0.26f, 0.64f), role, alpha: 0.72f);
                    break;
                case PresentationPrefabRole.DoorLocked:
                case PresentationPrefabRole.DoorActive:
                case PresentationPrefabRole.DoorCleared:
                case PresentationPrefabRole.DoorUnavailable:
                case PresentationPrefabRole.SecretDoorDebug:
                    AddPrimitive(parent, PrimitiveType.Cube, "door_slab", Vector3.zero, new Vector3(0.92f, 1f, 0.12f), role, alpha: 0.78f);
                    AddPrimitive(parent, PrimitiveType.Cylinder, "door_dot", new Vector3(0f, 0.2f, -0.08f), new Vector3(0.16f, 0.04f, 0.16f), role);
                    break;
                case PresentationPrefabRole.HubShop:
                    AddPrimitive(parent, PrimitiveType.Cube, "shop_stand", Vector3.zero, new Vector3(0.9f, 0.56f, 0.72f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "shop_sign", new Vector3(0f, 0.55f, -0.08f), new Vector3(0.72f, 0.22f, 0.08f), role, alpha: 0.85f);
                    break;
                case PresentationPrefabRole.HubShopCard:
                    AddPrimitive(parent, PrimitiveType.Cube, "card_back", Vector3.zero, new Vector3(1f, 0.72f, 0.08f), role, alpha: 0.88f);
                    AddPrimitive(parent, PrimitiveType.Cube, "card_frame", new Vector3(0f, 0.01f, -0.045f), new Vector3(1.08f, 0.8f, 0.025f), role, alpha: 0.42f);
                    AddPrimitive(parent, PrimitiveType.Sphere, "price_gem", new Vector3(0.36f, 0.22f, -0.08f), Vector3.one * 0.12f, role, alpha: 0.95f);
                    break;
                case PresentationPrefabRole.WeaponMelee:
                    AddPrimitive(parent, PrimitiveType.Cube, "blade", new Vector3(0f, 0.32f, 0f), new Vector3(0.12f, 0.7f, 0.12f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "hilt", new Vector3(0f, -0.05f, 0f), new Vector3(0.42f, 0.1f, 0.1f), role, alpha: 0.78f);
                    break;
                case PresentationPrefabRole.WeaponRanged:
                    AddPrimitive(parent, PrimitiveType.Cube, "bow_spine", Vector3.zero, new Vector3(0.12f, 0.72f, 0.12f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "string", new Vector3(0.18f, 0f, 0f), new Vector3(0.035f, 0.7f, 0.035f), role, alpha: 0.64f);
                    AddPrimitive(parent, PrimitiveType.Sphere, "bolt_core", new Vector3(-0.1f, 0f, 0f), Vector3.one * 0.16f, role, alpha: 0.86f);
                    break;
                case PresentationPrefabRole.Armor:
                    AddPrimitive(parent, PrimitiveType.Cube, "chest", new Vector3(0f, 0.32f, 0f), new Vector3(0.58f, 0.64f, 0.28f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "pauldron_left", new Vector3(-0.38f, 0.46f, 0f), new Vector3(0.22f, 0.2f, 0.26f), role, alpha: 0.84f);
                    AddPrimitive(parent, PrimitiveType.Cube, "pauldron_right", new Vector3(0.38f, 0.46f, 0f), new Vector3(0.22f, 0.2f, 0.26f), role, alpha: 0.84f);
                    break;
                case PresentationPrefabRole.ActiveItemPickup:
                    AddPrimitive(parent, PrimitiveType.Sphere, "active_core", new Vector3(0f, 0.12f, 0f), Vector3.one * 0.36f, role);
                    AddPrimitive(parent, PrimitiveType.Cylinder, "active_ring", new Vector3(0f, 0.12f, 0f), new Vector3(0.54f, 0.04f, 0.54f), role, alpha: 0.52f);
                    break;
                case PresentationPrefabRole.ConsumableCardPickup:
                    AddPrimitive(parent, PrimitiveType.Cube, "card_face", new Vector3(0f, 0.14f, 0f), new Vector3(0.36f, 0.52f, 0.04f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "card_mark", new Vector3(0f, 0.16f, -0.04f), new Vector3(0.18f, 0.18f, 0.025f), role, alpha: 0.72f);
                    break;
                case PresentationPrefabRole.RoomHazardSpike:
                    AddPrimitive(parent, PrimitiveType.Cube, "spike_base", Vector3.zero, new Vector3(0.74f, 0.08f, 0.74f), role, alpha: 0.72f);
                    AddPrimitive(parent, PrimitiveType.Cylinder, "spike_warning_core", new Vector3(0f, 0.12f, 0f), new Vector3(0.32f, 0.16f, 0.32f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "spike_cross", new Vector3(0f, 0.16f, 0f), new Vector3(0.62f, 0.06f, 0.12f), role, alpha: 0.86f);
                    AddPrimitive(parent, PrimitiveType.Cube, "spike_cross_alt", new Vector3(0f, 0.16f, 0f), new Vector3(0.12f, 0.06f, 0.62f), role, alpha: 0.86f);
                    break;
                case PresentationPrefabRole.StandardBarrel:
                    AddPrimitive(parent, PrimitiveType.Cylinder, "barrel_body", new Vector3(0f, 0.42f, 0f), new Vector3(0.72f, 0.82f, 0.72f), role);
                    AddPrimitive(parent, PrimitiveType.Cylinder, "barrel_lid", new Vector3(0f, 0.86f, 0f), new Vector3(0.78f, 0.08f, 0.78f), role, alpha: 0.76f);
                    break;
                case PresentationPrefabRole.ExplosiveBarrel:
                    AddPrimitive(parent, PrimitiveType.Cylinder, "explosive_barrel_body", new Vector3(0f, 0.42f, 0f), new Vector3(0.72f, 0.82f, 0.72f), role);
                    AddPrimitive(parent, PrimitiveType.Sphere, "explosive_core", new Vector3(0f, 0.78f, 0f), Vector3.one * 0.24f, role, alpha: 0.92f);
                    AddPrimitive(parent, PrimitiveType.Cube, "explosive_stripe", new Vector3(0f, 0.46f, -0.36f), new Vector3(0.62f, 0.14f, 0.04f), role, alpha: 0.82f);
                    break;
                case PresentationPrefabRole.HazardCoinDrop:
                case PresentationPrefabRole.CoinCopper:
                case PresentationPrefabRole.CoinSilver:
                case PresentationPrefabRole.CoinGold:
                    AddPrimitive(parent, PrimitiveType.Sphere, "coin_core", new Vector3(0f, 0.12f, 0f), Vector3.one * 0.24f, role);
                    AddPrimitive(parent, PrimitiveType.Cylinder, "coin_ring", new Vector3(0f, 0.12f, 0f), new Vector3(0.34f, 0.04f, 0.34f), role, alpha: 0.74f);
                    break;
                case PresentationPrefabRole.ChestNormal:
                case PresentationPrefabRole.ChestGolden:
                    AddPrimitive(parent, PrimitiveType.Cube, "chest_body", new Vector3(0f, 0.26f, 0f), new Vector3(0.78f, 0.42f, 0.58f), role);
                    AddPrimitive(parent, PrimitiveType.Cube, "chest_lid", new Vector3(0f, 0.54f, -0.04f), new Vector3(0.84f, 0.16f, 0.62f), role, alpha: 0.88f);
                    AddPrimitive(parent, PrimitiveType.Cube, "chest_clasp", new Vector3(0f, 0.42f, -0.34f), new Vector3(0.18f, 0.18f, 0.06f), role, alpha: 0.96f);
                    break;
                default:
                    AddPrimitive(parent, PrimitiveType.Cube, "toy_body", new Vector3(0f, 0.28f, 0f), new Vector3(0.52f, 0.52f, 0.52f), role);
                    AddPrimitive(parent, PrimitiveType.Sphere, "toy_marker", new Vector3(0f, 0.72f, 0f), Vector3.one * 0.18f, role, alpha: 0.76f);
                    break;
            }
        }

        private static GameObject AddPrimitive(
            Transform parent,
            PrimitiveType primitive,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            PresentationPrefabRole role,
            float alpha = 1f)
        {
            var child = GameObject.CreatePrimitive(primitive);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = localScale;
            var collider = child.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MaterialResolver.Resolve(MaterialRoleFor(role));
                if (alpha < 0.99f)
                {
                    var color = renderer.sharedMaterial.color;
                    color.a = alpha;
                }
            }

            return child;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static VfxCueDefinition[] GenerateArtPassVfxCues()
        {
            var cues = new List<VfxCueDefinition>();
            foreach (VfxCueId cueId in Enum.GetValues(typeof(VfxCueId)))
            {
                var path = $"{Milestone9AssetGenerator.VfxCueDirectory}/VfxCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<VfxCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<VfxCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, VfxPrefabFor(cueId), MaterialResolver.FallbackColorFor(MaterialRole.VfxDebug), 0.16f, nextCreateDebugPrimitive: false);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static AudioCueDefinition[] GenerateArtPassAudioCues()
        {
            var cues = new List<AudioCueDefinition>();
            foreach (AudioCueId cueId in Enum.GetValues(typeof(AudioCueId)))
            {
                var wavPath = $"{ArtPassAudioDirectory}/AP_AudioCue_{cueId}.wav";
                WritePlaceholderWav(wavPath, 220f + ((int)cueId * 37f), cueId is AudioCueId.DesignerPlace or AudioCueId.DesignerErase ? 0.08f : 0.14f);
                AssetDatabase.ImportAsset(wavPath, ImportAssetOptions.ForceUpdate);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavPath);

                var path = $"{Milestone9AssetGenerator.AudioCueDirectory}/AudioCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<AudioCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<AudioCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, clip, 0.42f, cueId is AudioCueId.DesignerPlace or AudioCueId.DesignerErase ? 0f : 0.55f);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static GameObject VfxPrefabFor(VfxCueId cueId)
        {
            var role = cueId switch
            {
                VfxCueId.ProjectileFire => PresentationPrefabRole.VfxProjectileFire,
                VfxCueId.EnemyHit => PresentationPrefabRole.VfxEnemyHit,
                VfxCueId.EnemyDeath => PresentationPrefabRole.VfxEnemyDeath,
                VfxCueId.PlayerHit => PresentationPrefabRole.VfxPlayerHit,
                VfxCueId.RewardClaim => PresentationPrefabRole.VfxRewardClaim,
                VfxCueId.DoorUnlock => PresentationPrefabRole.VfxDoorUnlock,
                VfxCueId.RoomClear => PresentationPrefabRole.VfxRoomClear,
                VfxCueId.PortalComplete => PresentationPrefabRole.VfxPortalComplete,
                VfxCueId.ChestOpen => PresentationPrefabRole.VfxChestOpen,
                VfxCueId.CoinPickup => PresentationPrefabRole.VfxCoinPickup,
                _ => PresentationPrefabRole.VfxProjectileFire
            };

            return AssetDatabase.LoadAssetAtPath<GameObject>($"{ArtPassVfxDirectory}/VFX_{role}.prefab");
        }

        private static void ConfigureAddressables(
            MaterialPaletteDefinition palette,
            IEnumerable<PresentationPrefabBinding> prefabBindings,
            IEnumerable<VfxCueDefinition> vfxCues,
            IEnumerable<AudioCueDefinition> audioCues,
            PresentationContentCatalog catalog)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            foreach (var label in RequiredAddressableLabels)
            {
                settings.AddLabel(label, postEvent: false);
            }

            var group = settings.FindGroup(AddressablesGroupName) ?? settings.CreateGroup(
                AddressablesGroupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));

            MarkAddressable(settings, group, AssetDatabase.GetAssetPath(palette), "hollow.artpass.palette", "hollow.artpass.materials");
            foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
            {
                MarkAddressable(settings, group, $"{ArtPassMaterialDirectory}/AP_M_{role}.mat", $"hollow.artpass.material.{role}", "hollow.artpass.materials");
            }

            foreach (var binding in prefabBindings)
            {
                var prefab = binding.Prefab;
                if (prefab == null)
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(prefab);
                var label = binding.Role.ToString().StartsWith("Vfx", StringComparison.Ordinal)
                    ? "hollow.artpass.vfx"
                    : "hollow.artpass.prefabs";
                MarkAddressable(settings, group, path, $"hollow.artpass.prefab.{binding.Role}", label);
            }

            foreach (var cue in audioCues)
            {
                if (cue.Clip != null)
                {
                    MarkAddressable(settings, group, AssetDatabase.GetAssetPath(cue.Clip), $"hollow.artpass.audio.clip.{cue.CueId}", "hollow.artpass.audio");
                }
            }

            RestoreSharedPresentationAddressables(settings, vfxCues, audioCues, catalog);
            EditorUtility.SetDirty(settings);
        }

        private static void RestoreSharedPresentationAddressables(
            AddressableAssetSettings settings,
            IEnumerable<VfxCueDefinition> vfxCues,
            IEnumerable<AudioCueDefinition> audioCues,
            PresentationContentCatalog catalog)
        {
            var localGroup = settings.FindGroup(Milestone9AssetGenerator.AddressablesGroupName);
            if (localGroup == null)
            {
                return;
            }

            MarkSharedAddressable(settings, localGroup, AssetDatabase.GetAssetPath(catalog), "hollow.presentation.catalog", "hollow.data");
            foreach (var cue in vfxCues)
            {
                MarkSharedAddressable(settings, localGroup, AssetDatabase.GetAssetPath(cue), $"hollow.vfx.{cue.CueId}", "hollow.vfx");
            }

            foreach (var cue in audioCues)
            {
                MarkSharedAddressable(settings, localGroup, AssetDatabase.GetAssetPath(cue), $"hollow.audio.{cue.CueId}", "hollow.audio");
            }
        }

        private static void MarkAddressable(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address, string label)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel("hollow.artpass", true, force: true, postEvent: false);
            entry.SetLabel(label, true, force: true, postEvent: false);
        }

        private static void MarkSharedAddressable(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address, string label)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = address;
            entry.SetLabel("hollow.artpass", false, force: true, postEvent: false);
            entry.SetLabel("hollow.artpass.prefabs", false, force: true, postEvent: false);
            entry.SetLabel("hollow.artpass.materials", false, force: true, postEvent: false);
            entry.SetLabel("hollow.artpass.vfx", false, force: true, postEvent: false);
            entry.SetLabel("hollow.artpass.audio", false, force: true, postEvent: false);
            entry.SetLabel(label, true, force: true, postEvent: false);
        }

        private static void WritePlaceholderWav(string path, float frequencyHz, float durationSeconds)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * durationSeconds));
            var dataSize = sampleCount * 2;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ArtPassAudioDirectory);
            using var writer = new BinaryWriter(File.Open(path, FileMode.Create));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            for (var sample = 0; sample < sampleCount; sample++)
            {
                var t = sample / (float)sampleRate;
                var envelope = Mathf.Clamp01(1f - t / Mathf.Max(0.001f, durationSeconds));
                var value = Mathf.Sin(2f * Mathf.PI * frequencyHz * t) * 0.18f * envelope;
                writer.Write((short)Mathf.RoundToInt(value * short.MaxValue));
            }
        }

        private static Color ArtPassColorFor(MaterialRole role)
        {
            var fallback = MaterialResolver.FallbackColorFor(role);
            var baseColor = Color.Lerp(new Color(0.035f, 0.032f, 0.045f, fallback.a), fallback, 0.7f);
            if (role is MaterialRole.RoomFloor or MaterialRole.DesignerGround)
            {
                return new Color(0.13f, 0.16f, 0.18f, fallback.a);
            }

            if (role is MaterialRole.RoomObstacleRock or MaterialRole.DesignerRock)
            {
                return new Color(0.2f, 0.19f, 0.17f, fallback.a);
            }

            return new Color(baseColor.r, baseColor.g, baseColor.b, fallback.a);
        }

        private static MaterialRole MaterialRoleFor(PresentationPrefabRole role)
        {
            return role switch
            {
                PresentationPrefabRole.Player => MaterialRole.PlayerBody,
                PresentationPrefabRole.EnemyFlying => MaterialRole.EnemyFlying,
                PresentationPrefabRole.EnemyFast => MaterialRole.EnemyFast,
                PresentationPrefabRole.EnemyHeavy => MaterialRole.EnemyHeavy,
                PresentationPrefabRole.EnemyCharger => MaterialRole.EnemyCharger,
                PresentationPrefabRole.EnemyTurret => MaterialRole.EnemyTurret,
                PresentationPrefabRole.EnemySplitter => MaterialRole.EnemySplitter,
                PresentationPrefabRole.EnemySpittingPod => MaterialRole.EnemySpittingPod,
                PresentationPrefabRole.EnemyRat => MaterialRole.EnemyRat,
                PresentationPrefabRole.EnemySpider => MaterialRole.EnemySpider,
                PresentationPrefabRole.EnemyHollowBird => MaterialRole.EnemyHollowBird,
                PresentationPrefabRole.EnemyHollowBeast => MaterialRole.EnemyHollowBeast,
                PresentationPrefabRole.EnemySkeletonSword => MaterialRole.EnemySkeletonSword,
                PresentationPrefabRole.EnemySkeletonSpear => MaterialRole.EnemySkeletonSpear,
                PresentationPrefabRole.EnemyKnight => MaterialRole.EnemyKnight,
                PresentationPrefabRole.EnemyGiant => MaterialRole.EnemyGiant,
                PresentationPrefabRole.EnemyHollowArcher => MaterialRole.EnemyHollowArcher,
                PresentationPrefabRole.EnemyPowderGunner => MaterialRole.EnemyPowderGunner,
                PresentationPrefabRole.EnemyKnifeThrower => MaterialRole.EnemyKnifeThrower,
                PresentationPrefabRole.EnemyRepeaterTurret => MaterialRole.EnemyRepeaterTurret,
                PresentationPrefabRole.EnemyClockworkSentry => MaterialRole.EnemyClockworkSentry,
                PresentationPrefabRole.EnemyStarforgedOctantSentry => MaterialRole.EnemyStarforgedOctantSentry,
                PresentationPrefabRole.EnemyCrimsonRailSpider => MaterialRole.EnemyCrimsonRailSpider,
                PresentationPrefabRole.EnemyAzureMinigunTurret => MaterialRole.EnemyAzureMinigunTurret,
                PresentationPrefabRole.EnemyHollowAcolyte => MaterialRole.EnemyHollowAcolyte,
                PresentationPrefabRole.EnemyWraith => MaterialRole.EnemyWraith,
                PresentationPrefabRole.EnemySoulEater => MaterialRole.EnemySoulEater,
                PresentationPrefabRole.EnemyCurseBinder => MaterialRole.EnemyCurseBinder,
                PresentationPrefabRole.EnemyGraveLantern => MaterialRole.EnemyGraveLantern,
                PresentationPrefabRole.EnemyBoss => MaterialRole.EnemyBoss,
                PresentationPrefabRole.Projectile => MaterialRole.Projectile,
                PresentationPrefabRole.EnemyProjectile => MaterialRole.EnemyProjectile,
                PresentationPrefabRole.RoomFloor => MaterialRole.RoomFloor,
                PresentationPrefabRole.RoomObstacleRock => MaterialRole.RoomObstacleRock,
                PresentationPrefabRole.DoorLocked => MaterialRole.DoorLocked,
                PresentationPrefabRole.DoorActive => MaterialRole.DoorActive,
                PresentationPrefabRole.DoorCleared => MaterialRole.DoorCleared,
                PresentationPrefabRole.DoorUnavailable => MaterialRole.DoorUnavailable,
                PresentationPrefabRole.RewardPickup => MaterialRole.RewardPickup,
                PresentationPrefabRole.BossKeyPickup => MaterialRole.BossKeyPickup,
                PresentationPrefabRole.HubShop or PresentationPrefabRole.HubShopCard => MaterialRole.HubShop,
                PresentationPrefabRole.HubReturnPortal => MaterialRole.HubReturnPortal,
                PresentationPrefabRole.NextBranchPortal => MaterialRole.NextBranchPortal,
                PresentationPrefabRole.SecretDoorDebug => MaterialRole.SecretDoorDebug,
                PresentationPrefabRole.WeaponMelee => MaterialRole.PlayerBody,
                PresentationPrefabRole.WeaponRanged => MaterialRole.Projectile,
                PresentationPrefabRole.Armor => MaterialRole.DoorLocked,
                PresentationPrefabRole.ActiveItemPickup => MaterialRole.RewardPickup,
                PresentationPrefabRole.ConsumableCardPickup => MaterialRole.SpawnReward,
                PresentationPrefabRole.RoomHazardSpike => MaterialRole.RoomHazardSpike,
                PresentationPrefabRole.StandardBarrel => MaterialRole.RoomBarrel,
                PresentationPrefabRole.ExplosiveBarrel => MaterialRole.RoomExplosiveBarrel,
                PresentationPrefabRole.HazardCoinDrop => MaterialRole.HazardCoinDrop,
                PresentationPrefabRole.ChestNormal => MaterialRole.ChestNormal,
                PresentationPrefabRole.ChestGolden => MaterialRole.ChestGolden,
                PresentationPrefabRole.CoinCopper => MaterialRole.CoinCopper,
                PresentationPrefabRole.CoinSilver => MaterialRole.CoinSilver,
                PresentationPrefabRole.CoinGold => MaterialRole.CoinGold,
                PresentationPrefabRole.VfxEnemyHit or PresentationPrefabRole.VfxPlayerHit => MaterialRole.CombatHitFlash,
                PresentationPrefabRole.VfxRewardClaim => MaterialRole.RewardPickup,
                PresentationPrefabRole.VfxDoorUnlock or PresentationPrefabRole.VfxRoomClear => MaterialRole.DoorCleared,
                PresentationPrefabRole.VfxPortalComplete => MaterialRole.HubReturnPortal,
                PresentationPrefabRole.VfxProjectileFire => MaterialRole.Projectile,
                PresentationPrefabRole.VfxChestOpen => MaterialRole.ChestGolden,
                PresentationPrefabRole.VfxCoinPickup => MaterialRole.CoinGold,
                PresentationPrefabRole.VfxEnemyDeath => MaterialRole.EnemyNormal,
                _ => MaterialRole.EnemyNormal
            };
        }
    }
}
