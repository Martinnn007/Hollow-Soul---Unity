using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rewards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone52AssetGenerator
    {
        public const string RewardDirectory = "Assets/_Hollow/Data/Rewards/M52";
        public const string StandardRewardPoolPath = RewardDirectory + "/StandardRoomRewardPool_M52.asset";
        public const string DocsPath = "Docs/Milestone52ChestsCoinDrops.md";
        public const string ReportPath = "output/reports/m52_chests_coin_drops.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        private static readonly VfxCueId[] M52VfxCues =
        {
            VfxCueId.ChestOpen,
            VfxCueId.CoinPickup
        };

        private static readonly AudioCueId[] M52AudioCues =
        {
            AudioCueId.ChestOpen,
            AudioCueId.CoinPickup
        };

        [MenuItem("Hollow/Generation/Generate Milestone 52 Assets")]
        public static void Generate()
        {
            Milestone51AssetGenerator.Generate();
            Directory.CreateDirectory(RewardDirectory);
            Directory.CreateDirectory(Milestone9AssetGenerator.VfxCueDirectory);
            Directory.CreateDirectory(Milestone9AssetGenerator.AudioCueDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var smallCoins = SaveReward("Reward_LooseCoins.asset", ChestRewardResolver.SmallCoinPouchRewardId, "Loose Coins", RewardKind.Currency, RewardRarity.Common, 0, 6, System.Array.Empty<RewardEffect>());
            var hpRefill = SaveReward("Reward_HpRefill.asset", ChestRewardResolver.HpRefillRewardId, "HP Refill", RewardKind.Heal, RewardRarity.Common, 0, 0, new[] { new RewardEffect(RewardEffectKind.Heal, intValue: 99) });
            var normalChest = SaveReward("Reward_NormalChest.asset", ChestRewardResolver.NormalChestRewardId, "Normal Chest", RewardKind.Currency, RewardRarity.Common, 0, 0, System.Array.Empty<RewardEffect>());
            var goldenChest = SaveReward("Reward_GoldenChest.asset", ChestRewardResolver.GoldenChestRewardId, "Golden Chest", RewardKind.Currency, RewardRarity.Rare, 0, 0, System.Array.Empty<RewardEffect>());
            var standardPool = SavePool(StandardRewardPoolPath, ProceduralRewardResolver.PreBetaStandardPoolId, new[] { smallCoins, hpRefill, normalChest, goldenChest });

            var vfxCues = GenerateVfxCues();
            var audioCues = GenerateAudioCues();
            UpdatePresentationCatalog(vfxCues, audioCues);
            AssignToGameScenes(standardPool);
            WriteDocs();
            WriteReport(standardPool);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 52 chest and coin-drop assets.");
        }

        private static RewardDefinition SaveReward(string fileName, string rewardId, string displayName, RewardKind kind, RewardRarity rarity, int souls, int coins, IEnumerable<RewardEffect> effects)
        {
            var path = $"{RewardDirectory}/{fileName}";
            var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (reward == null)
            {
                reward = ScriptableObject.CreateInstance<RewardDefinition>();
                AssetDatabase.CreateAsset(reward, path);
            }

            reward.Configure(rewardId, displayName, kind, rarity, souls, coins, effects);
            EditorUtility.SetDirty(reward);
            return reward;
        }

        private static RewardPoolDefinition SavePool(string path, string poolId, IEnumerable<RewardDefinition> rewards)
        {
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(path);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<RewardPoolDefinition>();
                AssetDatabase.CreateAsset(pool, path);
            }

            pool.Configure(poolId, rewards.Where(reward => reward != null).ToArray());
            EditorUtility.SetDirty(pool);
            return pool;
        }

        private static VfxCueDefinition[] GenerateVfxCues()
        {
            var cues = new List<VfxCueDefinition>();
            foreach (var cueId in M52VfxCues)
            {
                var path = $"{Milestone9AssetGenerator.VfxCueDirectory}/VfxCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<VfxCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<VfxCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, null, cueId == VfxCueId.ChestOpen ? MaterialResolver.FallbackColorFor(MaterialRole.ChestGolden) : MaterialResolver.FallbackColorFor(MaterialRole.CoinGold), cueId == VfxCueId.ChestOpen ? 0.22f : 0.14f, nextCreateDebugPrimitive: true);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static AudioCueDefinition[] GenerateAudioCues()
        {
            var cues = new List<AudioCueDefinition>();
            foreach (var cueId in M52AudioCues)
            {
                var path = $"{Milestone9AssetGenerator.AudioCueDirectory}/AudioCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<AudioCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<AudioCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, null, 0.48f, 0.55f);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static void UpdatePresentationCatalog(VfxCueDefinition[] vfxCues, AudioCueDefinition[] audioCues)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var vfx = (catalog.VfxCues ?? System.Array.Empty<VfxCueDefinition>())
                .Where(cue => cue != null && !vfxCues.Any(next => next.CueId == cue.CueId))
                .Concat(vfxCues)
                .ToArray();
            var audio = (catalog.AudioCues ?? System.Array.Empty<AudioCueDefinition>())
                .Where(cue => cue != null && !audioCues.Any(next => next.CueId == cue.CueId))
                .Concat(audioCues)
                .ToArray();
            catalog.Configure(catalog.MaterialPalette, vfx, audio, catalog.PrefabBindings);
            PresentationContentProvider.Configure(catalog);
            EditorUtility.SetDirty(catalog);
        }

        private static void AssignToGameScenes(RewardPoolDefinition standardPool)
        {
            var treasurePool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone51AssetGenerator.TreasureRewardPoolPath);
            var bossPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone51AssetGenerator.BossRewardPoolPath);
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureRewardPools(standardPool, treasurePool, bossPool);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteDocs()
        {
            File.WriteAllText(DocsPath, @"# M52: Chests + Coin Drops

M52 turns the M51 placeholder chest into real room-local containers and visible coins.

- Standard combat/reward rooms roll: 2% Golden Chest, 12% Normal Chest, 38% loose coins, 24% HP refill, 24% nothing.
- Normal Chests open with Interact and grant either 8-14 coins or HP refill.
- Golden Chests open with Interact and can grant 15-30 coins, HP plus coins, or a passive/consumable card plus coins.
- Copper/Silver/Gold coins are visible pickups worth 1/5/10 and collect by walking over them.
- Room Designer `Chest` markers only guide placement. The runtime reward roll still decides chest kind and contents.
- Starter/origin rooms remain empty and safe.
");
        }

        private static void WriteReport(RewardPoolDefinition standardPool)
        {
            File.WriteAllText(ReportPath, $@"# M52 Chests + Coin Drops Report

- Standard reward pool: `{standardPool.PoolId}` at `{StandardRewardPoolPath}`.
- Chest reward IDs: `{ChestRewardResolver.NormalChestRewardId}`, `{ChestRewardResolver.GoldenChestRewardId}`.
- Coin values: Copper `{CoinDenominationResolver.CopperValue}`, Silver `{CoinDenominationResolver.SilverValue}`, Gold `{CoinDenominationResolver.GoldValue}`.
- Coin pickup max per value roll: `{CoinDenominationResolver.DefaultMaxPhysicalCoins}`.
- Presentation roles: `ChestNormal`, `ChestGolden`, `CoinCopper`, `CoinSilver`, `CoinGold`.
");
        }
    }
}
