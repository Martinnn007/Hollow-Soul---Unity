using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone115MechanicalEnemyMeshyIntegrationTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void MechanicalRosterDefinitionsProfilesTreesAndSpawnKindsResolve()
        {
            var catalog = EnemyCatalog.CreateRuntimeDefault();
            foreach (var spec in Milestone115AssetGenerator.EnemyRows())
            {
                var enemy = catalog.Resolve(spec.SpawnKind);
                Assert.NotNull(enemy, spec.SpawnKind);
                Assert.AreEqual(spec.DisplayName, enemy.DisplayName, spec.SpawnKind);
                Assert.AreEqual(spec.ArchetypeId, enemy.ArchetypeId, spec.SpawnKind);
                Assert.AreEqual(spec.BehaviorId, enemy.BehaviorId, spec.SpawnKind);
                Assert.AreEqual(EnemyMovementMode.Grounded, enemy.MovementMode, spec.SpawnKind);
                Assert.AreEqual(spec.MaxHealth, enemy.MaxHealth, spec.SpawnKind);
                Assert.AreEqual(spec.SpeedMetersPerSecond, enemy.SpeedMetersPerSecond, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.RadiusMeters, enemy.RadiusMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.AttackRangeMeters, enemy.AttackRangeMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.BodyClass, enemy.BodyClass, spec.SpawnKind);
                Assert.AreEqual(spec.Intelligence, enemy.Intelligence, spec.SpawnKind);
                Assert.AreEqual(spec.Disposition, enemy.Disposition, spec.SpawnKind);
                Assert.AreEqual(EnemyContactDamagePolicy.ActiveOnly, enemy.ContactDamagePolicy, spec.SpawnKind);
                Assert.AreEqual(EnemyPassiveContactHazardType.None, enemy.PassiveContactHazardType, spec.SpawnKind);
                Assert.AreEqual(spec.PreferredRangeMinMeters, enemy.PreferredRangeMinMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.PreferredRangeMaxMeters, enemy.PreferredRangeMaxMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.SightRadiusMeters, enemy.SightRadiusMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.SightAngleDegrees, enemy.SightAngleDegrees, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.HearingRadiusMeters, enemy.HearingRadiusMeters, 0.001f, spec.SpawnKind);
                Assert.AreEqual(spec.PrefabRole, enemy.PresentationPrefabRole, spec.SpawnKind);
                Assert.AreEqual(spec.MaterialRole, enemy.PresentationMaterialRole, spec.SpawnKind);
                Assert.IsFalse(enemy.LungeAttackEnabled, spec.SpawnKind);
                Assert.NotNull(enemy.BehaviorTree, spec.SpawnKind);
                Assert.Greater(enemy.AttackProfiles.Count, 0, spec.SpawnKind);
                Assert.Greater(enemy.ActionProfiles.Count, 0, spec.SpawnKind);
            }

            Assert.NotNull(catalog.Resolve("spawnEnemyStarforgedOctantSentry").ResolveAttackProfile("octant_clockwise_shot"));
            Assert.NotNull(catalog.Resolve("spawnEnemyCrimsonRailSpider").ResolveAttackProfile("railgun_lock_beam"));
            Assert.NotNull(catalog.Resolve("spawnEnemyAzureMinigunTurret").ResolveAttackProfile("ion_minigun_stream"));
        }

        [Test]
        public void MechanicalProfilesUseExpectedRuntimeKindsAndCadence()
        {
            AssertProfile(
                "spawnEnemyStarforgedOctantSentry",
                "octant_clockwise_shot",
                EnemyAttackRuntimeKind.SequentialRadialProjectile,
                DamageChannel.Physical,
                DamageDelivery.Projectile,
                DamageElement.None,
                1,
                0.5f,
                8,
                5.8f);
            AssertProfile(
                "spawnEnemyCrimsonRailSpider",
                "railgun_lock_beam",
                EnemyAttackRuntimeKind.LockingBeam,
                DamageChannel.Elemental,
                DamageDelivery.Area,
                DamageElement.Energy,
                3,
                3f,
                0,
                0f);
            AssertProfile(
                "spawnEnemyAzureMinigunTurret",
                "ion_minigun_stream",
                EnemyAttackRuntimeKind.Projectile,
                DamageChannel.Elemental,
                DamageDelivery.Projectile,
                DamageElement.Energy,
                1,
                0.2f,
                1,
                9.5f);
        }

        [Test]
        public void StarforgedFiresOneClockwiseOctantPerShot()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyStarforgedOctantSentry");
                var profile = definition.ResolveAttackProfile("octant_clockwise_shot");
                var enemy = CreateEnemy(root.transform, null, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 3.6f);

                var expectedDirections = new[]
                {
                    Vector3.forward,
                    new Vector3(1f, 0f, 1f).normalized,
                    Vector3.right,
                    new Vector3(1f, 0f, -1f).normalized,
                    Vector3.back,
                    new Vector3(-1f, 0f, -1f).normalized,
                    Vector3.left,
                    new Vector3(-1f, 0f, 1f).normalized
                };

                for (var index = 0; index < expectedDirections.Length; index++)
                {
                    var startTime = 2f + index * 0.65f;
                    var projectile = FireSingleRangedProjectile(root, enemy, profile, startTime, "octant_clockwise_shot");
                    Assert.That(projectile.Direction.x, Is.EqualTo(expectedDirections[index].x).Within(0.001f), $"Shot {index}");
                    Assert.That(projectile.Direction.z, Is.EqualTo(expectedDirections[index].z).Within(0.001f), $"Shot {index}");
                }

                Assert.AreEqual(8, root.GetComponentsInChildren<EnemyProjectileController>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AzureMinigunTracksPlayerAndCanFireFiveShotsPerSecond()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyAzureMinigunTurret");
                var profile = definition.ResolveAttackProfile("ion_minigun_stream");
                var enemy = CreateEnemy(root.transform, null, player, definition);
                enemy.transform.localPosition = Vector3.zero;

                player.transform.localPosition = new Vector3(0f, 0f, 3.7f);
                var first = FireSingleRangedProjectile(root, enemy, profile, 2f, "ion_minigun_stream");
                Assert.That(first.Direction.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(first.Direction.z, Is.EqualTo(1f).Within(0.001f));

                player.transform.localPosition = new Vector3(1.4f, 0f, 3.35f);
                var second = FireSingleRangedProjectile(root, enemy, profile, 2.23f, "ion_minigun_stream");
                var expectedSecond = player.transform.localPosition.normalized;
                Assert.That(second.Direction.x, Is.EqualTo(expectedSecond.x).Within(0.001f));
                Assert.That(second.Direction.z, Is.EqualTo(expectedSecond.z).Within(0.001f));

                player.transform.localPosition = new Vector3(-1.3f, 0f, 3.25f);
                FireSingleRangedProjectile(root, enemy, profile, 2.46f, "ion_minigun_stream");
                player.transform.localPosition = new Vector3(0.8f, 0f, 3.55f);
                FireSingleRangedProjectile(root, enemy, profile, 2.69f, "ion_minigun_stream");
                player.transform.localPosition = new Vector3(-0.6f, 0f, 3.75f);
                FireSingleRangedProjectile(root, enemy, profile, 2.92f, "ion_minigun_stream");

                Assert.AreEqual(5, root.GetComponentsInChildren<EnemyProjectileController>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CrimsonRailgunTracksThenLocksAndOnlyDamagesLockedLane()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var health = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyCrimsonRailSpider");
                var profile = definition.ResolveAttackProfile("railgun_lock_beam");
                var enemy = CreateEnemy(root.transform, null, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 6f);

                Assert.IsTrue(StartRanged(enemy, 2f, "railgun_lock_beam"));
                Assert.AreEqual(EnemyRangedTelegraphPhase.Tracking, enemy.RangedTelegraphPhase);
                Assert.IsFalse(enemy.IsRangedTelegraphLocked);

                player.transform.localPosition = new Vector3(3f, 0f, 6f);
                enemy.Tick(0.05f, 2.5f);
                var trackingDirection = new Vector3(3f, 0f, 6f).normalized;
                Assert.AreEqual(EnemyRangedTelegraphPhase.Tracking, enemy.RangedTelegraphPhase);
                Assert.That(enemy.TelegraphDirection.x, Is.EqualTo(trackingDirection.x).Within(0.001f));
                Assert.That(enemy.TelegraphDirection.z, Is.EqualTo(trackingDirection.z).Within(0.001f));

                enemy.Tick(0.05f, 3.05f);
                var lockedDirection = enemy.TelegraphDirection;
                Assert.AreEqual(EnemyRangedTelegraphPhase.Locked, enemy.RangedTelegraphPhase);
                Assert.IsTrue(enemy.IsRangedTelegraphLocked);

                player.transform.localPosition = new Vector3(-3f, 0f, 6f);
                enemy.Tick(0.05f, 3.5f);
                Assert.That(enemy.TelegraphDirection.x, Is.EqualTo(lockedDirection.x).Within(0.001f));
                Assert.That(enemy.TelegraphDirection.z, Is.EqualTo(lockedDirection.z).Within(0.001f));

                enemy.Tick(0.05f, 4.05f);
                Assert.AreEqual(EnemyReadabilityState.RangedActive, enemy.ReadabilityState);
                Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
                Assert.AreEqual(0, root.GetComponentsInChildren<EnemyProjectileController>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CrimsonRailgunDamagesPlayerOnLockedLane()
        {
            var root = CreateHarness(out var room, out var player);
            try
            {
                var health = player.GetComponent<CombatantHealth>();
                var definition = EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyCrimsonRailSpider");
                var profile = definition.ResolveAttackProfile("railgun_lock_beam");
                var enemy = CreateEnemy(root.transform, null, player, definition);
                enemy.transform.localPosition = Vector3.zero;
                player.transform.localPosition = new Vector3(0f, 0f, 6f);

                Assert.IsTrue(StartRanged(enemy, 2f, "railgun_lock_beam"));
                enemy.Tick(0.05f, 3.05f);
                Assert.AreEqual(EnemyRangedTelegraphPhase.Locked, enemy.RangedTelegraphPhase);
                enemy.Tick(0.05f, 4.05f);

                Assert.AreEqual(EnemyReadabilityState.RangedActive, enemy.ReadabilityState);
                Assert.AreEqual(health.MaxHealth - profile.Damage, health.CurrentHealth);
                Assert.AreEqual(0, root.GetComponentsInChildren<EnemyProjectileController>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MeshyPrefabsUseCanonicalEmissiveMaterialsAndRemainVisualOnly()
        {
            foreach (var spec in Milestone115AssetGenerator.EnemyRows())
            {
                var materialPath = $"{Milestone23AssetGenerator.ArtPassMaterialDirectory}/AP_M_{spec.MaterialRole}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                Assert.IsNotNull(material, materialPath);
                AssertTexture(material, "_BaseMap", spec.AlbedoPath);
                AssertTexture(material, "_BumpMap", spec.NormalPath);
                AssertTexture(material, "_MetallicGlossMap", spec.MetallicPath);
                AssertTexture(material, "_EmissionMap", spec.EmissionPath);
                var emissionColor = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                Assert.Greater(Mathf.Max(emissionColor.r, emissionColor.g, emissionColor.b), 1f, spec.MaterialRole.ToString());

                var prefabPath = $"{Milestone23AssetGenerator.ArtPassRoot}/AP_{spec.PrefabRole}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.IsNotNull(prefab, prefabPath);
                Assert.IsNotNull(prefab.transform.Find("MeshyMechanicalModel"), $"{spec.PrefabRole} should contain the Meshy FBX model root.");
                Assert.AreEqual(1, prefab.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == spec.PrefabRole));
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(includeInactive: true).Length, spec.PrefabRole.ToString());
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length, spec.PrefabRole.ToString());

                var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
                Assert.Greater(renderers.Length, 0, spec.PrefabRole.ToString());
                Assert.IsTrue(renderers.All(renderer => renderer.sharedMaterials.Length > 0), spec.PrefabRole.ToString());
                Assert.IsTrue(renderers
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .All(slot => slot != null && AssetDatabase.GetAssetPath(slot) == materialPath), spec.PrefabRole.ToString());
            }
        }

        [Test]
        public void MechanicalRuntimePresentationHidesGameplayCapsuleAndShowsMeshyVisual()
        {
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            Assert.IsNotNull(presentationCatalog, Milestone9AssetGenerator.CatalogPath);
            PresentationContentProvider.Configure(presentationCatalog);

            var root = CreateHarness(out _, out var player);
            try
            {
                foreach (var spec in Milestone115AssetGenerator.EnemyRows())
                {
                    var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    enemyObject.transform.SetParent(root.transform, false);
                    var placeholderRenderer = enemyObject.GetComponent<Renderer>();
                    var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                    var definition = EnemyCatalog.CreateRuntimeDefault().Resolve(spec.SpawnKind);

                    enemy.Configure(null, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());

                    Assert.IsFalse(placeholderRenderer.enabled, $"{spec.SpawnKind} should hide the primitive gameplay capsule renderer.");
                    var marker = enemyObject
                        .GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                        .SingleOrDefault(candidate => candidate.Role == spec.PrefabRole);
                    Assert.IsNotNull(marker, spec.SpawnKind);
                    Assert.IsFalse(marker.IsFallback, spec.SpawnKind);
                    Assert.IsNotNull(marker.transform.Find("MeshyMechanicalModel"), spec.SpawnKind);
                    var visualRenderers = marker.GetComponentsInChildren<Renderer>(includeInactive: true);
                    Assert.Greater(visualRenderers.Length, 0, spec.SpawnKind);
                    Assert.IsTrue(visualRenderers.Any(renderer => renderer.enabled), spec.SpawnKind);

                    Object.DestroyImmediate(enemyObject);
                }
            }
            finally
            {
                PresentationContentProvider.Reset();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CuratedM115RoomsExistAndShowcaseMechanicalEnemies()
        {
            foreach (var roomId in Milestone115AssetGenerator.MechanicalRoomIds)
            {
                var path = $"{Milestone115AssetGenerator.MechanicalRoomDirectory}/{roomId}.hollowruntime.json";
                Assert.IsTrue(File.Exists(path), path);
                var asset = HollowRuntimeV2Importer.Import(File.ReadAllText(path));
                Assert.Greater(asset.Layout.WalkableTiles.Count, 0, roomId);
                Assert.Greater(asset.EnemySpawns.Count(spawn => Milestone115AssetGenerator.SpawnKinds.Contains(spawn.kind)), 0, roomId);
                Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.RoomReward), roomId);
            }
        }

        private static void AssertProfile(
            string ownerId,
            string attackId,
            EnemyAttackRuntimeKind runtimeKind,
            DamageChannel channel,
            DamageDelivery delivery,
            DamageElement element,
            int damage,
            float cooldown,
            int projectileCount,
            float projectileSpeed)
        {
            var spec = EnemyAttackProfileDefaults.AllEnemySpecs.Single(row => row.OwnerId == ownerId && row.AttackId == attackId);
            Assert.AreEqual(runtimeKind, spec.RuntimeKind, attackId);
            Assert.AreEqual(channel, spec.DamageChannel, attackId);
            Assert.AreEqual(delivery, spec.DamageDelivery, attackId);
            Assert.AreEqual(element, spec.DamageElement, attackId);
            Assert.AreEqual(damage, spec.Damage, attackId);
            Assert.AreEqual(cooldown, spec.CooldownSeconds, 0.001f, attackId);
            Assert.AreEqual(projectileCount, spec.ProjectileCount, attackId);
            Assert.AreEqual(projectileSpeed, spec.ProjectileSpeedMetersPerSecond, 0.001f, attackId);
        }

        private static EnemyProjectileController FireSingleRangedProjectile(
            GameObject root,
            EnemyRuntimeController enemy,
            EnemyAttackProfileDefinition profile,
            float startTime,
            string actionId)
        {
            var before = root.GetComponentsInChildren<EnemyProjectileController>().Length;
            Assert.IsTrue(StartRanged(enemy, startTime, actionId), $"{actionId} at {startTime:0.00}");
            var activeTime = startTime + profile.WindupSeconds * enemy.AttackWindupScale + 0.01f;
            enemy.Tick(0.02f, activeTime);
            var projectiles = root.GetComponentsInChildren<EnemyProjectileController>();
            Assert.AreEqual(before + 1, projectiles.Length, $"{actionId} should fire one projectile.");

            enemy.Tick(0.02f, activeTime + profile.ActiveSeconds + 0.01f);
            enemy.Tick(0.02f, activeTime + profile.ActiveSeconds + profile.RecoverySeconds + 0.05f);
            Assert.AreEqual(EnemyReadabilityState.Idle, enemy.ReadabilityState, $"{actionId} should return to idle.");
            return projectiles.Last();
        }

        private static GameObject CreateHarness(out RoomRuntimeRoot room, out PlaceholderPlayerController player)
        {
            var root = new GameObject("M115Harness");
            var roomObject = new GameObject("RoomRuntimeRoot");
            roomObject.transform.SetParent(root.transform, false);
            room = roomObject.AddComponent<RoomRuntimeRoot>();
            room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

            var playerObject = new GameObject("PlayerCharacter");
            playerObject.transform.SetParent(root.transform, false);
            player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);
            playerObject.AddComponent<CombatKnockbackReceiver>().Configure(null, PlaceholderPlayerController.DefaultRadiusMeters, true, 1f);
            return root;
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.SetParent(parent, false);
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, DifficultyTierDefinition.CreateRuntimeDeveloperSample());
            return enemy;
        }

        private static bool StartRanged(EnemyRuntimeController enemy, float timeSeconds, string actionId)
        {
            var method = typeof(EnemyRuntimeController).GetMethod("TryRangedAttack", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(string) }, null);
            Assert.NotNull(method);
            return (bool)method.Invoke(enemy, new object[] { timeSeconds, actionId });
        }

        private static void AssertTexture(Material material, string propertyName, string expectedPath)
        {
            Assert.IsTrue(material.HasProperty(propertyName), $"{material.name} missing {propertyName}");
            var texture = material.GetTexture(propertyName);
            Assert.IsNotNull(texture, $"{material.name} missing texture {propertyName}");
            Assert.AreEqual(expectedPath, AssetDatabase.GetAssetPath(texture), $"{material.name} {propertyName}");
        }
    }
}
