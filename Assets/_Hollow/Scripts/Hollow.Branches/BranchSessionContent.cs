using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Rooms;

namespace Hollow.Branches
{
    public sealed class BranchSessionContent
    {
        private BranchSessionContent(
            ImportedRoomRuntimeAsset legacySampleRoomAsset,
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> fixtureRoomPool,
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> approvedRoomPool,
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> macroRoomPool,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, ImportedRoomRuntimeAsset>> biomeRoomPools,
            int branchSeed)
        {
            LegacySampleRoomAsset = legacySampleRoomAsset;
            FixtureRoomPool = fixtureRoomPool ?? new Dictionary<string, ImportedRoomRuntimeAsset>();
            ApprovedRoomPool = approvedRoomPool ?? new Dictionary<string, ImportedRoomRuntimeAsset>();
            MacroRoomPool = macroRoomPool ?? new Dictionary<string, ImportedRoomRuntimeAsset>();
            BiomeRoomPools = biomeRoomPools ?? new Dictionary<string, IReadOnlyDictionary<string, ImportedRoomRuntimeAsset>>();
            BranchSeed = branchSeed == 0 ? BranchGenerator.DefaultMacroFixtureSeed : branchSeed;
        }

        public ImportedRoomRuntimeAsset LegacySampleRoomAsset { get; }

        public IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> FixtureRoomPool { get; }

        public IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> ApprovedRoomPool { get; }

        public IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> MacroRoomPool { get; }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, ImportedRoomRuntimeAsset>> BiomeRoomPools { get; }

        public int BranchSeed { get; }

        public bool HasMacroFixturePool =>
            FixtureRoomPool.ContainsKey("combat_macro_single_1x1") &&
            FixtureRoomPool.ContainsKey("combat_macro_wide_2x1") &&
            FixtureRoomPool.ContainsKey("combat_macro_tall_1x2") &&
            FixtureRoomPool.ContainsKey("combat_macro_block_2x2") &&
            FixtureRoomPool.ContainsKey("combat_macro_l_3cell");

        public bool TryGetRoomAsset(string roomAssetId, out ImportedRoomRuntimeAsset asset)
        {
            if (!string.IsNullOrWhiteSpace(roomAssetId) && MacroRoomPool.TryGetValue(roomAssetId, out asset))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(roomAssetId))
            {
                foreach (var pool in BiomeRoomPools.Values)
                {
                    if (pool != null && pool.TryGetValue(roomAssetId, out asset))
                    {
                        return true;
                    }
                }
            }

            asset = LegacySampleRoomAsset;
            return asset != null;
        }

        public IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> ResolveRoomPoolForBiome(string biomeId, out bool usedFallback)
        {
            usedFallback = false;
            var normalized = RoomBiomeIds.Normalize(biomeId);
            if (!RoomBiomeIds.Matches(normalized, RoomBiomeIds.HollowThreshold) &&
                BiomeRoomPools.TryGetValue(normalized, out var pool) &&
                HasCompleteShapeCoverage(pool))
            {
                return pool;
            }

            usedFallback = !RoomBiomeIds.Matches(normalized, RoomBiomeIds.HollowThreshold);
            return MacroRoomPool;
        }

        public bool HasCompleteBiomePool(string biomeId)
        {
            return BiomeRoomPools.TryGetValue(RoomBiomeIds.Normalize(biomeId), out var pool) && HasCompleteShapeCoverage(pool);
        }

