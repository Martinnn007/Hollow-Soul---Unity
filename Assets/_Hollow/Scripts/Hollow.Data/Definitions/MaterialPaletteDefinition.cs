using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Presentation/Material Palette", fileName = "MaterialPalette")]
    public sealed class MaterialPaletteDefinition : HollowDefinition
    {
        [SerializeField] private MaterialRoleBinding[] bindings = Array.Empty<MaterialRoleBinding>();
        [NonSerialized] private Dictionary<MaterialRole, Material> materialLookup;
        [NonSerialized] private Dictionary<MaterialRole, Color> fallbackColorLookup;

        public MaterialRoleBinding[] Bindings => bindings;

        public bool TryResolve(MaterialRole role, out Material material)
        {
            EnsureLookupCache();
            return materialLookup.TryGetValue(role, out material) && material != null;
        }

        public bool TryGetFallbackColor(MaterialRole role, out Color color)
        {
            EnsureLookupCache();
            if (fallbackColorLookup.TryGetValue(role, out color))
            {
                return true;
            }

            color = Color.white;
            return false;
        }

        public void Configure(MaterialRoleBinding[] nextBindings)
        {
            bindings = nextBindings ?? Array.Empty<MaterialRoleBinding>();
            ClearLookupCache();
        }

        private void OnEnable()
        {
            ClearLookupCache();
        }

        private void ClearLookupCache()
        {
            materialLookup = null;
            fallbackColorLookup = null;
        }

        private void EnsureLookupCache()
        {
            if (materialLookup != null && fallbackColorLookup != null)
            {
                return;
            }

            materialLookup = new Dictionary<MaterialRole, Material>();
            fallbackColorLookup = new Dictionary<MaterialRole, Color>();
            foreach (var binding in bindings ?? Array.Empty<MaterialRoleBinding>())
            {
                if (!fallbackColorLookup.ContainsKey(binding.Role))
                {
                    fallbackColorLookup.Add(binding.Role, binding.FallbackColor);
                }

                if (binding.Material != null && !materialLookup.ContainsKey(binding.Role))
                {
                    materialLookup.Add(binding.Role, binding.Material);
                }
            }
        }
    }

    [Serializable]
    public struct MaterialRoleBinding
    {
        [SerializeField] private MaterialRole role;
        [SerializeField] private Material material;
        [SerializeField] private Color fallbackColor;

        public MaterialRole Role => role;

        public Material Material => material;

        public Color FallbackColor => fallbackColor;

        public MaterialRoleBinding(MaterialRole role, Material material, Color fallbackColor)
        {
            this.role = role;
            this.material = material;
            this.fallbackColor = fallbackColor;
        }
    }
}
