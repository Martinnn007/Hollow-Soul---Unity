using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.UI.Shell;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class Milestone50AssetGenerator
    {
        public const string CatalogId = "m50_hollow_star_world_identity_catalog_v2";
        public const string WorldIdentityDirectory = "Assets/_Hollow/Data/Worlds/M50";
        public const string FramingDirectory = WorldIdentityDirectory + "/Framing";
        public const string RunFramingCatalogPath = WorldIdentityDirectory + "/RunFramingCatalog_M50.asset";
        public const string ReportPath = "output/reports/m50_story_world_identity_run_framing_v2.md";
        public const string DocsPath = "Docs/Milestone50StoryWorldIdentityRunFramingV2.md";

        private static readonly string[] GameScenes =
        {
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity",
            "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 50 Assets")]
        public static void Generate()
        {
            Milestone49AssetGenerator.Generate();
            EnsureDirectories();
            var definitions = CreateWorldDefinitions();
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(RunFramingCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<RunFramingCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, RunFramingCatalogPath);
            }

            catalog.Configure(CatalogId, definitions);
            EditorUtility.SetDirty(catalog);
            AssignToGameScenes(catalog);
            WriteReport(definitions);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 50 world identity catalog with {definitions.Count} Hollow Star worlds.");
        }

        public static IReadOnlyList<RunFramingDefinition> CreateWorldDefinitions()
        {
            return Specs.Select(SaveWorld).ToArray();
        }

        private static IReadOnlyList<WorldSpec> Specs => new[]
        {
            new WorldSpec(
                "broken_meridian",
                1,
                "The Broken Meridian",
                "A mixed threshold where timelines scrape against the same door.",
                new[] { WorldBiomeTag.MixedThreshold, WorldBiomeTag.ShatteredTimeline, WorldBiomeTag.Memory },
                "cold slate, bruised green, dead-gold seams",
                "thin dawn light cutting through black dust",
                "fractured stone, mismatched eras, memory-glass edges",
                "The Hollow Star has folded a thousand first steps into one room.",
                "Branches stitch old minutes to new wounds.",
                "The memory anchor holds, but it does not forgive weight.",
                "The warden remembers a road that no longer exists.",
                "You slip from one collapse, not from the Star.",
                new[] { "Ash Suture", "Split Noon", "Clockless Gate", "Mourning Lane" }),
            new WorldSpec(
                "before_teeth",
                2,
                "Before Teeth",
                "Prehistoric hunger preserved before language learned mercy.",
                new[] { WorldBiomeTag.Prehistoric, WorldBiomeTag.ShatteredTimeline },
                "bone white, wet fern, tar black, volcanic ember",
                "low amber stormlight with deep leaf shadow",
                "fossil ribs, cracked clay, fern mats, obsidian teeth",
                "The old world breathes under your feet, huge and unfinished.",
                "Every branch is a jaw waiting to become history.",
                "The anchor smells of rain before extinction.",
                "Something ancient stands where fear first got a name.",
                "The beast-era sinks back under black gravity.",
                new[] { "Rib Orchard", "Tar Choir", "Fern Grave", "First Maw" }),
            new WorldSpec(
                "sunken_cartouche",
                3,
                "The Sunken Cartouche",
                "A drowned royal afterlife, rewritten by impossible tides.",
                new[] { WorldBiomeTag.AncientEgypt, WorldBiomeTag.Ritual, WorldBiomeTag.Memory },
                "lapis, sand-gold, oxidized teal, black river silt",
                "sun shafts through dust and water that should not be there",
                "carved stone, wet papyrus, cracked gold leaf, ritual ink",
                "A name is sealed in every wall, and none of them are yours.",
                "Branches open like tomb texts read in the wrong century.",
                "The anchor is a dry island in a drowned burial spell.",
                "The warden kneels before a crown the Star has already eaten.",
                "The cartouche closes. The name inside keeps scratching.",
                new[] { "Silt Throne", "Lapis Teeth", "False Nile", "Mummy Sun" }),
            new WorldSpec(
                "black_keep",
                4,
                "The Black Keep",
                "Medieval terror built from siege smoke and failed prayers.",
                new[] { WorldBiomeTag.MedievalTerror, WorldBiomeTag.Ritual },
                "iron black, candle ochre, dried blood, ash gray",
                "torch pockets swallowed by cold vaulted dark",
                "ironwork, torn banners, wet stone, wax, splintered oak",
                "The keep lowers its chains as if you were expected.",
                "Branches grind like portcullises between bad vows.",
                "The anchor is a chapel with no saint left in it.",
                "The warden guards a throne that punishes the sitter.",
                "The gate opens behind you. The keep does not call it mercy.",
                new[] { "Candle Gallows", "Iron Chapel", "Widow Wall", "Murder Bailey" }),
            new WorldSpec(
                "rust_choir",
                5,
                "The Rust Choir",
                "A fallen future still singing through broken machines.",
                new[] { WorldBiomeTag.FallenFuture, WorldBiomeTag.EndTimes },
                "rust orange, oil black, warning yellow, dead cyan",
                "failing neon under dust-heavy industrial haze",
                "corroded alloy, cracked screens, cable nests, coolant stains",
                "The future has died already. Its alarms keep practicing.",
                "Branches route power through rooms that forgot their purpose.",
                "The anchor hums like a generator under a battlefield.",
                "The warden is old code wearing stone like a uniform.",
                "The choir cuts out. Something beneath it keeps tone.",
                new[] { "Static Nave", "Oil Psalm", "Battery Grave", "Redline Atrium" }),
            new WorldSpec(
                "choir_below",
                6,
                "The Choir Below",
                "Hell and heaven collided, and both kept singing.",
                new[] { WorldBiomeTag.Hell, WorldBiomeTag.Heaven, WorldBiomeTag.Ritual },
                "sulfur red, pearl white, bruised violet, molten gold",
                "holy backlight broken by furnace smoke",
                "charred marble, feathers in ash, brass halos, cracked bone",
                "A hymn rises from below, too beautiful to trust.",
                "Branches braid punishment and blessing until both cut.",
                "The anchor is a confessional built in a furnace.",
                "The warden hears every prayer and answers with weight.",
                "You leave the hymn behind. It continues in your teeth.",
                new[] { "Ash Halo", "Mercy Furnace", "Seraph Pit", "Choir Wound" }),
            new WorldSpec(
                "last_hour",
                7,
                "The Last Hour",
                "The end of times, looped until even endings are tired.",
                new[] { WorldBiomeTag.EndTimes, WorldBiomeTag.ShatteredTimeline },
                "eclipse black, pale brass, smoke blue, dying crimson",
                "long sunset with stars visible through the floor",
                "broken clocks, meteor dust, glass sand, burnt paper",
                "The final hour has struck so often it has gone hoarse.",
                "Branches count down to doors that refuse to stay closed.",
                "The anchor is a minute hand nailed to nothing.",
                "The warden waits at the second before everything stops.",
                "The hour breaks. Another hour crawls out underneath.",
                new[] { "Eclipse Dial", "Meteor Choir", "Ash Calendar", "Zero Bell" }),
            new WorldSpec(
                "blind_deep",
                8,
                "The Blind Deep",
                "An abyss without horizon, hungry enough to become a god.",
                new[] { WorldBiomeTag.Abyss, WorldBiomeTag.Memory },
                "void black, cold blue, pale biolume, drowned silver",
                "narrow bioluminescent pools in pressure-dark silence",
                "basalt, salt crystal, slick shell, pressure-warped metal",
                "The Deep has no sky. The Hollow Star put one inside it.",
                "Branches drift like wreckage with doors still attached.",
                "The anchor is a breath held under impossible water.",
                "The warden moves by pressure, not sight.",
                "You surface for a moment. The abyss learns your shape.",
                new[] { "No-Sun Trench", "Salt Lung", "Blind Lantern", "Pressure Shrine" })
        };

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(WorldIdentityDirectory);
            Directory.CreateDirectory(FramingDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
        }

        private static RunFramingDefinition SaveWorld(WorldSpec spec)
        {
            var path = $"{FramingDirectory}/RunFraming_{spec.IdentityId}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<RunFramingDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RunFramingDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(
                spec.IdentityId,
                spec.WorldIndex,
                spec.DisplayName,
                spec.Subtitle,
                spec.BiomeTags,
                spec.PaletteHint,
                spec.LightingHint,
                spec.MaterialNotes,
                spec.PrologueLine,
                spec.BranchLine,
                spec.HubLine,
                spec.BossLine,
                spec.ExtractionLine,
                spec.BranchEchoNames);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void AssignToGameScenes(RunFramingCatalogDefinition catalog)
        {
            foreach (var scenePath in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var branch = Object.FindFirstObjectByType<BranchSessionController>();
                if (branch == null)
                {
                    throw new MissingComponentException($"{scenePath} is missing BranchSessionController.");
                }

                branch.ConfigureRunFramingCatalog(catalog);
                EditorUtility.SetDirty(branch);

                var shellCanvas = GameObject.Find("PlatformShellCanvas");
                if (shellCanvas == null)
                {
                    throw new MissingReferenceException($"{scenePath} is missing PlatformShellCanvas.");
                }

                var framingHud = shellCanvas.GetComponent<RunFramingHudController>();
                if (framingHud == null)
                {
                    framingHud = shellCanvas.AddComponent<RunFramingHudController>();
                }

                framingHud.Configure(catalog);
                EditorUtility.SetDirty(framingHud);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void WriteReport(IReadOnlyList<RunFramingDefinition> definitions)
        {
            var sampleItinerary = RunWorldItineraryService.ResolveItinerary(
                AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(RunFramingCatalogPath),
                15001,
                RunWorldItineraryService.DefaultMechanicalWorldCount);
            var markdown =
                "# M50 Story, World Identity, And Run Framing V2\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                $"- Catalog: `{RunFramingCatalogPath}`\n" +
                "- Catastrophe: `The Hollow Star` has eaten worlds and spat out mixed timelines.\n" +
                "- Scope: seeded three-world itinerary, cryptic world text, biome metadata, entry toasts, and hub branch echo labels.\n" +
                "- Non-goals: no biome filtering, encounter changes, rewards, saves, materials, difficulty, or branch mechanics.\n\n" +
                "## Active World Identities\n\n" +
                string.Join("\n", definitions.Select(definition => $"- `{definition.IdentityId}` - {definition.DisplayName}: {definition.Subtitle}")) +
                "\n\n## Sample Seed 15001 Itinerary\n\n" +
                string.Join("\n", sampleItinerary.Select((definition, index) => $"- World {index + 1}: {definition.DisplayName}")) +
                "\n\n## Runtime Notes\n\n" +
                "- `RunWorldItineraryService` resolves three distinct world identities from the root run seed.\n" +
                "- `RunFramingHudController` shows compact framing plus a short world-entry toast on world changes.\n" +
                "- Hub branch portals use current-world branch echo names when an M50 catalog is wired.\n";
            File.WriteAllText(ReportPath, markdown);
        }

        private readonly struct WorldSpec
        {
            public WorldSpec(
                string identityId,
                int worldIndex,
                string displayName,
                string subtitle,
                IReadOnlyList<WorldBiomeTag> biomeTags,
                string paletteHint,
                string lightingHint,
                string materialNotes,
                string prologueLine,
                string branchLine,
                string hubLine,
                string bossLine,
                string extractionLine,
                IReadOnlyList<string> branchEchoNames)
            {
                IdentityId = identityId;
                WorldIndex = worldIndex;
                DisplayName = displayName;
                Subtitle = subtitle;
                BiomeTags = biomeTags?.ToArray() ?? Array.Empty<WorldBiomeTag>();
                PaletteHint = paletteHint;
                LightingHint = lightingHint;
                MaterialNotes = materialNotes;
                PrologueLine = prologueLine;
                BranchLine = branchLine;
                HubLine = hubLine;
                BossLine = bossLine;
                ExtractionLine = extractionLine;
                BranchEchoNames = branchEchoNames?.ToArray() ?? Array.Empty<string>();
            }

            public string IdentityId { get; }
            public int WorldIndex { get; }
            public string DisplayName { get; }
            public string Subtitle { get; }
            public WorldBiomeTag[] BiomeTags { get; }
            public string PaletteHint { get; }
            public string LightingHint { get; }
            public string MaterialNotes { get; }
            public string PrologueLine { get; }
            public string BranchLine { get; }
            public string HubLine { get; }
            public string BossLine { get; }
            public string ExtractionLine { get; }
            public string[] BranchEchoNames { get; }
        }
    }
}
