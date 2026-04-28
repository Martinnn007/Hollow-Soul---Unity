using System;
using System.Collections.Generic;
using System.IO;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone37AssetGenerator
    {
        public const string BaselineReportPath = "output/reports/m37_enemy_boss_behavior_readability.md";

        [MenuItem("Hollow/Generation/Generate Milestone 37 Assets")]
        public static void Generate()
        {
            Milestone23AssetGenerator.Generate();
            Milestone30AssetGenerator.Generate();
            Milestone36AssetGenerator.Generate();
            PatchMaterialPalette(Milestone9AssetGenerator.PalettePath, Milestone9AssetGenerator.MaterialDirectory, "M_");
            PatchMaterialPalette(Milestone23AssetGenerator.ArtPassPalettePath, Milestone23AssetGenerator.ArtPassMaterialDirectory, "AP_M_");
            Directory.CreateDirectory(Path.GetDirectoryName(BaselineReportPath) ?? "output/reports");
            File.WriteAllText(
                BaselineReportPath,
                "# M37 Enemy/Boss Behavior Readability Pass\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                "- Scope: entry grace labels, charge windup, ranged windup, boss burst windup, and combat telegraph materials.\n" +
                "- Runtime authority remains unchanged: telegraphs are visual/readability state only, while damage and movement still run through combat controllers.\n" +
                "- Verification: run Milestone37Validator and the M32 QA gate.\n");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void PatchMaterialPalette(string palettePath, string materialDirectory, string materialPrefix)
        {
            Directory.CreateDirectory(materialDirectory);
            var palette = AssetDatabase.LoadAssetAtPath<MaterialPaletteDefinition>(palettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<MaterialPaletteDefinition>();
                AssetDatabase.CreateAsset(palette, palettePath);
            }

            var bindings = new List<MaterialRoleBinding>();
            foreach (MaterialRole role in Enum.GetValues(typeof(MaterialRole)))
            {
                var material = LoadOrCreateMaterial(materialDirectory, materialPrefix, role, MaterialResolver.FallbackColorFor(role));
                var color = material.color;
                material.color = color;
                EditorUtility.SetDirty(material);
                bindings.Add(new MaterialRoleBinding(role, material, color));
            }

            palette.Configure(bindings.ToArray());
            EditorUtility.SetDirty(palette);
        }

        private static Material LoadOrCreateMaterial(string materialDirectory, string materialPrefix, MaterialRole role, Color color)
        {
            var path = $"{materialDirectory}/{materialPrefix}{role}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = MaterialResolver.CreateRuntimeMaterial(color);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = $"{materialPrefix}{role}";
            return material;
        }
    }
}
