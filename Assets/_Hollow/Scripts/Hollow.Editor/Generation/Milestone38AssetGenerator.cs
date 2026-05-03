using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone38AssetGenerator
    {
        public const string ArtPassSprintDirectory = "Assets/_Hollow/Data/ArtPass/M38";
        public const string TargetDirectory = ArtPassSprintDirectory + "/Targets";
        public const string TargetCatalogPath = ArtPassSprintDirectory + "/ArtPassTargetCatalog_M38.asset";
        public const string RafalIntakeDirectory = "Assets/_Hollow/Art/Intake/Rafal/M38";
        public const string RafalModelDirectory = "Assets/_Hollow/Art/Models/Rafal/M38";
        public const string RafalTextureDirectory = "Assets/_Hollow/Art/Textures/Rafal/M38";
        public const string RafalMaterialDirectory = "Assets/_Hollow/Art/Materials/Rafal/M38";
        public const string RafalPrefabDirectory = "Assets/_Hollow/Prefabs/ArtPass/Rafal/M38";
        public const string HandoffReportPath = "output/reports/m38_artpass_rafal_pipeline.md";

        [MenuItem("Hollow/Generation/Generate Milestone 38 Assets")]
        public static void Generate()
        {
            Milestone37AssetGenerator.Generate();
            EnsureDirectories();

            var targets = BuildTargets()
                .Select(SaveTarget)
                .ToArray();
            var catalog = AssetDatabase.LoadAssetAtPath<ArtPassTargetCatalogDefinition>(TargetCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArtPassTargetCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, TargetCatalogPath);
            }

            catalog.Configure("m38_artpass_rafal_targets_v1", targets);
            EditorUtility.SetDirty(catalog);

            WriteIntakeReadme(targets);
            WriteReport(targets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 38 ArtPass target catalog with {targets.Length} production targets.");
        }

        public static IReadOnlyList<PresentationPrefabRole> RequiredRuntimeRoles => new[]
        {
            PresentationPrefabRole.Player,
            PresentationPrefabRole.RoomFloor,
            PresentationPrefabRole.RoomObstacleRock,
            PresentationPrefabRole.DoorActive,
            PresentationPrefabRole.DoorLocked,
            PresentationPrefabRole.DoorCleared,
            PresentationPrefabRole.EnemyNormal,
            PresentationPrefabRole.EnemyBoss,
            PresentationPrefabRole.Projectile,
            PresentationPrefabRole.EnemyProjectile,
            PresentationPrefabRole.RewardPickup,
            PresentationPrefabRole.BossKeyPickup,
            PresentationPrefabRole.HubShop,
            PresentationPrefabRole.HubShopCard,
            PresentationPrefabRole.NextBranchPortal,
            PresentationPrefabRole.HubReturnPortal
        };

        private static void EnsureDirectories()
        {
            foreach (var directory in new[]
            {
                ArtPassSprintDirectory,
                TargetDirectory,
                RafalIntakeDirectory,
                RafalModelDirectory,
                RafalTextureDirectory,
                RafalMaterialDirectory,
                RafalPrefabDirectory,
                Path.GetDirectoryName(HandoffReportPath) ?? "output/reports"
            })
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static IEnumerable<TargetSpec> BuildTargets()
        {
            yield return Target("player_body", "Player Body", "Characters", PresentationPrefabRole.Player, ArtPassAssetTargetPriority.Critical, true, "Readable one-piece hero silhouette with subtle soul glow.", Scale("0.45m wide", "1.1m tall"));
            yield return Target("enemy_normal", "Enemy Normal", "Enemies", PresentationPrefabRole.EnemyNormal, ArtPassAssetTargetPriority.Critical, true, "Small corrupted toy/soul chaser silhouette.", Scale("0.55m wide", "0.65m tall"));
            yield return Target("enemy_flying", "Enemy Flying", "Enemies", PresentationPrefabRole.EnemyFlying, ArtPassAssetTargetPriority.High, true, "Hovering enemy with obvious flying read and no floor contact.", Scale("0.5m wide", "0.75m tall"));
            yield return Target("enemy_fast", "Enemy Fast", "Enemies", PresentationPrefabRole.EnemyFast, ArtPassAssetTargetPriority.High, true, "Lean fast enemy silhouette with strong direction/readability.", Scale("0.45m wide", "0.55m tall"));
            yield return Target("enemy_heavy", "Enemy Heavy", "Enemies", PresentationPrefabRole.EnemyHeavy, ArtPassAssetTargetPriority.High, true, "Chunky heavy enemy silhouette with slow/tanky read.", Scale("0.7m wide", "0.8m tall"));
            yield return Target("enemy_charger", "Enemy Charger", "Enemies", PresentationPrefabRole.EnemyCharger, ArtPassAssetTargetPriority.High, true, "Forward-leaning charger with visible windup direction.", Scale("0.65m wide", "0.65m tall"));
            yield return Target("enemy_turret", "Enemy Turret", "Enemies", PresentationPrefabRole.EnemyTurret, ArtPassAssetTargetPriority.High, true, "Stationary ranged enemy with readable eye/barrel direction.", Scale("0.65m wide", "0.7m tall"));
            yield return Target("enemy_splitter", "Enemy Splitter", "Enemies", PresentationPrefabRole.EnemySplitter, ArtPassAssetTargetPriority.High, true, "Enemy that visually suggests splitting/fragility.", Scale("0.55m wide", "0.65m tall"));
            yield return Target("enemy_spitting_pod", "Enemy Spitting Pod", "Enemies", PresentationPrefabRole.EnemySpittingPod, ArtPassAssetTargetPriority.High, true, "Stationary pod with upward lob/spit read and planted silhouette.", Scale("0.78m wide", "0.58m tall"));
            yield return Target("enemy_rat", "Enemy Rat", "Enemies", PresentationPrefabRole.EnemyRat, ArtPassAssetTargetPriority.High, true, "Small fast territorial critter with low profile and quick bite read.", Scale("0.46m wide", "0.22m tall"));
            yield return Target("enemy_spider", "Enemy Spider", "Enemies", PresentationPrefabRole.EnemySpider, ArtPassAssetTargetPriority.High, true, "Low skittish critter with leggy silhouette and hop-forward read.", Scale("0.5m wide", "0.2m tall"));
            yield return Target("enemy_hollow_bird", "Enemy Hollow Bird", "Enemies", PresentationPrefabRole.EnemyHollowBird, ArtPassAssetTargetPriority.High, true, "Small flying harasser with clear swoop, wing-retreat, and caw read.", Scale("0.48m wide", "0.28m tall"));
            yield return Target("enemy_hollow_beast", "Enemy Hollow Beast", "Enemies", PresentationPrefabRole.EnemyHollowBeast, ArtPassAssetTargetPriority.High, true, "Grounded creature with crouched leap-bite and body-check silhouette.", Scale("0.68m wide", "0.42m tall"));
            yield return Target("enemy_skeleton_sword", "Enemy Skeleton Sword", "Enemies", PresentationPrefabRole.EnemySkeletonSword, ArtPassAssetTargetPriority.High, true, "Light weapon-user with readable sword arm and follow-up slash stance.", Scale("0.54m wide", "0.78m tall"));
            yield return Target("enemy_skeleton_spear", "Enemy Skeleton Spear", "Enemies", PresentationPrefabRole.EnemySkeletonSpear, ArtPassAssetTargetPriority.High, true, "Longer weapon-user silhouette with clear spear lane read.", Scale("0.54m wide", "0.78m tall"));
            yield return Target("enemy_knight", "Enemy Knight", "Enemies", PresentationPrefabRole.EnemyKnight, ArtPassAssetTargetPriority.High, true, "Shield-bearing armored enemy with obvious frontal guard side.", Scale("0.68m wide", "0.98m tall"));
            yield return Target("enemy_giant", "Enemy Giant", "Enemies", PresentationPrefabRole.EnemyGiant, ArtPassAssetTargetPriority.High, true, "Large slow weapon-user with club/slam silhouette and heavy recovery read.", Scale("1.05m wide", "1.35m tall"));
            yield return Target("enemy_hollow_archer", "Enemy Hollow Archer", "Enemies", PresentationPrefabRole.EnemyHollowArcher, ArtPassAssetTargetPriority.High, true, "Bow-user silhouette with readable draw direction and narrow aim line.", Scale("0.52m wide", "0.82m tall"));
            yield return Target("enemy_powder_gunner", "Enemy Powder Gunner", "Enemies", PresentationPrefabRole.EnemyPowderGunner, ArtPassAssetTargetPriority.High, true, "Firearm enemy with obvious muzzle and long recovery stance.", Scale("0.62m wide", "0.86m tall"));
            yield return Target("enemy_knife_thrower", "Enemy Knife Thrower", "Enemies", PresentationPrefabRole.EnemyKnifeThrower, ArtPassAssetTargetPriority.High, true, "Agile thrower silhouette with raised throwing arm and evasive stance.", Scale("0.5m wide", "0.72m tall"));
            yield return Target("enemy_repeater_turret", "Enemy Repeater Turret", "Enemies", PresentationPrefabRole.EnemyRepeaterTurret, ArtPassAssetTargetPriority.High, true, "Stationary machine turret with rotating barrel/fan shot read.", Scale("0.78m wide", "0.66m tall"));
            yield return Target("enemy_clockwork_sentry", "Enemy Clockwork Sentry", "Enemies", PresentationPrefabRole.EnemyClockworkSentry, ArtPassAssetTargetPriority.High, true, "Slow machine with radial projectile pattern silhouette and exposed gear core.", Scale("0.82m wide", "0.92m tall"));
            yield return Target("enemy_hollow_acolyte", "Enemy Hollow Acolyte", "Enemies", PresentationPrefabRole.EnemyHollowAcolyte, ArtPassAssetTargetPriority.High, true, "Readable robe/catalyst caster silhouette with slow soul orb and rune burst tells.", Scale("0.56m wide", "0.78m tall"));
            yield return Target("enemy_wraith", "Enemy Wraith", "Enemies", PresentationPrefabRole.EnemyWraith, ArtPassAssetTargetPriority.High, true, "Ghost silhouette with visible phase-shift posture and harmless body overlap read.", Scale("0.5m wide", "0.86m tall"));
            yield return Target("enemy_soul_eater", "Enemy Soul Eater", "Enemies", PresentationPrefabRole.EnemySoulEater, ArtPassAssetTargetPriority.High, true, "Heavy occult predator with lane-drain cast posture and soul-burst core.", Scale("0.74m wide", "0.96m tall"));
            yield return Target("enemy_curse_binder", "Enemy Curse Binder", "Enemies", PresentationPrefabRole.EnemyCurseBinder, ArtPassAssetTargetPriority.High, true, "Territorial curse caster with sigil/fan hand shapes and punishable recovery stance.", Scale("0.58m wide", "0.82m tall"));
            yield return Target("enemy_grave_lantern", "Enemy Grave Lantern", "Enemies", PresentationPrefabRole.EnemyGraveLantern, ArtPassAssetTargetPriority.High, true, "Stationary magical pattern turret with lantern-core ownership and radial shot read.", Scale("0.72m wide", "0.9m tall"));
            yield return Target("boss_stone_warden", "Boss Stone Warden", "Boss", PresentationPrefabRole.EnemyBoss, ArtPassAssetTargetPriority.Critical, true, "Large guardian silhouette with readable charge/burst telegraph surfaces.", Scale("1.2m wide", "1.4m tall"));

            yield return Target("floor_tile", "Room Floor Tile", "Rooms", PresentationPrefabRole.RoomFloor, ArtPassAssetTargetPriority.Critical, true, "Dark toy-diorama floor module that remains readable under grid/lighting.", Scale("1m x 1m top", "0.08m thick"));
            yield return Target("rock_obstacle", "Rock Obstacle", "Rooms", PresentationPrefabRole.RoomObstacleRock, ArtPassAssetTargetPriority.Critical, true, "1m gameplay blocker visual, bottom sits exactly at y=0.", Scale("1m x 1m footprint", "1m tall"));
            yield return Target("door_active", "Door Active", "Doors", PresentationPrefabRole.DoorActive, ArtPassAssetTargetPriority.Critical, true, "Open/usable branch door state.", Scale("Fits 1m lane", "bottom at y=0"));
            yield return Target("door_locked", "Door Locked", "Doors", PresentationPrefabRole.DoorLocked, ArtPassAssetTargetPriority.Critical, true, "Clearly locked boss-key door state.", Scale("Fits 1m lane", "bottom at y=0"));
            yield return Target("door_cleared", "Door Cleared", "Doors", PresentationPrefabRole.DoorCleared, ArtPassAssetTargetPriority.Critical, true, "Cleared/unlocked door state with calm green read.", Scale("Fits 1m lane", "bottom at y=0"));
            yield return Target("door_unavailable", "Door Unavailable", "Doors", PresentationPrefabRole.DoorUnavailable, ArtPassAssetTargetPriority.High, true, "Dim blocked/unconnected door state.", Scale("Fits 1m lane", "bottom at y=0"));
            yield return Target("secret_door_debug", "Secret Door Debug", "Doors", PresentationPrefabRole.SecretDoorDebug, ArtPassAssetTargetPriority.Medium, false, "Visible prototype secret/debug door state.", Scale("Fits 1m lane", "bottom at y=0"));

            yield return Target("player_projectile", "Player Projectile", "Combat FX", PresentationPrefabRole.Projectile, ArtPassAssetTargetPriority.Critical, true, "Readable small projectile core, not visually confused with enemy shots.", Scale("0.25m diameter", "fast trail optional"));
            yield return Target("enemy_projectile", "Enemy Projectile", "Combat FX", PresentationPrefabRole.EnemyProjectile, ArtPassAssetTargetPriority.Critical, true, "Danger-colored projectile core with readable ownership.", Scale("0.25m diameter", "fast trail optional"));
            yield return Target("reward_pickup", "Reward Pickup", "Rewards", PresentationPrefabRole.RewardPickup, ArtPassAssetTargetPriority.Critical, true, "Generic pickup that reads as valuable from top-down and perspective.", Scale("0.45m diameter", "floor hover allowed"));
            yield return Target("boss_key", "Boss Key Pickup", "Rewards", PresentationPrefabRole.BossKeyPickup, ArtPassAssetTargetPriority.Critical, true, "Distinct key reward; must not look like a normal pickup.", Scale("0.5m wide", "0.6m tall"));

            yield return Target("hub_shop", "Hub Shop Stand", "Hub", PresentationPrefabRole.HubShop, ArtPassAssetTargetPriority.Critical, true, "Compact shop stand readable beside card offers.", Scale("1m wide", "0.8m tall"));
            yield return Target("hub_shop_card", "Hub Shop Card", "Hub", PresentationPrefabRole.HubShopCard, ArtPassAssetTargetPriority.Critical, true, "Reusable visible offer card frame behind generated text.", Scale("1m wide", "0.7m tall"));
            yield return Target("branch_portal", "Branch Portal", "Hub", PresentationPrefabRole.NextBranchPortal, ArtPassAssetTargetPriority.Critical, true, "Open branch portal, readable from hub camera.", Scale("0.8m diameter", "low profile"));
            yield return Target("next_world_portal", "Next World Portal", "Hub", PresentationPrefabRole.HubReturnPortal, ArtPassAssetTargetPriority.Critical, true, "Fourth right-side portal for deeper world/final extraction states.", Scale("0.95m diameter", "more important than branch portals"));

            yield return Target("melee_weapon", "Melee Weapon Pickup", "Equipment", PresentationPrefabRole.WeaponMelee, ArtPassAssetTargetPriority.High, false, "Generic visual wrapper for swords/blades until per-weapon art exists.", Scale("0.6m long", "pickup-safe"));
            yield return Target("ranged_weapon", "Ranged Weapon Pickup", "Equipment", PresentationPrefabRole.WeaponRanged, ArtPassAssetTargetPriority.High, false, "Generic visual wrapper for bows/bolts until per-weapon art exists.", Scale("0.6m wide", "pickup-safe"));
            yield return Target("armor_pickup", "Armor Pickup", "Equipment", PresentationPrefabRole.Armor, ArtPassAssetTargetPriority.High, false, "Generic armor/suit pickup wrapper.", Scale("0.65m wide", "0.7m tall"));
            yield return Target("active_item_pickup", "Active Item Pickup", "Items", PresentationPrefabRole.ActiveItemPickup, ArtPassAssetTargetPriority.High, false, "Active item pickup shell for charms/totems.", Scale("0.45m diameter", "pickup-safe"));
            yield return Target("consumable_card_pickup", "Consumable Card Pickup", "Items", PresentationPrefabRole.ConsumableCardPickup, ArtPassAssetTargetPriority.High, false, "Consumable card visual shell.", Scale("0.35m wide", "0.5m tall"));
            yield return Target("room_hazard_spike", "Spike Hazard", "Hazards", PresentationPrefabRole.RoomHazardSpike, ArtPassAssetTargetPriority.High, false, "Always-on floor hazard tile that reads clearly from perspective and top-down.", Scale("1m tile footprint", "low profile"));
            yield return Target("standard_barrel", "Standard Barrel", "Hazards", PresentationPrefabRole.StandardBarrel, ArtPassAssetTargetPriority.High, false, "Breakable barrel cover prop, visual-only wrapper around authoritative blocker.", Scale("0.8m footprint", "1m tall"));
            yield return Target("explosive_barrel", "Explosive Barrel", "Hazards", PresentationPrefabRole.ExplosiveBarrel, ArtPassAssetTargetPriority.High, false, "Explosive barrel with strong danger read and chain-reaction silhouette.", Scale("0.8m footprint", "1m tall"));
            yield return Target("hazard_coin_drop", "Hazard Coin Drop", "Hazards", PresentationPrefabRole.HazardCoinDrop, ArtPassAssetTargetPriority.Medium, false, "Tiny coin pickup dropped by destructible props.", Scale("0.25m diameter", "pickup-safe"));
            yield return Target("normal_chest", "Normal Chest", "Chests", PresentationPrefabRole.ChestNormal, ArtPassAssetTargetPriority.High, false, "Brown wooden chest opened with Interact after room clear.", Scale("0.8m wide", "0.55m tall"));
            yield return Target("golden_chest", "Golden Chest", "Chests", PresentationPrefabRole.ChestGolden, ArtPassAssetTargetPriority.High, false, "Rare gold-accent chest with better standard-room prizes.", Scale("0.85m wide", "0.6m tall"));
            yield return Target("coin_copper", "Copper Coin", "Coins", PresentationPrefabRole.CoinCopper, ArtPassAssetTargetPriority.Medium, false, "Common coin pickup worth 1.", Scale("0.22m diameter", "pickup-safe"));
            yield return Target("coin_silver", "Silver Coin", "Coins", PresentationPrefabRole.CoinSilver, ArtPassAssetTargetPriority.Medium, false, "Rare coin pickup worth 5.", Scale("0.24m diameter", "pickup-safe"));
            yield return Target("coin_gold", "Gold Coin", "Coins", PresentationPrefabRole.CoinGold, ArtPassAssetTargetPriority.Medium, false, "Very rare coin pickup worth 10.", Scale("0.28m diameter", "pickup-safe"));

            foreach (var role in new[]
            {
                PresentationPrefabRole.VfxProjectileFire,
                PresentationPrefabRole.VfxEnemyHit,
                PresentationPrefabRole.VfxEnemyDeath,
                PresentationPrefabRole.VfxPlayerHit,
                PresentationPrefabRole.VfxRewardClaim,
                PresentationPrefabRole.VfxDoorUnlock,
                PresentationPrefabRole.VfxRoomClear,
                PresentationPrefabRole.VfxPortalComplete,
                PresentationPrefabRole.VfxChestOpen,
                PresentationPrefabRole.VfxCoinPickup
            })
            {
                yield return Target(
                    $"vfx_{role.ToString().Replace("Vfx", string.Empty).ToLowerInvariant()}",
                    role.ToString(),
                    "VFX",
                    role,
                    ArtPassAssetTargetPriority.Medium,
                    role is PresentationPrefabRole.VfxProjectileFire or PresentationPrefabRole.VfxEnemyHit or PresentationPrefabRole.VfxEnemyDeath,
                    "Lightweight visual effect prefab with low hierarchy count and no gameplay scripts.",
                    Scale("short-lived", "Vision-safe"));
            }
        }

        private static ArtPassAssetTargetDefinition SaveTarget(TargetSpec spec)
        {
            var path = $"{TargetDirectory}/{SafeFileName(spec.TargetId)}.asset";
            var target = AssetDatabase.LoadAssetAtPath<ArtPassAssetTargetDefinition>(path);
            if (target == null)
            {
                target = ScriptableObject.CreateInstance<ArtPassAssetTargetDefinition>();
                AssetDatabase.CreateAsset(target, path);
            }

            target.Configure(
                spec.TargetId,
                spec.DisplayName,
                spec.Group,
                spec.Role,
                spec.Priority,
                spec.RequiredForVerticalSlice,
                "Rafal",
                $"{RafalIntakeDirectory}/{SafeFileName(spec.Group)}/{SafeFileName(spec.TargetId)}",
                PrefabPathFor(spec.Role),
                spec.Goal,
                RequiredAssetsFor(spec.Group),
                AcceptanceChecksFor(spec.Role, spec.ScaleNotes),
                spec.Notes);
            EditorUtility.SetDirty(target);
            Directory.CreateDirectory($"{RafalIntakeDirectory}/{SafeFileName(spec.Group)}/{SafeFileName(spec.TargetId)}");
            return target;
        }

        private static string PrefabPathFor(PresentationPrefabRole role)
        {
            return role.ToString().StartsWith("Vfx", StringComparison.Ordinal)
                ? $"{Milestone23AssetGenerator.ArtPassVfxDirectory}/VFX_{role}.prefab"
                : $"{Milestone23AssetGenerator.ArtPassRoot}/AP_{role}.prefab";
        }

        private static string[] RequiredAssetsFor(string group)
        {
            return group == "VFX"
                ? new[] { "Prefab wrapper", "Effect mesh/particle primitives", "Material(s)", "Optional texture flipbook" }
                : new[] { "Low-poly mesh or prefab wrapper", "Material(s)", "Optional base-color texture", "Optional normal/emissive texture", "Unity prefab under ArtPass" };
        }

        private static string[] AcceptanceChecksFor(PresentationPrefabRole role, string scaleNotes)
        {
            return new[]
            {
                $"Scale target: {scaleNotes}.",
                "Prefab root has PresentationVisualMarker with the matching role.",
                "No gameplay colliders and no gameplay scripts on visual children.",
                "Pivot/origin is centered; floor objects and doors use bottom at y=0 where applicable.",
                "Readable from Windows perspective camera and Vision Pro bounded tabletop scale 0.1.",
                "Uses simple materials compatible with Unity 6 URP Linear color."
            };
        }

        private static void WriteIntakeReadme(IReadOnlyList<ArtPassAssetTargetDefinition> targets)
        {
            var path = $"{RafalIntakeDirectory}/README_M38_Rafal_Intake.md";
            File.WriteAllText(
                path,
                "# M38 Rafal ArtPass Intake\n\n" +
                "Drop raw Blender/FBX/texture/source files into the matching target folders here. Runtime-ready wrappers still need to live under `Assets/_Hollow/Prefabs/ArtPass/` and remain visual-only.\n\n" +
                "Rules:\n" +
                "- Keep pivots centered and meter-scale sane.\n" +
                "- Do not add gameplay colliders or gameplay scripts to visual prefabs.\n" +
                "- Use low-poly, low-hierarchy geometry suitable for Vision Pro bounded tabletop.\n" +
                "- Keep names stable: `AP_<PresentationPrefabRole>` for runtime wrappers.\n\n" +
                "Critical targets:\n" +
                string.Join("\n", targets
                    .Where(target => target.Priority == ArtPassAssetTargetPriority.Critical)
                    .Select(target => $"- {target.TargetId}: {target.DisplayName} -> {target.PrefabPath}")) +
                "\n");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static void WriteReport(IReadOnlyList<ArtPassAssetTargetDefinition> targets)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(HandoffReportPath) ?? "output/reports");
            File.WriteAllText(
                HandoffReportPath,
                "# M38 ArtPass Integration Sprint With Rafal Pipeline\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                $"- Target catalog: `{TargetCatalogPath}`\n" +
                $"- Intake folder: `{RafalIntakeDirectory}`\n" +
                $"- Runtime wrapper folder: `{Milestone23AssetGenerator.ArtPassRoot}`\n" +
                $"- Total targets: {targets.Count}\n\n" +
                "## Priority Counts\n\n" +
                string.Join("\n", targets
                    .GroupBy(target => target.Priority)
                    .OrderBy(group => group.Key)
                    .Select(group => $"- {group.Key}: {group.Count()}")) +
                "\n\n## Critical Runtime Targets\n\n" +
                string.Join("\n", targets
                    .Where(target => target.Priority == ArtPassAssetTargetPriority.Critical)
                    .Select(target => $"- {target.DisplayName} ({target.PrefabRole}) - {target.Goal}")) +
                "\n\n## What Programming Can Do Without Final Art\n\n" +
                "- Keep binding generated ArtPass wrappers to runtime roles.\n" +
                "- Validate no visual prefab takes over gameplay collision or scripts.\n" +
                "- Replace AP_* generated placeholders with Rafal-provided prefabs as they arrive.\n\n" +
                "## What Needs Rafal Input\n\n" +
                "- Final silhouettes, textures/material mood, VFX timing, and basic animation poses.\n" +
                "- Any target marked Critical should be prioritized before secondary equipment pickup wrappers.\n");
        }

        private static TargetSpec Target(
            string id,
            string name,
            string group,
            PresentationPrefabRole role,
            ArtPassAssetTargetPriority priority,
            bool required,
            string goal,
            string scaleNotes)
        {
            return new TargetSpec(id, name, group, role, priority, required, goal, scaleNotes, "Visual-only replacement target.");
        }

        private static string Scale(string primary, string secondary)
            => $"{primary}; {secondary}";

        private static string SafeFileName(string value)
        {
            var safe = new string((value ?? string.Empty)
                .Select(character => char.IsLetterOrDigit(character) ? character : '_')
                .ToArray());
            return string.IsNullOrWhiteSpace(safe) ? "target" : safe.ToLowerInvariant();
        }

        private sealed class TargetSpec
        {
            public TargetSpec(
                string targetId,
                string displayName,
                string group,
                PresentationPrefabRole role,
                ArtPassAssetTargetPriority priority,
                bool requiredForVerticalSlice,
                string goal,
                string scaleNotes,
                string notes)
            {
                TargetId = targetId;
                DisplayName = displayName;
                Group = group;
                Role = role;
                Priority = priority;
                RequiredForVerticalSlice = requiredForVerticalSlice;
                Goal = goal;
                ScaleNotes = scaleNotes;
                Notes = notes;
            }

            public string TargetId { get; }
            public string DisplayName { get; }
            public string Group { get; }
            public PresentationPrefabRole Role { get; }
            public ArtPassAssetTargetPriority Priority { get; }
            public bool RequiredForVerticalSlice { get; }
            public string Goal { get; }
            public string ScaleNotes { get; }
            public string Notes { get; }
        }
    }
}
