using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Data.Definitions;

namespace Hollow.Editor.Generation
{
    public static class PlayerAnimationProfileStaticScanner
    {
        public const string ReportType = "STATIC_PREVIEW";
        public const string ReportVersion = "1";
        public const string AnimationPackRoot = "Assets/_Hollow/Animation Packs";
        public const string ProfileCatalogPath = "Assets/_Hollow/Data/AnimationProfiles/PlayerAnimationProfileCatalog.asset";
        public const string StaticPreviewReportPath = "Assets/_Hollow/Data/AnimationProfiles/ProfileMappingReport.StaticPreview.txt";
        public const string StaticPreviewWarning = "This report does not prove Avatar/Humanoid import validity, clip loop settings, rig compatibility, Animator generation, prefab generation, debug scene generation, or gameplay correctness.";

        public static PlayerAnimationProfileStaticScanResult Scan(string animationPackRoot = AnimationPackRoot)
        {
            var root = NormalizePath(animationPackRoot);
            var packFolders = Directory.Exists(root)
                ? Directory.GetDirectories(root)
                    .Select(NormalizePath)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray()
                : Array.Empty<string>();
            var candidates = Directory.Exists(root)
                ? Directory.GetFiles(root, "*.fbx", SearchOption.AllDirectories)
                    .Select(NormalizePath)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray()
                : Array.Empty<string>();
            var context = new StaticScanContext(root, candidates);
            var profiles = new[]
            {
                BuildUnarmed(context),
                BuildSwordShield(context),
                BuildGreatSword(context),
                BuildRifle(context),
                BuildPistol(context)
            };

            return new PlayerAnimationProfileStaticScanResult(root, packFolders, candidates, profiles);
        }

        public static bool TryWriteStaticPreviewReport(
            string animationPackRoot = AnimationPackRoot,
            string reportPath = StaticPreviewReportPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? string.Empty);
                File.WriteAllText(reportPath, BuildStaticPreviewReport(Scan(animationPackRoot)));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string BuildStaticPreviewReport(PlayerAnimationProfileStaticScanResult scan)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Hollow Soul Player Animation Profile Static Mapping Preview");
            builder.AppendLine($"ReportType: {ReportType}");
            builder.AppendLine($"ReportVersion: {ReportVersion}");
            builder.AppendLine($"GeneratedAtUtc: {DateTime.UtcNow:O}");
            builder.AppendLine($"AnimationPackRoot: {scan.AnimationPackRoot}");
            builder.AppendLine($"ProfileCatalogPath: {ProfileCatalogPath}");
            builder.AppendLine($"StaticPreviewReportPath: {StaticPreviewReportPath}");
            builder.AppendLine($"Warning: {StaticPreviewWarning}");
            builder.AppendLine("InfrastructureFreezeGate: active until Unity profile generation, debug scene, player prefab, profile switching, shield gate, locomotion, Mixamo mapping, and targeted regressions are validated in a real editor session.");
            builder.AppendLine();

            AppendSection(builder, "DetectedPackFolders", scan.DetectedPackFolders);
            AppendSection(builder, "DetectedCandidateFbxFiles", scan.CandidateFbxFiles);
            builder.AppendLine("SummaryChecks:");
            builder.AppendLine($"- RifleReal8WayCandidateCoverage: {Bool(scan.Resolve(PlayerAnimationProfileId.RifleCombat)?.HasReal8WayCandidateCoverage == true)}");
            builder.AppendLine($"- SwordShieldBlockGuardCandidates: {Bool(HasMatchedSlotPrefix(scan.Resolve(PlayerAnimationProfileId.SwordShieldCombat), "ShieldGuard"))}");
            builder.AppendLine($"- GreatSwordWeaponBlockOnly: {Bool(IsGreatSwordWeaponBlockOnly(scan.Resolve(PlayerAnimationProfileId.GreatSwordCombat)))}");
            builder.AppendLine($"- RifleShieldGuardDisabled: {Bool(IsShieldGuardDisabled(scan.Resolve(PlayerAnimationProfileId.RifleCombat)))}");
            builder.AppendLine($"- PistolShieldGuardDisabled: {Bool(IsShieldGuardDisabled(scan.Resolve(PlayerAnimationProfileId.PistolCombat)))}");
            builder.AppendLine($"- GreatSwordShieldGuardDisabled: {Bool(IsShieldGuardDisabled(scan.Resolve(PlayerAnimationProfileId.GreatSwordCombat)))}");
            builder.AppendLine($"- UnarmedSafeFallback: {Bool(IsUnarmedSafeFallback(scan.Resolve(PlayerAnimationProfileId.UnarmedLocomotion)))}");
            builder.AppendLine();

