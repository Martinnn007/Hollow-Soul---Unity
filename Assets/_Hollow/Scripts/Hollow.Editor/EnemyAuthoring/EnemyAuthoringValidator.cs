using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.EnemyAuthoring
{
    public sealed class EnemyAuthoringValidationResult
    {
        private readonly List<string> errors = new();
        private readonly List<string> warnings = new();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool IsValid => errors.Count == 0;

        public void Error(string message) => errors.Add(message);
        public void Warning(string message) => warnings.Add(message);
    }

    public static class EnemyAuthoringValidator
    {
        public static EnemyAuthoringValidationResult Validate(UnityEngine.Object root)
        {
            var result = new EnemyAuthoringValidationResult();
            switch (root)
            {
                case EnemyDefinition enemy:
                    ValidateEnemy(enemy, result);
                    break;
                case BossDefinition boss:
                    ValidateBoss(boss, result);
                    break;
                case EnemyAttackProfileDefinition attack:
                    ValidateAttack(attack, result);
                    break;
                case EnemyActionProfileDefinition action:
                    ValidateAction(action, null, result);
                    break;
                case EnemyBehaviorTreeDefinition tree:
                    ValidateTree(tree, Array.Empty<EnemyActionProfileDefinition>(), result);
                    break;
                case EnemySpacingProfileDefinition spacing:
                    ValidateSpacing(spacing, Array.Empty<EnemyActionProfileDefinition>(), result);
                    break;
                case null:
                    result.Error("No asset selected.");
                    break;
            }

            return result;
        }

        private static void ValidateEnemy(EnemyDefinition enemy, EnemyAuthoringValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(enemy.SpawnKind))
            {
                result.Error("Enemy spawn kind is empty.");
            }

            if (enemy.MaxHealth <= 0)
            {
                result.Error($"{enemy.DisplayName}: max health must be positive.");
            }

            if (enemy.SpeedMetersPerSecond < 0f)
            {
                result.Error($"{enemy.DisplayName}: speed cannot be negative.");
            }

            if (enemy.RadiusMeters <= 0f)
            {
                result.Error($"{enemy.DisplayName}: radius must be positive.");
            }

            if (enemy.SightRadiusMeters > 0f && enemy.SightAngleDegrees <= 0f)
            {
                result.Error($"{enemy.DisplayName}: sight radius is enabled but sight angle is zero.");
            }

            ValidatePresentationRole(enemy.PresentationPrefabRole, result, $"{enemy.DisplayName} body");
            if (enemy.HasWeaponPrefabRoleOverride)
            {
                ValidatePresentationRole(enemy.WeaponPrefabRole, result, $"{enemy.DisplayName} weapon");
            }

            if (enemy.HasOffhandPrefabRoleOverride)
            {
                ValidatePresentationRole(enemy.OffhandPrefabRole, result, $"{enemy.DisplayName} offhand");
            }

            var attacks = enemy.AttackProfiles.Where(profile => profile != null).ToArray();
            var actions = enemy.ActionProfiles.Where(profile => profile != null).ToArray();
            if (attacks.Length == 0)
            {
                result.Error($"{enemy.DisplayName}: no attack profiles resolve.");
            }

            ValidateDuplicateIds(attacks.Select(profile => profile.AttackId), "attack", result);
            ValidateDuplicateIds(actions.Select(profile => profile.ActionId), "action", result, asWarning: true);
            foreach (var attack in attacks)
            {
                ValidateAttack(attack, result);
            }

            foreach (var action in actions)
            {
                ValidateAction(action, attacks, result);
            }

            ValidateTree(enemy.BehaviorTree, actions, result);
            ValidateSpacing(enemy.SpacingProfile, actions, result);
            if (enemy.BehaviorTree == null)
            {
                result.Warning($"{enemy.DisplayName}: behavior tree is fallback/generated, not an authored asset.");
            }

            if (enemy.SpacingProfile == null)
            {
                result.Warning($"{enemy.DisplayName}: spacing profile is fallback/generated, not an authored asset.");
            }
        }

        private static void ValidateBoss(BossDefinition boss, EnemyAuthoringValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(boss.BossId))
            {
                result.Error("Boss id is empty.");
            }

            if (boss.MaxHealth <= 0)
            {
                result.Error($"{boss.DisplayName}: max health must be positive.");
            }

            ValidatePresentationRole(boss.PresentationPrefabRole, result, $"{boss.DisplayName} body");
            var attacks = boss.AttackProfiles.Where(profile => profile != null).ToArray();
            var actions = boss.ActionProfiles.Where(profile => profile != null).ToArray();
            if (attacks.Length == 0)
            {
                result.Error($"{boss.DisplayName}: no attack profiles resolve.");
            }

            foreach (var attack in attacks)
            {
                ValidateAttack(attack, result);
            }

            foreach (var action in actions)
            {
                ValidateAction(action, attacks, result);
            }

            ValidateTree(boss.BehaviorTreeMetadata, actions, result);
            ValidateSpacing(boss.SpacingProfileMetadata, actions, result);
        }

        private static void ValidateAttack(EnemyAttackProfileDefinition attack, EnemyAuthoringValidationResult result)
        {
            if (attack == null)
            {
                result.Error("Attack profile reference is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(attack.AttackId))
            {
                result.Error($"{attack.name}: attack id is empty.");
            }

            if (attack.ActiveSeconds <= 0f)
            {
                result.Error($"{attack.AttackId}: active time must be positive.");
            }

            if (attack.CooldownSeconds <= 0f)
            {
                result.Error($"{attack.AttackId}: cooldown must be positive.");
            }

            if (attack.KnockbackMeters < 0f)
            {
                result.Error($"{attack.AttackId}: knockback cannot be negative.");
            }

            if (attack.RuntimeKind is EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile &&
                attack.ProjectileCount <= 0)
            {
                result.Error($"{attack.AttackId}: projectile runtime kind requires projectile count.");
            }
        }

        private static void ValidateAction(EnemyActionProfileDefinition action, IReadOnlyCollection<EnemyAttackProfileDefinition> attacks, EnemyAuthoringValidationResult result)
        {
            if (action == null)
            {
                result.Error("Action profile reference is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(action.ActionId))
            {
                result.Error($"{action.name}: action id is empty.");
            }

            if (action.MinRangeMeters > action.MaxRangeMeters)
            {
                result.Error($"{action.ActionId}: min range is greater than max range.");
            }

            if (action.HasLinkedAttack && attacks != null)
            {
                var linked = action.LinkedAttackProfile != null
                    ? action.LinkedAttackProfile.AttackId
                    : action.LinkedAttackId;
                if (attacks.All(attack => !string.Equals(attack.AttackId, linked, StringComparison.Ordinal)))
                {
                    result.Error($"{action.ActionId}: linked attack `{linked}` is not in the owning roster attack list.");
                }
            }

            if (!action.HasLinkedAttack && !action.ExplicitlyNonDamaging && action.Intent == EnemyActionIntent.Damage)
            {
                result.Warning($"{action.ActionId}: damaging action has no linked attack profile.");
            }
        }

        private static void ValidateTree(EnemyBehaviorTreeDefinition tree, IReadOnlyCollection<EnemyActionProfileDefinition> actions, EnemyAuthoringValidationResult result)
        {
            if (tree == null)
            {
                result.Error("Behavior tree reference is missing.");
                return;
            }

            if (tree.RootNode == null)
            {
                result.Error($"{tree.DisplayName}: root node is missing.");
            }

            var nodes = tree.Nodes.ToArray();
            ValidateDuplicateIds(nodes.Select(node => node.NodeId), "tree node", result);
            foreach (var node in nodes)
            {
                if (node == null)
                {
                    continue;
                }

                if (node.Kind is EnemyBehaviorTreeNodeKind.Selector or EnemyBehaviorTreeNodeKind.Sequence && node.Children.Count == 0)
                {
                    result.Error($"{tree.DisplayName}/{node.NodeId}: selector/sequence has no children.");
                }

                if (node is EnemyBehaviorActionNodeDefinition actionNode && actionNode.CommandKind.StartsCommittedAction())
                {
                    if (string.IsNullOrWhiteSpace(actionNode.ActionId))
                    {
                        result.Error($"{tree.DisplayName}/{node.NodeId}: committed action node has no action id.");
                    }
                    else if (actions != null && actions.Count > 0 && actions.All(action => action.ActionId != actionNode.ActionId))
                    {
                        result.Warning($"{tree.DisplayName}/{node.NodeId}: action id `{actionNode.ActionId}` is not in the owning action profiles.");
                    }
                }
            }

            if (tree.RootNode != null)
            {
                var visited = new HashSet<EnemyBehaviorTreeNodeDefinition>();
                WalkTree(tree.RootNode, visited);
                foreach (var node in nodes.Where(node => node != null && !visited.Contains(node)))
                {
                    result.Warning($"{tree.DisplayName}/{node.NodeId}: node is unreachable from root.");
                }
            }
        }

        private static void ValidateSpacing(EnemySpacingProfileDefinition spacing, IReadOnlyCollection<EnemyActionProfileDefinition> actions, EnemyAuthoringValidationResult result)
        {
            if (spacing == null)
            {
                result.Error("Spacing profile reference is missing.");
                return;
            }

            if (spacing.DefaultIdealDistanceMeters <= 0f)
            {
                result.Error($"{spacing.DisplayName}: default ideal distance must be positive.");
            }

            if (actions == null || actions.Count == 0)
            {
                return;
            }

            foreach (var action in actions.Where(action => action != null && action.UsageState == EnemyActionUsageState.CurrentRuntime))
            {
                if (spacing.ResolveOverride(action.ActionId) == null)
                {
                    result.Warning($"{spacing.DisplayName}: action `{action.ActionId}` uses fallback spacing.");
                }
            }
        }

        private static void ValidatePresentationRole(PresentationPrefabRole role, EnemyAuthoringValidationResult result, string label)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>("Assets/_Hollow/Resources/Hollow/Presentation/PresentationContentCatalog.asset");
            if (catalog == null)
            {
                result.Warning("PresentationContentCatalog not found; prefab role bindings cannot be checked.");
                return;
            }

            if (!catalog.TryGetPrefab(role, out _) && role != PresentationPrefabRole.EnemyProjectile)
            {
                result.Warning($"{label}: role `{role}` is not bound in the presentation catalog and will use fallback art.");
            }
        }

        private static void ValidateDuplicateIds(IEnumerable<string> ids, string label, EnemyAuthoringValidationResult result, bool asWarning = false)
        {
            foreach (var duplicate in ids.Where(id => !string.IsNullOrWhiteSpace(id)).GroupBy(id => id).Where(group => group.Count() > 1))
            {
                if (asWarning)
                {
                    result.Warning($"Duplicate {label} id `{duplicate.Key}`.");
                }
                else
                {
                    result.Error($"Duplicate {label} id `{duplicate.Key}`.");
                }
            }
        }

        private static void WalkTree(EnemyBehaviorTreeNodeDefinition node, HashSet<EnemyBehaviorTreeNodeDefinition> visited)
        {
            if (node == null || !visited.Add(node))
            {
                return;
            }

            foreach (var child in node.Children)
            {
                WalkTree(child, visited);
            }
        }
    }

    public static class EnemyBehaviorCommandKindAuthoringExtensions
    {
        public static bool StartsCommittedAction(this EnemyBehaviorCommandKind kind)
        {
            return kind is EnemyBehaviorCommandKind.StartMeleeAction
                or EnemyBehaviorCommandKind.StartRangedAction
                or EnemyBehaviorCommandKind.StartChargeAction
                or EnemyBehaviorCommandKind.StartAreaAction
                or EnemyBehaviorCommandKind.StartFeintWarning
                or EnemyBehaviorCommandKind.StartGuardAction
                or EnemyBehaviorCommandKind.StartCreatureMoveAction
                or EnemyBehaviorCommandKind.StartCreatureSignalAction;
        }
    }
}
