using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class ArtPassProductionValidator
    {
        private static readonly HashSet<PresentationPrefabRole> CorePriorityRoles = new()
        {
            PresentationPrefabRole.Player,
            PresentationPrefabRole.RoomFloor,
            PresentationPrefabRole.RoomObstacleRock,
            PresentationPrefabRole.DoorActive,
            PresentationPrefabRole.DoorLocked,
            PresentationPrefabRole.DoorCleared,
            PresentationPrefabRole.EnemyNormal,
            PresentationPrefabRole.EnemyBoss,
            PresentationPrefabRole.Projectile,
            PresentationPrefabRole.EnemyProjectile,
            PresentationPrefabRole.RewardPickup,
            PresentationPrefabRole.BossKeyPickup,
            PresentationPrefabRole.HubShop,
            PresentationPrefabRole.HubShopCard,
            PresentationPrefabRole.NextBranchPortal,
            PresentationPrefabRole.HubReturnPortal
        };

        private static readonly HashSet<PresentationPrefabRole> RoomDesignerSceneRoles = new()
        {
            PresentationPrefabRole.RoomFloor,
            PresentationPrefabRole.RoomObstacleRock,
            PresentationPrefabRole.RoomHazardSpike,
            PresentationPrefabRole.DoorActive,
            PresentationPrefabRole.DoorUnavailable,
            PresentationPrefabRole.SecretDoorDebug,
            PresentationPrefabRole.Player,
            PresentationPrefabRole.RewardPickup,
            PresentationPrefabRole.ChestNormal,
            PresentationPrefabRole.StandardBarrel,
            PresentationPrefabRole.ExplosiveBarrel,
            PresentationPrefabRole.EnemyNormal,
            PresentationPrefabRole.EnemyFlying,
            PresentationPrefabRole.EnemyFast,
            PresentationPrefabRole.EnemyHeavy,
            PresentationPrefabRole.EnemyCharger,
            PresentationPrefabRole.EnemyTurret,
            PresentationPrefabRole.EnemySplitter,
            PresentationPrefabRole.EnemySpittingPod,
            PresentationPrefabRole.EnemyRat,
            PresentationPrefabRole.EnemySpider,
            PresentationPrefabRole.EnemyHollowBird,
            PresentationPrefabRole.EnemyHollowBeast,
            PresentationPrefabRole.EnemySkeletonSword,
            PresentationPrefabRole.EnemySkeletonSpear,
            PresentationPrefabRole.EnemyKnight,
            PresentationPrefabRole.EnemyGiant,
            PresentationPrefabRole.EnemyHollowArcher,
            PresentationPrefabRole.EnemyPowderGunner,
            PresentationPrefabRole.EnemyKnifeThrower,
            PresentationPrefabRole.EnemyRepeaterTurret,
            PresentationPrefabRole.EnemyClockworkSentry,
            PresentationPrefabRole.EnemyStarforgedOctantSentry,
            PresentationPrefabRole.EnemyCrimsonRailSpider,
            PresentationPrefabRole.EnemyAzureMinigunTurret,
            PresentationPrefabRole.EnemyHollowAcolyte,
            PresentationPrefabRole.EnemyWraith,
            PresentationPrefabRole.EnemySoulEater,
            PresentationPrefabRole.EnemyCurseBinder,
            PresentationPrefabRole.EnemyGraveLantern
        };

        public static ArtPassProductionStatusReport BuildReport()
        {
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            var records = new List<ArtPassProductionTargetRecord>();
            var duplicateRoles = catalog?.PrefabBindings
                .GroupBy(binding => binding.Role)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet() ?? new HashSet<PresentationPrefabRole>();

            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                records.Add(BuildRecord(role, catalog, duplicateRoles));
            }

            var report = new ArtPassProductionStatusReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                catalogPath = Milestone9AssetGenerator.CatalogPath,
                artPassRoot = Milestone23AssetGenerator.ArtPassRoot,
                targets = records.OrderBy(record => record.group).ThenBy(record => record.role).ToArray()
            };
            report.Recalculate();
            return report;
        }

        public static IReadOnlyList<string> ValidatePrefabSafetyForTests(GameObject prefab, PresentationPrefabRole expectedRole)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            ValidatePrefabSafety(prefab, expectedRole, "(in-memory prefab)", errors, warnings);
            return errors;
        }

        private static ArtPassProductionTargetRecord BuildRecord(
            PresentationPrefabRole role,
            PresentationContentCatalog catalog,
            HashSet<PresentationPrefabRole> duplicateRoles)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var prefabPath = ExpectedPrefabPathFor(role);
            var status = ArtPassProductionStatus.PrototypeFallback;

            if (catalog == null)
            {
                errors.Add($"Missing presentation catalog: {Milestone9AssetGenerator.CatalogPath}");
                status = ArtPassProductionStatus.MissingBinding;
            }
            else if (!catalog.TryGetPrefab(role, out var prefab) || prefab == null)
            {
                errors.Add($"Presentation catalog is missing an active ArtPass prefab binding for {role}.");
                status = ArtPassProductionStatus.MissingBinding;
            }
            else
            {
                prefabPath = AssetDatabase.GetAssetPath(prefab);
                if (duplicateRoles.Contains(role))
                {
                    errors.Add($"Presentation catalog has duplicate prefab bindings for {role}.");
                }

                ValidatePrefabSafety(prefab, role, prefabPath, errors, warnings);
                ValidateAddressableBinding(prefabPath, role, errors);
                status = errors.Count > 0
                    ? ArtPassProductionStatus.UnsafePrefab
                    : HasProductionAssetEvidence(prefab)
                        ? ArtPassProductionStatus.ProductionReady
                        : ArtPassProductionStatus.PrototypeFallback;

                if (status == ArtPassProductionStatus.PrototypeFallback)
                {
                    warnings.Add($"{role} still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later.");
                }
            }

            return new ArtPassProductionTargetRecord
            {
                role = role.ToString(),
                displayName = DisplayNameFor(role),
                group = GroupFor(role),
                prefabPath = prefabPath ?? string.Empty,
                status = status,
                corePriority = CorePriorityRoles.Contains(role),
                sceneModePreviewRole = RoomDesignerSceneRoles.Contains(role),
                warnings = warnings.ToArray(),
                errors = errors.ToArray()
            };
        }

        private static void ValidatePrefabSafety(
            GameObject prefab,
            PresentationPrefabRole expectedRole,
            string path,
            List<string> errors,
            List<string> warnings)
        {
            if (prefab == null)
            {
                errors.Add($"Missing prefab for {expectedRole}.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(path) &&
                path != "(in-memory prefab)" &&
                !path.StartsWith(Milestone23AssetGenerator.ArtPassRoot, StringComparison.Ordinal))
            {
                errors.Add($"Prefab for {expectedRole} must live under {Milestone23AssetGenerator.ArtPassRoot}: {path}");
            }

            var marker = prefab.GetComponent<PresentationVisualMarker>();
            if (marker == null)
            {
                errors.Add($"Prefab {path} is missing PresentationVisualMarker.");
            }
            else if (marker.Role != expectedRole)
            {
                errors.Add($"Prefab {path} declares marker role {marker.Role}; expected {expectedRole}.");
            }

            if (!IsFinite(prefab.transform.localScale) ||
                prefab.transform.localScale.x <= 0f ||
                prefab.transform.localScale.y <= 0f ||
                prefab.transform.localScale.z <= 0f)
            {
                errors.Add($"Prefab {path} has invalid root scale {prefab.transform.localScale}.");
            }

            if (prefab.transform.localPosition.magnitude > 2f)
            {
                errors.Add($"Prefab {path} root pivot is more than 2m away from local origin.");
            }
            else if (prefab.transform.localPosition.magnitude > 0.05f)
            {
                warnings.Add($"Prefab {path} root pivot is offset from local origin; verify replacement art alignment.");
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                errors.Add($"Prefab {path} has no renderer.");
            }

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.sharedMaterial == null)
                {
                    errors.Add($"Prefab {path}/{renderer.gameObject.name} has no material.");
                }

                var size = renderer.bounds.size;
                if (!IsFinite(size))
                {
                    errors.Add($"Prefab {path}/{renderer.gameObject.name} has non-finite renderer bounds.");
                }
                else if (size.x > 12f || size.y > 12f || size.z > 12f)
                {
                    errors.Add($"Prefab {path}/{renderer.gameObject.name} has unsafe renderer bounds {size}.");
                }
                else if (size.x > 8f || size.y > 8f || size.z > 8f)
                {
                    warnings.Add($"Prefab {path}/{renderer.gameObject.name} has large renderer bounds {size}; verify Vision Pro comfort.");
                }
            }

            foreach (var collider in prefab.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                errors.Add($"Visual prefab must not include gameplay colliders: {path}/{collider.gameObject.name}");
            }

            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                if (component == null)
                {
                    errors.Add($"Prefab {path} has a missing script component.");
                    continue;
                }

                if (component is not PresentationVisualMarker)
                {
                    errors.Add($"Visual prefab must not include gameplay scripts: {path}/{component.GetType().Name}");
                }
            }
        }

        private static void ValidateAddressableBinding(string prefabPath, PresentationPrefabRole role, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(prefabPath) || !File.Exists(prefabPath))
            {
                errors.Add($"Prefab path for {role} is missing on disk: {prefabPath}");
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null)
            {
                errors.Add("Addressables settings are missing.");
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = string.IsNullOrWhiteSpace(guid)
                ? null
                : settings.FindAssetEntry(guid, includeImplicit: false);
            if (entry == null)
            {
                errors.Add($"ArtPass prefab is not addressable: {prefabPath}");
                return;
            }

            var requiredLabel = role.ToString().StartsWith("Vfx", StringComparison.Ordinal)
                ? "hollow.artpass.vfx"
                : "hollow.artpass.prefabs";
            if (!entry.labels.Contains("hollow.artpass") || !entry.labels.Contains(requiredLabel))
            {
                errors.Add($"ArtPass prefab {prefabPath} is missing labels hollow.artpass and/or {requiredLabel}.");
            }
        }

        private static bool HasProductionAssetEvidence(GameObject prefab)
        {
            foreach (var meshFilter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
            {
                var meshPath = meshFilter.sharedMesh != null ? AssetDatabase.GetAssetPath(meshFilter.sharedMesh) : string.Empty;
                if (!string.IsNullOrWhiteSpace(meshPath) && meshPath.StartsWith("Assets/_Hollow/Art/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    var materialPath = material != null ? AssetDatabase.GetAssetPath(material) : string.Empty;
                    if (!string.IsNullOrWhiteSpace(materialPath) &&
                        materialPath.StartsWith("Assets/_Hollow/Art/", StringComparison.Ordinal) &&
                        !Path.GetFileNameWithoutExtension(materialPath).StartsWith("AP_M_", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ExpectedPrefabPathFor(PresentationPrefabRole role)
        {
            return role.ToString().StartsWith("Vfx", StringComparison.Ordinal)
                ? $"{Milestone23AssetGenerator.ArtPassVfxDirectory}/VFX_{role}.prefab"
                : $"{Milestone23AssetGenerator.ArtPassRoot}/AP_{role}.prefab";
        }

        private static string GroupFor(PresentationPrefabRole role)
        {
            return role switch
            {
                PresentationPrefabRole.Player => "Player",
                PresentationPrefabRole.EnemyNormal or PresentationPrefabRole.EnemyFlying or PresentationPrefabRole.EnemyFast or
                    PresentationPrefabRole.EnemyHeavy or PresentationPrefabRole.EnemyCharger or PresentationPrefabRole.EnemyTurret or
                    PresentationPrefabRole.EnemySplitter or PresentationPrefabRole.EnemySpittingPod or
                    PresentationPrefabRole.EnemyRat or PresentationPrefabRole.EnemySpider or
                    PresentationPrefabRole.EnemyHollowBird or PresentationPrefabRole.EnemyHollowBeast or
                    PresentationPrefabRole.EnemySkeletonSword or PresentationPrefabRole.EnemySkeletonSpear or
                    PresentationPrefabRole.EnemyKnight or PresentationPrefabRole.EnemyGiant or
                    PresentationPrefabRole.EnemyHollowArcher or PresentationPrefabRole.EnemyPowderGunner or
                    PresentationPrefabRole.EnemyKnifeThrower or PresentationPrefabRole.EnemyRepeaterTurret or
                    PresentationPrefabRole.EnemyClockworkSentry or PresentationPrefabRole.EnemyStarforgedOctantSentry or
                    PresentationPrefabRole.EnemyCrimsonRailSpider or PresentationPrefabRole.EnemyAzureMinigunTurret or
                    PresentationPrefabRole.EnemyHollowAcolyte or
                    PresentationPrefabRole.EnemyWraith or PresentationPrefabRole.EnemySoulEater or
                    PresentationPrefabRole.EnemyCurseBinder or PresentationPrefabRole.EnemyGraveLantern => "Enemies",
                PresentationPrefabRole.EnemyBoss => "Boss",
                PresentationPrefabRole.RoomFloor or PresentationPrefabRole.RoomObstacleRock => "Rooms",
                PresentationPrefabRole.DoorLocked or PresentationPrefabRole.DoorActive or PresentationPrefabRole.DoorCleared or
                    PresentationPrefabRole.DoorUnavailable or PresentationPrefabRole.SecretDoorDebug => "Doors",
                PresentationPrefabRole.Projectile or PresentationPrefabRole.EnemyProjectile => "Projectiles",
                PresentationPrefabRole.RewardPickup or PresentationPrefabRole.BossKeyPickup => "Rewards",
                PresentationPrefabRole.HubShop or PresentationPrefabRole.HubShopCard or PresentationPrefabRole.HubReturnPortal or
                    PresentationPrefabRole.NextBranchPortal => "Hub",
                PresentationPrefabRole.WeaponMelee or PresentationPrefabRole.WeaponRanged or PresentationPrefabRole.Armor => "Equipment",
                PresentationPrefabRole.ActiveItemPickup or PresentationPrefabRole.ConsumableCardPickup => "Items",
                PresentationPrefabRole.RoomHazardSpike or PresentationPrefabRole.StandardBarrel or
                    PresentationPrefabRole.ExplosiveBarrel or PresentationPrefabRole.HazardCoinDrop => "Hazards",
                PresentationPrefabRole.ChestNormal or PresentationPrefabRole.ChestGolden => "Chests",
                PresentationPrefabRole.CoinCopper or PresentationPrefabRole.CoinSilver or PresentationPrefabRole.CoinGold => "Coins",
                _ when role.ToString().StartsWith("Vfx", StringComparison.Ordinal) => "VFX",
                _ => "Other"
            };
        }

        private static string DisplayNameFor(PresentationPrefabRole role)
        {
            var text = role.ToString();
            for (var index = text.Length - 1; index > 0; index--)
            {
                if (char.IsUpper(text[index]) && !char.IsUpper(text[index - 1]))
                {
                    text = text.Insert(index, " ");
                }
            }

            return text.Replace("Vfx ", "VFX ");
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