            foreach (var profile in scan.Profiles)
            {
                AppendProfile(builder, profile);
            }

            return builder.ToString();
        }

        private static StaticProfileMappingResult BuildUnarmed(StaticScanContext context)
        {
            var profile = new StaticProfileMappingResult(PlayerAnimationProfileId.UnarmedLocomotion, "UnarmedLocomotionProfile", Capabilities(false, false, false, false, true, false));
            AddExact(context, profile, Pack.MaleLocomotion, "Idle", "idle.fbx", required: true);
            AddExact(context, profile, Pack.MaleLocomotion, "Walk.Forward", "walking.fbx", required: true);
            AddExact(context, profile, Pack.MaleLocomotion, "Run.Forward", "standard run.fbx", required: true);
            AddExact(context, profile, Pack.MaleLocomotion, "Walk.Left", "left strafe walking.fbx", required: false);
            AddExact(context, profile, Pack.MaleLocomotion, "Run.Left", "left strafe.fbx", required: false);
            AddExact(context, profile, Pack.MaleLocomotion, "Walk.Right", "right strafe walking.fbx", required: false);
            AddExact(context, profile, Pack.MaleLocomotion, "Run.Right", "right strafe.fbx", required: false);
            AddExact(context, profile, Pack.MaleLocomotion, "Turn.Left", "left turn 90.fbx", required: false);
            AddExact(context, profile, Pack.MaleLocomotion, "Turn.Right", "right turn 90.fbx", required: false);
            AddExact(context, profile, Pack.MaleLocomotion, "Jump", "jump.fbx", required: false);
            AddDirectionalPlaceholders(profile, fallbackWalkSlot: "Walk.Forward", fallbackRunSlot: "Run.Forward");
            return profile;
        }

        private static StaticProfileMappingResult BuildSwordShield(StaticScanContext context)
        {
            var profile = new StaticProfileMappingResult(PlayerAnimationProfileId.SwordShieldCombat, "SwordShieldCombatProfile", Capabilities(true, true, false, false, true, true));
            AddExact(context, profile, Pack.SwordShield, "Idle", "sword and shield idle.fbx", required: true);
            AddExact(context, profile, Pack.SwordShield, "Walk.Forward", "sword and shield walk.fbx", required: true);
            AddExact(context, profile, Pack.SwordShield, "Run.Forward", "sword and shield run.fbx", required: true);
            AddExact(context, profile, Pack.SwordShield, "Walk.Left", "sword and shield strafe.fbx", required: false);
            AddExact(context, profile, Pack.SwordShield, "Run.Left", "sword and shield strafe (2).fbx", required: false);
            AddExact(context, profile, Pack.SwordShield, "Walk.Right", "sword and shield strafe (3).fbx", required: false);
            AddExact(context, profile, Pack.SwordShield, "Run.Right", "sword and shield strafe (4).fbx", required: false);
            AddExactMany(context, profile, Pack.SwordShield, "Turn", required: false, "sword and shield turn.fbx", "sword and shield turn (2).fbx", "sword and shield 180 turn.fbx", "sword and shield 180 turn (2).fbx");
            AddExactMany(context, profile, Pack.SwordShield, "Draw", required: false, "draw sword 1.fbx", "draw sword 2.fbx");
            AddExactMany(context, profile, Pack.SwordShield, "Sheathe", required: false, "sheath sword 1.fbx", "sheath sword 2.fbx");
            AddMatching(context, profile, Pack.SwordShield, "Attack", required: false, "attack", "slash");
            AddMatching(context, profile, Pack.SwordShield, "ShieldGuard", required: false, "block");
            AddMatching(context, profile, Pack.SwordShield, "Impact", required: false, "impact");
            AddMatching(context, profile, Pack.SwordShield, "Death", required: false, "death");
            AddMatching(context, profile, Pack.SwordShield, "Jump", required: false, "jump");
            AddMatching(context, profile, Pack.SwordShield, "Crouch", required: false, "crouch", "crouching");
            AddDirectionalPlaceholders(profile, fallbackWalkSlot: "Walk.Forward", fallbackRunSlot: "Run.Forward");
            return profile;
        }

