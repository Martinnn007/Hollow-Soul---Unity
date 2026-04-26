using System.Collections.Generic;
using Hollow.Data.Definitions;
using Hollow.Rooms;

namespace Hollow.Branches
{
    public sealed class BranchSessionContent
    {
        private BranchSessionContent(
            ImportedRoomRuntimeAsset legacySampleRoomAsset,
            IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> macroRoomPool,
            int branchSeed)
        {
            LegacySampleRoomAsset = legacySampleRoomAsset;
            MacroRoomPool = macroRoomPool ?? new Dictionary<string, ImportedRoomRuntimeAsset>();
            BranchSeed = branchSeed == 0 ? BranchGenerator.DefaultMacroFixtureSeed : branchSeed;
        }

        public ImportedRoomRuntimeAsset LegacySampleRoomAsset { get; }

        public IReadOnlyDictionary<string, ImportedRoomRuntimeAsset> MacroRoomPool { get; }

        public int BranchSeed { get; }

        public bool HasMacroFixturePool =>
            MacroRoomPool.ContainsKey("combat_macro_single_1x1") &&
            MacroRoomPool.ContainsKey("combat_macro_wide_2x1") &&
            MacroRoomPool.ContainsKey("combat_macro_tall_1x2") &&
            MacroRoomPool.ContainsKey("combat_macro_block_2x2") &&
            MacroRoomPool.ContainsKey("combat_macro_l_3cell");

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
            var roomPool = new Dictionary<string, ImportedRoomRuntimeAsset>();
            error = string.Empty;
            if (catalog != null)
            {
                foreach (var template in catalog.AllTemplates)
                {
                    if (template == null)
                    {
                        continue;
                    }

                    if (HollowRuntimeV2Importer.TryImport(template.text, out var asset, out var importError))
                    {
                        roomPool[asset.Id] = asset;
                    }
                    else
                    {
                        error = string.IsNullOrWhiteSpace(error) ? importError : $"{error}; {importError}";
                    }
                }
            }

            return new BranchSessionContent(
                legacySampleRoomAsset,
                roomPool,
                seed == 0 ? catalog?.DefaultSeed ?? BranchGenerator.DefaultMacroFixtureSeed : seed);
        }
    }
}
