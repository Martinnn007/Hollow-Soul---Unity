using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class BetaStabilizationReportBuilder
    {
        public static ArtPassPrefabCalibrationReport BuildArtPassCalibrationReport()
        {
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            var records = new List<ArtPassPrefabCalibrationRecord>();
            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                records.Add(BuildCalibrationRecord(role, catalog));
            }

            var report = new ArtPassPrefabCalibrationReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                catalogPath = Milestone9AssetGenerator.CatalogPath,
                artPassRoot = Milestone23AssetGenerator.ArtPassRoot,
                records = records.OrderBy(record => record.group).ThenBy(record => record.role).ToArray()
            };
            report.Recalculate();
            return report;
        }

        public static DeveloperInspectionCoverageReport BuildDeveloperInspectionCoverageReport(ArtPassPrefabCalibrationReport calibrationReport = null)
        {
            calibrationReport ??= BuildArtPassCalibrationReport();
            var byRole = calibrationReport.records
                .Where(record => record != null)
                .ToDictionary(record => record.role, record => record);
            var entries = new List<DeveloperInspectionEntry>();
            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                byRole.TryGetValue(role.ToString(), out var record);
                entries.Add(new DeveloperInspectionEntry
                {
                    group = GroupFor(role),
                    entityId = role.ToString(),
                    displayName = DisplayNameFor(role),
                    prefabRole = role.ToString(),
                    labRoom = LabRoomFor(role),
                    bindingStatus = record != null ? record.bindingStatus : ArtPassBindingStatus.Missing,
                    spawnMode = SpawnModeFor(role),
                    notes = "Inspect through Developer Lab display stand and debug spawn menu."
                });
            }

            entries.Add(new DeveloperInspectionEntry
            {
                group = "Rooms",
                entityId = "pit_hole_marker",
                displayName = "Pit / Hole Marker",
                prefabRole = "RoomDesigner hole",
                labRoom = "Developer Lab 01 - Environment Basics",
                bindingStatus = ArtPassBindingStatus.Bound,
                spawnMode = InspectionSpawnMode.StaticDisplay,
                notes = "Authoring/runtime floor-hole marker; dedicated ArtPass role can be added later."
            });

            var report = new DeveloperInspectionCoverageReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                entries = entries.OrderBy(entry => entry.group).ThenBy(entry => entry.entityId).ToArray()
            };
            report.Recalculate();
            return report;
        }

        public static BetaLockReport BuildBetaLockReport(BetaContentLockDefinition contentLock, BetaQaChecklistDefinition qaChecklist)
        {
            var checks = new List<BetaLockCheckResult>();
            checks.Add(contentLock != null && !string.IsNullOrWhiteSpace(contentLock.LockId)
                ? BetaLockCheckResult.Passed("content-lock", $"Beta content lock `{contentLock.LockId}` is present.")
                : BetaLockCheckResult.Failed("content-lock", "Beta content lock asset is missing or empty.", "Run Hollow/Generation/Generate Milestone 63 Assets."));
            checks.Add(qaChecklist != null && qaChecklist.SmokeRoutes.Length >= 6
                ? BetaLockCheckResult.Passed("qa-checklist", $"QA checklist `{qaChecklist.ChecklistId}` contains {qaChecklist.SmokeRoutes.Length} smoke routes.")
                : BetaLockCheckResult.Failed("qa-checklist", "Beta QA checklist is missing required smoke routes.", "Run Hollow/Generation/Generate Milestone 64 Assets."));
            checks.Add(File.Exists(Milestone56AssetGenerator.ReportJsonPath)
                ? BetaLockCheckResult.Passed("artpass-calibration", "M56 ArtPass calibration report exists.")
                : BetaLockCheckResult.Failed("artpass-calibration", "M56 ArtPass calibration report is missing.", "Run Hollow/Generation/Generate Milestone 56 Assets."));
            checks.Add(File.Exists(Milestone57AssetGenerator.ReportJsonPath)
                ? BetaLockCheckResult.Passed("developer-lab-coverage", "M57 Developer Lab coverage report exists.")
                : BetaLockCheckResult.Failed("developer-lab-coverage", "M57 Developer Lab coverage report is missing.", "Run Hollow/Generation/Generate Milestone 57 Assets."));
            checks.Add(File.Exists(Milestone63AssetGenerator.PdfPath)
                ? BetaLockCheckResult.Passed("beta-content-catalogue", "M63 beta content catalogue PDF exists.")
                : BetaLockCheckResult.Failed("beta-content-catalogue", "M63 beta content catalogue PDF is missing.", "Run Hollow/Generation/Generate Milestone 63 Assets."));
            checks.Add(BetaLockCheckResult.BlockedByEnvironment(
                "platform-builds",
                "Windows/VisionOS build execution depends on locally installed Unity platform modules and signing/simulator tooling.",
                "Run M65 on the target build machine and attach generated build/device QA logs."));

            var report = new BetaLockReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                lockId = contentLock != null ? contentLock.LockId : "m64_vertical_slice_beta_lock_gate",
                checks = checks.ToArray()
            };
            report.Recalculate();
            return report;
        }

        private static ArtPassPrefabCalibrationRecord BuildCalibrationRecord(PresentationPrefabRole role, PresentationContentCatalog catalog)
        {
            var warnings = new List<string>();
            var errors = new List<string>();
            var record = new ArtPassPrefabCalibrationRecord
            {
                role = role.ToString(),
                displayName = DisplayNameFor(role),
                group = GroupFor(role),
                prefabPath = ExpectedPrefabPathFor(role),
                safetyStatus = ArtPassPrefabSafetyStatus.MissingBinding,
                readinessStatus = ArtPassProductionReadinessStatus.MissingBinding,
                bindingStatus = ArtPassBindingStatus.Missing
            };

            if (catalog == null || !catalog.TryGetPrefab(role, out var prefab) || prefab == null)
            {
                errors.Add($"Missing active catalog binding for {role}.");
                record.errors = errors.ToArray();
                return record;
            }

            record.prefabPath = AssetDatabase.GetAssetPath(prefab);
            record.rootScale = prefab.transform.localScale;
            record.bindingStatus = ArtPassBindingStatus.Bound;
            var marker = prefab.GetComponent<PresentationVisualMarker>();
            record.hasRootMarker = marker != null;
            if (marker == null)
            {
                warnings.Add("PresentationVisualMarker should be on the AP_* root so runtime wrappers do not need to repair it.");
            }
            else if (marker.Role != role)
            {
                errors.Add($"Root PresentationVisualMarker declares {marker.Role}, expected {role}.");
            }

            if (!IsApproximatelyOne(prefab.transform.localScale))
            {
                warnings.Add($"Root scale should be 1,1,1; found {prefab.transform.localScale}.");
            }

            if (TryGetRendererBounds(prefab.transform, out var bounds))
            {
                record.hasRenderer = true;
                record.rendererBoundsSize = bounds.size;
                record.rendererLocalCenter = prefab.transform.InverseTransformPoint(bounds.center);
                record.rendererBottomLocalY = prefab.transform.InverseTransformPoint(bounds.min).y;
                if (Mathf.Abs(record.rendererLocalCenter.x) > 0.25f || Mathf.Abs(record.rendererLocalCenter.z) > 0.25f)
                {
                    warnings.Add($"Renderer should be centered around X/Z origin; center is {record.rendererLocalCenter}.");
                }

                if (Mathf.Abs(record.rendererBottomLocalY) > 0.1f)
                {
                    warnings.Add($"Renderer bottom should sit near y=0; bottom is {record.rendererBottomLocalY:0.###}.");
                }

                if (bounds.size.x < 0.03f || bounds.size.y < 0.03f || bounds.size.z < 0.03f)
                {
                    warnings.Add($"Renderer bounds look very small: {bounds.size}.");
                }

                if (bounds.size.x > 8f || bounds.size.y > 8f || bounds.size.z > 8f)
                {
                    warnings.Add($"Renderer bounds look too large for Vision-safe ArtPass use: {bounds.size}.");
                }
            }
            else
            {
                errors.Add("Prefab has no renderer.");
            }

            record.materialCount = CountMaterials(prefab);
            if (record.materialCount == 0)
            {
                errors.Add("Prefab renderers have no assigned material.");
            }

            foreach (var collider in prefab.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                errors.Add($"Visual prefab must not include collider: {collider.gameObject.name}.");
            }

            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
            {
                if (component == null)
                {
                    errors.Add("Prefab has a missing script component.");
                }
                else if (component is not PresentationVisualMarker)
                {
                    errors.Add($"Visual prefab must not include runtime script: {component.GetType().Name}.");
                }
            }

            record.warnings = warnings.ToArray();
            record.errors = errors.ToArray();
            record.safetyStatus = ResolveSafetyStatus(record, warnings, errors);
            record.readinessStatus = ResolveReadinessStatus(prefab, record.safetyStatus);
            record.bindingStatus = record.safetyStatus == ArtPassPrefabSafetyStatus.UnsafePrefab
                ? ArtPassBindingStatus.Unsafe
                : record.readinessStatus == ArtPassProductionReadinessStatus.PrototypeFallback
                    ? ArtPassBindingStatus.PrototypeFallback
                    : ArtPassBindingStatus.Bound;
            return record;
        }

        private static ArtPassPrefabSafetyStatus ResolveSafetyStatus(ArtPassPrefabCalibrationRecord record, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
        {
            if (errors.Any(error => error.Contains("collider", StringComparison.OrdinalIgnoreCase) ||
                                    error.Contains("script", StringComparison.OrdinalIgnoreCase) ||
                                    error.Contains("marker declares", StringComparison.OrdinalIgnoreCase)))
            {
                return ArtPassPrefabSafetyStatus.UnsafePrefab;
            }

            if (errors.Any(error => error.Contains("no renderer", StringComparison.OrdinalIgnoreCase)))
            {
                return ArtPassPrefabSafetyStatus.MissingRenderer;
            }

            if (errors.Any(error => error.Contains("material", StringComparison.OrdinalIgnoreCase)))
            {
                return ArtPassPrefabSafetyStatus.MissingMaterial;
            }

            return warnings.Count > 0 ? ArtPassPrefabSafetyStatus.NeedsScaleFix : ArtPassPrefabSafetyStatus.Ready;
        }

        private static ArtPassProductionReadinessStatus ResolveReadinessStatus(GameObject prefab, ArtPassPrefabSafetyStatus safetyStatus)
        {
            return safetyStatus switch
            {
                ArtPassPrefabSafetyStatus.MissingBinding => ArtPassProductionReadinessStatus.MissingBinding,
                ArtPassPrefabSafetyStatus.UnsafePrefab => ArtPassProductionReadinessStatus.UnsafePrefab,
                ArtPassPrefabSafetyStatus.MissingRenderer or ArtPassPrefabSafetyStatus.MissingMaterial or ArtPassPrefabSafetyStatus.NeedsScaleFix => ArtPassProductionReadinessStatus.NeedsAssetWork,
                _ => HasProductionAssetEvidence(prefab) ? ArtPassProductionReadinessStatus.Ready : ArtPassProductionReadinessStatus.PrototypeFallback
            };
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds && bounds.size.sqrMagnitude > 0.0001f;
        }

        private static int CountMaterials(GameObject prefab)
        {
            return prefab.GetComponentsInChildren<Renderer>(includeInactive: true)
                .SelectMany(renderer => renderer.sharedMaterials ?? Array.Empty<Material>())
                .Count(material => material != null);
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

        private static bool IsApproximatelyOne(Vector3 value)
        {
            return Mathf.Abs(value.x - 1f) < 0.01f &&
                Mathf.Abs(value.y - 1f) < 0.01f &&
                Mathf.Abs(value.z - 1f) < 0.01f;
        }

        private static string ExpectedPrefabPathFor(PresentationPrefabRole role)
        {
            return role.ToString().StartsWith("Vfx", StringComparison.Ordinal)
                ? $"{Milestone23AssetGenerator.ArtPassVfxDirectory}/VFX_{role}.prefab"
                : $"{Milestone23AssetGenerator.ArtPassRoot}/AP_{role}.prefab";
        }

        public static string GroupFor(PresentationPrefabRole role)
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
                PresentationPrefabRole.EnemyBoss => "Bosses",
                PresentationPrefabRole.RoomFloor or PresentationPrefabRole.RoomObstacleRock or PresentationPrefabRole.RoomHazardSpike => "Rooms",
                PresentationPrefabRole.StandardBarrel or PresentationPrefabRole.ExplosiveBarrel => "Hazards",
                PresentationPrefabRole.ChestNormal or PresentationPrefabRole.ChestGolden => "Chests",
                PresentationPrefabRole.CoinCopper or PresentationPrefabRole.CoinSilver or PresentationPrefabRole.CoinGold or PresentationPrefabRole.HazardCoinDrop => "Coins",
                PresentationPrefabRole.DoorLocked or PresentationPrefabRole.DoorActive or PresentationPrefabRole.DoorCleared or
                    PresentationPrefabRole.DoorUnavailable or PresentationPrefabRole.SecretDoorDebug => "Doors",
                PresentationPrefabRole.Projectile or PresentationPrefabRole.EnemyProjectile => "Projectiles",
                PresentationPrefabRole.RewardPickup or PresentationPrefabRole.BossKeyPickup => "Rewards",
                PresentationPrefabRole.HubShop or PresentationPrefabRole.HubShopCard or PresentationPrefabRole.HubReturnPortal or
                    PresentationPrefabRole.NextBranchPortal => "Hub",
                PresentationPrefabRole.WeaponMelee or PresentationPrefabRole.WeaponRanged or PresentationPrefabRole.Armor => "Equipment",
                PresentationPrefabRole.ActiveItemPickup or PresentationPrefabRole.ConsumableCardPickup => "Items",
                _ when role.ToString().StartsWith("Vfx", StringComparison.Ordinal) => "VFX",
                _ => "Other"
            };
        }

        public static string DisplayNameFor(PresentationPrefabRole role)
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

        private static string LabRoomFor(PresentationPrefabRole role)
        {
            return GroupFor(role) switch
            {
                "Rooms" or "Doors" or "Hazards" => "Developer Lab 01 - Environment Basics",
                "Chests" or "Coins" or "Rewards" => "Developer Lab 02 - Economy And Sustain",
                "Equipment" or "Items" => "Developer Lab 03 - Build Pickups",
                "Enemies" => "Developer Lab 04 - Enemy Gallery",
                "Projectiles" or "VFX" => "Developer Lab 05 - Projectile VFX Gallery",
                "Hub" => "Developer Lab 07 - Progression Props",
                "Bosses" => "Developer Lab 08-10 - Boss Galleries",
                _ => "Developer Lab"
            };
        }

        private static InspectionSpawnMode SpawnModeFor(PresentationPrefabRole role)
        {
            return GroupFor(role) switch
            {
                "Enemies" or "Bosses" => InspectionSpawnMode.FrozenRuntime,
                "Projectiles" => InspectionSpawnMode.LiveRuntime,
                _ => InspectionSpawnMode.StaticDisplay
            };
        }
    }
}
