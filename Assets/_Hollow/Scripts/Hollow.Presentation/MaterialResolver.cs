using System.Collections.Generic;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public static class MaterialResolver
    {
        private static readonly Dictionary<MaterialRole, Material> FallbackMaterials = new();

        public static Material Resolve(MaterialRole role)
        {
            var palette = PresentationContentProvider.ActiveCatalog != null
                ? PresentationContentProvider.ActiveCatalog.MaterialPalette
                : null;
            if (palette != null && palette.TryResolve(role, out var material) && material != null)
            {
                return material;
            }

            if (!FallbackMaterials.TryGetValue(role, out var fallback) || fallback == null)
            {
                fallback = CreateRuntimeMaterial(FallbackColorFor(role), IsDoubleSidedFallback(role));
                fallback.name = $"Fallback_{role}";
                FallbackMaterials[role] = fallback;
            }

            return fallback;
        }

        public static void ApplyTo(GameObject target, MaterialRole role)
        {
            if (target == null)
            {
                return;
            }

            ApplyTo(target.GetComponentInChildren<Renderer>(), role);
        }

        public static void ApplyTo(Renderer renderer, MaterialRole role)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = Resolve(role);
        }

        public static Material CreateRuntimeMaterial(Color color)
        {
            return CreateRuntimeMaterial(color, doubleSided: false);
        }

        private static Material CreateRuntimeMaterial(Color color, bool doubleSided)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = color
            };
            if (doubleSided)
            {
                SetFloat(material, "_Cull", 0f);
            }

            ConfigureSurfaceForAlpha(material, color.a);
            return material;
        }

        public static Color FallbackColorFor(MaterialRole role)
        {
            var palette = PresentationContentProvider.ActiveCatalog != null
                ? PresentationContentProvider.ActiveCatalog.MaterialPalette
                : null;
            if (palette != null && palette.TryGetFallbackColor(role, out var color))
            {
                return color;
            }

            return role switch
            {
                MaterialRole.RoomFloor => new Color(0.22f, 0.29f, 0.34f, 1f),
                MaterialRole.RoomWall => new Color(0.28f, 0.31f, 0.34f, 1f),
                MaterialRole.RoomWallTransparent => new Color(0.28f, 0.31f, 0.34f, 0.32f),
                MaterialRole.DecorGrassTuft => new Color(0.22f, 0.55f, 0.24f, 1f),
                MaterialRole.DecorCrystalCluster => new Color(0.34f, 0.9f, 0.78f, 1f),
                MaterialRole.DecorSmallTree => new Color(0.18f, 0.42f, 0.22f, 1f),
                MaterialRole.DecorStoneRuin => new Color(0.4f, 0.42f, 0.38f, 1f),
                MaterialRole.RoomOriginMarker => new Color(0.1f, 0.8f, 1f, 1f),
                MaterialRole.RoomObstacleRock => new Color(0.36f, 0.34f, 0.31f, 1f),
                MaterialRole.DoorLocked => new Color(0.82f, 0.28f, 0.18f, 1f),
                MaterialRole.DoorActive => new Color(0.12f, 0.62f, 1f, 1f),
                MaterialRole.DoorCleared => new Color(0.25f, 1f, 0.45f, 1f),
                MaterialRole.DoorUnavailable => new Color(0.34f, 0.38f, 0.44f, 0.9f),
                MaterialRole.SpawnSafeStart => new Color(0.36f, 1f, 0.54f, 1f),
                MaterialRole.SpawnEnemy => new Color(1f, 0.25f, 0.22f, 1f),
                MaterialRole.SpawnReward => new Color(1f, 0.82f, 0.18f, 1f),
                MaterialRole.PlayerBody => new Color(0.36f, 0.92f, 0.72f, 1f),
                MaterialRole.Projectile => new Color(0.9f, 0.95f, 1f, 1f),
                MaterialRole.ProjectilePower => new Color(1f, 0.12f, 0.08f, 1f),
                MaterialRole.EnemyNormal => new Color(0.85f, 0.16f, 0.14f, 1f),
                MaterialRole.EnemyFlying => new Color(0.25f, 0.65f, 1f, 1f),
                MaterialRole.EnemyFast => new Color(1f, 0.66f, 0.18f, 1f),
                MaterialRole.EnemyHeavy => new Color(0.62f, 0.22f, 0.82f, 1f),
                MaterialRole.EnemyCharger => new Color(1f, 0.34f, 0.12f, 1f),
                MaterialRole.EnemyTurret => new Color(0.72f, 0.86f, 0.94f, 1f),
                MaterialRole.EnemySplitter => new Color(0.55f, 0.95f, 0.35f, 1f),
                MaterialRole.EnemySpittingPod => new Color(0.38f, 0.78f, 0.42f, 1f),
                MaterialRole.EnemyRat => new Color(0.58f, 0.5f, 0.42f, 1f),
                MaterialRole.EnemySpider => new Color(0.16f, 0.12f, 0.2f, 1f),
                MaterialRole.EnemyHollowBird => new Color(0.36f, 0.42f, 0.56f, 1f),
                MaterialRole.EnemyHollowBeast => new Color(0.28f, 0.24f, 0.2f, 1f),
                MaterialRole.EnemySkeletonSword => new Color(0.73f, 0.68f, 0.58f, 1f),
                MaterialRole.EnemySkeletonSpear => new Color(0.62f, 0.66f, 0.72f, 1f),
                MaterialRole.EnemyKnight => new Color(0.42f, 0.48f, 0.58f, 1f),
                MaterialRole.EnemyGiant => new Color(0.48f, 0.39f, 0.32f, 1f),
                MaterialRole.EnemyHollowArcher => new Color(0.45f, 0.52f, 0.36f, 1f),
                MaterialRole.EnemyPowderGunner => new Color(0.34f, 0.38f, 0.42f, 1f),
                MaterialRole.EnemyKnifeThrower => new Color(0.5f, 0.43f, 0.62f, 1f),
                MaterialRole.EnemyRepeaterTurret => new Color(0.46f, 0.6f, 0.64f, 1f),
                MaterialRole.EnemyClockworkSentry => new Color(0.62f, 0.56f, 0.42f, 1f),
                MaterialRole.EnemyHollowAcolyte => new Color(0.36f, 0.32f, 0.72f, 1f),
                MaterialRole.EnemyWraith => new Color(0.66f, 0.88f, 1f, 0.92f),
                MaterialRole.EnemySoulEater => new Color(0.12f, 0.34f, 0.38f, 1f),
                MaterialRole.EnemyCurseBinder => new Color(0.56f, 0.34f, 0.64f, 1f),
                MaterialRole.EnemyGraveLantern => new Color(0.28f, 0.58f, 0.78f, 1f),
                MaterialRole.EnemyStarforgedOctantSentry => new Color(0.72f, 0.62f, 0.44f, 1f),
                MaterialRole.EnemyCrimsonRailSpider => new Color(0.62f, 0.22f, 0.2f, 1f),
                MaterialRole.EnemyAzureMinigunTurret => new Color(0.24f, 0.66f, 0.9f, 1f),
                MaterialRole.EnemyBoss => new Color(0.42f, 0.34f, 0.28f, 1f),
                MaterialRole.EnemyProjectile => new Color(1f, 0.36f, 0.24f, 1f),
                MaterialRole.CombatHitFlash => Color.white,
                MaterialRole.CombatCorpseGhost => new Color(0.64f, 0.74f, 0.78f, 0.42f),
                MaterialRole.ShieldGuard => new Color(0.22f, 0.78f, 1f, 0.48f),
                MaterialRole.ShieldParry => new Color(0.45f, 1f, 0.72f, 0.82f),
                MaterialRole.ShieldBlock => new Color(0.78f, 0.92f, 1f, 0.72f),
                MaterialRole.ShieldUnavailable => new Color(0.95f, 0.28f, 0.2f, 0.68f),
                MaterialRole.CombatTelegraphSafe => new Color(0.28f, 1f, 0.72f, 0.72f),
                MaterialRole.CombatTelegraphTracking => new Color(0.16f, 0.62f, 1f, 0.82f),
                MaterialRole.CombatTelegraphLocked => new Color(1f, 0.68f, 0.08f, 0.9f),
                MaterialRole.CombatTelegraphWarning => new Color(1f, 0.72f, 0.18f, 0.82f),
                MaterialRole.CombatTelegraphDanger => new Color(1f, 0.12f, 0.08f, 0.9f),
                MaterialRole.RoomHazardSpike => new Color(0.9f, 0.08f, 0.06f, 1f),
                MaterialRole.RoomBarrel => new Color(0.58f, 0.35f, 0.16f, 1f),
                MaterialRole.RoomExplosiveBarrel => new Color(1f, 0.38f, 0.08f, 1f),
                MaterialRole.HazardCoinDrop => new Color(1f, 0.78f, 0.12f, 1f),
                MaterialRole.ChestNormal => new Color(0.54f, 0.31f, 0.13f, 1f),
                MaterialRole.ChestGolden => new Color(1f, 0.76f, 0.18f, 1f),
                MaterialRole.CoinCopper => new Color(0.82f, 0.42f, 0.2f, 1f),
                MaterialRole.CoinSilver => new Color(0.78f, 0.82f, 0.86f, 1f),
                MaterialRole.CoinGold => new Color(1f, 0.82f, 0.18f, 1f),
                MaterialRole.RewardPickup => new Color(1f, 0.82f, 0.18f, 1f),
                MaterialRole.HubReturnPortal => new Color(0.25f, 1f, 0.92f, 1f),
                MaterialRole.BossKeyPickup => new Color(1f, 0.88f, 0.22f, 1f),
                MaterialRole.HubShop => new Color(0.35f, 0.78f, 1f, 1f),
                MaterialRole.NextBranchPortal => new Color(0.45f, 0.32f, 1f, 1f),
                MaterialRole.SecretDoorDebug => new Color(1f, 0.24f, 0.95f, 1f),
                MaterialRole.DesignerGrid => new Color(0.85f, 0.9f, 1f, 0.65f),
                MaterialRole.DesignerCursor => new Color(1f, 0.9f, 0.15f, 1f),
                MaterialRole.DesignerGround => new Color(0.23f, 0.32f, 0.38f, 1f),
                MaterialRole.DesignerHole => Color.black,
                MaterialRole.DesignerRock => new Color(0.42f, 0.39f, 0.34f, 1f),
                MaterialRole.DesignerDoorAvailable => new Color(0.6f, 0.68f, 0.78f, 0.8f),
                MaterialRole.DesignerDoorActive => new Color(0.1f, 0.55f, 1f, 1f),
                MaterialRole.DesignerDoorSecret => new Color(0.9f, 0.25f, 1f, 1f),
                MaterialRole.DesignerSpawnSafeStart => new Color(0.32f, 1f, 0.56f, 1f),
                MaterialRole.DesignerSpawnEnemy => new Color(1f, 0.2f, 0.18f, 1f),
                MaterialRole.DesignerSpawnReward => new Color(1f, 0.82f, 0.18f, 1f),
                MaterialRole.DesignerSpike => new Color(1f, 0.18f, 0.12f, 0.92f),
                MaterialRole.DesignerBarrel => new Color(0.62f, 0.38f, 0.18f, 1f),
                MaterialRole.DesignerExplosiveBarrel => new Color(1f, 0.44f, 0.08f, 1f),
                MaterialRole.DesignerChest => new Color(0.86f, 0.58f, 0.22f, 1f),
                MaterialRole.VfxDebug => new Color(1f, 1f, 1f, 0.85f),
                _ => Color.white
            };
        }

        internal static void ClearCache()
        {
            FallbackMaterials.Clear();
            PresentationPrefabResolver.ClearCache();
        }

        private static bool IsDoubleSidedFallback(MaterialRole role)
        {
            return role is MaterialRole.RoomWall or MaterialRole.RoomWallTransparent;
        }

        private static void ConfigureSurfaceForAlpha(Material material, float alpha)
        {
            if (material == null || alpha >= 0.999f)
            {
                return;
            }

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
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
