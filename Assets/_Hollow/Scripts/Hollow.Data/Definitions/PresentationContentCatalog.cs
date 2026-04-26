using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Presentation/Presentation Content Catalog", fileName = "PresentationContentCatalog")]
    public sealed class PresentationContentCatalog : ScriptableObject
    {
        [SerializeField] private MaterialPaletteDefinition materialPalette;
        [SerializeField] private VfxCueDefinition[] vfxCues = Array.Empty<VfxCueDefinition>();
        [SerializeField] private AudioCueDefinition[] audioCues = Array.Empty<AudioCueDefinition>();
        [SerializeField] private PresentationPrefabBinding[] prefabBindings = Array.Empty<PresentationPrefabBinding>();

        public MaterialPaletteDefinition MaterialPalette => materialPalette;

        public VfxCueDefinition[] VfxCues => vfxCues;

        public AudioCueDefinition[] AudioCues => audioCues;

        public PresentationPrefabBinding[] PrefabBindings => prefabBindings;

        public bool TryGetVfxCue(VfxCueId cueId, out VfxCueDefinition definition)
        {
            foreach (var cue in vfxCues)
            {
                if (cue != null && cue.CueId == cueId)
                {
                    definition = cue;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public bool TryGetAudioCue(AudioCueId cueId, out AudioCueDefinition definition)
        {
            foreach (var cue in audioCues)
            {
                if (cue != null && cue.CueId == cueId)
                {
                    definition = cue;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public void Configure(MaterialPaletteDefinition nextPalette, VfxCueDefinition[] nextVfxCues, AudioCueDefinition[] nextAudioCues)
        {
            Configure(nextPalette, nextVfxCues, nextAudioCues, prefabBindings);
        }

        public bool TryGetPrefab(PresentationPrefabRole role, out GameObject prefab)
        {
            foreach (var binding in prefabBindings)
            {
                if (binding.Role == role && binding.Prefab != null)
                {
                    prefab = binding.Prefab;
                    return true;
                }
            }

            prefab = null;
            return false;
        }

        public void Configure(
            MaterialPaletteDefinition nextPalette,
            VfxCueDefinition[] nextVfxCues,
            AudioCueDefinition[] nextAudioCues,
            PresentationPrefabBinding[] nextPrefabBindings)
        {
            materialPalette = nextPalette;
            vfxCues = nextVfxCues ?? Array.Empty<VfxCueDefinition>();
            audioCues = nextAudioCues ?? Array.Empty<AudioCueDefinition>();
            prefabBindings = nextPrefabBindings ?? Array.Empty<PresentationPrefabBinding>();
        }
    }
}
