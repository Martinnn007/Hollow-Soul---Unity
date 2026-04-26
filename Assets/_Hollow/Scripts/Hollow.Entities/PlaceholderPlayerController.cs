using UnityEngine;

namespace Hollow.Entities
{
    public sealed class PlaceholderPlayerController : MonoBehaviour
    {
        public const float DefaultHeightMeters = 1.78f;
        public const float DefaultRadiusMeters = 0.28f;

        [SerializeField] private float heightMeters = DefaultHeightMeters;

        public float HeightMeters => heightMeters;

        public void ConfigureDefault()
        {
            heightMeters = DefaultHeightMeters;
        }
    }
}
