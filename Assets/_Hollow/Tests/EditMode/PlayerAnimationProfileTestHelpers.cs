using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    internal static class PlayerAnimationProfileTestHelpers
    {
        public static PlayerAnimationProfileController BindProfileCatalog(GameObject player, PlayerWeaponController weapon)
        {
            var controller = player.GetComponent<PlayerAnimationProfileController>() ??
                player.AddComponent<PlayerAnimationProfileController>();
            controller.Configure(CreateCatalog());
            controller.Bind(weapon);
            return controller;
        }

        public static PlayerAnimationProfileController ForceSwordShieldProfile(GameObject player, PlayerWeaponController weapon = null)
        {
            var controller = BindProfileCatalog(player, weapon);
            controller.SetDebugProfileOverride(controller.Catalog.Resolve(PlayerAnimationProfileId.SwordShieldCombat));
            return controller;
        }

        public static PlayerAnimationProfileCatalogDefinition CreateCatalog()
        {
            var unarmed = CreateProfile(PlayerAnimationProfileId.UnarmedLocomotion, false, false, false, false);
            var sword = CreateProfile(PlayerAnimationProfileId.SwordShieldCombat, true, true, false, false);
            var great = CreateProfile(PlayerAnimationProfileId.GreatSwordCombat, false, false, true, false);
            var rifle = CreateProfile(PlayerAnimationProfileId.RifleCombat, false, false, false, true);
            var pistol = CreateProfile(PlayerAnimationProfileId.PistolCombat, false, false, false, true);
            var catalog = ScriptableObject.CreateInstance<PlayerAnimationProfileCatalogDefinition>();
            catalog.Configure("editmode_profile_test_catalog", new[] { unarmed, sword, great, rifle, pistol }, unarmed);
            return catalog;
        }

        private static PlayerAnimationProfileDefinition CreateProfile(
            PlayerAnimationProfileId profileId,
            bool allowsShieldInHand,
            bool allowsShieldGuard,
            bool requiresTwoHanded,
            bool usesRangedAim)
        {
            var profile = ScriptableObject.CreateInstance<PlayerAnimationProfileDefinition>();
            profile.Configure(
                profileId,
                profileId + "Profile",
                allowsShieldInHand,
                allowsShieldGuard,
                requiresTwoHanded,
                usesRangedAim,
                nextUsesFootIk: true,
                nextUsesTorsoAim: allowsShieldGuard || usesRangedAim || requiresTwoHanded,
                nextIdleClip: null,
                nextDirectionalClips: Enumerable.Empty<DirectionalAnimationClipSet>(),
                nextStrafingClips: null,
                nextTurnClips: null,
                nextDrawClips: null,
                nextSheatheClips: null,
                nextAttackClips: null,
                nextFireClips: null,
                nextShieldGuardClips: null,
                nextWeaponBlockClips: null,
                nextImpactClips: null,
                nextDeathClips: null,
                nextJumpClips: null,
                nextCrouchClips: null,
                nextMissingRequiredClipSlots: null,
                nextMissingOptionalClipSlots: null,
                nextPlaceholderClipSlots: null,
                nextMappedClipReports: null);
            return profile;
        }
    }
}
