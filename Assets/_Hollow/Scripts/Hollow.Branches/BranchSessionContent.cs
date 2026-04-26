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
            int branchSeed)
        {
            LegacySampleRoomAsset = legacySampleRoomAsset;
            FixtureRoomPool = fixtureRoomPool ?? new Dictionary<string, ImportedRoomRuntimeAsset>();
            ApprovedRoomPool = approvedRoomPool ?? new Dictionary<string, ImportedRoomRuntimeAsset>();
            MacroRoomPool = macroRoomPool ?? new Dictionary<string, ImportedRoomRuntimeAsset>();
            BranchSeed = branchSeed == 0 ? BranchGenerator.DefaultMacroFixtureSeed : branchSeed;
        }

        public ImportedRoomRuntimeAsset LegacySampleRoomAsset { get; }

        public IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> FixtureRoomPool { get; }

        public IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> ApprovedRoomPool { get; }

        public IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> MacroRoomPool { get; }

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

            asset = LegacySampleRoomAsset;
            return asset != null;
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

            return new BranchSessionContent(
                legacySampleRoomAsset,
                fixturePool,
                approvedPool,
                roomPool,
                seed == 0 ? catalog?.DefaultSeed ?? BranchGenerator.DefaultMacroFixtureSeed : seed);
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
