namespace Hollow.Combat
{
    public static class EnemyUnityBehaviorPilotEvaluator
    {
        public static EnemyBehaviorCommand Evaluate(
            EnemyUnityBehaviorPilotGraphDefinition pilotGraph,
            EnemyBehaviorTreeContext context)
        {
            var kind = pilotGraph != null
                ? pilotGraph.PilotKind
                : EnemyUnityBehaviorPilotGraphDefinition.PilotKindFor(context.Enemy != null && context.Enemy.Definition != null ? context.Enemy.Definition.SpawnKind : string.Empty);
            return kind switch
            {
                EnemyUnityBehaviorPilotKind.Rat => EvaluateRat(context),
                EnemyUnityBehaviorPilotKind.SkeletonSword => EvaluateSkeletonSword(context),
                EnemyUnityBehaviorPilotKind.CritterFamily => EvaluateCritterFamily(context),
                EnemyUnityBehaviorPilotKind.ChaserFamily => EvaluateChaserFamily(context),
                EnemyUnityBehaviorPilotKind.WeaponUserFamily => EvaluateWeaponUserFamily(context),
                EnemyUnityBehaviorPilotKind.RangedFirearmFamily => EvaluateRangedFirearmFamily(context),
                EnemyUnityBehaviorPilotKind.MagicGhostFamily => EvaluateMagicGhostFamily(context),
                _ => EnemyBehaviorCommand.None("unity_behavior_no_pilot")
            };
        }

        private static EnemyBehaviorCommand EvaluateRat(EnemyBehaviorTreeContext context)
        {
            if (!context.IsIdle)
            {
                return EnemyBehaviorCommand.None("unity_behavior_rat_committed");
            }

            if (context.IsEndangered)
            {
                if (context.CanStartCreatureMoveAction("skitter_retreat"))
                {
                    return new EnemyBehaviorCommand(
                        EnemyBehaviorCommandKind.StartCreatureMoveAction,
                        "skitter_retreat",
                        1.1f,
                        "unity_behavior_rat_skitter_retreat");
                }

                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Flee, string.Empty, 1.1f, "unity_behavior_rat_flee");
            }

