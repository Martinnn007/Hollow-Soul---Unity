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
    public static class Milestone45AssetGenerator
    {
        public const string ProfilePath = "Assets/_Hollow/Resources/Hollow/Combat/RoomHazardTuningProfile_M45.asset";
        public const string ReportPath = "output/reports/m45_room_hazards_interactive_physics_v1.md";

        private static readonly VfxCueId[] M45VfxCues =
        {
            VfxCueId.HazardHit,
            VfxCueId.BarrelBreak,
            VfxCueId.BarrelExplode,
            VfxCueId.HazardCoinDrop
        };

        private static readonly AudioCueId[] M45AudioCues =
        {
            AudioCueId.HazardHit,
            AudioCueId.BarrelBreak,
            AudioCueId.BarrelExplode,
            AudioCueId.HazardCoinDrop
        };

        [MenuItem("Hollow/Generation/Generate Milestone 45 Assets")]
        public static void Generate()
        {
            Milestone44AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath) ?? "Assets/_Hollow/Resources/Hollow/Combat");
            Directory.CreateDirectory(Milestone9AssetGenerator.VfxCueDirectory);
            Directory.CreateDirectory(Milestone9AssetGenerator.AudioCueDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var profile = GenerateHazardProfile();
            var vfxCues = GenerateM45VfxCues();
            var audioCues = GenerateM45AudioCues();
            UpdatePresentationCatalog(vfxCues, audioCues);
            WriteReport(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 45 hazard tuning profile, cue hooks, and validation report.");
        }

        private static RoomHazardTuningProfileDefinition GenerateHazardProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<RoomHazardTuningProfileDefinition>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<RoomHazardTuningProfileDefinition>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.ConfigureM45Defaults();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static VfxCueDefinition[] GenerateM45VfxCues()
        {
            var cues = new List<VfxCueDefinition>();
            foreach (var cueId in M45VfxCues)
            {
                var path = $"{Milestone9AssetGenerator.VfxCueDirectory}/VfxCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<VfxCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<VfxCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, null, DebugColorFor(cueId), cueId == VfxCueId.BarrelExplode ? 0.22f : 0.14f, nextCreateDebugPrimitive: true);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static AudioCueDefinition[] GenerateM45AudioCues()
        {
            var cues = new List<AudioCueDefinition>();
            foreach (var cueId in M45AudioCues)
            {
                var path = $"{Milestone9AssetGenerator.AudioCueDirectory}/AudioCue_{cueId}.asset";
                var cue = AssetDatabase.LoadAssetAtPath<AudioCueDefinition>(path);
                if (cue == null)
                {
                    cue = ScriptableObject.CreateInstance<AudioCueDefinition>();
                    AssetDatabase.CreateAsset(cue, path);
                }

                cue.Configure(cueId, null, 0.55f, 0.6f);
                EditorUtility.SetDirty(cue);
                cues.Add(cue);
            }

            return cues.ToArray();
        }

        private static void UpdatePresentationCatalog(VfxCueDefinition[] m45VfxCues, AudioCueDefinition[] m45AudioCues)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var vfx = (catalog.VfxCues ?? Array.Empty<VfxCueDefinition>())
                .Where(cue => cue != null && !m45VfxCues.Any(next => next.CueId == cue.CueId))
                .Concat(m45VfxCues)
                .ToArray();
            var audio = (catalog.AudioCues ?? Array.Empty<AudioCueDefinition>())
                .Where(cue => cue != null && !m45AudioCues.Any(next => next.CueId == cue.CueId))
                .Concat(m45AudioCues)
                .ToArray();
            catalog.Configure(catalog.MaterialPalette, vfx, audio, catalog.PrefabBindings);
            PresentationContentProvider.Configure(catalog);
            EditorUtility.SetDirty(catalog);
        }

        private static Color DebugColorFor(VfxCueId cueId)
        {
            return cueId switch
            {
                VfxCueId.HazardHit => MaterialResolver.FallbackColorFor(MaterialRole.RoomHazardSpike),
                VfxCueId.BarrelBreak => MaterialResolver.FallbackColorFor(MaterialRole.RoomBarrel),
                VfxCueId.BarrelExplode => MaterialResolver.FallbackColorFor(MaterialRole.RoomExplosiveBarrel),
                VfxCueId.HazardCoinDrop => MaterialResolver.FallbackColorFor(MaterialRole.HazardCoinDrop),
                _ => MaterialResolver.FallbackColorFor(MaterialRole.VfxDebug)
            };
        }

        private static void WriteReport(RoomHazardTuningProfileDefinition profile)
        {
            File.WriteAllText(
                ReportPath,
                "# M45 Room Hazards + Interactive Physics V1\n\n" +
                $"- Spikes: `{profile.SpikeDamage}` damage, `{profile.SpikeCooldownSeconds:0.##}s` target cooldown.\n" +
                $"- Barrels: `{profile.BarrelHealth}` HP; standard barrels block movement/projectiles and may drop `{profile.StandardBarrelCoinDropAmount}` coin at `{profile.StandardBarrelCoinDropChancePercent}%`.\n" +
                $"- Explosive barrels: `{profile.ExplosionRadiusMeters:0.##}m` radius, `{profile.ExplosiveBarrelDamage}` enemy damage, `{profile.ExplosiveBarrelPlayerDamage}` player damage, boss multiplier `{profile.BossExplosionDamageMultiplier:0.##}`.\n" +
                "- Starter/origin rooms clear hazards at runtime, so branch starters remain safe.\n");
        }
    }
}
