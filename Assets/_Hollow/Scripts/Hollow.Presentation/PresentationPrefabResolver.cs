using System.Collections.Generic;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public static class PresentationPrefabResolver
    {
        private static readonly Dictionary<PresentationPrefabRole, GameObject> FallbackPrefabs = new();

        public static GameObject Resolve(PresentationPrefabRole role)
        {
            var catalog = PresentationContentProvider.ActiveCatalog;
            if (catalog != null && catalog.TryGetPrefab(role, out var prefab) && prefab != null)
            {
                return prefab;
            }

            return FallbackPrefabFor(role);
        }

        public static GameObject InstantiateVisual(PresentationPrefabRole role, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            return InstantiateVisual(role, null, parent, localPosition, localScale);
        }

        public static GameObject InstantiateVisual(PresentationPrefabRole role, GameObject prefabOverride, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            if (parent == null)
            {
                return null;
            }

            RemoveExistingVisual(parent, role);
            var prefab = prefabOverride != null ? prefabOverride : Resolve(role);
            if (prefab == null)
            {
                return null;
            }

            var visual = Object.Instantiate(prefab, parent);
            visual.name = $"ArtPassVisual.{role}";
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            visual.SetActive(true);
            EnsureMarker(visual, role, prefab.TryGetComponent<PresentationVisualMarker>(out var marker) && marker.IsFallback);
            StripColliders(visual);
            return visual;
        }

        internal static void ClearCache()
        {
            foreach (var fallback in FallbackPrefabs.Values)
            {
                if (fallback == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(fallback);
                }
                else
                {
                    Object.DestroyImmediate(fallback);
                }
            }

            FallbackPrefabs.Clear();
        }

        private static GameObject FallbackPrefabFor(PresentationPrefabRole role)
        {
            if (FallbackPrefabs.TryGetValue(role, out var cached) && cached != null)
            {
                return cached;
            }

            var fallback = GameObject.CreatePrimitive(PrimitiveFor(role));
            fallback.name = $"FallbackArtPass.{role}";
            fallback.hideFlags = HideFlags.HideAndDontSave;
            fallback.SetActive(false);
            fallback.transform.localScale = DefaultScaleFor(role);
            MaterialResolver.ApplyTo(fallback, MaterialRoleFor(role));
            StripColliders(fallback);
            EnsureMarker(fallback, role, isFallback: true);
            FallbackPrefabs[role] = fallback;
            return fallback;
        }

        private static void RemoveExistingVisual(Transform parent, PresentationPrefabRole role)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                var marker = child.GetComponent<PresentationVisualMarker>();
                if (marker == null || marker.Role != role)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void EnsureMarker(GameObject target, PresentationPrefabRole role, bool isFallback)
        {
            if (target == null)
            {
                return;
            }

            var marker = target.GetComponent<PresentationVisualMarker>() ?? target.AddComponent<PresentationVisualMarker>();
            marker.Configure(role, isFallback);
        }

        private static void StripColliders(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            foreach (var collider in visual.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(collider);
                }
                else
                {
                    Object.DestroyImmediate(collider);
                }
            }
        }

        private static PrimitiveType PrimitiveFor(PresentationPrefabRole role)
        {
            return role is PresentationPrefabRole.HubReturnPortal or PresentationPrefabRole.NextBranchPortal
                ? PrimitiveType.Cylinder
                : role is PresentationPrefabRole.Projectile or PresentationPrefabRole.EnemyProjectile or
                    PresentationPrefabRole.RewardPickup or PresentationPrefabRole.BossKeyPickup or
                    PresentationPrefabRole.ActiveItemPickup or PresentationPrefabRole.CoinCopper or
                    PresentationPrefabRole.CoinSilver or PresentationPrefabRole.CoinGold
                    ? PrimitiveType.Sphere
                    : PrimitiveType.Cube;
        }

        private static Vector3 DefaultScaleFor(PresentationPrefabRole role)
        {
            return role switch
            {
                PresentationPrefabRole.RoomFloor => new Vector3(1f, 0.08f, 1f),
                PresentationPrefabRole.DoorLocked or PresentationPrefabRole.DoorActive or PresentationPrefabRole.DoorCleared or PresentationPrefabRole.DoorUnavailable => new Vector3(0.8f, 1.1f, 0.16f),
                PresentationPrefabRole.DecorGrassTuft => new Vector3(0.55f, 0.32f, 0.55f),
                PresentationPrefabRole.DecorCrystalCluster => new Vector3(0.48f, 0.58f, 0.48f),
                PresentationPrefabRole.DecorSmallTree => new Vector3(0.72f, 1.2f, 0.72f),
                PresentationPrefabRole.DecorStoneRuin => new Vector3(0.82f, 0.58f, 0.5f),
                PresentationPrefabRole.Projectile or PresentationPrefabRole.EnemyProjectile => Vector3.one * 0.22f,
                PresentationPrefabRole.EnemySpittingPod => new Vector3(0.78f, 0.58f, 0.78f),
                PresentationPrefabRole.EnemyRat => new Vector3(0.46f, 0.22f, 0.28f),
                PresentationPrefabRole.EnemySpider => new Vector3(0.5f, 0.2f, 0.5f),
                PresentationPrefabRole.EnemyHollowBird => new Vector3(0.48f, 0.28f, 0.58f),
                PresentationPrefabRole.EnemyHollowBeast => new Vector3(0.68f, 0.42f, 0.52f),
                PresentationPrefabRole.EnemySkeletonSword => new Vector3(0.54f, 0.78f, 0.42f),
                PresentationPrefabRole.EnemySkeletonSpear => new Vector3(0.54f, 0.78f, 0.5f),
                PresentationPrefabRole.EnemyKnight => new Vector3(0.68f, 0.98f, 0.52f),
                PresentationPrefabRole.EnemyGiant => new Vector3(1.05f, 1.35f, 0.82f),
                PresentationPrefabRole.EnemyHollowArcher => new Vector3(0.52f, 0.82f, 0.42f),
                PresentationPrefabRole.EnemyPowderGunner => new Vector3(0.62f, 0.86f, 0.5f),
                PresentationPrefabRole.EnemyKnifeThrower => new Vector3(0.5f, 0.72f, 0.42f),
                PresentationPrefabRole.EnemyRepeaterTurret => new Vector3(0.78f, 0.66f, 0.78f),
                PresentationPrefabRole.EnemyClockworkSentry => new Vector3(0.82f, 0.92f, 0.72f),
                PresentationPrefabRole.EnemyHollowAcolyte => new Vector3(0.56f, 0.78f, 0.44f),
                PresentationPrefabRole.EnemyWraith => new Vector3(0.5f, 0.86f, 0.46f),
                PresentationPrefabRole.EnemySoulEater => new Vector3(0.74f, 0.96f, 0.62f),
                PresentationPrefabRole.EnemyCurseBinder => new Vector3(0.58f, 0.82f, 0.48f),
                PresentationPrefabRole.EnemyGraveLantern => new Vector3(0.72f, 0.9f, 0.72f),
                PresentationPrefabRole.EnemyStarforgedOctantSentry => new Vector3(0.92f, 0.86f, 0.92f),
                PresentationPrefabRole.EnemyCrimsonRailSpider => new Vector3(0.9f, 0.72f, 1.05f),
                PresentationPrefabRole.EnemyAzureMinigunTurret => new Vector3(0.94f, 0.78f, 0.94f),
                PresentationPrefabRole.RewardPickup or PresentationPrefabRole.BossKeyPickup or PresentationPrefabRole.ActiveItemPickup => Vector3.one * 0.32f,
                PresentationPrefabRole.HubReturnPortal or PresentationPrefabRole.NextBranchPortal => new Vector3(0.7f, 0.08f, 0.7f),
                PresentationPrefabRole.HubShopCard => new Vector3(1f, 0.7f, 0.08f),
                PresentationPrefabRole.WeaponMelee or PresentationPrefabRole.WeaponRanged or PresentationPrefabRole.Armor => Vector3.one * 0.55f,
                PresentationPrefabRole.ConsumableCardPickup => new Vector3(0.36f, 0.52f, 0.04f),
                PresentationPrefabRole.RoomHazardSpike => new Vector3(0.72f, 0.08f, 0.72f),
                PresentationPrefabRole.StandardBarrel or PresentationPrefabRole.ExplosiveBarrel => new Vector3(0.82f, 1f, 0.82f),
                PresentationPrefabRole.HazardCoinDrop or PresentationPrefabRole.CoinCopper or PresentationPrefabRole.CoinSilver or PresentationPrefabRole.CoinGold => Vector3.one * 0.22f,
                PresentationPrefabRole.ChestNormal or PresentationPrefabRole.ChestGolden => new Vector3(0.75f, 0.5f, 0.6f),
                _ => Vector3.one
            };
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
                PresentationPrefabRole.EnemyHollowAcolyte => MaterialRole.EnemyHollowAcolyte,
                PresentationPrefabRole.EnemyWraith => MaterialRole.EnemyWraith,
                PresentationPrefabRole.EnemySoulEater => MaterialRole.EnemySoulEater,
                PresentationPrefabRole.EnemyCurseBinder => MaterialRole.EnemyCurseBinder,
                PresentationPrefabRole.EnemyGraveLantern => MaterialRole.EnemyGraveLantern,
                PresentationPrefabRole.EnemyStarforgedOctantSentry => MaterialRole.EnemyStarforgedOctantSentry,
                PresentationPrefabRole.EnemyCrimsonRailSpider => MaterialRole.EnemyCrimsonRailSpider,
                PresentationPrefabRole.EnemyAzureMinigunTurret => MaterialRole.EnemyAzureMinigunTurret,
                PresentationPrefabRole.EnemyBoss => MaterialRole.EnemyBoss,
                PresentationPrefabRole.Projectile => MaterialRole.Projectile,
                PresentationPrefabRole.EnemyProjectile => MaterialRole.EnemyProjectile,
                PresentationPrefabRole.RoomFloor => MaterialRole.RoomFloor,
                PresentationPrefabRole.RoomObstacleRock => MaterialRole.RoomObstacleRock,
                PresentationPrefabRole.DecorGrassTuft => MaterialRole.DecorGrassTuft,
                PresentationPrefabRole.DecorCrystalCluster => MaterialRole.DecorCrystalCluster,
                PresentationPrefabRole.DecorSmallTree => MaterialRole.DecorSmallTree,
                PresentationPrefabRole.DecorStoneRuin => MaterialRole.DecorStoneRuin,
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