        private static StaticProfileMappingResult BuildGreatSword(StaticScanContext context)
        {
            var profile = new StaticProfileMappingResult(PlayerAnimationProfileId.GreatSwordCombat, "GreatSwordCombatProfile", Capabilities(false, false, true, false, true, true));
            AddExact(context, profile, Pack.GreatSword, "Idle", "great sword idle.fbx", required: true);
            AddExact(context, profile, Pack.GreatSword, "Walk.Forward", "great sword walk.fbx", required: true);
            AddExact(context, profile, Pack.GreatSword, "Run.Forward", "great sword run.fbx", required: true);
            AddExact(context, profile, Pack.GreatSword, "Walk.Left", "great sword strafe.fbx", required: false);
            AddExact(context, profile, Pack.GreatSword, "Run.Left", "great sword strafe (2).fbx", required: false);
            AddExact(context, profile, Pack.GreatSword, "Walk.Right", "great sword strafe (3).fbx", required: false);
            AddExact(context, profile, Pack.GreatSword, "Run.Right", "great sword strafe (4).fbx", required: false);
            AddExactMany(context, profile, Pack.GreatSword, "Turn", required: false, "great sword turn.fbx", "great sword turn (2).fbx", "great sword 180 turn.fbx", "great sword 180 turn (2).fbx");
            AddExactMany(context, profile, Pack.GreatSword, "Draw", required: false, "draw a great sword 1.fbx", "draw a great sword 2.fbx");
            AddMatching(context, profile, Pack.GreatSword, "Attack", required: false, "attack", "slash", "spin");
            AddMatching(context, profile, Pack.GreatSword, "WeaponBlock", required: false, "blocking");
            AddMatching(context, profile, Pack.GreatSword, "Impact", required: false, "impact");
            AddMatching(context, profile, Pack.GreatSword, "Death", required: false, "death");
            AddMatching(context, profile, Pack.GreatSword, "Jump", required: false, "jump");
            AddMatching(context, profile, Pack.GreatSword, "Crouch", required: false, "crouching");
            AddDirectionalPlaceholders(profile, fallbackWalkSlot: "Walk.Forward", fallbackRunSlot: "Run.Forward");
            return profile;
        }

        private static StaticProfileMappingResult BuildRifle(StaticScanContext context)
        {
            var profile = new StaticProfileMappingResult(PlayerAnimationProfileId.RifleCombat, "RifleCombatProfile", Capabilities(false, false, false, true, true, true));
            AddExact(context, profile, Pack.Rifle, "Idle", "idle aiming.fbx", required: true);
            AddRifleDirection(context, profile, PlayerAnimationDirection.Forward, "forward");
            AddRifleDirection(context, profile, PlayerAnimationDirection.ForwardRight, "forward right");
            AddRifleDirection(context, profile, PlayerAnimationDirection.Right, "right");
            AddRifleDirection(context, profile, PlayerAnimationDirection.BackwardRight, "backward right");
            AddRifleDirection(context, profile, PlayerAnimationDirection.Backward, "backward");
            AddRifleDirection(context, profile, PlayerAnimationDirection.BackwardLeft, "backward left");
            AddRifleDirection(context, profile, PlayerAnimationDirection.Left, "left");
            AddRifleDirection(context, profile, PlayerAnimationDirection.ForwardLeft, "forward left");
            AddExactMany(context, profile, Pack.Rifle, "Turn", required: false, "turn 90 left.fbx", "turn 90 right.fbx");
            AddExact(context, profile, Pack.Shooter, "Fire", "firing rifle.fbx", required: false);
            AddMatching(context, profile, Pack.Rifle, "Death", required: false, "death");
            AddMatching(context, profile, Pack.Rifle, "Jump", required: false, "jump");
            AddMatching(context, profile, Pack.Rifle, "Crouch", required: false, "crouch", "crouching");
            AddDirectionalPlaceholders(profile, fallbackWalkSlot: null, fallbackRunSlot: null);
            return profile;
        }

