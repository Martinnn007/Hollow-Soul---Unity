using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Room Designer/Curated Draft Catalog", fileName = "CuratedRoomDesignerDraftCatalog")]
    public sealed class RoomDesignerCuratedDraftCatalogDefinition : HollowDefinition
    {
        [SerializeField] private string catalogId = "curated_runtime_room_designer_drafts_v1";
        [SerializeField] private List<TextAsset> curatedDrafts = new();

        public string CatalogId => catalogId;

        public TextAsset[] CuratedDrafts => curatedDrafts?.Where(draft => draft != null).ToArray() ?? Array.Empty<TextAsset>();

        public void Configure(string nextCatalogId, IEnumerable<TextAsset> nextCuratedDrafts)
        {
            catalogId = string.IsNullOrWhiteSpace(nextCatalogId) ? "curated_runtime_room_designer_drafts_v1" : nextCatalogId;
            curatedDrafts = (nextCuratedDrafts ?? Enumerable.Empty<TextAsset>())
                .Where(draft => draft != null)
                .Distinct()
                .OrderBy(draft => draft.name, StringComparer.Ordinal)
                .ToList();
        }
    }
}
