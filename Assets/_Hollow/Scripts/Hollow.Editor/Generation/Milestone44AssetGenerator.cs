using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone44AssetGenerator
    {
        public const string ProfilePath = "Assets/_Hollow/Resources/Hollow/Combat/ShieldGuardProfile_M44.asset";
        public const string ReportPath = "output/reports/m44_shield_armor_behavior_v2.md";

        private static readonly VfxCueId[] M44VfxCues =
        {
            VfxCueId.ShieldGuardStart,
            VfxCueId.ShieldBlock,
            VfxCueId.ShieldParryCounter,
            VfxCueId.ShieldUnavailable
        };

        private static readonly AudioCueId[] M44AudioCues =
        {
            AudioCueId.ShieldGuardStart,
            AudioCueId.ShieldBlock,
            AudioCueId.ShieldParryCounter,
            AudioCueId.ShieldUnavailable
        };

        [MenuItem("Hollow/Generation/Generate Milestone 44 Assets")]
        public static void Generate()
        {
            Milestone43AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath) ?? "Assets/_Hollow/Resources/Hollow/Combat");
            Directory.CreateDirectory(Milestone9AssetGenerator.VfxCueDirectory);
            Directory.CreateDirectory(Milestone9AssetGenerator.AudioCueDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var profile = GenerateShieldProfile();
            var vfxCues = GenerateM44VfxCues();
            var audioCues = GenerateM44AudioCues();
            UpdatePresentationCatalog(vfxCues, audioCues);
            WriteReport(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 44 shield guard profile, cue hooks, validation report, and fallback presentation data.");
        }

        private static ShieldGuardProfileDefinition GenerateShieldProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<ShieldGuardProfileDefinition>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ShieldGuardProfileDefinition>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.ConfigureM44Defaults();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static VfxCueDefinition[] GenerateM44VfxCues()
        {
            var cues = new List<VfxCueDefinition>();
            foreach (var cueId in M44VfxCues)
            {
                var path = $"{Milestone9AssetGenerator.VfxCueDirectory}/VfxCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<VfxCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<VfxCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, null, DebugColorFor(cueId), DebugScaleFor(cueId), nextCreateDebugPrimitive: true);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static AudioCueDefinition[] GenerateM44AudioCues()
        {
            var cues = new List<AudioCueDefinition>();
            foreach (var cueId in M44AudioCues)
            {
                var path = $"{Milestone9AssetGenerator.AudioCueDirectory}/AudioCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<AudioCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<AudioCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, null, 0.55f, 0.55f);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static void UpdatePresentationCatalog(VfxCueDefinition[] m44VfxCues, AudioCueDefinition[] m44AudioCues)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var vfx = (catalog.VfxCues ?? Array.Empty<VfxCueDefinition>())
                .Where(cue => cue != null && !m44VfxCues.Any(next => next.CueId == cue.CueId))
                .Concat(m44VfxCues)
                .ToArray();
            var audio = (catalog.AudioCues ?? Array.Empty<AudioCueDefinition>())
                .Where(cue => cue != null && !m44AudioCues.Any(next => next.CueId == cue.CueId))
                .Concat(m44AudioCues)
                .ToArray();
            catalog.Configure(catalog.MaterialPalette, vfx, audio, catalog.PrefabBindings);
            PresentationContentProvider.Configure(catalog);
            EditorUtility.SetDirty(catalog);
        }

        private static Color DebugColorFor(VfxCueId cueId)
        {
            return cueId switch
            {
                VfxCueId.ShieldGuardStart => new Color(0.22f, 0.78f, 1f, 0.58f),
                VfxCueId.ShieldBlock => new Color(0.78f, 0.92f, 1f, 0.78f),
                VfxCueId.ShieldParryCounter => new Color(0.45f, 1f, 0.72f, 0.9f),
                VfxCueId.ShieldUnavailable => new Color(0.95f, 0.28f, 0.2f, 0.75f),
                _ => MaterialResolver.FallbackColorFor(MaterialRole.VfxDebug)
            };
        }

        private static float DebugScaleFor(VfxCueId cueId)
        {
            return cueId == VfxCueId.ShieldParryCounter ? 0.22f : 0.14f;
        }

        private static void WriteReport(ShieldGuardProfileDefinition profile)
        {
            File.WriteAllText(
                ReportPath,
                "# M44 Shield / Armor Behavior V2\n\n" +
                $"- Parry window: `{profile.ParryWindowSeconds:0.##}s`; guard cone: `{profile.GuardConeDegrees:0} degrees`.\n" +
                $"- Guard stamina drain/cost: `{profile.GuardDrainStaminaPerSecond:0}/s` / `{profile.GuardHitStaminaCost:0}`; parry cost: `{profile.ParryStaminaCost:0}`.\n" +
                $"- Guard movement multiplier: `x{profile.GuardMoveMultiplier:0.##}`.\n" +
                $"- Parry counter damage: `{profile.ParryCounterDamage}`; armor remains stat-only in M44.\n" +
                "- No shield inventory, projectile reflection, hit-stop, or camera shake is generated in this milestone.\n");
        }
    }
}
