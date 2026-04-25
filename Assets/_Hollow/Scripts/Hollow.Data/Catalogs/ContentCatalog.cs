using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Data.Catalogs
{
    [CreateAssetMenu(menuName = "Hollow/Content Catalog", fileName = "ContentCatalog")]
    public sealed class ContentCatalog : ScriptableObject
    {
        [SerializeField] private WorldDefinition[] worlds;
        [SerializeField] private PlatformPresentationProfile[] platformPresentationProfiles;

        public WorldDefinition[] Worlds => worlds;

        public PlatformPresentationProfile[] PlatformPresentationProfiles => platformPresentationProfiles;
    }
}
