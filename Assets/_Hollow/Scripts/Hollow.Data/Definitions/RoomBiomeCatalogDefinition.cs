using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Rooms/Room Biome Catalog", fileName = "RoomBiomeCatalog")]
    public sealed class RoomBiomeCatalogDefinition : ScriptableObject
    {
        public const string DefaultResourcePath = "Hollow/Biomes/RoomBiomeCatalog";

        private static RoomBiomeCatalogDefinition runtimeFallback;

        [SerializeField] private string fallbackBiomeId = RoomBiomeIds.HollowThreshold;
        [SerializeField] private List<RoomBiomeDefinition> biomes = new();

        public string FallbackBiomeId => RoomBiomeIds.Normalize(fallbackBiomeId);

        public IReadOnlyList<RoomBiomeDefinition> Biomes => biomes ?? new List<RoomBiomeDefinition>();

        public static RoomBiomeCatalogDefinition LoadDefault()
        {
            var loaded = Resources.Load<RoomBiomeCatalogDefinition>(DefaultResourcePath);
            if (loaded != null)
            {
                return loaded;
            }

            return runtimeFallback != null ? runtimeFallback : runtimeFallback = CreateRuntimeFallback();
        }

        public string ResolveBiomeId(string biomeId)
        {
            return TryGetBiome(biomeId, out var definition) ? definition.BiomeId : FallbackBiomeId;
        }

        public bool TryGetBiome(string biomeId, out RoomBiomeDefinition definition)
        {
            var normalized = RoomBiomeIds.Normalize(biomeId);
            definition = (biomes ?? new List<RoomBiomeDefinition>())
                .FirstOrDefault(candidate => candidate != null && RoomBiomeIds.Matches(candidate.BiomeId, normalized));
            if (definition != null)
            {
                return true;
            }

            var fallback = FallbackBiomeId;
            definition = (biomes ?? new List<RoomBiomeDefinition>())
                .FirstOrDefault(candidate => candidate != null && RoomBiomeIds.Matches(candidate.BiomeId, fallback));
            return definition != null;
        }

        public IReadOnlyList<TextAsset> ResolveRoomTemplates(string biomeId)
        {
            return TryGetBiome(biomeId, out var definition)
                ? definition.RoomTemplates
                : Array.Empty<TextAsset>();
        }

        public void Configure(string nextFallbackBiomeId, IEnumerable<RoomBiomeDefinition> nextBiomes)
        {
            fallbackBiomeId = RoomBiomeIds.Normalize(nextFallbackBiomeId);
            biomes = (nextBiomes ?? Array.Empty<RoomBiomeDefinition>())
                .Where(definition => definition != null)
                .GroupBy(definition => definition.BiomeId)
                .Select(group => group.First())
                .ToList();
        }

        private static RoomBiomeCatalogDefinition CreateRuntimeFallback()
        {
            var catalog = CreateInstance<RoomBiomeCatalogDefinition>();
            catalog.name = "RuntimeFallbackRoomBiomeCatalog";
            var hollow = CreateInstance<RoomBiomeDefinition>();
            hollow.name = "RuntimeFallback_HollowThreshold";
            hollow.Configure(
                RoomBiomeIds.HollowThreshold,
                "The Hollow Threshold",
                new[] { WorldBiomeTag.MixedThreshold },
                Array.Empty<TextAsset>(),
                null,
                null,
                DefaultDecorBindings());
            var verdant = CreateInstance<RoomBiomeDefinition>();
            verdant.name = "RuntimeFallback_VerdantRuins";
            verdant.Configure(
                RoomBiomeIds.VerdantRuins,
                "Verdant Ruins",
                new[] { WorldBiomeTag.VerdantRuins, WorldBiomeTag.Memory },
                Array.Empty<TextAsset>(),
                null,
                null,
                DefaultDecorBindings());
            catalog.Configure(RoomBiomeIds.HollowThreshold, new[] { hollow, verdant });
            return catalog;
        }

        public static RoomBiomeDecorBinding[] DefaultDecorBindings()
        {
            return new[]
            {
                new RoomBiomeDecorBinding(RoomBiomeDecorKinds.GrassTuft, PresentationPrefabRole.DecorGrassTuft),
                new RoomBiomeDecorBinding(RoomBiomeDecorKinds.CrystalCluster, PresentationPrefabRole.DecorCrystalCluster),
                new RoomBiomeDecorBinding(RoomBiomeDecorKinds.SmallTree, PresentationPrefabRole.DecorSmallTree),
                new RoomBiomeDecorBinding(RoomBiomeDecorKinds.StoneRuin, PresentationPrefabRole.DecorStoneRuin)
            };
        }
    }
}