        public static BranchSessionContent Create(
            ImportedRoomRuntimeAsset legacySampleRoomAsset,
            BranchRoomTemplateCatalogDefinition catalog,
            int seed,
            out string error)
        {
            var fixturePool = new Dictionary<string, ImportedRoomRuntimeAsset>();
            var approvedPool = new Dictionary<string, ImportedRoomRuntimeAsset>();
            var roomPool = new Dictionary<string, ImportedRoomRuntimeAsset>();
            var biomePools = new Dictionary<string, IReadOnlyDictionary<string, ImportedRoomRuntimeAsset>>();
            error = string.Empty;
            if (catalog != null)
            {
                foreach (var template in catalog.FixtureTemplates)
                {
                    if (template == null)
                    {
                        continue;
                    }

                    if (HollowRuntimeV2Importer.TryImport(template.text, out var asset, out var importError))
                    {
                        if (string.IsNullOrWhiteSpace(asset.Id))
                        {
                            AppendError(ref error, $"Fixture room '{template.name}' is missing canonicalRoomId.");
                            continue;
                        }

                        fixturePool[asset.Id] = asset;
                        roomPool[asset.Id] = asset;
                    }
                    else
                    {
                        AppendError(ref error, importError);
                    }
                }

                var approvedReport = ApprovedDesignerRoomImporter.ImportApprovedRooms(catalog.AdditionalTemplates);
                foreach (var approvedError in approvedReport.Errors)
                {
                    AppendError(ref error, approvedError);
                }

                foreach (var asset in approvedReport.ValidRooms.OrderBy(asset => asset.Id))
                {
                    if (roomPool.ContainsKey(asset.Id))
                    {
                        AppendError(ref error, $"Approved room canonicalRoomId '{asset.Id}' duplicates an existing branch template.");
                        continue;
                    }

                    approvedPool[asset.Id] = asset;
                    roomPool[asset.Id] = asset;
                }
            }

            var biomeCatalog = RoomBiomeCatalogDefinition.LoadDefault();
            if (biomeCatalog != null)
            {
                foreach (var biome in biomeCatalog.Biomes)
                {
                    if (biome == null)
                    {
                        continue;
                    }

                    var biomePool = ImportBiomePool(biome, ref error);
                    if (biomePool.Count > 0)
                    {
                        biomePools[biome.BiomeId] = biomePool;
                    }
                }
            }

            return new BranchSessionContent(
                legacySampleRoomAsset,
                fixturePool,
                approvedPool,
                roomPool,
                biomePools,
                seed == 0 ? catalog?.DefaultSeed ?? BranchGenerator.DefaultMacroFixtureSeed : seed);
        }

        private static Dictionary<string, ImportedRoomRuntimeAsset> ImportBiomePool(RoomBiomeDefinition biome, ref string error)
        {
            var pool = new Dictionary<string, ImportedRoomRuntimeAsset>();
            foreach (var template in biome.RoomTemplates)
            {
                if (template == null)
                {
                    continue;
                }

                if (!HollowRuntimeV2Importer.TryImport(template.text, out var asset, out var importError))
                {
                    AppendError(ref error, $"Biome '{biome.BiomeId}' template '{template.name}' import failed: {importError}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(asset.Id))
                {
                    AppendError(ref error, $"Biome '{biome.BiomeId}' template '{template.name}' is missing canonicalRoomId.");
                    continue;
                }

                pool[asset.Id] = asset;
            }

            return pool;
        }

        private static bool HasCompleteShapeCoverage(IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> pool)
        {
            if (pool == null)
            {
                return false;
            }

            var shapes = pool.Values
                .Where(asset => asset != null)
                .Select(asset => RoomFootprintShapeUtility.Classify(asset.Footprint))
                .ToHashSet();
            return shapes.Contains(RoomFootprintShape.Single1x1) &&
                   shapes.Contains(RoomFootprintShape.Wide2x1) &&
                   shapes.Contains(RoomFootprintShape.Tall1x2) &&
                   shapes.Contains(RoomFootprintShape.Block2x2) &&
                   shapes.Contains(RoomFootprintShape.L3Cell);
        }

        private static void AppendError(ref string error, string nextError)
        {
            if (string.IsNullOrWhiteSpace(nextError))
            {
                return;
            }

            error = string.IsNullOrWhiteSpace(error) ? nextError : $"{error}; {nextError}";
        }
    }
}
