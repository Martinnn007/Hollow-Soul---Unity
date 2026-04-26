using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Presentation/Material Palette", fileName = "MaterialPalette")]
    public sealed class MaterialPaletteDefinition : HollowDefinition
    {
        [SerializeField] private MaterialRoleBinding[] bindings = Array.Empty<MaterialRoleBinding>();

        public MaterialRoleBinding[] Bindings => bindings;

        public bool TryResolve(MaterialRole role, out Material material)
        {
            foreach (var binding in bindings)
            {
                if (binding.Role == role && binding.Material != null)
                {
                    material = binding.Material;
                    return true;
                }
            }

            material = null;
            return false;
        }

        public bool TryGetFallbackColor(MaterialRole role, out Color color)
        {
            foreach (var binding in bindings)
            {
                if (binding.Role == role)
                {
                    color = binding.FallbackColor;
                    return true;
                }
            }

            color = Color.white;
            return false;
        }

        public void Configure(MaterialRoleBinding[] nextBindings)
        {
            bindings = nextBindings ?? Array.Empty<MaterialRoleBinding>();
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
