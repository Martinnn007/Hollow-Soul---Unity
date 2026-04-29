using System.Collections.Generic;
using System.IO;
using Hollow.Branches;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone29AssetGenerator
    {
        public const string CharacterDirectory = "Assets/_Hollow/Data/Characters/M29";
        public const string CharacterCatalogPath = CharacterDirectory + "/CharacterCatalog_M29.asset";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 29 Assets")]
        public static void Generate()
        {
            Milestone28AssetGenerator.Generate();
            Directory.CreateDirectory(CharacterDirectory);

            var steadyForm = SavePassiveSkill(
                "Passive_SteadyForm.asset",
                "steady_form",
                "Steady Form",
                "+10 stamina and +1 stamina regen.",
                new CharacterStatModifier(maxStamina: 10f, staminaRegen: 1f),
                new[] { BuildTag.Stamina, BuildTag.Fast });
            var crushingGrip = SavePassiveSkill(
                "Passive_CrushingGrip.asset",
                "crushing_grip",
                "Crushing Grip",
                "+1 melee damage.",
                new CharacterStatModifier(meleeDamage: 1),
                new[] { BuildTag.Melee, BuildTag.Heavy });

            var balanced = SaveCharacter(
                "Character_Balanced.asset",
                "balanced",
                "Balanced",
                new PlayerBaseStats(
                    maxHealth: 3,
                    speedMetersPerSecond: 4f,
                    strength: 1,
                    maxStamina: 100f,
                    staminaRegenPerSecond: 18f,
                    defense: 0,
                    meleeDamageBonus: 0,
                    rangedDamageBonus: 0,
                    attackCooldownMultiplier: 1f),
                steadyForm,
                new[] { BuildTag.Stamina, BuildTag.Ranged, BuildTag.Melee });
            var heavy = SaveCharacter(
                "Character_Heavy.asset",
                "heavy",
                "Heavy",
                new PlayerBaseStats(
                    maxHealth: 5,
                    speedMetersPerSecond: 3.15f,
                    strength: 2,
                    maxStamina: 130f,
                    staminaRegenPerSecond: 15f,
                    defense: 2,
                    meleeDamageBonus: 1,
                    rangedDamageBonus: 0,
                    attackCooldownMultiplier: 1f),
                crushingGrip,
                new[] { BuildTag.Melee, BuildTag.Heavy, BuildTag.Defense });

            var catalog = SaveCatalog(new[] { balanced, heavy });
            AssignToGameScenes(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 29 character catalog, character assets, passive skills, and scene wiring.");
        }

        private static CharacterPassiveSkillDefinition SavePassiveSkill(
            string fileName,
            string skillId,
            string displayName,
            string description,
            CharacterStatModifier statModifier,
            IEnumerable<BuildTag> tags)
        {
            var path = $"{CharacterDirectory}/{fileName}";
            var skill = AssetDatabase.LoadAssetAtPath<CharacterPassiveSkillDefinition>(path);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<CharacterPassiveSkillDefinition>();
                AssetDatabase.CreateAsset(skill, path);
            }

            skill.Configure(skillId, displayName, description, statModifier, tags);
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static CharacterDefinition SaveCharacter(
            string fileName,
            string characterId,
            string displayName,
            PlayerBaseStats baseStats,
            CharacterPassiveSkillDefinition passiveSkill,
            IEnumerable<BuildTag> tags)
        {
            var path = $"{CharacterDirectory}/{fileName}";
            var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (character == null)
            {
                character = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(character, path);
            }

            character.Configure(
                characterId,
                displayName,
                baseStats,
                "starter_blade",
                "starter_bolt",
                passiveSkill,
                string.Empty,
                tags);
            EditorUtility.SetDirty(character);
            return character;
        }

        private static CharacterCatalogDefinition SaveCatalog(IEnumerable<CharacterDefinition> characters)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(CharacterCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CharacterCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, CharacterCatalogPath);
            }

            catalog.Configure("m29_character_catalog_v1", characters);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AssignToGameScenes(CharacterCatalogDefinition catalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureCharacterCatalog(catalog);
                EditorUtility.SetDirty(branch);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
