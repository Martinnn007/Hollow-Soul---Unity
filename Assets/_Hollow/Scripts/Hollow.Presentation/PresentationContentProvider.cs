using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public static class PresentationContentProvider
    {
        public const string DefaultCatalogResourcePath = "Hollow/Presentation/PresentationContentCatalog";

        private static PresentationContentCatalog activeCatalog;
        private static bool attemptedResourceLoad;

        public static PresentationContentCatalog ActiveCatalog
        {
            get
            {
                if (activeCatalog == null && !attemptedResourceLoad)
                {
                    attemptedResourceLoad = true;
                    activeCatalog = Resources.Load<PresentationContentCatalog>(DefaultCatalogResourcePath);
                }

                return activeCatalog;
            }
        }

        public static void Configure(PresentationContentCatalog catalog)
        {
            activeCatalog = catalog;
            attemptedResourceLoad = true;
            MaterialResolver.ClearCache();
        }

        public static void Reset()
        {
            activeCatalog = null;
            attemptedResourceLoad = false;
            MaterialResolver.ClearCache();
        }
    }
}