        private static StaticProfileMappingResult BuildPistol(StaticScanContext context)
        {
            var profile = new StaticProfileMappingResult(PlayerAnimationProfileId.PistolCombat, "PistolCombatProfile", Capabilities(false, false, false, true, true, true));
            AddExact(context, profile, Pack.Pistol, "Idle", "pistol idle.fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Walk.Forward", "pistol walk.fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Run.Forward", "pistol run.fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Walk.Backward", "pistol walk backward.fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Run.Backward", "pistol run backward.fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Walk.Left", "pistol strafe.fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Run.Left", "pistol strafe.fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Walk.Right", "pistol strafe (2).fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Run.Right", "pistol strafe (2).fbx", required: true);
            AddExact(context, profile, Pack.Pistol, "Walk.ForwardRight", "pistol walk arc.fbx", required: false);
            AddExact(context, profile, Pack.Pistol, "Run.ForwardRight", "pistol run arc.fbx", required: false);
            AddExact(context, profile, Pack.Pistol, "Walk.ForwardLeft", "pistol walk arc (2).fbx", required: false);
            AddExact(context, profile, Pack.Pistol, "Run.ForwardLeft", "pistol run arc (2).fbx", required: false);
            AddExact(context, profile, Pack.Pistol, "Walk.BackwardRight", "pistol walk backward arc.fbx", required: false);
            AddExact(context, profile, Pack.Pistol, "Run.BackwardRight", "pistol run backward arc.fbx", required: false);
            AddExact(context, profile, Pack.Pistol, "Walk.BackwardLeft", "pistol walk backward arc (2).fbx", required: false);
            AddExact(context, profile, Pack.Pistol, "Run.BackwardLeft", "pistol run backward arc (2).fbx", required: false);
            AddMatching(context, profile, Pack.Pistol, "Jump", required: false, "jump");
            AddMatching(context, profile, Pack.Pistol, "Kneel", required: false, "kneel", "kneeling");
            AddDirectionalPlaceholders(profile, fallbackWalkSlot: "Walk.Forward", fallbackRunSlot: "Run.Forward");
            return profile;
        }

        private static void AddRifleDirection(StaticScanContext context, StaticProfileMappingResult profile, PlayerAnimationDirection direction, string name)
        {
            AddExact(context, profile, Pack.Rifle, $"Walk.{direction}", $"walk {name}.fbx", required: true);
            AddExact(context, profile, Pack.Rifle, $"Run.{direction}", $"run {name}.fbx", required: true);
        }

        private static void AddExactMany(StaticScanContext context, StaticProfileMappingResult profile, Pack pack, string slotPrefix, bool required, params string[] files)
        {
            foreach (var file in files)
            {
                AddExact(context, profile, pack, slotPrefix, file, required);
            }
        }

        private static void AddExact(StaticScanContext context, StaticProfileMappingResult profile, Pack pack, string slot, string file, bool required)
        {
            var path = context.Exact(pack, file);
            profile.AddSlot(slot, required, path != null ? new[] { path } : Array.Empty<string>());
        }

        private static void AddMatching(StaticScanContext context, StaticProfileMappingResult profile, Pack pack, string slot, bool required, params string[] tokens)
        {
            profile.AddSlot(slot, required, context.Matching(pack, tokens));
        }

        private static void AddDirectionalPlaceholders(StaticProfileMappingResult profile, string fallbackWalkSlot, string fallbackRunSlot)
        {
            foreach (PlayerAnimationDirection direction in Enum.GetValues(typeof(PlayerAnimationDirection)))
            {
                var walkSlot = $"Walk.{direction}";
                var runSlot = $"Run.{direction}";
                if (!profile.HasMatchedSlot(walkSlot))
                {
                    if (!string.IsNullOrWhiteSpace(fallbackWalkSlot) && profile.HasMatchedSlot(fallbackWalkSlot))
                    {
                        profile.AddPlaceholderNeeded(walkSlot);
                    }
                    else if (!profile.MissingRequiredSlots.Any(slot => slot.StartsWith(walkSlot + ":", StringComparison.Ordinal)))
                    {
                        profile.AddMissingRequired($"{walkSlot}: missing real clip and fallback source");
                    }
                }

                if (!profile.HasMatchedSlot(runSlot))
                {
                    if (!string.IsNullOrWhiteSpace(fallbackRunSlot) && profile.HasMatchedSlot(fallbackRunSlot))
                    {
                        profile.AddPlaceholderNeeded(runSlot);
                    }
                    else if (!profile.MissingRequiredSlots.Any(slot => slot.StartsWith(runSlot + ":", StringComparison.Ordinal)))
                    {
                        profile.AddMissingRequired($"{runSlot}: missing real clip and fallback source");
                    }
                }
            }
        }

