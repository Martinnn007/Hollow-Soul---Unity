using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Rooms/Room Biome Definition", fileName = "RoomBiomeDefinition")]
    public sealed class RoomBiomeDefinition : ScriptableObject
    {
        [SerializeField] private string biomeId = RoomBiomeIds.HollowThreshold;
        [SerializeField] private string displayName = "The Hollow Threshold";
        [SerializeField] private WorldBiomeTag[] biomeTags = { WorldBiomeTag.MixedThreshold };
        [SerializeField] private TextAsset[] roomTemplates = Array.Empty<TextAsset>();
        [SerializeField] private RoomBiomeMaterialOverride[] materialOverrides = Array.Empty<RoomBiomeMaterialOverride>();
        [SerializeField] private RoomBiomePrefabOverride[] prefabOverrides = Array.Empty<RoomBiomePrefabOverride>();
        [SerializeField] private RoomBiomeDecorBinding[] decorPrefabBindings = Array.Empty<RoomBiomeDecorBinding>();
        [NonSerialized] private Dictionary<MaterialRole, Material> materialLookup;
        [NonSerialized] private Dictionary<PresentationPrefabRole, GameObject> prefabLookup;
        [NonSerialized] private Dictionary<string, PresentationPrefabRole> decorLookup;

        public string BiomeId => RoomBiomeIds.Normalize(biomeId);

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? BiomeId : displayName;

        public IReadOnlyList<WorldBiomeTag> BiomeTags => biomeTags ?? Array.Empty<WorldBiomeTag>();

        public IReadOnlyList<TextAsset> RoomTemplates => roomTemplates ?? Array.Empty<TextAsset>();

        public IReadOnlyList<RoomBiomeMaterialOverride> MaterialOverrides => materialOverrides ?? Array.Empty<RoomBiomeMaterialOverride>();

        public IReadOnlyList<RoomBiomePrefabOverride> PrefabOverrides => prefabOverrides ?? Array.Empty<RoomBiomePrefabOverride>();

        public IReadOnlyList<RoomBiomeDecorBinding> DecorPrefabBindings => decorPrefabBindings ?? Array.Empty<RoomBiomeDecorBinding>();

        public bool TryResolve(MaterialRole role, out Material material)
        {
            EnsureLookupCache();
            return materialLookup.TryGetValue(role, out material) && material != null;
        }

        public bool TryResolve(PresentationPrefabRole role, out GameObject prefab)
        {
            EnsureLookupCache();
            return prefabLookup.TryGetValue(role, out prefab) && prefab != null;
        }

        public bool TryResolveDecorRole(string decorKind, out PresentationPrefabRole role)
        {
            EnsureLookupCache();
            var normalizedKind = RoomBiomeDecorKinds.Normalize(decorKind);
            if (decorLookup.TryGetValue(normalizedKind, out role))
            {
                return true;
            }

            return RoomBiomeDecorKinds.TryResolveDefaultPrefabRole(normalizedKind, out role);
        }

        public void Configure(
            string nextBiomeId,
            string nextDisplayName,
            IEnumerable<WorldBiomeTag> nextBiomeTags,
            IEnumerable<TextAsset> nextRoomTemplates,
            IEnumerable<RoomBiomeMaterialOverride> nextMaterialOverrides = null,
            IEnumerable<RoomBiomePrefabOverride> nextPrefabOverrides = null,
            IEnumerable<RoomBiomeDecorBinding> nextDecorPrefabBindings = null)
        {
            biomeId = RoomBiomeIds.Normalize(nextBiomeId);
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? biomeId : nextDisplayName.Trim();
            biomeTags = (nextBiomeTags ?? Array.Empty<WorldBiomeTag>()).Distinct().ToArray();
            roomTemplates = (nextRoomTemplates ?? Array.Empty<TextAsset>()).Where(template => template != null).Distinct().ToArray();
            materialOverrides = (nextMaterialOverrides ?? Array.Empty<RoomBiomeMaterialOverride>()).ToArray();
            prefabOverrides = (nextPrefabOverrides ?? Array.Empty<RoomBiomePrefabOverride>()).ToArray();
            decorPrefabBindings = (nextDecorPrefabBindings ?? Array.Empty<RoomBiomeDecorBinding>()).ToArray();
            ClearLookupCache();
        }

        private void OnEnable()
        {
            ClearLookupCache();
        }

        private void ClearLookupCache()
        {
            materialLookup = null;
            prefabLookup = null;
            decorLookup = null;
        }

        private void EnsureLookupCache()
        {
            if (materialLookup != null && prefabLookup != null && decorLookup != null)
            {
                return;
            }

            materialLookup = new Dictionary<MaterialRole, Material>();
            foreach (var binding in materialOverrides ?? Array.Empty<RoomBiomeMaterialOverride>())
            {
                if (binding.Material != null && !materialLookup.ContainsKey(binding.Role))
                {
                    materialLookup.Add(binding.Role, binding.Material);
                }
            }

            prefabLookup = new Dictionary<PresentationPrefabRole, GameObject>();
            foreach (var binding in prefabOverrides ?? Array.Empty<RoomBiomePrefabOverride>())
            {
                if (binding.Prefab != null && !prefabLookup.ContainsKey(binding.Role))
                {
                    prefabLookup.Add(binding.Role, binding.Prefab);
                }
            }

            decorLookup = new Dictionary<string, PresentationPrefabRole>(StringComparer.Ordinal);
            foreach (var binding in decorPrefabBindings ?? Array.Empty<RoomBiomeDecorBinding>())
            {
                var normalizedKind = RoomBiomeDecorKinds.Normalize(binding.DecorKind);
                if (!decorLookup.ContainsKey(normalizedKind))
                {
                    decorLookup.Add(normalizedKind, binding.PrefabRole);
                }
            }
        }
    }

    [Serializable]
    public struct RoomBiomeMaterialOverride
    {
        [SerializeField] private MaterialRole role;
        [SerializeField] private Material material;

        public RoomBiomeMaterialOverride(MaterialRole role, Material material)
        {
            this.role = role;
            this.material = material;
        }

        public MaterialRole Role => role;

        public Material Material => material;
    }

    [Serializable]
    public struct RoomBiomePrefabOverride
    {
        [SerializeField] private PresentationPrefabRole role;
        [SerializeField] private GameObject prefab;

        public RoomBiomePrefabOverride(PresentationPrefabRole role, GameObject prefab)
        {
            this.role = role;
            this.prefab = prefab;
        }

        public PresentationPrefabRole Role => role;

        public GameObject Prefab => prefab;
    }

    [Serializable]
    public struct RoomBiomeDecorBinding
    {
        [SerializeField] private string decorKind;
        [SerializeField] private PresentationPrefabRole prefabRole;

        public RoomBiomeDecorBinding(string decorKind, PresentationPrefabRole prefabRole)
        {
            this.decorKind = RoomBiomeDecorKinds.Normalize(decorKind);
            this.prefabRole = prefabRole;
        }

        public string DecorKind => decorKind ?? string.Empty;

        public PresentationPrefabRole PrefabRole => prefabRole;
    }
}
