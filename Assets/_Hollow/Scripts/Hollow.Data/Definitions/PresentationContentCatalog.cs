using System;
using System.Collections.Generic;
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
        [NonSerialized] private Dictionary<VfxCueId, VfxCueDefinition> vfxCueLookup;
        [NonSerialized] private Dictionary<AudioCueId, AudioCueDefinition> audioCueLookup;
        [NonSerialized] private Dictionary<PresentationPrefabRole, GameObject> prefabLookup;

        public MaterialPaletteDefinition MaterialPalette => materialPalette;

        public VfxCueDefinition[] VfxCues => vfxCues;

        public AudioCueDefinition[] AudioCues => audioCues;

        public PresentationPrefabBinding[] PrefabBindings => prefabBindings;

        public bool TryGetVfxCue(VfxCueId cueId, out VfxCueDefinition definition)
        {
            EnsureLookupCache();
            return vfxCueLookup.TryGetValue(cueId, out definition) && definition != null;
        }

        public bool TryGetAudioCue(AudioCueId cueId, out AudioCueDefinition definition)
        {
            EnsureLookupCache();
            return audioCueLookup.TryGetValue(cueId, out definition) && definition != null;
        }

        public void Configure(MaterialPaletteDefinition nextPalette, VfxCueDefinition[] nextVfxCues, AudioCueDefinition[] nextAudioCues)
        {
            Configure(nextPalette, nextVfxCues, nextAudioCues, prefabBindings);
        }

        public bool TryGetPrefab(PresentationPrefabRole role, out GameObject prefab)
        {
            EnsureLookupCache();
            return prefabLookup.TryGetValue(role, out prefab) && prefab != null;
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
            ClearLookupCache();
        }

        private void OnEnable()
        {
            ClearLookupCache();
        }

        private void ClearLookupCache()
        {
            vfxCueLookup = null;
            audioCueLookup = null;
            prefabLookup = null;
        }

        private void EnsureLookupCache()
        {
            if (vfxCueLookup != null && audioCueLookup != null && prefabLookup != null)
            {
                return;
            }

            vfxCueLookup = new Dictionary<VfxCueId, VfxCueDefinition>();
            foreach (var cue in vfxCues ?? Array.Empty<VfxCueDefinition>())
            {
                if (cue != null && !vfxCueLookup.ContainsKey(cue.CueId))
                {
                    vfxCueLookup.Add(cue.CueId, cue);
                }
            }

            audioCueLookup = new Dictionary<AudioCueId, AudioCueDefinition>();
            foreach (var cue in audioCues ?? Array.Empty<AudioCueDefinition>())
            {
                if (cue != null && !audioCueLookup.ContainsKey(cue.CueId))
                {
                    audioCueLookup.Add(cue.CueId, cue);
                }
            }

            prefabLookup = new Dictionary<PresentationPrefabRole, GameObject>();
            foreach (var binding in prefabBindings ?? Array.Empty<PresentationPrefabBinding>())
            {
                if (binding.Prefab != null && !prefabLookup.ContainsKey(binding.Role))
                {
                    prefabLookup.Add(binding.Role, binding.Prefab);
                }
            }
        }
    }
}