        private static void AppendProfile(StringBuilder builder, StaticProfileMappingResult profile)
        {
            builder.AppendLine($"Profile: {profile.ProfileName} ({profile.ProfileId})");
            builder.AppendLine("- CapabilityFlags:");
            builder.AppendLine($"  AllowsShieldInHand: {Bool(profile.Capabilities.AllowsShieldInHand)}");
            builder.AppendLine($"  AllowsShieldGuard: {Bool(profile.Capabilities.AllowsShieldGuard)}");
            builder.AppendLine($"  RequiresTwoHandedWeapon: {Bool(profile.Capabilities.RequiresTwoHandedWeapon)}");
            builder.AppendLine($"  UsesRangedAim: {Bool(profile.Capabilities.UsesRangedAim)}");
            builder.AppendLine($"  UsesFootIk: {Bool(profile.Capabilities.UsesFootIk)}");
            builder.AppendLine($"  UsesTorsoAim: {Bool(profile.Capabilities.UsesTorsoAim)}");
            builder.AppendLine($"- Real8WayCandidateCoverage: {Bool(profile.HasReal8WayCandidateCoverage)}");
            builder.AppendLine("- SlotMatches:");
            foreach (var slot in profile.MatchedSlots.OrderBy(slot => slot.SlotName, StringComparer.Ordinal))
            {
                builder.AppendLine($"  - {slot.SlotName}:");
                foreach (var path in slot.MatchedPaths)
                {
                    builder.AppendLine($"    - {path}");
                }
            }

            AppendSection(builder, "MissingRequiredSlots", profile.MissingRequiredSlots);
            AppendSection(builder, "MissingOptionalSlots", profile.MissingOptionalSlots);
            AppendSection(
                builder,
                "PlaceholderNeededSlots_NON_PRODUCTION",
                profile.PlaceholderNeededSlots.Select(slot => $"{slot} (PLACEHOLDER_NEEDED_NON_PRODUCTION)"));
            AppendSection(builder, "Warnings", ProfileWarnings(profile));
            builder.AppendLine();
        }

        private static IEnumerable<string> ProfileWarnings(StaticProfileMappingResult profile)
        {
            if (profile.MissingRequiredSlots.Count > 0)
            {
                yield return "Missing required filename candidates; Unity generation will need real clips or an existing fallback rule.";
            }

            if (profile.PlaceholderNeededSlots.Count > 0)
            {
                yield return "Placeholder-needed slots are diagnostic-only and must not be treated as production animation.";
            }

            if (profile.ProfileId == PlayerAnimationProfileId.GreatSwordCombat)
            {
                yield return "GreatSword blocking candidates are mapped as WeaponBlock only; shield guard remains disabled.";
            }

            if (profile.ProfileId == PlayerAnimationProfileId.RifleCombat ||
                profile.ProfileId == PlayerAnimationProfileId.PistolCombat ||
                profile.ProfileId == PlayerAnimationProfileId.GreatSwordCombat ||
                profile.ProfileId == PlayerAnimationProfileId.UnarmedLocomotion)
            {
                yield return "Shield guard is disabled for this profile by capability flags.";
            }
        }

