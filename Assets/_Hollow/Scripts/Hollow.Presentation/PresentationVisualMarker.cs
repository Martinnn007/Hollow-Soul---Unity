using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public sealed class PresentationVisualMarker : MonoBehaviour
    {
        [SerializeField] private PresentationPrefabRole role;
        [SerializeField] private bool fallback;

        public PresentationPrefabRole Role => role;

        public bool IsFallback => fallback;

        public void Configure(PresentationPrefabRole nextRole, bool isFallback)
        {
            role = nextRole;
            fallback = isFallback;
        }
    }
}
