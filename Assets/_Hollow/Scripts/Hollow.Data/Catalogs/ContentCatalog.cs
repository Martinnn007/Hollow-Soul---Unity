using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Data.Catalogs
{
    [CreateAssetMenu(menuName = "Hollow/Content Catalog", fileName = "ContentCatalog")]
    public sealed class ContentCatalog : ScriptableObject
    {
        [SerializeField] private WorldDefinition[] worlds;
        [SerializeField] private PlatformPresentationProfile[] platformPresentationProfiles;
        [SerializeField] private PlatformPolishProfileDefinition[] platformPolishProfiles;
        [SerializeField] private PresentationContentCatalog presentationContent;

        public WorldDefinition[] Worlds => worlds;

        public PlatformPresentationProfile[] PlatformPresentationProfiles => platformPresentationProfiles;

        public PlatformPolishProfileDefinition[] PlatformPolishProfiles => platformPolishProfiles;

        public PresentationContentCatalog PresentationContent => presentationContent;
    }
}
