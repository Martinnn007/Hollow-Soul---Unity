using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Animation/Player Animation Profile", fileName = "PlayerAnimationProfile")]
    public sealed class PlayerAnimationProfileDefinition : ScriptableObject
    {
        [SerializeField] private PlayerAnimationProfileId profileId;
        [SerializeField] private string profileName;
        [SerializeField] private bool allowsShieldInHand;
        [SerializeField] private bool allowsShieldGuard;
        [SerializeField] private bool requiresTwoHandedWeapon;
        [SerializeField] private bool usesRangedAim;
        [SerializeField] private bool usesFootIk = true;
        [SerializeField] private bool usesTorsoAim;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private DirectionalAnimationClipSet[] directionalClips = Array.Empty<DirectionalAnimationClipSet>();
        [SerializeField] private AnimationClip[] strafingClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] turnClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] drawClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] sheatheClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] attackClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] fireClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] shieldGuardClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] weaponBlockClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] impactClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] deathClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] jumpClips = Array.Empty<AnimationClip>();
        [SerializeField] private AnimationClip[] crouchClips = Array.Empty<AnimationClip>();
        [SerializeField] private string[] missingRequiredClipSlots = Array.Empty<string>();
        [SerializeField] private string[] missingOptionalClipSlots = Array.Empty<string>();
        [SerializeField] private string[] placeholderClipSlots = Array.Empty<string>();
        [SerializeField] private string[] mappedClipReports = Array.Empty<string>();

        public PlayerAnimationProfileId ProfileId => profileId;

        public string ProfileName => string.IsNullOrWhiteSpace(profileName) ? profileId.ToString() : profileName;

        public bool AllowsShieldInHand => allowsShieldInHand;

        public bool AllowsShieldGuard => allowsShieldGuard && allowsShieldInHand;

        public bool RequiresTwoHandedWeapon => requiresTwoHandedWeapon;

        public bool UsesRangedAim => usesRangedAim;

        public bool UsesFootIk => usesFootIk;

        public bool UsesTorsoAim => usesTorsoAim;

        public AnimationClip IdleClip => idleClip;

        public IReadOnlyList<DirectionalAnimationClipSet> DirectionalClips => directionalClips;

        public IReadOnlyList<AnimationClip> StrafingClips => strafingClips;

        public IReadOnlyList<AnimationClip> TurnClips => turnClips;

        public IReadOnlyList<AnimationClip> DrawClips => drawClips;

        public IReadOnlyList<AnimationClip> SheatheClips => sheatheClips;

        public IReadOnlyList<AnimationClip> AttackClips => attackClips;

        public IReadOnlyList<AnimationClip> FireClips => fireClips;

        public IReadOnlyList<AnimationClip> ShieldGuardClips => shieldGuardClips;

        public IReadOnlyList<AnimationClip> WeaponBlockClips => weaponBlockClips;

        public IReadOnlyList<AnimationClip> ImpactClips => impactClips;

        public IReadOnlyList<AnimationClip> DeathClips => deathClips;

        public IReadOnlyList<AnimationClip> JumpClips => jumpClips;

        public IReadOnlyList<AnimationClip> CrouchClips => crouchClips;

        public IReadOnlyList<string> MissingRequiredClipSlots => missingRequiredClipSlots;

        public IReadOnlyList<string> MissingOptionalClipSlots => missingOptionalClipSlots;

        public IReadOnlyList<string> PlaceholderClipSlots => placeholderClipSlots;

        public IReadOnlyList<string> MappedClipReports => mappedClipReports;

        public bool HasMissingRequiredClips => missingRequiredClipSlots.Length > 0;

        public bool UsesTemporaryPlaceholders => placeholderClipSlots.Length > 0;

        public void Configure(
            PlayerAnimationProfileId nextProfileId,
            string nextProfileName,
            bool nextAllowsShieldInHand,
            bool nextAllowsShieldGuard,
            bool nextRequiresTwoHandedWeapon,
            bool nextUsesRangedAim,
            bool nextUsesFootIk,
            bool nextUsesTorsoAim,
            AnimationClip nextIdleClip,
            IEnumerable<DirectionalAnimationClipSet> nextDirectionalClips,
            IEnumerable<AnimationClip> nextStrafingClips,
            IEnumerable<AnimationClip> nextTurnClips,
            IEnumerable<AnimationClip> nextDrawClips,
            IEnumerable<AnimationClip> nextSheatheClips,
            IEnumerable<AnimationClip> nextAttackClips,
            IEnumerable<AnimationClip> nextFireClips,
            IEnumerable<AnimationClip> nextShieldGuardClips,
            IEnumerable<AnimationClip> nextWeaponBlockClips,
            IEnumerable<AnimationClip> nextImpactClips,
            IEnumerable<AnimationClip> nextDeathClips,
            IEnumerable<AnimationClip> nextJumpClips,
            IEnumerable<AnimationClip> nextCrouchClips,
            IEnumerable<string> nextMissingRequiredClipSlots,
            IEnumerable<string> nextMissingOptionalClipSlots,
            IEnumerable<string> nextPlaceholderClipSlots,
            IEnumerable<string> nextMappedClipReports)
        {
            profileId = nextProfileId;
            profileName = nextProfileName ?? nextProfileId.ToString();
            allowsShieldInHand = nextAllowsShieldInHand;
            allowsShieldGuard = nextAllowsShieldGuard && nextAllowsShieldInHand;
            requiresTwoHandedWeapon = nextRequiresTwoHandedWeapon;
            usesRangedAim = nextUsesRangedAim;
            usesFootIk = nextUsesFootIk;
            usesTorsoAim = nextUsesTorsoAim;
            idleClip = nextIdleClip;
            directionalClips = (nextDirectionalClips ?? Enumerable.Empty<DirectionalAnimationClipSet>())
                .OrderBy(set => set.Direction)
                .ToArray();
            strafingClips = Compact(nextStrafingClips);
            turnClips = Compact(nextTurnClips);
            drawClips = Compact(nextDrawClips);
            sheatheClips = Compact(nextSheatheClips);
            attackClips = Compact(nextAttackClips);
            fireClips = Compact(nextFireClips);
            shieldGuardClips = Compact(nextShieldGuardClips);
            weaponBlockClips = Compact(nextWeaponBlockClips);
            impactClips = Compact(nextImpactClips);
            deathClips = Compact(nextDeathClips);
            jumpClips = Compact(nextJumpClips);
            crouchClips = Compact(nextCrouchClips);
            missingRequiredClipSlots = CompactStrings(nextMissingRequiredClipSlots);
            missingOptionalClipSlots = CompactStrings(nextMissingOptionalClipSlots);
            placeholderClipSlots = CompactStrings(nextPlaceholderClipSlots);
            mappedClipReports = CompactStrings(nextMappedClipReports);
        }

        public bool TryGetDirectionalClipSet(PlayerAnimationDirection direction, out DirectionalAnimationClipSet clipSet)
        {
            foreach (var candidate in directionalClips)
            {
                if (candidate.Direction == direction)
                {
                    clipSet = candidate;
                    return true;
                }
            }

            clipSet = default;
            return false;
        }

        public AnimationClip FirstAttackClip()
        {
            return attackClips.FirstOrDefault() ?? weaponBlockClips.FirstOrDefault();
        }

        public AnimationClip FirstImpactClip()
        {
            return impactClips.FirstOrDefault();
        }

        public AnimationClip FirstDeathClip()
        {
            return deathClips.FirstOrDefault();
        }

        private static AnimationClip[] Compact(IEnumerable<AnimationClip> clips)
        {
            return (clips ?? Enumerable.Empty<AnimationClip>())
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
        }

        private static string[] CompactStrings(IEnumerable<string> values)
        {
            return (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }
    }

    [Serializable]
    public struct DirectionalAnimationClipSet
    {
        [SerializeField] private PlayerAnimationDirection direction;
        [SerializeField] private AnimationClip walkClip;
        [SerializeField] private AnimationClip runClip;
        [SerializeField] private bool walkUsesTemporaryPlaceholder;
        [SerializeField] private bool runUsesTemporaryPlaceholder;

        public DirectionalAnimationClipSet(
            PlayerAnimationDirection direction,
            AnimationClip walkClip,
            AnimationClip runClip,
            bool walkUsesTemporaryPlaceholder = false,
            bool runUsesTemporaryPlaceholder = false)
        {
            this.direction = direction;
            this.walkClip = walkClip;
            this.runClip = runClip;
            this.walkUsesTemporaryPlaceholder = walkUsesTemporaryPlaceholder;
            this.runUsesTemporaryPlaceholder = runUsesTemporaryPlaceholder;
        }

        public PlayerAnimationDirection Direction => direction;

        public AnimationClip WalkClip => walkClip;

        public AnimationClip RunClip => runClip;

        public bool WalkUsesTemporaryPlaceholder => walkUsesTemporaryPlaceholder;

        public bool RunUsesTemporaryPlaceholder => runUsesTemporaryPlaceholder;

        public bool HasWalk => walkClip != null;

        public bool HasRun => runClip != null;
    }
}
