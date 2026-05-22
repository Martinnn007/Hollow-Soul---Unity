using System.Linq;
using Hollow.Core;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Rewards;
using Hollow.UI.MainMenu;
using Hollow.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone29CharacterPassiveSkillTests
    {
        [Test]
        public void CharacterCatalogContainsBalancedAndHeavyDefinitions()
        {
            var catalog = LoadCatalog();

            Assert.IsTrue(catalog.TryGetCharacter("balanced", out var balanced));
            Assert.AreEqual("Balanced", balanced.DisplayName);
            Assert.AreEqual("starter_blade", balanced.StarterMeleeWeaponId);
            Assert.AreEqual("starter_pistol", balanced.StarterRangedWeaponId);
            Assert.AreEqual(10f, balanced.PassiveSkill.StatModifier.MaxStamina, 0.001f);
            Assert.AreEqual(1f, balanced.PassiveSkill.StatModifier.StaminaRegen, 0.001f);
            Assert.AreEqual(3, balanced.BaseStats.MaxHealth);
            Assert.AreEqual(100f, balanced.BaseStats.MaxStamina, 0.001f);
            Assert.AreEqual(11f, balanced.BaseStats.StaminaRegenPerSecond, 0.001f);

            Assert.IsTrue(catalog.TryGetCharacter("heavy", out var heavy));
            Assert.AreEqual("Heavy", heavy.DisplayName);
            Assert.AreEqual(5, heavy.BaseStats.MaxHealth);
            Assert.AreEqual(3.15f, heavy.BaseStats.SpeedMetersPerSecond, 0.001f);
            Assert.AreEqual(2, heavy.BaseStats.Strength);
            Assert.AreEqual(120f, heavy.BaseStats.MaxStamina, 0.001f);
            Assert.AreEqual(8f, heavy.BaseStats.StaminaRegenPerSecond, 0.001f);
            Assert.AreEqual(2, heavy.BaseStats.Defense);
            Assert.AreEqual(1, heavy.BaseStats.MeleeDamageBonus);
            Assert.AreEqual(1, heavy.PassiveSkill.StatModifier.MeleeDamage);
        }

        [Test]
        public void PlayerRunBuildAppliesCharacterBaseStatsStartersAndPassiveModifier()
        {
            var catalog = LoadCatalog();
            var heavy = catalog.Resolve("heavy");
            var build = new PlayerRunBuild();

            build.ConfigureCharacter(heavy);

            Assert.AreEqual("heavy", build.SelectedCharacterId);
            Assert.AreEqual("starter_blade", build.Equipment.MeleeWeaponId);
            Assert.AreEqual("starter_pistol", build.Equipment.RangedWeaponId);
            Assert.AreEqual(WeaponSlot.Melee, build.Equipment.ActiveWeaponSlot);
            Assert.AreEqual(5, build.DerivedStats.MaxHealth);
            Assert.AreEqual(3.15f, build.DerivedStats.SpeedMetersPerSecond, 0.001f);
            Assert.AreEqual(2, build.DerivedStats.Strength);
            Assert.AreEqual(120f, build.DerivedStats.MaxStamina, 0.001f);
            Assert.AreEqual(8f, build.DerivedStats.StaminaRegenPerSecond, 0.001f);
            Assert.AreEqual(2, build.DerivedStats.Defense);
            Assert.AreEqual(2, build.DerivedStats.MeleeDamageBonus);
            Assert.IsTrue(build.Modifiers.Any(modifier => modifier.sourceId == "character:crushing_grip"));
        }

        [Test]
        public void CharacterSelectFlowStoresSessionOnlyCharacterBeforeLaunch()
        {
            var tempRoot = System.IO.Path.Combine(Application.temporaryCachePath, "hollow_m29_menu", System.IO.Path.GetRandomFileName());
            System.IO.Directory.CreateDirectory(tempRoot);
            try
            {
                var store = new JsonProfileStore(tempRoot);
                var selected = new SelectedProfileContext();
                var appState = new AppStateMachine();
                var viewModel = new MainMenuViewModel(store, selected, appState);

                viewModel.SelectOrCreateSlot(0);
                viewModel.BeginNewRun(HollowPlatformKind.WindowsStandard3D);
                Assert.AreEqual(MainMenuState.CharacterSelect, viewModel.State);

                var route = viewModel.SelectCharacterAndLaunch("heavy");

                Assert.AreEqual(AppShellRoute.GameWindows, route);
                Assert.AreEqual(AppShellRoute.GameWindows, appState.CurrentRoute);
                Assert.AreEqual(RunLaunchMode.NewRun, selected.LaunchMode);
                Assert.AreEqual("heavy", selected.SelectedCharacterId);
                Assert.AreEqual(MainMenuState.Launching, viewModel.State);
            }
            finally
            {
                if (System.IO.Directory.Exists(tempRoot))
                {
                    System.IO.Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Test]
        public void GameSessionStateCarriesSelectedCharacterId()
        {
            var summary = new ProfileSlotSummary(0, "profile", "Runner", 1, 2, 0, false, 0, 0);

            var state = GameSessionState.Create(RuntimeSessionMode.ProfileBacked, HollowPlatformKind.WindowsStandard3D, RunLaunchMode.NewRun, summary, Vector3.zero, "heavy");

            Assert.AreEqual("heavy", state.SelectedCharacterId);
        }

        [Test]
        public void LegacyRunBuildDefaultsToBalancedCharacter()
        {
            var restored = PlayerRunBuild.FromSaveState(new PlayerRunBuildSaveState { selectedCharacterId = string.Empty });

            Assert.AreEqual("balanced", restored.SelectedCharacterId);
        }

        [Test]
        public void CharacterPassiveSkillCanBeCreatedFromDataOnlyModifier()
        {
            var skill = ScriptableObject.CreateInstance<CharacterPassiveSkillDefinition>();
            try
            {
                skill.Configure("test_skill", "Test Skill", "Test", new CharacterStatModifier(maxHealth: 1, meleeDamage: 2), new[] { BuildTag.Melee });

                var modifier = PlayerStatModifier.FromCharacterStatModifier($"character:{skill.SkillId}", skill.StatModifier);

                Assert.AreEqual("character:test_skill", modifier.sourceId);
                Assert.AreEqual(1, modifier.maxHealth);
                Assert.AreEqual(2, modifier.meleeDamage);
            }
            finally
            {
                Object.DestroyImmediate(skill);
            }
        }

        [Test]
        public void Milestone29ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone29Validator.Validate());
        }

        private static CharacterCatalogDefinition LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(Milestone29AssetGenerator.CharacterCatalogPath);
            Assert.IsNotNull(catalog, "Run M29 generation before validating character catalog.");
            return catalog;
        }
    }
}
