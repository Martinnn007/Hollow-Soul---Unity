using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyCatalog : ScriptableObject
    {
        [SerializeField] private List<EnemyDefinition> definitions = new();
        [SerializeField] private EnemyDefinition fallbackDefinition;

        public IReadOnlyList<EnemyDefinition> Definitions => definitions;

        public EnemyDefinition FallbackDefinition => fallbackDefinition != null ? fallbackDefinition : definitions.FirstOrDefault();

        public void Configure(IEnumerable<EnemyDefinition> nextDefinitions, EnemyDefinition nextFallback)
        {
            definitions = nextDefinitions?.Where(definition => definition != null).ToList() ?? new List<EnemyDefinition>();
            fallbackDefinition = nextFallback != null ? nextFallback : definitions.FirstOrDefault();
        }

        public EnemyDefinition Resolve(string spawnKind)
        {
            var definition = definitions.FirstOrDefault(candidate => candidate != null && candidate.SpawnKind == spawnKind);
            return definition != null ? definition : FallbackDefinition;
        }

        public static EnemyCatalog CreateRuntimeDefault()
        {
            var normal = EnemyDefinition.CreateRuntimeNormal();
            var flying = EnemyDefinition.CreateRuntime("spawnEnemyFlying", "Flying Chaser", EnemyArchetypeId.Flying, EnemyMovementMode.Flying, 3, 1.8f, 1, EnemyIntelligenceLevel.Instinctive, EnemyInstinctDisposition.Prey, new Color(0.25f, 0.65f, 1f, 1f));
            var fast = EnemyDefinition.CreateRuntime("spawnEnemyFast", "Fast Chaser", EnemyArchetypeId.Fast, EnemyMovementMode.Grounded, 2, 2.4f, 1, new Color(1f, 0.66f, 0.18f, 1f));
            var heavy = EnemyDefinition.CreateRuntime("spawnEnemyHeavy", "Heavy Chaser", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 6, 0.9f, 2, new Color(0.62f, 0.22f, 0.82f, 1f));
            var boss = EnemyDefinition.CreateRuntimeBoss();
            var charger = EnemyDefinition.CreateRuntime("spawnEnemyCharger", "Ash Charger", EnemyArchetypeId.Fast, EnemyMovementMode.Grounded, 4, 1.2f, 1, new Color(1f, 0.34f, 0.12f, 1f));
            charger.Configure("spawnEnemyCharger", "Ash Charger", EnemyArchetypeId.Fast, EnemyBehaviorId.Charger, EnemyMovementMode.Grounded, 4, 1.2f, 1, 1f, 0.34f, 5.5f, 1.2f, 1, 5f, 5.5f, 2f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Instinctive, EnemyInstinctDisposition.Predator, new Color(1f, 0.34f, 0.12f, 1f));
            var turret = EnemyDefinition.CreateRuntime("spawnEnemyTurret", "Bone Turret", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 4, 0f, 1, new Color(0.72f, 0.86f, 0.94f, 1f));
            turret.Configure("spawnEnemyTurret", "Bone Turret", EnemyArchetypeId.Heavy, EnemyBehaviorId.TurretShooter, EnemyMovementMode.Grounded, 4, 0f, 1, 1f, 0.34f, 8f, 1.35f, 1, 4.8f, 0f, 2f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, new Color(0.72f, 0.86f, 0.94f, 1f));
            var splitter = EnemyDefinition.CreateRuntime("spawnEnemySplitter", "Husk Splitter", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 5, 1.1f, 1, new Color(0.55f, 0.95f, 0.35f, 1f));
            splitter.Configure("spawnEnemySplitter", "Husk Splitter", EnemyArchetypeId.Normal, EnemyBehaviorId.Splitter, EnemyMovementMode.Grounded, 5, 1.1f, 1, 1f, 0.36f, 4f, 1.4f, 1, 4f, 4f, 2f, "spawnEnemyNormal", 2, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Predator, new Color(0.55f, 0.95f, 0.35f, 1f));
            var pod = EnemyDefinition.CreateRuntime("spawnEnemySpittingPod", "Spitting Pod", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 10, 0f, 0, new Color(0.38f, 0.78f, 0.42f, 1f));
            pod.Configure("spawnEnemySpittingPod", "Spitting Pod", EnemyArchetypeId.Heavy, EnemyBehaviorId.SpittingPod, EnemyMovementMode.Grounded, 10, 0f, 0, 1f, 0.44f, 8f, 1f, 1, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Sentinel, 5.5f, 8f, new Color(0.38f, 0.78f, 0.42f, 1f));
            pod.ConfigureSenseAndLunge(0f, 0f, 9f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var rat = EnemyDefinition.CreateRuntime("spawnEnemyRat", "Rat", EnemyArchetypeId.Fast, EnemyMovementMode.Grounded, 3, 2.65f, 1, new Color(0.58f, 0.5f, 0.42f, 1f));
            rat.Configure("spawnEnemyRat", "Rat", EnemyArchetypeId.Fast, EnemyBehaviorId.Rat, EnemyMovementMode.Grounded, 3, 2.65f, 1, 0.85f, 0.2f, 2.2f, 1f, 0, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Light, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Territorial, 1.2f, 2.2f, new Color(0.58f, 0.5f, 0.42f, 1f));
            rat.ConfigureSenseAndLunge(8f, 260f, 7.5f, true, 0.95f, 0.14f, 0.14f, 0.55f, 0.9f);
            var spider = EnemyDefinition.CreateRuntime("spawnEnemySpider", "Spider", EnemyArchetypeId.Fast, EnemyMovementMode.Grounded, 2, 2.9f, 1, new Color(0.16f, 0.12f, 0.2f, 1f));
            spider.Configure("spawnEnemySpider", "Spider", EnemyArchetypeId.Fast, EnemyBehaviorId.Spider, EnemyMovementMode.Grounded, 2, 2.9f, 1, 0.8f, 0.22f, 2.1f, 1f, 0, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Light, EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Prey, 1f, 1.9f, new Color(0.16f, 0.12f, 0.2f, 1f));
            spider.ConfigureSenseAndLunge(8.5f, 300f, 8f, true, 1.15f, 0.12f, 0.16f, 0.7f, 0.85f);
            var hollowBird = EnemyDefinition.CreateRuntime("spawnEnemyHollowBird", "Hollow Bird", EnemyArchetypeId.Flying, EnemyMovementMode.Flying, 3, 2.25f, 1, new Color(0.36f, 0.42f, 0.56f, 1f));
            hollowBird.Configure("spawnEnemyHollowBird", "Hollow Bird", EnemyArchetypeId.Flying, EnemyBehaviorId.HollowBird, EnemyMovementMode.Flying, 3, 2.25f, 1, 0.9f, 0.24f, 1.55f, 1.1f, 0, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Light, EnemyIntelligenceLevel.Simple, EnemyInstinctDisposition.Predator, 1.8f, 3.6f, new Color(0.36f, 0.42f, 0.56f, 1f));
            hollowBird.ConfigureSenseAndLunge(8.2f, 235f, 6.4f, true, 1.55f, 0.18f, 0.15f, 0.95f, 1.1f);
            var hollowBeast = EnemyDefinition.CreateRuntime("spawnEnemyHollowBeast", "Hollow Beast", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 5, 1.9f, 1, new Color(0.28f, 0.24f, 0.2f, 1f));
            hollowBeast.Configure("spawnEnemyHollowBeast", "Hollow Beast", EnemyArchetypeId.Normal, EnemyBehaviorId.HollowBeast, EnemyMovementMode.Grounded, 5, 1.9f, 1, 0.95f, 0.34f, 1.65f, 1.15f, 0, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Predator, 1.15f, 2.1f, new Color(0.28f, 0.24f, 0.2f, 1f));
            hollowBeast.ConfigureSenseAndLunge(7.2f, 165f, 6.2f, true, 1.65f, 0.22f, 0.16f, 0.8f, 1.15f);
            var skeletonSword = EnemyDefinition.CreateRuntime("spawnEnemySkeletonSword", "Skeleton Sword", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 4, 1.55f, 0, new Color(0.73f, 0.68f, 0.58f, 1f));
            skeletonSword.Configure("spawnEnemySkeletonSword", "Skeleton Sword", EnemyArchetypeId.Normal, EnemyBehaviorId.SkeletonSword, EnemyMovementMode.Grounded, 4, 1.55f, 0, 1f, 0.32f, 1.45f, 1.35f, 0, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Predator, 1.15f, 1.85f, new Color(0.73f, 0.68f, 0.58f, 1f));
            skeletonSword.ConfigureSenseAndLunge(6.5f, 160f, 5f, true, 1.45f, 0.28f, 0.14f, 0f, 1.35f);
            var skeletonSpear = EnemyDefinition.CreateRuntime("spawnEnemySkeletonSpear", "Skeleton Spear", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 4, 1.45f, 0, new Color(0.62f, 0.66f, 0.72f, 1f));
            skeletonSpear.Configure("spawnEnemySkeletonSpear", "Skeleton Spear", EnemyArchetypeId.Normal, EnemyBehaviorId.SkeletonSpear, EnemyMovementMode.Grounded, 4, 1.45f, 0, 1f, 0.32f, 2.4f, 1.55f, 0, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Sentinel, 1.75f, 2.75f, new Color(0.62f, 0.66f, 0.72f, 1f));
            skeletonSpear.ConfigureSenseAndLunge(7f, 150f, 5.2f, true, 2.4f, 0.34f, 0.12f, 0.28f, 1.55f);
            var knight = EnemyDefinition.CreateRuntime("spawnEnemyKnight", "Knight", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 8, 1.15f, 0, new Color(0.42f, 0.48f, 0.58f, 1f));
            knight.Configure("spawnEnemyKnight", "Knight", EnemyArchetypeId.Heavy, EnemyBehaviorId.Knight, EnemyMovementMode.Grounded, 8, 1.15f, 0, 1f, 0.38f, 2.15f, 1.6f, 0, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, 1.35f, 2.35f, new Color(0.42f, 0.48f, 0.58f, 1f));
            knight.ConfigureSenseAndLunge(7f, 140f, 5f, true, 2.15f, 0.36f, 0.16f, 0.08f, 1.6f);
            knight.ConfigureGuardProfile(EnemyGuardProfileDefinition.CreateRuntime(EnemyShieldTier.Medium));
            var giant = EnemyDefinition.CreateRuntime("spawnEnemyGiant", "Giant", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 14, 0.75f, 0, new Color(0.48f, 0.39f, 0.32f, 1f));
            giant.Configure("spawnEnemyGiant", "Giant", EnemyArchetypeId.Heavy, EnemyBehaviorId.Giant, EnemyMovementMode.Grounded, 14, 0.75f, 0, 1f, 0.58f, 2.25f, 2.35f, 0, 5f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Massive, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Mindless, 1.85f, 3.1f, new Color(0.48f, 0.39f, 0.32f, 1f));
            giant.ConfigureSenseAndLunge(6f, 115f, 4.5f, true, 2.25f, 0.65f, 0.22f, 0.08f, 2.65f);
            var archer = EnemyDefinition.CreateRuntime("spawnEnemyHollowArcher", "Hollow Archer", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 4, 1.35f, 0, new Color(0.45f, 0.52f, 0.36f, 1f));
            archer.Configure("spawnEnemyHollowArcher", "Hollow Archer", EnemyArchetypeId.Normal, EnemyBehaviorId.HollowArcher, EnemyMovementMode.Grounded, 4, 1.35f, 0, 1f, 0.31f, 7.5f, 1.45f, 1, 6.3f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Sentinel, 4f, 7.25f, new Color(0.45f, 0.52f, 0.36f, 1f));
            archer.ConfigureSenseAndLunge(8.5f, 135f, 5.4f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var gunner = EnemyDefinition.CreateRuntime("spawnEnemyPowderGunner", "Powder Gunner", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 5, 1.05f, 0, new Color(0.34f, 0.38f, 0.42f, 1f));
            gunner.Configure("spawnEnemyPowderGunner", "Powder Gunner", EnemyArchetypeId.Heavy, EnemyBehaviorId.PowderGunner, EnemyMovementMode.Grounded, 5, 1.05f, 0, 1f, 0.36f, 8.8f, 2.2f, 2, 10f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, 4.75f, 8.5f, new Color(0.34f, 0.38f, 0.42f, 1f));
            gunner.ConfigureSenseAndLunge(9f, 115f, 6f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var thrower = EnemyDefinition.CreateRuntime("spawnEnemyKnifeThrower", "Knife Thrower", EnemyArchetypeId.Fast, EnemyMovementMode.Grounded, 4, 1.75f, 0, new Color(0.5f, 0.43f, 0.62f, 1f));
            thrower.Configure("spawnEnemyKnifeThrower", "Knife Thrower", EnemyArchetypeId.Fast, EnemyBehaviorId.KnifeThrower, EnemyMovementMode.Grounded, 4, 1.75f, 0, 1f, 0.28f, 5.8f, 1.1f, 1, 7f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Territorial, 2.7f, 5.25f, new Color(0.5f, 0.43f, 0.62f, 1f));
            thrower.ConfigureSenseAndLunge(8f, 190f, 6.4f, false, 1.05f, 0.18f, 0.14f, 0.45f, 1.1f);
            var repeater = EnemyDefinition.CreateRuntime("spawnEnemyRepeaterTurret", "Repeater Turret", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 6, 0f, 0, new Color(0.46f, 0.6f, 0.64f, 1f));
            repeater.Configure("spawnEnemyRepeaterTurret", "Repeater Turret", EnemyArchetypeId.Heavy, EnemyBehaviorId.RepeaterTurret, EnemyMovementMode.Grounded, 6, 0f, 0, 1f, 0.42f, 9.25f, 1.35f, 1, 6.8f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, 6f, 9.25f, new Color(0.46f, 0.6f, 0.64f, 1f));
            repeater.ConfigureSenseAndLunge(10f, 95f, 3.2f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var clockwork = EnemyDefinition.CreateRuntime("spawnEnemyClockworkSentry", "Clockwork Sentry", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 8, 0.65f, 0, new Color(0.62f, 0.56f, 0.42f, 1f));
            clockwork.Configure("spawnEnemyClockworkSentry", "Clockwork Sentry", EnemyArchetypeId.Heavy, EnemyBehaviorId.ClockworkSentry, EnemyMovementMode.Grounded, 8, 0.65f, 0, 1f, 0.44f, 7.8f, 1.4f, 1, 5.8f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Tactical, EnemyInstinctDisposition.Sentinel, 4.8f, 7.8f, new Color(0.62f, 0.56f, 0.42f, 1f));
            clockwork.ConfigureSenseAndLunge(9f, 220f, 6.5f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var acolyte = EnemyDefinition.CreateRuntime("spawnEnemyHollowAcolyte", "Hollow Acolyte", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 4, 1.05f, 0, new Color(0.36f, 0.32f, 0.72f, 1f));
            acolyte.Configure("spawnEnemyHollowAcolyte", "Hollow Acolyte", EnemyArchetypeId.Normal, EnemyBehaviorId.HollowAcolyte, EnemyMovementMode.Grounded, 4, 1.05f, 0, 1f, 0.31f, 7.2f, 1.5f, 1, 4.8f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Sentinel, 3.8f, 6.8f, new Color(0.36f, 0.32f, 0.72f, 1f));
            acolyte.ConfigureSenseAndLunge(8.4f, 180f, 6.2f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var wraith = EnemyDefinition.CreateRuntime("spawnEnemyWraith", "Wraith", EnemyArchetypeId.Flying, EnemyMovementMode.Flying, 3, 1.75f, 0, new Color(0.66f, 0.88f, 1f, 0.92f));
            wraith.Configure("spawnEnemyWraith", "Wraith", EnemyArchetypeId.Flying, EnemyBehaviorId.Wraith, EnemyMovementMode.Flying, 3, 1.75f, 0, 1f, 0.28f, 6.4f, 1.2f, 1, 5.2f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Light, EnemyIntelligenceLevel.Tactical, EnemyInstinctDisposition.Predator, 2.2f, 5.2f, new Color(0.66f, 0.88f, 1f, 0.92f));
            wraith.ConfigureSenseAndLunge(8.8f, 300f, 7f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var soulEater = EnemyDefinition.CreateRuntime("spawnEnemySoulEater", "Soul Eater", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 7, 1.2f, 0, new Color(0.12f, 0.34f, 0.38f, 1f));
            soulEater.Configure("spawnEnemySoulEater", "Soul Eater", EnemyArchetypeId.Heavy, EnemyBehaviorId.SoulEater, EnemyMovementMode.Grounded, 7, 1.2f, 0, 1f, 0.38f, 5.8f, 1.6f, 1, 4.6f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Trained, EnemyInstinctDisposition.Predator, 2.4f, 4.8f, new Color(0.12f, 0.34f, 0.38f, 1f));
            soulEater.ConfigureSenseAndLunge(7.6f, 170f, 6f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var curseBinder = EnemyDefinition.CreateRuntime("spawnEnemyCurseBinder", "Curse Binder", EnemyArchetypeId.Normal, EnemyMovementMode.Grounded, 5, 0.85f, 0, new Color(0.56f, 0.34f, 0.64f, 1f));
            curseBinder.Configure("spawnEnemyCurseBinder", "Curse Binder", EnemyArchetypeId.Normal, EnemyBehaviorId.CurseBinder, EnemyMovementMode.Grounded, 5, 0.85f, 0, 1f, 0.34f, 7.4f, 1.8f, 1, 4.8f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Medium, EnemyIntelligenceLevel.Tactical, EnemyInstinctDisposition.Territorial, 4f, 7f, new Color(0.56f, 0.34f, 0.64f, 1f));
            curseBinder.ConfigureSenseAndLunge(8.2f, 150f, 5.8f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);
            var lantern = EnemyDefinition.CreateRuntime("spawnEnemyGraveLantern", "Grave Lantern", EnemyArchetypeId.Heavy, EnemyMovementMode.Grounded, 6, 0f, 0, new Color(0.28f, 0.58f, 0.78f, 1f));
            lantern.Configure("spawnEnemyGraveLantern", "Grave Lantern", EnemyArchetypeId.Heavy, EnemyBehaviorId.GraveLantern, EnemyMovementMode.Grounded, 6, 0f, 0, 1f, 0.42f, 8.5f, 1.55f, 1, 4.6f, 0f, 1f, "spawnEnemyNormal", 0, EnemyBodyClass.Heavy, EnemyIntelligenceLevel.Basic, EnemyInstinctDisposition.Sentinel, 5.5f, 8.5f, new Color(0.28f, 0.58f, 0.78f, 1f));
            lantern.ConfigureSenseAndLunge(9.2f, 240f, 7.2f, false, 1.4f, 0.22f, 0.18f, 0.75f, 1.15f);

            var catalog = CreateInstance<EnemyCatalog>();
            catalog.Configure(new[] { normal, flying, fast, heavy, boss, charger, turret, splitter, pod, rat, spider, hollowBird, hollowBeast, skeletonSword, skeletonSpear, knight, giant, archer, gunner, thrower, repeater, clockwork, acolyte, wraith, soulEater, curseBinder, lantern }, normal);
            return catalog;
        }
    }
}
