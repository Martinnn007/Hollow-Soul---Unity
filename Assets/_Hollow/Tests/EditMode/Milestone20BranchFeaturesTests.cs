using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using Hollow.Rewards;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone20BranchFeaturesTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void M20GraphHasSecretKeySourceAndBossKeyLock()
        {
            var graph = CreateM20Graph();

            Assert.AreEqual(BranchGenerator.BranchFeaturesId, graph.BranchId);
            Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Secret));
            Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss));
            Assert.AreEqual(2, graph.Connections.Count(connection => connection.LockKind == BranchConnectionLockKind.BossKey));
            Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);

            var boss = graph.Rooms.Single(room => room.Role == BranchRoomRole.Boss);
            Assert.AreEqual(1, graph.ConnectionsFrom(boss.Id).Count);
            var secret = graph.Rooms.Single(room => room.Role == BranchRoomRole.Secret);
            Assert.IsTrue(BranchGenerator.IsSingleRoomFootprint(secret));
            Assert.AreEqual(1, secret.Footprint.OccupiedCellCount);

            var plan = BranchFeaturePlan.Create(graph);
            Assert.IsTrue(plan.HasBossKeyRoom);
            Assert.AreNotEqual(BranchRoomId.Origin.Value, plan.BossKeyRoomId);
            Assert.AreNotEqual(plan.SecretRoomId, plan.BossKeyRoomId);
            Assert.AreNotEqual(plan.BossRoomId, plan.BossKeyRoomId);
        }

        [Test]
        public void M20GraphIsDeterministicForSameSeed()
        {
            var first = CreateM20Graph();
            var second = CreateM20Graph();

            Assert.AreEqual(Signature(first), Signature(second));
        }

        [Test]
        public void SecretRoomsSkipEncounterAndUseTreasureRewardPool()
        {
            var graph = CreateM20Graph();
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);
            var encounterPlan = EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed);
            var secret = graph.Rooms.Single(room => room.Role == BranchRoomRole.Secret);

            Assert.IsFalse(encounterPlan.TryResolve(secret.Id.Value, out _));

            var rewardPlan = ProceduralRewardResolver.CreatePlan(graph);
            Assert.IsTrue(rewardPlan.TryResolve(secret.Id.Value, out var secretReward));
            Assert.AreEqual("treasure_cache", secretReward.RewardId);
        }

        [Test]
        public void MiniMapModelReportsVisibleSecretAndBossKeyLock()
        {
            var graph = CreateM20Graph();
            var state = BranchSessionState.Create(graph);
            foreach (var room in graph.Rooms)
            {
                room.MarkVisited();
            }

            var model = new BranchMiniMapModel(state);

            Assert.IsTrue(model.Nodes.Any(node => node.Role == BranchRoomRole.Secret && node.IsRevealed));
            Assert.IsTrue(model.Connections.Any(connection => connection.LockKind == BranchConnectionLockKind.BossKey));
            Assert.AreEqual(graph.Connections.Count / 2, model.Connections.Count);
        }

        [Test]
        public void HubShopOffersAreSeededAndSpendRunSoulsOnce()
        {
            var offers = InterBranchHubState.Create(20001, 0, null).ShopOffers;
            var economy = new RunEconomy();
            economy.ApplyReward(new RewardGrant("debug_seed_coins", "debug_coins", "Debug Coins", RewardKind.Currency, 0, 40, null));

            var offer = offers.First(candidate => candidate.OfferId == "heal_2");
            Assert.IsTrue(offer.TryPurchase(economy, out _, out var healAmount));
            Assert.AreEqual(2, healAmount);
            Assert.AreEqual(32, economy.RunCoins);
            Assert.IsFalse(offer.TryPurchase(economy, out _, out _));
            Assert.AreEqual(32, economy.RunCoins);
        }

        [Test]
        public void NextBranchChoicesDeriveStableDifferentSeeds()
        {
            var hub = InterBranchHubState.Create(20001, 2, null);

            Assert.AreEqual(3, hub.NextBranchChoices.Count);
            Assert.AreEqual(3, hub.NextBranchChoices.Select(choice => choice.Seed).Distinct().Count());
            Assert.AreEqual(
                string.Join("|", hub.NextBranchChoices.Select(choice => choice.Seed)),
                string.Join("|", InterBranchHubState.Create(20001, 2, null).NextBranchChoices.Select(choice => choice.Seed)));
        }

        [Test]
        public void WorldHubStartsWithThreeOpenBranchPortalsAndNoFourth()
        {
            var hub = InterBranchHubState.CreateWorldHub(12345, 1, 0, null);

            Assert.AreEqual(3, hub.NextBranchChoices.Count);
            Assert.IsTrue(hub.NextBranchChoices.All(choice => choice.Kind == HubPortalKind.Branch));
            Assert.IsTrue(hub.NextBranchChoices.All(choice => choice.State == HubBranchPortalState.Open));
            Assert.IsFalse(hub.NextBranchChoices.Any(choice => choice.Kind == HubPortalKind.NextWorld));
            Assert.AreEqual(
                RunSeedDeriver.ShopSeed(12345, 1, 0),
                RunSeedDeriver.ShopSeed(hub.RunSeed, hub.WorldIndex, hub.ShopRefreshIndex));
        }

        [Test]
        public void DefeatingThreeHubBranchesRevealsNextWorldPortal()
        {
            var hub = InterBranchHubState.CreateWorldHub(12345, 1, 0, null);
            foreach (var choice in hub.NextBranchChoices.Where(choice => choice.Kind == HubPortalKind.Branch).ToArray())
            {
                hub = hub.MarkBranchPortalDefeated(choice.ChoiceId, null);
            }

            Assert.AreEqual(4, hub.NextBranchChoices.Count);
            Assert.IsTrue(hub.AreAllBranchPortalsDefeated);
            Assert.AreEqual(3, hub.NextBranchChoices.Count(choice => choice.Kind == HubPortalKind.Branch && choice.State == HubBranchPortalState.Defeated));
            var nextWorld = hub.NextBranchChoices.Single(choice => choice.Kind == HubPortalKind.NextWorld);
            Assert.AreEqual(2, nextWorld.WorldIndex);
            Assert.AreEqual(RunSeedDeriver.PrologueBranchSeed(12345, 2), nextWorld.Seed);
        }

        [Test]
        public void WorldThreeDefeatedHubBranchesRevealFinalExtraction()
        {
            var hub = InterBranchHubState.CreateWorldHub(12345, 3, 0, null);
            foreach (var choice in hub.NextBranchChoices.Where(choice => choice.Kind == HubPortalKind.Branch).ToArray())
            {
                hub = hub.MarkBranchPortalDefeated(choice.ChoiceId, null);
            }

            Assert.AreEqual(1, hub.NextBranchChoices.Count(choice => choice.Kind == HubPortalKind.FinalExtraction));
            Assert.IsFalse(hub.NextBranchChoices.Any(choice => choice.Kind == HubPortalKind.NextWorld));
        }

        [Test]
        public void HubPortalStatePersistsDefeatedAndPurchasedShop()
        {
            var hub = InterBranchHubState.CreateWorldHub(12345, 1, 0, null);
            var economy = new RunEconomy();
            economy.ApplyReward(new RewardGrant("debug_seed_coins", "debug_coins", "Debug Coins", RewardKind.Currency, 0, 40, null));
            Assert.IsTrue(hub.ShopOffers.First(offer => offer.OfferId == "heal_2").TryPurchase(economy, out _, out _));
            hub = hub.MarkBranchPortalDefeated(hub.NextBranchChoices[1].ChoiceId, null);

            var restored = InterBranchHubState.FromSaveState(hub.ToSaveState(), hub.RunSeed, 0, null);

            Assert.AreEqual(1, restored.NextBranchChoices.Count(choice => choice.State == HubBranchPortalState.Defeated));
            Assert.AreEqual(HubBranchPortalState.Defeated, restored.NextBranchChoices[1].State);
            Assert.AreEqual(hub.ShopRefreshIndex, restored.ShopRefreshIndex);
        }

        [Test]
        public void BossKeyPickupPresentationAnimatesAndPoolReturnRestoresBasePose()
        {
            var root = new GameObject("BossKeyAnimationRoot");
            try
            {
                root.transform.localPosition = new Vector3(0.15f, 0.45f, -0.2f);
                root.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                var pickup = root.AddComponent<BossKeyPickup>();

                pickup.Configure("secret_room");
                var basePosition = root.transform.localPosition;
                var baseRotation = root.transform.localRotation;

                pickup.TickPresentation(0.25f);

                Assert.AreEqual(basePosition.x, root.transform.localPosition.x, 0.001f);
                Assert.AreEqual(basePosition.z, root.transform.localPosition.z, 0.001f);
                Assert.Greater(Mathf.Abs(root.transform.localPosition.y - basePosition.y), 0.01f);
                Assert.LessOrEqual(Mathf.Abs(root.transform.localPosition.y - basePosition.y), BossKeyPickup.DefaultHoverAmplitudeMeters + 0.001f);
                Assert.AreEqual(6f, Quaternion.Angle(baseRotation, root.transform.localRotation), 0.1f);
                Assert.AreEqual(0f, Quaternion.Angle(baseRotation * Quaternion.Euler(0f, 6f, 0f), root.transform.localRotation), 0.001f);

                pickup.OnReturnToPool();

                Assert.AreEqual(string.Empty, pickup.RoomId);
                Assert.IsFalse(pickup.Claimed);
                Assert.AreEqual(basePosition, root.transform.localPosition);
                Assert.AreEqual(0f, Quaternion.Angle(baseRotation, root.transform.localRotation), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BossKeyGameplayPrefabIsInvisibleHost()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Milestone20AssetGenerator.BossKeyPickupPrefabPath);

            Assert.IsNotNull(prefab, "Run boss key Meshy generation before validating the M20 gameplay host.");
            Assert.IsNotNull(prefab.GetComponent<BossKeyPickup>());
            Assert.AreEqual(0, prefab.GetComponentsInChildren<Renderer>(includeInactive: true).Length);
            Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(includeInactive: true).Length);
        }

        [Test]
        public void BossKeyArtPassPrefabResolvesToMeshyVisualOnlyAsset()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossKeyMeshyAssetGenerator.ArtPassBossKeyPrefabPath);

            Assert.IsNotNull(prefab, "Run boss key Meshy generation before validating the ArtPass visual.");
            var errors = ArtPassProductionValidator.ValidatePrefabSafetyForTests(prefab, PresentationPrefabRole.BossKeyPickup);
            Assert.IsEmpty(errors, string.Join("; ", errors));

            var renderers = prefab.GetComponentsInChildren<Renderer>(includeInactive: true);
            Assert.Greater(renderers.Length, 0);
            Assert.AreEqual(200f, prefab.transform.GetChild(0).localScale.x, 0.01f, "Boss key ArtPass model should stay at the requested Meshy scale.");
            Assert.IsTrue(renderers
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Any(material => AssetDatabase.GetAssetPath(material).StartsWith("Assets/MeshyImports/Meshy_Model_20260602_223831/", System.StringComparison.Ordinal)),
                "Boss key ArtPass renderers should use the newly imported Meshy material.");

            var markerScripts = prefab.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            Assert.IsTrue(markerScripts.All(component => component is PresentationVisualMarker));

            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGetPrefab(PresentationPrefabRole.BossKeyPickup, out var boundPrefab));
            Assert.AreEqual(BossKeyMeshyAssetGenerator.ArtPassBossKeyPrefabPath, AssetDatabase.GetAssetPath(boundPrefab));
        }

        [Test]
        public void BossKeyHudSpriteUsesProvidedResourceIcon()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Hollow/Resources/UI/Hud/BossKeyIcon.png");

            Assert.IsNotNull(sprite);
            Assert.AreEqual("BossKeyIcon", sprite.name);
        }

        private static BranchFloorGraph CreateM20Graph()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return BranchGenerator.CreateSeededBranchFeatures(content, settings, settings.DefaultSeed);
        }

        private static string Signature(BranchFloorGraph graph)
        {
            var rooms = string.Join("|", graph.Rooms
                .OrderBy(room => room.Id.Value)
                .Select(room => $"{room.Id}:{room.Role}:{string.Join(",", room.Footprint.OccupiedCells.OrderBy(cell => cell.x).ThenBy(cell => cell.y).Select(cell => $"{cell.x},{cell.y}"))}"));
            var connections = string.Join("|", graph.Connections
                .OrderBy(connection => connection.FromRoomId.Value)
                .ThenBy(connection => connection.FromPortId)
                .Select(connection => $"{connection.FromRoomId}:{connection.FromPortId}->{connection.ToRoomId}:{connection.ToPortId}:{connection.LockKind}"));
            return rooms + "::" + connections;
        }
    }
}
