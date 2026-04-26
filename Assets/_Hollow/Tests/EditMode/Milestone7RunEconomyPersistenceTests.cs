using System.IO;
using Hollow.Branches;
using Hollow.Core;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Rewards;
using Hollow.World;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone7RunEconomyPersistenceTests
    {
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Application.temporaryCachePath, "hollow_m7_tests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrWhiteSpace(tempRoot) && Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void RewardResolverReturnsDeterministicM7Rewards()
        {
            AssertReward("north", "stone_heart", "Stone Heart");
            AssertReward("south", "quick_draw", "Quick Draw");
            AssertReward("east", "fleet_step", "Fleet Step");
            AssertReward("west", "ember_charm", "Ember Charm");
        }

        [Test]
        public void RunEconomyAppliesRewardOnceAndStatsApplyExpectedEffects()
        {
            var economy = new RunEconomy();
            var stats = new PlayerRunStats();

            Assert.IsTrue(economy.ApplyReward(RewardResolver.Resolve("north")));
            Assert.IsFalse(economy.ApplyReward(RewardResolver.Resolve("north")));
            var heal = stats.ApplyReward(RewardResolver.Resolve("north"));
            stats.ApplyReward(RewardResolver.Resolve("south"));
            stats.ApplyReward(RewardResolver.Resolve("east"));
            stats.ApplyReward(RewardResolver.Resolve("west"));

            Assert.AreEqual(10, economy.RunSouls);
            Assert.AreEqual(1, economy.CollectedRewards.Count);
            Assert.AreEqual(1, heal);
            Assert.AreEqual(1, stats.MaxHealthBonus);
            Assert.AreEqual(0.5f, stats.MoveSpeedBonus, 0.0001f);
            Assert.AreEqual(0.9f, stats.ShotCooldownMultiplier, 0.0001f);
            Assert.AreEqual(1, stats.ProjectileDamageBonus);
        }

        [Test]
        public void JsonProfileStoreMigratesSchemaOneToSchemaTwoDefaults()
        {
            File.WriteAllText(Path.Combine(tempRoot, "hollow_profiles.json"), "{\"schemaVersion\":1,\"slots\":[{\"slotIndex\":0,\"profileId\":\"abc\",\"displayName\":\"Old Save\",\"createdAtUtcTicks\":1,\"lastPlayedUtcTicks\":2,\"totalRuns\":3,\"hasActiveRun\":true}]}");

            var summaries = new JsonProfileStore(tempRoot).LoadSlotSummaries();

            Assert.AreEqual(3, summaries.Count);
            Assert.AreEqual("Old Save", summaries[0].DisplayName);
            Assert.AreEqual(0, summaries[0].BankedSouls);
            Assert.AreEqual(0, summaries[0].CompletedRuns);
            Assert.IsFalse(summaries[0].HasActiveRun);
        }

        [Test]
        public void NewRunAndCheckpointPersistActiveRunSnapshot()
        {
            var store = new JsonProfileStore(tempRoot);
            var slotId = new ProfileSlotId(0);
            store.CreateOrLoadProfile(slotId, "Runner");

            var started = store.MarkRunStarted(slotId);
            store.SaveActiveRun(slotId, CreateSnapshot("east", 4));

            Assert.AreEqual(1, started.TotalRuns);
            Assert.IsTrue(store.TryLoadActiveRun(slotId, out var snapshot));
            Assert.AreEqual("east", snapshot.currentRoomId);
            Assert.AreEqual(4, snapshot.playerCurrentHealth);
            Assert.AreEqual(40, snapshot.economy.runSouls);
            Assert.AreEqual(1, snapshot.playerStats.projectileDamageBonus);
            Assert.IsTrue(store.LoadSlotSummaries()[0].HasActiveRun);
        }

        [Test]
        public void CompleteActiveRunBanksSoulsAndClearsSnapshot()
        {
            var store = new JsonProfileStore(tempRoot);
            var slotId = new ProfileSlotId(1);
            store.CreateOrLoadProfile(slotId, "Closer");
            store.MarkRunStarted(slotId);
            store.SaveActiveRun(slotId, CreateSnapshot("west", 5));

            store.CompleteActiveRun(slotId, new RunCompletionSummary { soulsToBank = 40, rewardsClaimed = 4 });
            var summary = store.LoadSlotSummaries()[1];

            Assert.AreEqual(40, summary.BankedSouls);
            Assert.AreEqual(1, summary.CompletedRuns);
            Assert.IsFalse(summary.HasActiveRun);
            Assert.IsFalse(store.TryLoadActiveRun(slotId, out _));
        }

        [Test]
        public void DeathClearActiveRunDoesNotBankSouls()
        {
            var store = new JsonProfileStore(tempRoot);
            var slotId = new ProfileSlotId(2);
            store.CreateOrLoadProfile(slotId, "Fallen");
            store.MarkRunStarted(slotId);
            store.SaveActiveRun(slotId, CreateSnapshot("south", 1));

            store.ClearActiveRun(slotId);
            var summary = store.LoadSlotSummaries()[2];

            Assert.IsFalse(summary.HasActiveRun);
            Assert.AreEqual(0, summary.BankedSouls);
            Assert.AreEqual(0, summary.CompletedRuns);
        }

        [Test]
        public void TransientGuardBlocksNonProfileBackedSessions()
        {
            Assert.IsTrue(TransientSessionGuard.CanPersist(RuntimeSessionMode.ProfileBacked, hasProfile: true));
            Assert.IsFalse(TransientSessionGuard.CanPersist(RuntimeSessionMode.ProfileBacked, hasProfile: false));
            Assert.IsFalse(TransientSessionGuard.CanPersist(RuntimeSessionMode.TransientDeveloperSample, hasProfile: true));
            Assert.IsFalse(TransientSessionGuard.CanPersist(RuntimeSessionMode.TransientRoomDesignerPlaytest, hasProfile: true));
        }

        [Test]
        public void GameSessionStateCarriesRunLaunchModeAndProfileSlot()
        {
            var summary = new ProfileSlotSummary(1, "profile", "Runner", 1, 2, 3, true, 40, 1);
            var state = GameSessionState.Create(RuntimeSessionMode.ProfileBacked, HollowPlatformKind.WindowsStandard3D, RunLaunchMode.ContinueRun, summary, Vector3.zero);

            Assert.AreEqual(RunLaunchMode.ContinueRun, state.LaunchMode);
            Assert.AreEqual(1, state.ProfileSlotIndex);
            Assert.IsTrue(state.HasProfile);
        }

        private static void AssertReward(string roomId, string rewardId, string displayName)
        {
            var grant = RewardResolver.Resolve(roomId);
            Assert.AreEqual(rewardId, grant.RewardId);
            Assert.AreEqual(displayName, grant.DisplayName);
            Assert.AreEqual(10, grant.Souls);
        }

        private static RunSaveSnapshot CreateSnapshot(string currentRoomId, int playerHealth)
        {
            return new RunSaveSnapshot
            {
                runId = "test-run",
                currentRoomId = currentRoomId,
                playerCurrentHealth = playerHealth,
                rooms = new()
                {
                    new BranchRoomSaveState { roomId = BranchRoomId.Origin.Value, isVisited = true, isCleared = true, rewardState = RoomRewardState.Unavailable.ToString() },
                    new BranchRoomSaveState { roomId = currentRoomId, isVisited = true, isCleared = true, rewardState = RoomRewardState.Claimed.ToString() }
                },
                economy = new RunEconomySaveState
                {
                    runSouls = 40,
                    collectedRewards = new()
                    {
                        new RunRewardSaveState { roomId = currentRoomId, rewardId = "ember_charm", displayName = "Ember Charm", rewardKind = RewardKind.PassiveItem.ToString(), souls = 10 }
                    }
                },
                playerStats = new PlayerRunStatsSaveState
                {
                    projectileDamageBonus = 1,
                    shotCooldownMultiplier = 0.9f
                }
            };
        }
    }
}
