using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone29Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/CharacterCatalogDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/CharacterStatModifier.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone29AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone29Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone29CharacterPassiveSkillTests.cs",
            "Docs/Milestone29CharactersPassiveIdentitySkills.md",
            Milestone29AssetGenerator.CharacterCatalogPath
        };

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Validation/Run Milestone 29 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: Application.isBatchMode);
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M29 file: {file}");
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(Milestone29AssetGenerator.CharacterCatalogPath);
            ValidateCatalog(catalog, failures);
            ValidateScenes(catalog, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 29 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateCatalog(CharacterCatalogDefinition catalog, List<string> failures)
        {
            if (catalog == null)
            {
                failures.Add("M29 character catalog is missing.");
                return;
            }

            if (!catalog.TryGetCharacter("balanced", out var balanced) || balanced.PassiveSkill == null)
            {
                failures.Add("M29 catalog must contain Balanced with a passive skill.");
            }
            else
            {
                if (balanced.BaseStats.MaxHealth != 6 || balanced.BaseStats.SpeedMetersPerSecond < 3.99f)
                {
                    failures.Add("Balanced must use default stable base stats.");
                }

                if (balanced.PassiveSkill.StatModifier.MaxStamina != 10f || balanced.PassiveSkill.StatModifier.StaminaRegen != 1f)
                {
                    failures.Add("Balanced passive must grant +10 stamina and +1 regen.");
                }
            }

            if (!catalog.TryGetCharacter("heavy", out var heavy) || heavy.PassiveSkill == null)
            {
                failures.Add("M29 catalog must contain Heavy with a passive skill.");
            }
            else
            {
                if (heavy.BaseStats.MaxHealth != 9 || heavy.BaseStats.Defense != 2 || heavy.BaseStats.MeleeDamageBonus != 1)
                {
                    failures.Add("Heavy must use the locked tank base stats.");
                }

                if (heavy.PassiveSkill.StatModifier.MeleeDamage != 1)
                {
                    failures.Add("Heavy passive must grant +1 melee damage.");
                }
            }

            foreach (var character in catalog.Characters.Where(character => character != null))
            {
                if (character.StarterMeleeWeaponId != "starter_blade" || character.StarterRangedWeaponId != "starter_bolt")
                {
                    failures.Add($"{character.CharacterId} must use starter_blade and starter_bolt in M29.");
                }
            }
        }

        private static void ValidateScenes(CharacterCatalogDefinition catalog, List<string> failures)
        {
            foreach (var scenePath in GameScenes)
            {
                EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    failures.Add($"{scenePath} is missing BranchSessionController.");
                    continue;
                }

                if (branch.CharacterCatalog != catalog)
                {
                    failures.Add($"{scenePath} BranchSessionController is not wired to the M29 character catalog.");
                }
            }
        }
    }
}
