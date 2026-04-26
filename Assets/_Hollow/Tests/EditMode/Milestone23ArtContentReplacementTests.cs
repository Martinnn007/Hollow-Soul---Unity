using System;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone23ArtContentReplacementTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            PresentationContentProvider.Configure(catalog);
        }

        [TearDown]
        public void TearDown()
        {
            PresentationContentProvider.Reset();
        }

        [Test]
        public void PresentationPrefabResolverResolvesEveryCoreRole()
        {
            foreach (PresentationPrefabRole role in Enum.GetValues(typeof(PresentationPrefabRole)))
            {
                Assert.IsNotNull(PresentationPrefabResolver.Resolve(role), $"Missing prefab or fallback for {role}");
            }
        }

        [Test]
        public void ResolverInstantiatesVisualOnlyChildren()
        {
            var parent = new GameObject("M23VisualParent");
            try
            {
                var visual = PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Player, parent.transform, Vector3.zero, Vector3.one);

                Assert.IsNotNull(visual);
                Assert.AreEqual(parent.transform, visual.transform.parent);
                Assert.IsNotNull(visual.GetComponent<PresentationVisualMarker>());
                Assert.AreEqual(0, visual.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void RoomRuntimeRootAttachesArtPassVisualsWithoutReplacingGameplayColliders()
        {
            var rootObject = new GameObject("M23RoomRuntimeRoot");
            try
            {
                var root = rootObject.AddComponent<RoomRuntimeRoot>();
                root.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

                var floor = rootObject.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(transform => transform.name.StartsWith("tileGround.", StringComparison.Ordinal));
                var door = rootObject.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(transform => transform.name.StartsWith("doorAnchorActive.", StringComparison.Ordinal));
                var rock = rootObject.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .FirstOrDefault(marker => marker.Role == PresentationPrefabRole.RoomObstacleRock)
                    ?.transform.parent;

                Assert.IsNotNull(floor);
                Assert.IsNotNull(rock);
                Assert.IsNotNull(door);
                Assert.IsNotNull(floor.GetComponent<BoxCollider>(), "Gameplay floor collider should remain on authoritative runtime object.");
                Assert.IsNotNull(rock.GetComponent<BoxCollider>(), "Gameplay obstacle collider should remain on authoritative runtime object.");
                AssertVisualChild(floor, PresentationPrefabRole.RoomFloor);
                AssertVisualChild(rock, PresentationPrefabRole.RoomObstacleRock);
                AssertVisualChild(door, PresentationPrefabRole.DoorActive);
                Assert.AreEqual(0, floor.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .SelectMany(marker => marker.GetComponentsInChildren<Collider>(includeInactive: true))
                    .Count());
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void CombatObjectsAttachVisualChildrenWithoutChangingControllers()
        {
            var root = new GameObject("M23CombatHarness");
            try
            {
                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                var room = roomObject.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)));

                var playerObject = new GameObject("PlayerCharacter");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                var playerHealth = playerObject.AddComponent<CombatantHealth>();
                playerHealth.Configure(RoomCombatController.PlayerMaxHealth);

                var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObject.name = "EnemyRuntime";
                enemyObject.transform.SetParent(root.transform, false);
                var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(room, player, EnemyDefinition.CreateRuntimeNormal(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());

                var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileObject.name = "ProjectileRuntime";
                projectileObject.transform.SetParent(root.transform, false);
                var projectile = projectileObject.AddComponent<ProjectileController>();
                var combat = root.AddComponent<RoomCombatController>();
                projectile.Configure(room, combat, Vector3.forward);

                var enemyProjectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                enemyProjectileObject.name = "EnemyProjectileRuntime";
                enemyProjectileObject.transform.SetParent(root.transform, false);
                var enemyProjectile = enemyProjectileObject.AddComponent<EnemyProjectileController>();
                enemyProjectile.Configure(room, player, Vector3.forward, 1, 4f);

                PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Player, player.transform, Vector3.zero, Vector3.one);

                Assert.IsNotNull(enemy.GetComponent<EnemyRuntimeController>());
                Assert.IsNotNull(projectile.GetComponent<ProjectileController>());
                Assert.IsNotNull(enemyProjectile.GetComponent<EnemyProjectileController>());
                AssertVisualChild(player.transform, PresentationPrefabRole.Player);
                AssertVisualChild(enemy.transform, PresentationPrefabRole.EnemyNormal);
                AssertVisualChild(projectile.transform, PresentationPrefabRole.Projectile);
                AssertVisualChild(enemyProjectile.transform, PresentationPrefabRole.EnemyProjectile);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RewardShopKeyAndPortalRolesHaveVisualOnlyPrefabs()
        {
            var parent = new GameObject("M23Interactables");
            try
            {
                foreach (var role in new[]
                {
                    PresentationPrefabRole.RewardPickup,
                    PresentationPrefabRole.BossKeyPickup,
                    PresentationPrefabRole.HubShop,
                    PresentationPrefabRole.HubReturnPortal,
                    PresentationPrefabRole.NextBranchPortal
                })
                {
                    var visual = PresentationPrefabResolver.InstantiateVisual(role, parent.transform, Vector3.zero, Vector3.one);
                    Assert.IsNotNull(visual, $"Missing visual for {role}");
                    Assert.AreEqual(0, visual.GetComponentsInChildren<Collider>(includeInactive: true).Length, $"{role} visual should not carry colliders.");
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ArtPassContentValidatorReportsGeneratedContentValid()
        {
            var report = ArtPassContentValidator.ValidateAll();
            Assert.IsTrue(report.IsValid, string.Join("\n", report.Failures));
        }

        private static void AssertVisualChild(Transform parent, PresentationPrefabRole role)
        {
            Assert.IsTrue(
                parent.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Any(marker => marker.Role == role),
                $"Expected {parent.name} to contain ArtPass visual role {role}.");
        }
    }
}
