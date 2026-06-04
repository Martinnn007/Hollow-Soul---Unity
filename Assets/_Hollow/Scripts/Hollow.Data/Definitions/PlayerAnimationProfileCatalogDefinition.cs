using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Animation/Player Animation Profile Catalog", fileName = "PlayerAnimationProfileCatalog")]
    public sealed class PlayerAnimationProfileCatalogDefinition : ScriptableObject
    {
        [SerializeField] private string catalogId = "player_animation_profiles";
        [SerializeField] private PlayerAnimationProfileDefinition fallbackProfile;
        [SerializeField] private PlayerAnimationProfileDefinition[] profiles = System.Array.Empty<PlayerAnimationProfileDefinition>();

        public string CatalogId => catalogId;

        public PlayerAnimationProfileDefinition FallbackProfile => fallbackProfile;

        public IReadOnlyList<PlayerAnimationProfileDefinition> Profiles => profiles;

        public void Configure(
            string nextCatalogId,
            IEnumerable<PlayerAnimationProfileDefinition> nextProfiles,
            PlayerAnimationProfileDefinition nextFallbackProfile)
        {
            catalogId = string.IsNullOrWhiteSpace(nextCatalogId) ? "player_animation_profiles" : nextCatalogId;
            profiles = (nextProfiles ?? Enumerable.Empty<PlayerAnimationProfileDefinition>())
                .Where(profile => profile != null)
                .Distinct()
                .OrderBy(profile => profile.ProfileId)
                .ToArray();
            fallbackProfile = nextFallbackProfile != null
                ? nextFallbackProfile
                : profiles.FirstOrDefault(profile => profile.ProfileId == PlayerAnimationProfileId.UnarmedLocomotion) ??
                  profiles.FirstOrDefault();
        }

        public PlayerAnimationProfileDefinition Resolve(PlayerAnimationProfileId profileId)
        {
            return profiles.FirstOrDefault(profile => profile != null && profile.ProfileId == profileId) ??
                fallbackProfile ??
                profiles.FirstOrDefault();
        }
    }
}