        private static void AppendSection(StringBuilder builder, string title, IEnumerable<string> values)
        {
            var compact = (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            builder.AppendLine($"{title} ({compact.Length}):");
            if (compact.Length == 0)
            {
                builder.AppendLine("- none");
                return;
            }

            foreach (var value in compact)
            {
                builder.AppendLine($"- {value}");
            }
        }

        private static PlayerAnimationProfileStaticCapabilities Capabilities(
            bool allowsShieldInHand,
            bool allowsShieldGuard,
            bool requiresTwoHandedWeapon,
            bool usesRangedAim,
            bool usesFootIk,
            bool usesTorsoAim)
        {
            return new PlayerAnimationProfileStaticCapabilities(
                allowsShieldInHand,
                allowsShieldGuard && allowsShieldInHand,
                requiresTwoHandedWeapon,
                usesRangedAim,
                usesFootIk,
                usesTorsoAim);
        }

        private static bool IsShieldGuardDisabled(StaticProfileMappingResult profile)
        {
            return profile != null &&
                !profile.Capabilities.AllowsShieldGuard &&
                !profile.Capabilities.AllowsShieldInHand;
        }

        private static bool HasMatchedSlotPrefix(StaticProfileMappingResult profile, string slotPrefix)
        {
            return profile != null &&
                profile.MatchedSlots.Any(slot => slot.SlotName.StartsWith(slotPrefix, StringComparison.Ordinal));
        }

        private static bool IsGreatSwordWeaponBlockOnly(StaticProfileMappingResult profile)
        {
            return profile != null &&
                IsShieldGuardDisabled(profile) &&
                profile.MatchedSlots.Any(slot => slot.SlotName.StartsWith("WeaponBlock", StringComparison.Ordinal)) &&
                !profile.MatchedSlots.Any(slot => slot.SlotName.StartsWith("ShieldGuard", StringComparison.Ordinal));
        }

        private static bool IsUnarmedSafeFallback(StaticProfileMappingResult profile)
        {
            return profile != null &&
                profile.ProfileId == PlayerAnimationProfileId.UnarmedLocomotion &&
                IsShieldGuardDisabled(profile) &&
                !profile.Capabilities.RequiresTwoHandedWeapon &&
                !profile.Capabilities.UsesRangedAim;
        }

        private static string PackPath(StaticScanContext context, Pack pack)
        {
            return pack switch
            {
                Pack.MaleLocomotion => $"{context.AnimationPackRoot}/Male Locomotion Pack",
                Pack.SwordShield => $"{context.AnimationPackRoot}/Pro Sword and Shield Pack",
                Pack.GreatSword => $"{context.AnimationPackRoot}/Great Sword Pack",
                Pack.Rifle => $"{context.AnimationPackRoot}/Rifle 8-Way Locomotion Pack",
                Pack.Shooter => $"{context.AnimationPackRoot}/Shooter Pack",
                Pack.Pistol => $"{context.AnimationPackRoot}/Pistol_Handgun Locomotion Pack",
                _ => context.AnimationPackRoot
            };
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
        }

        private static string Bool(bool value)
        {
            return value ? "yes" : "no";
        }

        private enum Pack
        {
            MaleLocomotion,
            SwordShield,
            GreatSword,
            Rifle,
            Shooter,
            Pistol
        }

        private sealed class StaticScanContext
        {
            private readonly string[] candidates;

            public StaticScanContext(string animationPackRoot, IReadOnlyList<string> candidates)
            {
                AnimationPackRoot = NormalizePath(animationPackRoot);
                this.candidates = candidates.Select(NormalizePath).ToArray();
            }

            public string AnimationPackRoot { get; }

            public string Exact(Pack pack, string file)
            {
                var expected = NormalizePath($"{PackPath(this, pack)}/{file}");
                return candidates.FirstOrDefault(candidate => string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase));
            }

            public string[] Matching(Pack pack, params string[] tokens)
            {
                var packPath = PackPath(this, pack) + "/";
                return candidates
                    .Where(candidate => candidate.StartsWith(packPath, StringComparison.OrdinalIgnoreCase))
                    .Where(candidate =>
                    {
                        var name = Path.GetFileNameWithoutExtension(candidate);
                        return tokens.Any(token => name.Contains(token, StringComparison.OrdinalIgnoreCase));
                    })
                    .OrderBy(candidate => candidate, StringComparer.Ordinal)
                    .ToArray();
            }
        }
    }

    public sealed class PlayerAnimationProfileStaticScanResult
    {
        public PlayerAnimationProfileStaticScanResult(
            string animationPackRoot,
            IReadOnlyList<string> detectedPackFolders,
            IReadOnlyList<string> candidateFbxFiles,
            IReadOnlyList<StaticProfileMappingResult> profiles)
        {
            AnimationPackRoot = animationPackRoot;
            DetectedPackFolders = detectedPackFolders.ToArray();
            CandidateFbxFiles = candidateFbxFiles.ToArray();
            Profiles = profiles.ToArray();
        }

        public string AnimationPackRoot { get; }

        public IReadOnlyList<string> DetectedPackFolders { get; }

        public IReadOnlyList<string> CandidateFbxFiles { get; }

        public IReadOnlyList<StaticProfileMappingResult> Profiles { get; }

        public StaticProfileMappingResult Resolve(PlayerAnimationProfileId profileId)
        {
            return Profiles.FirstOrDefault(profile => profile.ProfileId == profileId);
        }
    }

    public sealed class StaticProfileMappingResult
    {
        private readonly List<StaticSlotMapping> matchedSlots = new();
        private readonly List<string> missingRequiredSlots = new();
        private readonly List<string> missingOptionalSlots = new();
        private readonly List<string> placeholderNeededSlots = new();

        public StaticProfileMappingResult(
            PlayerAnimationProfileId profileId,
            string profileName,
            PlayerAnimationProfileStaticCapabilities capabilities)
        {
            ProfileId = profileId;
            ProfileName = profileName;
            Capabilities = capabilities;
        }

        public PlayerAnimationProfileId ProfileId { get; }

        public string ProfileName { get; }

        public PlayerAnimationProfileStaticCapabilities Capabilities { get; }

        public IReadOnlyList<StaticSlotMapping> MatchedSlots => matchedSlots;

        public IReadOnlyList<string> MissingRequiredSlots => missingRequiredSlots;

        public IReadOnlyList<string> MissingOptionalSlots => missingOptionalSlots;

        public IReadOnlyList<string> PlaceholderNeededSlots => placeholderNeededSlots;

        public bool HasReal8WayCandidateCoverage
        {
            get
            {
                foreach (PlayerAnimationDirection direction in Enum.GetValues(typeof(PlayerAnimationDirection)))
                {
                    if (!HasMatchedSlot($"Walk.{direction}") || !HasMatchedSlot($"Run.{direction}"))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void AddSlot(string slotName, bool required, IReadOnlyList<string> matches)
        {
            if (matches.Count > 0)
            {
                matchedSlots.Add(new StaticSlotMapping(slotName, required, matches));
                return;
            }

            if (required)
            {
                AddMissingRequired(slotName);
            }
            else
            {
                missingOptionalSlots.Add(slotName);
            }
        }

        public void AddMissingRequired(string slotName)
        {
            missingRequiredSlots.Add(slotName);
        }

        public void AddPlaceholderNeeded(string slotName)
        {
            placeholderNeededSlots.Add(slotName);
        }

        public bool HasMatchedSlot(string slotName)
        {
            return matchedSlots.Any(slot => string.Equals(slot.SlotName, slotName, StringComparison.Ordinal));
        }
    }

    public readonly struct PlayerAnimationProfileStaticCapabilities
    {
        public PlayerAnimationProfileStaticCapabilities(
            bool allowsShieldInHand,
            bool allowsShieldGuard,
            bool requiresTwoHandedWeapon,
            bool usesRangedAim,
            bool usesFootIk,
            bool usesTorsoAim)
        {
            AllowsShieldInHand = allowsShieldInHand;
            AllowsShieldGuard = allowsShieldGuard;
            RequiresTwoHandedWeapon = requiresTwoHandedWeapon;
            UsesRangedAim = usesRangedAim;
            UsesFootIk = usesFootIk;
            UsesTorsoAim = usesTorsoAim;
        }

        public bool AllowsShieldInHand { get; }

        public bool AllowsShieldGuard { get; }

        public bool RequiresTwoHandedWeapon { get; }

        public bool UsesRangedAim { get; }

        public bool UsesFootIk { get; }

        public bool UsesTorsoAim { get; }
    }

    public readonly struct StaticSlotMapping
    {
        public StaticSlotMapping(string slotName, bool required, IReadOnlyList<string> matchedPaths)
        {
            SlotName = slotName;
            Required = required;
            MatchedPaths = matchedPaths.ToArray();
        }

        public string SlotName { get; }

        public bool Required { get; }

        public IReadOnlyList<string> MatchedPaths { get; }
    }
}
