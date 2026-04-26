using System.IO;
using Hollow.Rewards;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone7AssetGenerator
    {
        private const string RewardDirectory = "Assets/_Hollow/Data/Rewards";

        [MenuItem("Hollow/Generation/Generate Milestone 7 Assets")]
        public static void Generate()
        {
            Milestone6AssetGenerator.Generate();
            Directory.CreateDirectory(RewardDirectory);
            CreateReward("north_stone_heart.asset", "stone_heart", "Stone Heart", RewardKind.PassiveItem, 10);
            CreateReward("south_quick_draw.asset", "quick_draw", "Quick Draw", RewardKind.Card, 10);
            CreateReward("east_fleet_step.asset", "fleet_step", "Fleet Step", RewardKind.PassiveItem, 10);
            CreateReward("west_ember_charm.asset", "ember_charm", "Ember Charm", RewardKind.PassiveItem, 10);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 7 run economy, persistence, and meta-progression assets.");
        }

        private static void CreateReward(string fileName, string rewardId, string displayName, RewardKind rewardKind, int souls)
        {
            var path = $"{RewardDirectory}/{fileName}";
            var definition = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RewardDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(rewardId, displayName, rewardKind, souls);
            EditorUtility.SetDirty(definition);
        }
    }
}
