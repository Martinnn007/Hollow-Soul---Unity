using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/World Definition", fileName = "WorldDefinition")]
    public sealed class WorldDefinition : HollowDefinition
    {
        [SerializeField] private Color presentationTint = Color.white;

        public Color PresentationTint => presentationTint;
    }
}
