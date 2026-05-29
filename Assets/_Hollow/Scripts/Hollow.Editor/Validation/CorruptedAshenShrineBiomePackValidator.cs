using System;
using System.Collections.Generic;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class CorruptedAshenShrineBiomePackValidator
    {
        [MenuItem("Hollow/Validation/Run Corrupted Ashen Shrine Biome Pack Validation")]
        public static void ValidateMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static bool Validate()
        {
            return Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        public static bool Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = ValidateAll();
            if (failures.Count == 0)
            {
                Debug.Log("Corrupted Ashen Shrine biome pack validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return true;
            }

            Debug.LogError($"Corrupted Ashen Shrine biome pack validation failed.\n{string.Join("\n", failures)}");
            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }

            return false;
        }

        public static void ValidateOrThrow()
        {
            var failures = ValidateAll();
            if (failures.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", failures));
            }
        }

        public static List<string> ValidateAll()
        {
            var failures = new List<string>();
            ValidateSourceTextures(failures);
            ValidateBiomeCatalog(failures);
            ValidateCorruptedRoom(failures);
            ValidateNormalBiomeUnaffected(failures);
            ValidateRuntimeResolution(failures);
            return failures;
        }

        private static void ValidateSourceTextures(List<string> failures)
        {
            foreach (var path in new[]
                     {
                         CorruptedAshenShrineBiomeAssetGenerator.FloorTexturePath,
                         CorruptedAshenShrineBiomeAssetGenerator.WallTexturePath
                     })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    failures.Add($"Missing Ashen Shrine texture: {path}");
                    continue;
                }

                if (texture.width != CorruptedAshenShrineBiomeAssetGenerator.TextureSize ||
                    texture.height != CorruptedAshenShrineBiomeAssetGenerator.TextureSize)
                {
                    failures.Add($"{path} must be {CorruptedAshenShrineBiomeAssetGenerator.TextureSize}x{CorruptedAshenShrineBiomeAssetGenerator.TextureSize}; found {texture.width}x{texture.height}.");
                }

                if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                    importer.wrapMode != TextureWrapMode.Repeat)
                {
                    failures.Add($"{path} must import with Repeat wrapping.");
                }
            }
        }

        private static void ValidateBiomeCatalog(List<string> failures)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomBiomeCatalogDefinition>(CorruptedAshenShrineBiomeAssetGenerator.BiomeCatalogPath);
            if (catalog == null)
            {
                failures.Add($"Missing room biome catalog: {CorruptedAshenShrineBiomeAssetGenerator.BiomeCatalogPath}");
                return;
            }

            if (!catalog.TryGetBiome(RoomBiomeIds.CorruptedAshenShrine, out var biome) || biome == null)
            {
                failures.Add("Room biome catalog does not resolve corrupted_ashen_shrine.");
                return;
            }

            if (biome.DisplayName != "Ashen Shrine")
            {
                failures.Add($"Corrupted Ashen Shrine display name should be 'Ashen Shrine'; found '{biome.DisplayName}'.");
            }

            foreach (var role in CorruptedAshenShrineBiomeAssetGenerator.RequiredMaterialRoles)
            {
                if (!biome.TryResolve(role, out var material) || material == null)
                {
                    failures.Add($"Ashen Shrine biome is missing material override for {role}.");
                }
            }

            foreach (var role in new[]
                     {
                         PresentationPrefabRole.DecorGrassTuft,
                         PresentationPrefabRole.DecorCrystalCluster,
                         PresentationPrefabRole.DecorSmallTree,
                         PresentationPrefabRole.DecorStoneRuin
                     })
            {
                if (!biome.TryResolve(role, out var prefab) || prefab == null)
                {
                    failures.Add($"Ashen Shrine biome is missing prefab override for {role}.");
                    continue;
                }

                if (prefab.GetComponent<PresentationVisualMarker>() == null)
                {
                    failures.Add($"Ashen Shrine prefab {role} is missing PresentationVisualMarker.");
                }

                if (prefab.GetComponentsInChildren<Collider>(includeInactive: true).Length > 0 ||
                    prefab.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length > 0)
                {
                    failures.Add($"Ashen Shrine decor prefab {role} must be visual-only.");
                }
            }
        }

        private static void ValidateCorruptedRoom(List<string> failures)
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(CorruptedAshenShrineBiomeAssetGenerator.CorruptedRoomPath);
            if (text == null)
            {
                failures.Add($"Missing corrupted room fixture: {CorruptedAshenShrineBiomeAssetGenerator.CorruptedRoomPath}");
                return;
            }

            var asset = HollowRuntimeV2Importer.Import(text.text);
            if (!RoomBiomeIds.Matches(asset.BiomeId, RoomBiomeIds.CorruptedAshenShrine))
            {
                failures.Add($"Corrupted chest room should import as {RoomBiomeIds.CorruptedAshenShrine}; found {asset.BiomeId}.");
            }
        }

        private static void ValidateNormalBiomeUnaffected(List<string> failures)
        {
            var verdant = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Hollow/Data/Rooms/Biomes/VerdantRuins/verdant_macro_single_1x1.hollowruntime.json");
            if (verdant == null)
            {
                return;
            }

            var asset = HollowRuntimeV2Importer.Import(verdant.text);
            if (!RoomBiomeIds.Matches(asset.BiomeId, RoomBiomeIds.VerdantRuins))
            {
                failures.Add($"Verdant room biome should remain {RoomBiomeIds.VerdantRuins}; found {asset.BiomeId}.");
            }
        }

        private static void ValidateRuntimeResolution(List<string> failures)
        {
            var floor = RoomBiomePresentationResolver.ResolveMaterial(RoomBiomeIds.CorruptedAshenShrine, MaterialRole.RoomFloor);
            var wall = RoomBiomePresentationResolver.ResolveMaterial(RoomBiomeIds.CorruptedAshenShrine, MaterialRole.RoomWall);
            var chest = RoomBiomePresentationResolver.ResolvePrefab(RoomBiomeIds.CorruptedAshenShrine, PresentationPrefabRole.ChestCorrupted);
            if (floor == null)
            {
                failures.Add("RoomBiomePresentationResolver did not resolve Ashen Shrine floor material.");
            }

            if (wall == null)
            {
                failures.Add("RoomBiomePresentationResolver did not resolve Ashen Shrine wall material.");
            }

            if (chest == null)
            {
                failures.Add("RoomBiomePresentationResolver did not preserve corrupted chest prefab fallback resolution.");
            }

            var host = new GameObject("AshenShrinePreviewValidationHost");
            try
            {
                var visual = RoomBiomePresentationResolver.InstantiateVisual(
                    RoomBiomeIds.CorruptedAshenShrine,
                    PresentationPrefabRole.DecorStoneRuin,
                    host.transform,
                    Vector3.zero,
                    Vector3.one);
                if (visual == null)
                {
                    failures.Add("RoomDesigner/runtime preview could not instantiate Ashen Shrine decor override.");
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