            if (context.Awareness >= EnemyAwarenessState.Engaged &&
                context.DistanceToPlayer <= 1.35f &&
                context.CanStartMeleeAction("rat_bite"))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartMeleeAction, "rat_bite", 1f, "unity_behavior_rat_bite");
            }

            if (context.Awareness >= EnemyAwarenessState.Alerted &&
                context.DistanceToPlayer <= 2.6f &&
                context.Enemy != null &&
                context.Enemy.CanStartBehaviorCommand(EnemyBehaviorCommandKind.StartFeintWarning, "warning_squeal", context.TimeSeconds))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartFeintWarning, "warning_squeal", 0f, "unity_behavior_rat_warning");
            }

            if (context.Awareness >= EnemyAwarenessState.Suspicious)
            {
                return context.DistanceToPlayer > 1.15f
                    ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 1f, "unity_behavior_rat_pressure")
                    : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_rat_face_close");
            }

            return context.Deterministic01("unity_behavior_rat_idle") > 0.18f
                ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Wander, string.Empty, 1f, "unity_behavior_rat_random_wander")
                : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "unity_behavior_rat_pause");
        }

        private static EnemyBehaviorCommand EvaluateSkeletonSword(EnemyBehaviorTreeContext context)
        {
            if (!context.IsIdle)
            {
                return EnemyBehaviorCommand.None("unity_behavior_skeleton_committed");
            }

            if (context.Awareness < EnemyAwarenessState.Alerted)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_skeleton_idle_face");
            }

            if (context.CanStartMeleeAction("rusty_slash"))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartMeleeAction, "rusty_slash", 0.9f, "unity_behavior_skeleton_rusty_slash");
            }

            if (context.DistanceToPlayer > 1.15f)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 0.9f, "unity_behavior_skeleton_close_to_slash");
            }

            return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_skeleton_face_player");
        }

        private static EnemyBehaviorCommand EvaluateCritterFamily(EnemyBehaviorTreeContext context)
        {
            if (context.BehaviorId == EnemyBehaviorId.Rat)
            {
                return EvaluateRat(context);
            }

            if (!context.IsIdle)
            {
                return EnemyBehaviorCommand.None("unity_behavior_critter_committed");
            }

            if (context.IsEndangered)
            {
                var retreat = PreferredCreatureMoveAction(context);
                if (!string.IsNullOrWhiteSpace(retreat) && context.CanStartCreatureMoveAction(retreat))
                {
                    return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartCreatureMoveAction, retreat, 1.15f, "unity_behavior_critter_retreat");
                }

                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Flee, string.Empty, 1.15f, "unity_behavior_critter_flee");
            }

            if (context.Awareness >= EnemyAwarenessState.Engaged)
            {
                if (context.DistanceToPlayer <= 2.15f && HasStartableAction(context, EnemyBehaviorCommandKind.StartMeleeAction))
                {
                    return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartMeleeAction, string.Empty, 1f, "unity_behavior_critter_commit_body_attack");
                }

                if (HasStartableAction(context, EnemyBehaviorCommandKind.StartCreatureSignalAction) &&
                    context.Deterministic01("unity_behavior_critter_signal") > 0.72f)
                {
                    return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartCreatureSignalAction, string.Empty, 0f, "unity_behavior_critter_signal");
                }

                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 1f, "unity_behavior_critter_skirmish");
            }

            if (context.Awareness >= EnemyAwarenessState.Alerted)
            {
                if (context.Disposition == EnemyInstinctDisposition.Prey && context.DistanceToPlayer <= 2.4f)
                {
                    return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Flee, string.Empty, 1.1f, "unity_behavior_critter_startle_flee");
                }

                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_critter_warn_face");
            }

            return context.Deterministic01("unity_behavior_critter_idle") > 0.22f
                ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Wander, string.Empty, 1f, "unity_behavior_critter_wander")
                : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "unity_behavior_critter_pause");
        }

        private static EnemyBehaviorCommand EvaluateChaserFamily(EnemyBehaviorTreeContext context)
        {
            if (!context.IsIdle)
            {
                return EnemyBehaviorCommand.None("unity_behavior_chaser_committed");
            }

            if (context.Disposition == EnemyInstinctDisposition.Prey &&
                !context.IsEndangered &&
                context.Awareness < EnemyAwarenessState.Engaged)
            {
                return context.DistanceToPlayer <= 2.8f
                    ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Flee, string.Empty, 0.95f, "unity_behavior_chaser_prey_keepaway")
                    : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Wander, string.Empty, 0.8f, "unity_behavior_chaser_prey_wander");
            }

            if (context.Awareness < EnemyAwarenessState.Alerted)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Wander, string.Empty, 0.75f, "unity_behavior_chaser_patrol");
            }

            if (context.BehaviorId == EnemyBehaviorId.Charger &&
                context.CanStartChargeAttack &&
                context.Awareness >= EnemyAwarenessState.Engaged)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartChargeAction, string.Empty, 1f, "unity_behavior_chaser_charge_intent");
            }

            if (context.DistanceToPlayer <= 2.15f &&
                HasStartableAction(context, EnemyBehaviorCommandKind.StartAreaAction) &&
                context.Deterministic01("unity_behavior_chaser_area") > 0.58f)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartAreaAction, string.Empty, 0.9f, "unity_behavior_chaser_area_intent");
            }

            if (context.Awareness >= EnemyAwarenessState.Engaged &&
                HasStartableAction(context, EnemyBehaviorCommandKind.StartMeleeAction))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartMeleeAction, string.Empty, 1f, "unity_behavior_chaser_melee_intent");
            }

            return context.DistanceToPlayer > 1.05f
                ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 1f, "unity_behavior_chaser_close_to_commit")
                : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_chaser_face_close");
        }

        private static EnemyBehaviorCommand EvaluateWeaponUserFamily(EnemyBehaviorTreeContext context)
        {
            if (context.BehaviorId == EnemyBehaviorId.SkeletonSword)
            {
                return EvaluateSkeletonSword(context);
            }

            if (!context.IsIdle)
            {
                return EnemyBehaviorCommand.None("unity_behavior_weapon_committed");
            }

            if (context.Awareness < EnemyAwarenessState.Alerted)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_weapon_hold_ready");
            }

            if (context.BehaviorId == EnemyBehaviorId.Knight &&
                context.Awareness >= EnemyAwarenessState.Engaged &&
                HasStartableAction(context, EnemyBehaviorCommandKind.StartGuardAction) &&
                context.Deterministic01("unity_behavior_weapon_guard") > 0.62f)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartGuardAction, string.Empty, 0f, "unity_behavior_weapon_guard_intent");
            }

            if (context.BehaviorId == EnemyBehaviorId.Giant &&
                context.DistanceToPlayer <= 2.4f &&
                HasStartableAction(context, EnemyBehaviorCommandKind.StartAreaAction))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartAreaAction, string.Empty, 0.75f, "unity_behavior_weapon_giant_area_intent");
            }

            if (HasStartableAction(context, EnemyBehaviorCommandKind.StartMeleeAction))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartMeleeAction, string.Empty, 0.9f, "unity_behavior_weapon_melee_intent");
            }

            return context.DistanceToPlayer > 1.1f
                ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 0.85f, "unity_behavior_weapon_approach_envelope")
                : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_weapon_face_player");
        }

        private static EnemyBehaviorCommand EvaluateRangedFirearmFamily(EnemyBehaviorTreeContext context)
        {
            if (!context.IsIdle)
            {
                return EnemyBehaviorCommand.None("unity_behavior_ranged_committed");
            }

            if (context.Awareness < EnemyAwarenessState.Alerted && !context.ShouldSentinelEngage)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "unity_behavior_ranged_sentry_hold");
            }

            if (context.DistanceToPlayer < 2.25f &&
                context.Enemy != null &&
                context.Enemy.SpeedMetersPerSecond > 0f)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 0.85f, "unity_behavior_ranged_reset_distance");
            }

            if (HasStartableAction(context, EnemyBehaviorCommandKind.StartRangedAction))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartRangedAction, string.Empty, 1f, "unity_behavior_ranged_fire_intent");
            }

            return context.Enemy != null && context.Enemy.SpeedMetersPerSecond > 0f
                ? new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 0.75f, "unity_behavior_ranged_reposition")
                : new EnemyBehaviorCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_ranged_face_player");
        }

        private static EnemyBehaviorCommand EvaluateMagicGhostFamily(EnemyBehaviorTreeContext context)
        {
            if (!context.IsIdle)
            {
                return EnemyBehaviorCommand.None("unity_behavior_magic_committed");
            }

            if (context.Awareness < EnemyAwarenessState.Alerted)
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.Hold, string.Empty, 0f, "unity_behavior_magic_wait");
            }

            if (context.IsEndangered &&
                HasStartableAction(context, EnemyBehaviorCommandKind.StartCreatureMoveAction))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartCreatureMoveAction, string.Empty, 1f, "unity_behavior_magic_phase_or_drift");
            }

            if (context.DistanceToPlayer <= 2.2f &&
                HasStartableAction(context, EnemyBehaviorCommandKind.StartAreaAction))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartAreaAction, string.Empty, 0.85f, "unity_behavior_magic_area_pressure");
            }

            if (HasStartableAction(context, EnemyBehaviorCommandKind.StartRangedAction))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartRangedAction, string.Empty, 1f, "unity_behavior_magic_cast_intent");
            }

            if (HasStartableAction(context, EnemyBehaviorCommandKind.StartMeleeAction))
            {
                return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.StartMeleeAction, string.Empty, 0.9f, "unity_behavior_magic_close_pressure");
            }

            return new EnemyBehaviorCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, 0.75f, "unity_behavior_magic_reposition");
        }

        private static string PreferredCreatureMoveAction(EnemyBehaviorTreeContext context)
        {
            return context.BehaviorId switch
            {
                EnemyBehaviorId.Rat => "skitter_retreat",
                EnemyBehaviorId.Spider => "panic_flee",
                EnemyBehaviorId.HollowBird => "wing_retreat",
                EnemyBehaviorId.HollowBeast => "leap_back",
                EnemyBehaviorId.FlyingChaser => "fly_strafe",
                _ => string.Empty
            };
        }

        private static bool HasStartableAction(EnemyBehaviorTreeContext context, EnemyBehaviorCommandKind commandKind)
        {
            if (context.Enemy == null || context.Enemy.Definition == null)
            {
                return false;
            }

            var actions = context.Enemy.Definition.ActionProfiles;
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index];
                if (action == null || action.UsageState != EnemyActionUsageState.CurrentRuntime)
                {
                    continue;
                }

                var attackId = action.LinkedAttackId;
                var attack = context.Enemy.Definition.ResolveAttackProfile(attackId);
                var resolvedKind = CommandKindFor(action, attack);
                if (resolvedKind != commandKind)
                {
                    continue;
                }

                if (context.Enemy.CanStartBehaviorCommand(commandKind, attack != null ? attack.AttackId : action.ActionId, context.TimeSeconds))
                {
                    return true;
                }
            }

            if (commandKind == EnemyBehaviorCommandKind.StartChargeAction)
            {
                return context.CanStartChargeAttack;
            }

            if (commandKind == EnemyBehaviorCommandKind.StartRangedAction)
            {
                return context.CanStartRangedAttack;
            }

            return false;
        }

        private static EnemyBehaviorCommandKind CommandKindFor(EnemyActionProfileDefinition action, EnemyAttackProfileDefinition attack)
        {
            if (attack != null)
            {
                return attack.RuntimeKind switch
                {
                    EnemyAttackRuntimeKind.MeleeLunge or EnemyAttackRuntimeKind.Contact or EnemyAttackRuntimeKind.WeaponMelee => EnemyBehaviorCommandKind.StartMeleeAction,
                    EnemyAttackRuntimeKind.Projectile or EnemyAttackRuntimeKind.FanProjectile or EnemyAttackRuntimeKind.RadialProjectile or EnemyAttackRuntimeKind.SequentialRadialProjectile or EnemyAttackRuntimeKind.Beam or EnemyAttackRuntimeKind.LockingBeam => EnemyBehaviorCommandKind.StartRangedAction,
                    EnemyAttackRuntimeKind.Charge => EnemyBehaviorCommandKind.StartChargeAction,
                    EnemyAttackRuntimeKind.Area => EnemyBehaviorCommandKind.StartAreaAction,
                    EnemyAttackRuntimeKind.Defense => EnemyBehaviorCommandKind.StartGuardAction,
                    EnemyAttackRuntimeKind.CreatureMove or EnemyAttackRuntimeKind.PhaseMove => EnemyBehaviorCommandKind.StartCreatureMoveAction,
                    EnemyAttackRuntimeKind.CreatureSignal => EnemyBehaviorCommandKind.StartCreatureSignalAction,
                    _ => EnemyBehaviorCommandKind.None
                };
            }

            if (action == null)
            {
                return EnemyBehaviorCommandKind.None;
            }

            return action.Intent switch
            {
                EnemyActionIntent.Defend => EnemyBehaviorCommandKind.StartGuardAction,
                EnemyActionIntent.Escape or EnemyActionIntent.Reposition => EnemyBehaviorCommandKind.StartCreatureMoveAction,
                _ => EnemyBehaviorCommandKind.None
            };
        }
    }
}
