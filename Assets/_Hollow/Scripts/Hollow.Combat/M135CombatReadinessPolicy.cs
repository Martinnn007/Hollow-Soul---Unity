using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public static class M135CombatReadinessPolicy
    {
        public const string LockId = "m135_full_run_combat_readiness_lock_v1";
        public const float LockedRollStaminaCost = 30f;
        public const float LockedStaminaRegenDelaySeconds = 0.55f;
        public const float LockedRollStartupSeconds = 0.04f;
        public const float LockedRollInvulnerabilitySeconds = 0.26f;
        public const float LockedRollRecoverySeconds = 0.16f;
        public const float LockedRollDistanceMeters = 1.35f;
        public const float AnchorBossMinimumWindupSeconds = 0.45f;

        private static readonly string[] AnchorBossIds =
        {
            "stone_warden",
            "cartouche_widow",
            "choir_of_teeth"
        };

        public static IReadOnlyList<string> DeepPolishBossIds => AnchorBossIds;

        public static bool IsDeepPolishBoss(string bossId)
        {
            return AnchorBossIds.Any(anchor => string.Equals(anchor, bossId, StringComparison.Ordinal));
        }

        public static bool ValidateRollLock(out string detail)
        {
            var failures = new List<string>();
            Expect(nameof(PlayerWeaponController.RollStaminaCost), PlayerWeaponController.RollStaminaCost, LockedRollStaminaCost, failures);
            Expect(nameof(PlayerWeaponController.StaminaRegenDelaySeconds), PlayerWeaponController.StaminaRegenDelaySeconds, LockedStaminaRegenDelaySeconds, failures);
            Expect(nameof(PlayerWeaponController.RollStartupSeconds), PlayerWeaponController.RollStartupSeconds, LockedRollStartupSeconds, failures);
            Expect(nameof(PlayerWeaponController.RollInvulnerabilitySeconds), PlayerWeaponController.RollInvulnerabilitySeconds, LockedRollInvulnerabilitySeconds, failures);
            Expect(nameof(PlayerWeaponController.RollRecoverySeconds), PlayerWeaponController.RollRecoverySeconds, LockedRollRecoverySeconds, failures);
            Expect(nameof(PlayerWeaponController.RollDistanceMeters), PlayerWeaponController.RollDistanceMeters, LockedRollDistanceMeters, failures);
            Expect(
                nameof(PlayerWeaponController.RollDurationSeconds),
                PlayerWeaponController.RollDurationSeconds,
                LockedRollStartupSeconds + LockedRollInvulnerabilitySeconds + LockedRollRecoverySeconds,
                failures);

            detail = failures.Count == 0
                ? "M135 roll constants match the locked gentle-forgiveness values."
                : string.Join("; ", failures);
            return failures.Count == 0;
        }

        public static bool ValidateMinimumBossReadiness(BossDefinition boss, out string detail)
        {
            var failures = new List<string>();
            if (boss == null)
            {
                detail = "Boss definition is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(boss.BossId))
            {
                failures.Add("missing boss id");
            }

            if (string.IsNullOrWhiteSpace(boss.DisplayName))
            {
                failures.Add("missing display name");
            }

            if (boss.MaxHealth < 20 || boss.MaxHealth > 50)
            {
                failures.Add($"invalid health {boss.MaxHealth}");
            }

            if (boss.Arena == null || string.IsNullOrWhiteSpace(boss.Arena.arenaId))
            {
                failures.Add("missing arena id");
            }

            if (boss.Phases == null || boss.Phases.Count < 2)
            {
                failures.Add("missing two-phase status data");
            }

            if (boss.Attacks == null || boss.Attacks.Count < 2)
            {
                failures.Add("missing boss attack set");
            }

            if (boss.AttackProfiles == null || boss.AttackProfiles.Count == 0)
            {
                failures.Add("missing attack profiles");
            }

            if (boss.ActionProfiles == null || boss.ActionProfiles.Count == 0)
            {
                failures.Add("missing action profiles");
            }

            if (boss.BehaviorTreeMetadata == null || boss.SpacingProfileMetadata == null)
            {
                failures.Add("missing behavior or spacing metadata");
            }

            detail = failures.Count == 0
                ? $"{boss.DisplayName} satisfies the M135 minimum boss smoke contract."
                : $"{boss.BossId}: {string.Join(", ", failures)}.";
            return failures.Count == 0;
        }

        public static bool ValidateAnchorBossPolish(BossDefinition boss, out string detail)
        {
            if (boss == null)
            {
                detail = "Boss definition is null.";
                return false;
            }

            if (!IsDeepPolishBoss(boss.BossId))
            {
                detail = $"{boss.BossId} is not an M135 deep-polish anchor.";
                return true;
            }

            var failures = new List<string>();
            var attacks = boss.Attacks ?? Array.Empty<BossAttackDefinition>();
            if (attacks.Count < 2)
            {
                failures.Add("needs at least two authored attacks");
            }

            if (attacks.Any(attack => attack == null || attack.windupSeconds < AnchorBossMinimumWindupSeconds))
            {
                failures.Add($"all authored attacks need >= {AnchorBossMinimumWindupSeconds:0.00}s windup metadata");
            }

            var profiles = boss.AttackProfiles?.Where(profile => profile != null && profile.Damage > 0).ToArray()
                           ?? Array.Empty<EnemyAttackProfileDefinition>();
            if (profiles.Length < 2)
            {
                failures.Add("needs at least two damaging runtime attack profiles");
            }

            if (profiles.Any(profile => profile.WindupSeconds < AnchorBossMinimumWindupSeconds))
            {
                failures.Add($"runtime attack profiles need >= {AnchorBossMinimumWindupSeconds:0.00}s windups");
            }

            if (profiles.Any(profile => profile.RecoverySeconds < 0.18f))
            {
                failures.Add("runtime attack profiles need punishable recovery");
            }

            if (boss.Phases == null || boss.Phases.Any(phase => phase == null || string.IsNullOrWhiteSpace(phase.statusText)))
            {
                failures.Add("phase status copy must be present");
            }

            detail = failures.Count == 0
                ? $"{boss.DisplayName} satisfies the M135 deep-polish anchor contract."
                : $"{boss.BossId}: {string.Join(", ", failures)}.";
            return failures.Count == 0;
        }

        private static void Expect(string label, float actual, float expected, List<string> failures)
        {
            if (Math.Abs(actual - expected) > 0.0001f)
            {
                failures.Add($"{label} expected {expected:0.###} but was {actual:0.###}");
            }
        }
    }
}
