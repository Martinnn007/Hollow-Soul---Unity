using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.BehaviorTreeStudio
{
    public static class BehaviourTreeStudioTemplateGenerator
    {
        public const string TemplateFolder = "Assets/_Hollow/Data/EnemyBehaviorTreeTemplates";

        [MenuItem("Hollow/Enemy Authoring/Generate Behaviour Tree Templates")]
        public static void GenerateAssets()
        {
            Directory.CreateDirectory(TemplateFolder);
            foreach (var template in CreateRuntimeTemplates())
            {
                var path = $"{TemplateFolder}/{SanitizeFileName(template.TemplateId)}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<EnemyBehaviorTreeTemplateDefinition>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(template, path);
                    foreach (var node in template.Nodes)
                    {
                        AssetDatabase.AddObjectToAsset(node, template);
                    }
                }
                else
                {
                    ReplaceTemplateAsset(existing, template);
                    UnityEngine.Object.DestroyImmediate(template);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static IReadOnlyList<EnemyBehaviorTreeTemplateDefinition> CreateRuntimeTemplates()
        {
            return new[]
            {
                BuildChaserPressure(),
                BuildPreyFleeCommit(),
                BuildStationaryRangedSentinel(),
                BuildWeaponUser(),
                BuildCreatureSkirmisher(),
                BuildCasterRanged(),
                BuildBossMetadata()
            };
        }

        private static EnemyBehaviorTreeTemplateDefinition BuildChaserPressure()
        {
            var b = new TemplateBuilder("tpl_chaser_pressure");
            var root = b.Selector(
                "root_pressure",
                b.Sequence("seq_melee_commit", b.Condition("if_can_melee", EnemyBehaviorConditionKind.CanStartMeleeAction, actionId: "claw_lunge"), b.Action("start_melee", EnemyBehaviorCommandKind.StartMeleeAction, "claw_lunge", reason: "Commit to nearest melee action.")),
                b.Sequence("seq_too_close_reset", b.Condition("if_too_close", EnemyBehaviorConditionKind.IsTooClose), b.Action("short_reset", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.75f, reason: "Short spacing reset.")),
                b.Action("pressure_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 1f, reason: "Close to action spacing."));
            return b.Template(
                "template_chaser_pressure",
                "Chaser Pressure",
                "Aggressive body enemy: commit from idle, reset briefly when too close, otherwise close to action spacing.",
                EnemyBehaviorTreeTemplateRole.ChaserPressure,
                EnemyBehaviorId.Chaser,
                EnemyInstinctDisposition.Predator,
                EnemyIntelligenceLevel.Simple,
                false,
                root);
        }

        private static EnemyBehaviorTreeTemplateDefinition BuildPreyFleeCommit()
        {
            var b = new TemplateBuilder("tpl_prey_flee_commit");
            var root = b.Selector(
                "root_prey",
                b.Sequence("seq_endangered_attack", b.Condition("if_endangered", EnemyBehaviorConditionKind.IsEndangered), b.Condition("if_can_panic", EnemyBehaviorConditionKind.CanStartMeleeAction, actionId: "panic_peck"), b.Action("panic_commit", EnemyBehaviorCommandKind.StartMeleeAction, "panic_peck", reason: "Panic commit while endangered.")),
                b.Sequence("seq_close_flee", b.Condition("if_close", EnemyBehaviorConditionKind.DistanceLessOrEqual, threshold: 3.5f), b.Action("flee_space", EnemyBehaviorCommandKind.Flee, speed: 1f, reason: "Prey flees before committing.")),
                b.Action("prey_wander", EnemyBehaviorCommandKind.Wander, speed: 0.85f, reason: "Uncommitted prey drift."));
            return b.Template(
                "template_prey_flee_commit",
                "Prey Flee / Commit",
                "Prey identity: flee or wander until endangered, then use a short explicit panic attack.",
                EnemyBehaviorTreeTemplateRole.PreyFleeCommit,
                EnemyBehaviorId.FlyingChaser,
                EnemyInstinctDisposition.Prey,
                EnemyIntelligenceLevel.Instinctive,
                false,
                root);
        }

        private static EnemyBehaviorTreeTemplateDefinition BuildStationaryRangedSentinel()
        {
            var b = new TemplateBuilder("tpl_stationary_ranged_sentinel");
            var root = b.Selector(
                "root_stationary_sentinel",
                b.Sequence("seq_should_fire", b.Condition("if_sentinel_engaged", EnemyBehaviorConditionKind.ShouldSentinelEngage), b.Condition("if_can_ranged", EnemyBehaviorConditionKind.CanStartRangedAttack), b.Action("start_ranged", EnemyBehaviorCommandKind.StartRangedAction, "bone_dart", reason: "Fire budgeted ranged attack.")),
                b.Action("sentinel_face", EnemyBehaviorCommandKind.FacePlayer, reason: "Face and hold territory."),
                b.Action("sentinel_hold", EnemyBehaviorCommandKind.Hold, reason: "Stationary sentinel hold."));
            return b.Template(
                "template_stationary_ranged_sentinel",
                "Stationary Ranged Sentinel",
                "Blind or braced ranged enemy: hold position, face pressure, and fire only when sentinel engagement allows.",
                EnemyBehaviorTreeTemplateRole.StationaryRangedSentinel,
                EnemyBehaviorId.TurretShooter,
                EnemyInstinctDisposition.Sentinel,
                EnemyIntelligenceLevel.Basic,
                false,
                root);
        }

        private static EnemyBehaviorTreeTemplateDefinition BuildWeaponUser()
        {
            var b = new TemplateBuilder("tpl_weapon_user");
            var root = b.Selector(
                "root_weapon_user",
                b.Sequence("seq_guard_close", b.Condition("if_close", EnemyBehaviorConditionKind.IsTooClose), b.Condition("if_can_guard", EnemyBehaviorConditionKind.CanStartGuardAction, actionId: "shield_guard"), b.Action("start_guard", EnemyBehaviorCommandKind.StartGuardAction, "shield_guard", reason: "Raise guard to regain initiative.")),
                b.Sequence("seq_thrust", b.Condition("if_can_thrust", EnemyBehaviorConditionKind.CanStartMeleeAction, actionId: "spear_thrust"), b.Action("start_thrust", EnemyBehaviorCommandKind.StartMeleeAction, "spear_thrust", reason: "Commit weapon thrust.")),
                b.Action("weapon_spacing", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.85f, reason: "Maintain weapon range."));
            return b.Template(
                "template_weapon_user",
                "Weapon User",
                "Skeleton/knight style enemy: guard or commit to weapon arcs from idle, then recover punishably.",
                EnemyBehaviorTreeTemplateRole.WeaponUser,
                EnemyBehaviorId.Knight,
                EnemyInstinctDisposition.Sentinel,
                EnemyIntelligenceLevel.Basic,
                false,
                root);
        }

        private static EnemyBehaviorTreeTemplateDefinition BuildCreatureSkirmisher()
        {
            var b = new TemplateBuilder("tpl_creature_skirmisher");
            var weighted = b.Weighted(
                "skirmish_choice",
                (b.Sequence("seq_signal", b.Condition("if_alerted", EnemyBehaviorConditionKind.AwarenessAtLeast, awareness: EnemyAwarenessState.Alerted), b.Condition("if_can_signal", EnemyBehaviorConditionKind.CanStartCreatureSignalAction, actionId: "alarm_squeal"), b.Action("start_signal", EnemyBehaviorCommandKind.StartCreatureSignalAction, "alarm_squeal", reason: "Warn nearby family.")), 0.8f),
                (b.Sequence("seq_pounce", b.Condition("if_can_pounce", EnemyBehaviorConditionKind.CanStartMeleeAction, actionId: "panic_pounce"), b.Action("start_pounce", EnemyBehaviorCommandKind.StartMeleeAction, "panic_pounce", reason: "Skirmish pounce.")), 1.3f),
                (b.Action("skitter_reset", EnemyBehaviorCommandKind.Flee, speed: 1.1f, reason: "Short skitter reset."), 1f));
            var root = b.Selector(
                "root_skirmisher",
                b.Sequence("seq_endangered_flee", b.Condition("if_endangered", EnemyBehaviorConditionKind.IsEndangered), b.Condition("if_can_retreat", EnemyBehaviorConditionKind.CanStartCreatureMoveAction, actionId: "skitter_retreat"), b.Action("start_retreat", EnemyBehaviorCommandKind.StartCreatureMoveAction, "skitter_retreat", reason: "Damaged creature retreats.")),
                weighted,
                b.Action("skirmish_wander", EnemyBehaviorCommandKind.Wander, speed: 1f, reason: "Chaotic wander."));
            return b.Template(
                "template_creature_skirmisher",
                "Creature Skirmisher",
                "Rat/spider/bird style tree: skitter, signal, flee, or make one readable body commit.",
                EnemyBehaviorTreeTemplateRole.CreatureSkirmisher,
                EnemyBehaviorId.Rat,
                EnemyInstinctDisposition.Territorial,
                EnemyIntelligenceLevel.Simple,
                false,
                root);
        }

        private static EnemyBehaviorTreeTemplateDefinition BuildCasterRanged()
        {
            var b = new TemplateBuilder("tpl_caster_ranged");
            var root = b.Selector(
                "root_caster",
                b.Sequence("seq_phase_close", b.Condition("if_too_close", EnemyBehaviorConditionKind.IsTooClose), b.Condition("if_can_phase", EnemyBehaviorConditionKind.CanStartCreatureMoveAction, actionId: "phase_shift"), b.Action("start_phase", EnemyBehaviorCommandKind.StartCreatureMoveAction, "phase_shift", reason: "Phase/backstep once.")),
                b.Sequence("seq_cast", b.Condition("if_can_cast", EnemyBehaviorConditionKind.CanStartRangedAction, actionId: "soul_bolt"), b.Action("start_cast", EnemyBehaviorCommandKind.StartRangedAction, "soul_bolt", reason: "Commit ranged spell.")),
                b.Action("caster_range", EnemyBehaviorCommandKind.MovePreferredRange, speed: 0.7f, reason: "Seek caster spacing."));
            return b.Template(
                "template_caster_ranged",
                "Caster / Ranged",
                "Ranged or magic enemy: use one reset, then commit or hold punishably.",
                EnemyBehaviorTreeTemplateRole.CasterRanged,
                EnemyBehaviorId.HollowAcolyte,
                EnemyInstinctDisposition.Sentinel,
                EnemyIntelligenceLevel.Trained,
                false,
                root);
        }

        private static EnemyBehaviorTreeTemplateDefinition BuildBossMetadata()
        {
            var b = new TemplateBuilder("tpl_boss_metadata");
            var root = b.Selector(
                "root_boss_metadata",
                b.Sequence("seq_metadata_idle", b.Condition("if_idle", EnemyBehaviorConditionKind.IsIdle), b.Action("metadata_noop", EnemyBehaviorCommandKind.None, reason: "Boss trees are metadata-only in current runtime.")));
            return b.Template(
                "template_boss_metadata",
                "Boss Metadata",
                "Boss-facing preview/metadata template. Boss runtime ignores behavior trees until a later milestone.",
                EnemyBehaviorTreeTemplateRole.BossMetadata,
                EnemyBehaviorId.BossWarden,
                EnemyInstinctDisposition.Sentinel,
                EnemyIntelligenceLevel.Basic,
                true,
                root);
        }

        private static void ReplaceTemplateAsset(EnemyBehaviorTreeTemplateDefinition existing, EnemyBehaviorTreeTemplateDefinition generated)
        {
            var path = AssetDatabase.GetAssetPath(existing);
            foreach (var oldNode in AssetDatabase.LoadAllAssetsAtPath(path).OfType<EnemyBehaviorTreeNodeDefinition>().ToArray())
            {
                UnityEngine.Object.DestroyImmediate(oldNode, allowDestroyingAssets: true);
            }

            var map = new Dictionary<EnemyBehaviorTreeNodeDefinition, EnemyBehaviorTreeNodeDefinition>();
            foreach (var generatedNode in generated.Nodes)
            {
                var node = ScriptableObject.CreateInstance(generatedNode.GetType()) as EnemyBehaviorTreeNodeDefinition;
                if (node == null)
                {
                    continue;
                }

                EditorUtility.CopySerialized(generatedNode, node);
                node.name = generatedNode.name;
                AssetDatabase.AddObjectToAsset(node, existing);
                map[generatedNode] = node;
            }

            foreach (var node in map.Values)
            {
                RemapNodeReferences(node, map);
                EditorUtility.SetDirty(node);
            }

            existing.Configure(
                generated.TemplateId,
                generated.DisplayName,
                generated.Description,
                generated.Role,
                generated.RecommendedBehaviorId,
                generated.RecommendedDisposition,
                generated.MinimumIntelligence,
                generated.BossMetadataOnly,
                generated.RootNode != null && map.TryGetValue(generated.RootNode, out var root) ? root : null,
                map.Values);
            EditorUtility.SetDirty(existing);
            foreach (var generatedNode in generated.Nodes)
            {
                UnityEngine.Object.DestroyImmediate(generatedNode);
            }
        }

        private static void RemapNodeReferences(
            UnityEngine.Object target,
            IReadOnlyDictionary<EnemyBehaviorTreeNodeDefinition, EnemyBehaviorTreeNodeDefinition> map)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (property.objectReferenceValue is EnemyBehaviorTreeNodeDefinition oldNode &&
                    map.TryGetValue(oldNode, out var newNode))
                {
                    property.objectReferenceValue = newNode;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string((value ?? "template").Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        }

        private sealed class TemplateBuilder
        {
            private readonly List<EnemyBehaviorTreeNodeDefinition> nodes = new();
            private readonly string prefix;

            public TemplateBuilder(string nextPrefix)
            {
                prefix = nextPrefix;
            }

            public EnemyBehaviorTreeTemplateDefinition Template(
                string templateId,
                string displayName,
                string description,
                EnemyBehaviorTreeTemplateRole role,
                EnemyBehaviorId behaviorId,
                EnemyInstinctDisposition disposition,
                EnemyIntelligenceLevel intelligence,
                bool bossMetadataOnly,
                EnemyBehaviorTreeNodeDefinition root)
            {
                var template = ScriptableObject.CreateInstance<EnemyBehaviorTreeTemplateDefinition>();
                template.name = templateId;
                template.Configure(templateId, displayName, description, role, behaviorId, disposition, intelligence, bossMetadataOnly, root, nodes);
                return template;
            }

            public EnemyBehaviorSelectorNodeDefinition Selector(string id, params EnemyBehaviorTreeNodeDefinition[] children)
            {
                var node = Add<EnemyBehaviorSelectorNodeDefinition>(id);
                node.Configure(id, children);
                return node;
            }

            public EnemyBehaviorSequenceNodeDefinition Sequence(string id, params EnemyBehaviorTreeNodeDefinition[] children)
            {
                var node = Add<EnemyBehaviorSequenceNodeDefinition>(id);
                node.Configure(id, children);
                return node;
            }

            public EnemyBehaviorWeightedSelectorNodeDefinition Weighted(string id, params (EnemyBehaviorTreeNodeDefinition Child, float Weight)[] children)
            {
                var node = Add<EnemyBehaviorWeightedSelectorNodeDefinition>(id);
                node.Configure(id, children);
                return node;
            }

            public EnemyBehaviorConditionNodeDefinition Condition(
                string id,
                EnemyBehaviorConditionKind condition,
                bool invert = false,
                float threshold = 0f,
                string actionId = "",
                EnemyAwarenessState awareness = EnemyAwarenessState.Engaged)
            {
                var node = Add<EnemyBehaviorConditionNodeDefinition>(id);
                node.Configure(id, condition, invert, threshold, actionId, awareness);
                return node;
            }

            public EnemyBehaviorActionNodeDefinition Action(
                string id,
                EnemyBehaviorCommandKind command,
                string actionId = "",
                float speed = 1f,
                string reason = "")
            {
                var node = Add<EnemyBehaviorActionNodeDefinition>(id);
                node.Configure(id, command, actionId, speed, reason);
                return node;
            }

            private T Add<T>(string id)
                where T : EnemyBehaviorTreeNodeDefinition
            {
                var node = ScriptableObject.CreateInstance<T>();
                node.ConfigureNodeId($"{prefix}_{id}");
                nodes.Add(node);
                return node;
            }
        }
    }
}
