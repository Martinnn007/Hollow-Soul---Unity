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
    public static class Milestone43AssetGenerator
    {
        public const string ProfilePath = "Assets/_Hollow/Resources/Hollow/Combat/CombatFeelProfile_M43.asset";
        public const string ReportPath = "output/reports/m43_combat_feel_damage_feedback.md";

        private static readonly VfxCueId[] M43VfxCues =
        {
            VfxCueId.PlayerInvulnerable,
            VfxCueId.KnockbackImpact,
            VfxCueId.EnemyWindup,
            VfxCueId.EnemyCorpseGhost,
            VfxCueId.DamageBlocked
        };

        private static readonly AudioCueId[] M43AudioCues =
        {
            AudioCueId.PlayerInvulnerable,
            AudioCueId.KnockbackImpact,
            AudioCueId.EnemyWindup,
            AudioCueId.EnemyCorpseGhost,
            AudioCueId.DamageBlocked
        };

        [MenuItem("Hollow/Generation/Generate Milestone 43 Assets")]
        public static void Generate()
        {
            Milestone42AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath) ?? "Assets/_Hollow/Resources/Hollow/Combat");
            Directory.CreateDirectory(Milestone9AssetGenerator.VfxCueDirectory);
            Directory.CreateDirectory(Milestone9AssetGenerator.AudioCueDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");

            var profile = GenerateCombatFeelProfile();
            var vfxCues = GenerateM43VfxCues();
            var audioCues = GenerateM43AudioCues();
            UpdatePresentationCatalog(vfxCues, audioCues);
            WriteReport(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 43 combat feel profile, cue hooks, validation report, and fallback presentation data.");
        }

        private static CombatFeelProfileDefinition GenerateCombatFeelProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CombatFeelProfileDefinition>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CombatFeelProfileDefinition>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            profile.ConfigureM43Defaults();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static VfxCueDefinition[] GenerateM43VfxCues()
        {
            var cues = new List<VfxCueDefinition>();
            foreach (var cueId in M43VfxCues)
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

        private static AudioCueDefinition[] GenerateM43AudioCues()
        {
            var cues = new List<AudioCueDefinition>();
            foreach (var cueId in M43AudioCues)
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

        private static void UpdatePresentationCatalog(VfxCueDefinition[] m43VfxCues, AudioCueDefinition[] m43AudioCues)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PresentationContentCatalog>();
                AssetDatabase.CreateAsset(catalog, Milestone9AssetGenerator.CatalogPath);
            }

            var vfx = (catalog.VfxCues ?? Array.Empty<VfxCueDefinition>())
                .Where(cue => cue != null && !m43VfxCues.Any(next => next.CueId == cue.CueId))
                .Concat(m43VfxCues)
                .ToArray();
            var audio = (catalog.AudioCues ?? Array.Empty<AudioCueDefinition>())
                .Where(cue => cue != null && !m43AudioCues.Any(next => next.CueId == cue.CueId))
                .Concat(m43AudioCues)
                .ToArray();
            catalog.Configure(catalog.MaterialPalette, vfx, audio, catalog.PrefabBindings);
            PresentationContentProvider.Configure(catalog);
            EditorUtility.SetDirty(catalog);
        }

        private static Color DebugColorFor(VfxCueId cueId)
        {
            return cueId switch
            {
                VfxCueId.PlayerInvulnerable => new Color(0.35f, 0.9f, 1f, 0.72f),
                VfxCueId.KnockbackImpact => new Color(1f, 0.92f, 0.45f, 0.85f),
                VfxCueId.EnemyWindup => new Color(1f, 0.55f, 0.12f, 0.72f),
                VfxCueId.EnemyCorpseGhost => new Color(0.62f, 0.78f, 0.86f, 0.42f),
                VfxCueId.DamageBlocked => new Color(0.4f, 0.65f, 1f, 0.82f),
                _ => MaterialResolver.FallbackColorFor(MaterialRole.VfxDebug)
            };
        }

        private static float DebugScaleFor(VfxCueId cueId)
        {
            return cueId == VfxCueId.EnemyCorpseGhost ? 0.22f : 0.14f;
        }

        private static void WriteReport(CombatFeelProfileDefinition profile)
        {
            File.WriteAllText(
                ReportPath,
                "# M43 Combat Feel V2 + Damage Feedback\n\n" +
                $"- Player invulnerability: `{profile.PlayerInvulnerabilitySeconds:0.##}s`.\n" +
                $"- Player knockback: `{profile.PlayerKnockbackMeters:0.##}m`; enemy melee/projectile knockback: `{profile.EnemyMeleeKnockbackMeters:0.##}m` / `{profile.EnemyProjectileKnockbackMeters:0.##}m`.\n" +
                $"- Boss/heavy knockback resistance multipliers: `{profile.BossEnemyKnockbackMultiplier:0.##}` / `{profile.HeavyEnemyKnockbackMultiplier:0.##}`.\n" +
                $"- Corpse ghost linger: `{profile.CorpseGhostSeconds:0.##}s`.\n" +
                "- No hit-stop, camera shake, or damage-number UI is generated in this milestone.\n");
        }
    }
}
