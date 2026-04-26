using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Presentation/VFX Cue", fileName = "VfxCue")]
    public sealed class VfxCueDefinition : HollowDefinition
    {
        [SerializeField] private VfxCueId cueId;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Color debugColor = Color.white;
        [SerializeField] private float debugScale = 0.16f;
        [SerializeField] private bool createDebugPrimitive;

        public VfxCueId CueId => cueId;

        public GameObject Prefab => prefab;

        public Color DebugColor => debugColor;

        public float DebugScale => debugScale;

        public bool CreateDebugPrimitive => createDebugPrimitive;

        public void Configure(VfxCueId nextCueId, GameObject nextPrefab, Color nextDebugColor, float nextDebugScale, bool nextCreateDebugPrimitive)
        {
            cueId = nextCueId;
            prefab = nextPrefab;
            debugColor = nextDebugColor;
            debugScale = Mathf.Max(0.01f, nextDebugScale);
            createDebugPrimitive = nextCreateDebugPrimitive;
        }
    }
}
