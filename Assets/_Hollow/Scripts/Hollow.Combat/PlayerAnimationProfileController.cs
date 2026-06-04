using System;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerAnimationProfileController : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationProfileCatalogDefinition catalog;
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private bool debugOverrideEnabled;
        [SerializeField] private PlayerAnimationProfileDefinition debugOverrideProfile;

        private static PlayerAnimationProfileDefinition runtimeFallbackProfile;

        public PlayerAnimationProfileCatalogDefinition Catalog => catalog;

        public PlayerAnimationProfileDefinition CurrentProfile => ResolveCurrentProfile();

        public PlayerAnimationProfileId CurrentProfileId => CurrentProfile != null
            ? CurrentProfile.ProfileId
            : PlayerAnimationProfileId.UnarmedLocomotion;

        public bool HasResolvedProfile => ResolveCurrentProfile() != null;

        public bool IsDebugOverrideEnabled => debugOverrideEnabled && debugOverrideProfile != null;

        public bool AllowsShieldInHand => CurrentProfile != null && CurrentProfile.AllowsShieldInHand;

        public bool AllowsShieldGuard => CurrentProfile != null && CurrentProfile.AllowsShieldGuard;

        public bool RequiresTwoHandedWeapon => CurrentProfile != null && CurrentProfile.RequiresTwoHandedWeapon;

        public bool UsesRangedAim => CurrentProfile != null && CurrentProfile.UsesRangedAim;

        public bool UsesFootIk => CurrentProfile == null || CurrentProfile.UsesFootIk;

        public bool UsesTorsoAim => CurrentProfile != null && CurrentProfile.UsesTorsoAim;

        public event Action<PlayerAnimationProfileDefinition> ProfileChanged;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
        }

        public void Configure(PlayerAnimationProfileCatalogDefinition nextCatalog)
        {
            catalog = nextCatalog;
            if (catalog == null)
            {
                ClearDebugProfileOverride();
            }

            ResolveReferences();
            ProfileChanged?.Invoke(CurrentProfile);
        }

        public void Bind(PlayerWeaponController nextWeaponController)
        {
            weaponController = nextWeaponController;
            ResolveReferences();
            ProfileChanged?.Invoke(CurrentProfile);
        }

        public void SetDebugProfileOverride(PlayerAnimationProfileDefinition profile)
        {
            debugOverrideProfile = profile;
            debugOverrideEnabled = profile != null;
            ProfileChanged?.Invoke(CurrentProfile);
        }

        public void ClearDebugProfileOverride()
        {
            debugOverrideProfile = null;
            debugOverrideEnabled = false;
            ProfileChanged?.Invoke(CurrentProfile);
        }

        public PlayerAnimationProfileDefinition ResolveCurrentProfile()
        {
            ResolveReferences();
            if (catalog != null && debugOverrideEnabled && debugOverrideProfile != null)
            {
                return debugOverrideProfile;
            }

            var profileId = ResolveProfileIdFromWeapon();
            return catalog != null
                ? catalog.Resolve(profileId) ?? catalog.Resolve(PlayerAnimationProfileId.UnarmedLocomotion) ?? RuntimeFallbackProfile()
                : RuntimeFallbackProfile();
        }

        private PlayerAnimationProfileId ResolveProfileIdFromWeapon()
        {
            var weapon = ResolveActiveWeapon();
            if (weapon == null)
            {
                return PlayerAnimationProfileId.UnarmedLocomotion;
            }

            if (weapon.Slot == WeaponSlot.Ranged)
            {
                if (weapon.Category == WeaponCategory.Gun && !LooksLikeRifle(weapon.WeaponId, weapon.DisplayName))
                {
                    return PlayerAnimationProfileId.PistolCombat;
                }

                return PlayerAnimationProfileId.RifleCombat;
            }

            return weapon.IsDoubleHandedForPresentation || LooksLikeGreatSword(weapon.WeaponId, weapon.DisplayName)
                ? PlayerAnimationProfileId.GreatSwordCombat
                : PlayerAnimationProfileId.SwordShieldCombat;
        }

        private WeaponDefinition ResolveActiveWeapon()
        {
            if (weaponController == null || weaponController.WeaponCatalog == null)
            {
                return null;
            }

            var slot = weaponController.ActiveWeaponSlot;
            var weaponId = slot == WeaponSlot.Ranged ? weaponController.RangedWeaponId : weaponController.MeleeWeaponId;
            return weaponController.WeaponCatalog.Resolve(weaponId, slot);
        }

        private void ResolveReferences()
        {
            weaponController ??= GetComponent<PlayerWeaponController>();
        }

        private static bool LooksLikeRifle(string weaponId, string displayName)
        {
            return ContainsToken(weaponId, "rifle") ||
                ContainsToken(displayName, "rifle") ||
                ContainsToken(weaponId, "carbine") ||
                ContainsToken(displayName, "carbine");
        }

        private static bool LooksLikeGreatSword(string weaponId, string displayName)
        {
            return ContainsToken(weaponId, "great") ||
                ContainsToken(displayName, "great") ||
                ContainsToken(weaponId, "cleaver") ||
                ContainsToken(displayName, "cleaver") ||
                ContainsToken(weaponId, "two_handed") ||
                ContainsToken(displayName, "two handed");
        }

        private static bool ContainsToken(string value, string token)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.ToLowerInvariant().Contains(token.ToLowerInvariant());
        }

        private static PlayerAnimationProfileDefinition RuntimeFallbackProfile()
        {
            if (runtimeFallbackProfile != null)
            {
                return runtimeFallbackProfile;
            }

            runtimeFallbackProfile = ScriptableObject.CreateInstance<PlayerAnimationProfileDefinition>();
            runtimeFallbackProfile.name = "Runtime_UnarmedLocomotionProfile_Fallback";
            runtimeFallbackProfile.hideFlags = HideFlags.HideAndDontSave;
            runtimeFallbackProfile.Configure(
                PlayerAnimationProfileId.UnarmedLocomotion,
                "UnarmedLocomotionProfile",
                nextAllowsShieldInHand: false,
                nextAllowsShieldGuard: false,
                nextRequiresTwoHandedWeapon: false,
                nextUsesRangedAim: false,
                nextUsesFootIk: true,
                nextUsesTorsoAim: false,
                nextIdleClip: null,
                nextDirectionalClips: null,
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
            return runtimeFallbackProfile;
        }
    }
}
