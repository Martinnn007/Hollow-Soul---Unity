using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Hollow.Combat.UnityBehaviorNodes
{
    [Serializable]
    [GeneratePropertyBag]
    [Condition(
        name: "Hollow Enemy Is Engaged",
        story: "Hollow enemy is engaged",
        category: "Hollow/Conditions",
        id: "585df6af852e4d6b80efda1bdf1a81d9")]
    public sealed partial class HollowEnemyEngagedCondition : Condition
    {
        public override bool IsTrue()
        {
            return ResolveBridge(GameObject)?.CurrentBlackboard.IsEngaged == true;
        }

        internal static EnemyUnityBehaviorGraphBridge ResolveBridge(GameObject gameObject)
        {
            return gameObject != null ? gameObject.GetComponent<EnemyUnityBehaviorGraphBridge>() : null;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [Condition(
        name: "Hollow Enemy Is Endangered",
        story: "Hollow enemy is endangered",
        category: "Hollow/Conditions",
        id: "3b7ce22be40b4a3e9fbf9716e0974f67")]
    public sealed partial class HollowEnemyEndangeredCondition : Condition
    {
        public override bool IsTrue()
        {
            return HollowEnemyEngagedCondition.ResolveBridge(GameObject)?.CurrentBlackboard.IsEndangered == true;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [Condition(
        name: "Hollow Enemy Should Flee",
        story: "Hollow enemy should flee",
        category: "Hollow/Conditions",
        id: "18949e503db84fa2aa59a98062e1c534")]
    public sealed partial class HollowEnemyShouldFleeCondition : Condition
    {
        public override bool IsTrue()
        {
            return HollowEnemyEngagedCondition.ResolveBridge(GameObject)?.CurrentBlackboard.ShouldFlee == true;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [Condition(
        name: "Hollow Enemy Is Alerted",
        story: "Hollow enemy is alerted",
        category: "Hollow/Conditions",
        id: "15afe82c397142eca90a3759ef00a188")]
    public sealed partial class HollowEnemyAlertedCondition : Condition
    {
        public override bool IsTrue()
        {
            return HollowEnemyEngagedCondition.ResolveBridge(GameObject)?.CurrentBlackboard.IsAlertedOrBetter == true;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [Condition(
        name: "Hollow Enemy Can Start Action",
        story: "Hollow enemy can start [ActionId]",
        category: "Hollow/Conditions",
        id: "7b361929c8bf4204af6362badffb1d1a")]
    public sealed partial class HollowEnemyCanStartActionCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<int> CommandKind = new((int)EnemyBehaviorCommandKind.StartMeleeAction);
        [SerializeReference] public BlackboardVariable<string> ActionId = new(string.Empty);

        public override bool IsTrue()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            return bridge != null &&
                bridge.CanStartCommand((EnemyBehaviorCommandKind)CommandKind.Value, ActionId.Value);
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [Condition(
        name: "Hollow Enemy In Action Range",
        story: "Player is in range for [ActionId]",
        category: "Hollow/Conditions",
        id: "6dcc5cbac9b34883bdb4b9fe9ea962f5")]
    public sealed partial class HollowEnemyInActionRangeCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<string> ActionId = new(string.Empty);

        public override bool IsTrue()
        {
            return HollowEnemyEngagedCondition.ResolveBridge(GameObject)?.IsInActionRange(ActionId.Value) == true;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Set Command",
        description: "Writes a Hollow enemy behavior command from a Unity Behavior graph.",
        story: "Set Hollow command [CommandKind] using [ActionId].",
        icon: "",
        category: "Hollow",
        id: "7cf7940c782d4e6db67ff24a7c8be1b1",
        hideInSearch: false)]
    public sealed partial class HollowEnemySetCommandAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<int> CommandKind = new((int)EnemyBehaviorCommandKind.Hold);
        [SerializeReference] public BlackboardVariable<string> ActionId = new(string.Empty);
        [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new(1f);
        [SerializeReference] public BlackboardVariable<string> Reason = new("unity_behavior_set_command");

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(
                (EnemyBehaviorCommandKind)Mathf.Clamp(CommandKind.Value, 0, (int)EnemyBehaviorCommandKind.StartCreatureSignalAction),
                ActionId.Value,
                SpeedMultiplier.Value,
                Reason.Value);
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Wander",
        description: "Requests Hollow wander movement.",
        story: "Ask Hollow enemy to wander.",
        icon: "",
        category: "Hollow/Movement",
        id: "572d3df1b45943d084478a5d090fa390",
        hideInSearch: false)]
    public sealed partial class HollowEnemyWanderAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new(1f);

        protected override Node.Status OnStart()
        {
            return Set(EnemyBehaviorCommandKind.Wander, string.Empty, SpeedMultiplier.Value, "unity_behavior_wander");
        }

        private Node.Status Set(EnemyBehaviorCommandKind kind, string actionId, float speed, string reason)
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(kind, actionId, speed, reason);
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Chase Or Approach",
        description: "Requests Hollow action-spacing approach movement.",
        story: "Ask Hollow enemy to approach its action range.",
        icon: "",
        category: "Hollow/Movement",
        id: "d4212d6047e94a649a82bb38f5c375bf",
        hideInSearch: false)]
    public sealed partial class HollowEnemyChaseApproachAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new(1f);

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, SpeedMultiplier.Value, "unity_behavior_approach");
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Notice Player",
        description: "Requests a readable notice/face-player beat without starting damage.",
        story: "Notice the player.",
        icon: "",
        category: "Hollow/Intent",
        id: "0d39e0c43c9349219dc63bdb63fb46bb",
        hideInSearch: false)]
    public sealed partial class HollowEnemyNoticePlayerAction : Unity.Behavior.Action
    {
        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(EnemyBehaviorCommandKind.FacePlayer, string.Empty, 0f, "unity_behavior_notice_player");
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Investigate Noise",
        description: "Requests Hollow investigation movement toward the last disturbance.",
        story: "Investigate the last noise.",
        icon: "",
        category: "Hollow/Intent",
        id: "ea441e64d8954a01b4d3422d22e810ac",
        hideInSearch: false)]
    public sealed partial class HollowEnemyInvestigateNoiseAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new(0.8f);

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(EnemyBehaviorCommandKind.Wander, string.Empty, SpeedMultiplier.Value, "unity_behavior_investigate_noise");
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Circle",
        description: "Requests tactical circling/repositioning through Hollow spacing and NavMesh.",
        story: "Circle or reposition around the player.",
        icon: "",
        category: "Hollow/Intent",
        id: "241f1626e7b942de96ef29841d7fa91f",
        hideInSearch: false)]
    public sealed partial class HollowEnemyCircleAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new(0.75f);

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(EnemyBehaviorCommandKind.MovePreferredRange, string.Empty, SpeedMultiplier.Value, "unity_behavior_circle");
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Request Attack Slot",
        description: "Requests an attack lane; Hollow scorer, tactical director, and budgets choose/approve the concrete action.",
        story: "Request Hollow attack slot [CommandKind].",
        icon: "",
        category: "Hollow/Action",
        id: "c397388cc823418189445ec9623fd080",
        hideInSearch: false)]
    public sealed partial class HollowEnemyRequestAttackSlotAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<int> CommandKind = new((int)EnemyBehaviorCommandKind.StartMeleeAction);
        [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new(1f);

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            var commandKind = (EnemyBehaviorCommandKind)Mathf.Clamp(CommandKind.Value, (int)EnemyBehaviorCommandKind.StartMeleeAction, (int)EnemyBehaviorCommandKind.StartCreatureSignalAction);
            bridge.SetOutputCommand(commandKind, string.Empty, SpeedMultiplier.Value, "unity_behavior_request_attack_slot");
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Recover Hold",
        description: "Requests a punishable hold/facing beat after a committed action branch.",
        story: "Recover and hold.",
        icon: "",
        category: "Hollow/Intent",
        id: "0ca989c7d9454b23a4fc0fb7f8899680",
        hideInSearch: false)]
    public sealed partial class HollowEnemyRecoverHoldAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<bool> FacePlayer = new(true);

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(
                FacePlayer.Value ? EnemyBehaviorCommandKind.FacePlayer : EnemyBehaviorCommandKind.Hold,
                string.Empty,
                0f,
                "unity_behavior_recover_hold");
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Flee",
        description: "Requests Hollow flee movement.",
        story: "Ask Hollow enemy to flee.",
        icon: "",
        category: "Hollow/Movement",
        id: "f86ebcbfa86b47ddbd56d5126135b180",
        hideInSearch: false)]
    public sealed partial class HollowEnemyFleeAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new(1f);

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(EnemyBehaviorCommandKind.Flee, string.Empty, SpeedMultiplier.Value, "unity_behavior_flee");
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Hold Or Face",
        description: "Requests Hollow hold/face behavior.",
        story: "Ask Hollow enemy to hold and face.",
        icon: "",
        category: "Hollow/Movement",
        id: "e3c6ce515f944c1f9b28f409f064d27d",
        hideInSearch: false)]
    public sealed partial class HollowEnemyHoldFaceAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<bool> FacePlayer = new(true);

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(
                FacePlayer.Value ? EnemyBehaviorCommandKind.FacePlayer : EnemyBehaviorCommandKind.Hold,
                string.Empty,
                0f,
                FacePlayer.Value ? "unity_behavior_face_player" : "unity_behavior_hold");
            return Node.Status.Success;
        }
    }

    [Serializable]
    [GeneratePropertyBag]
    [NodeDescription(
        name: "Hollow Start Linked Action",
        description: "Requests a Hollow committed action by action id.",
        story: "Start Hollow linked action [ActionId].",
        icon: "",
        category: "Hollow/Action",
        id: "a7353716a1dc43fb8d7956e5d436df4c",
        hideInSearch: false)]
    public sealed partial class HollowEnemyStartLinkedAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<int> CommandKind = new((int)EnemyBehaviorCommandKind.StartMeleeAction);
        [SerializeReference] public BlackboardVariable<string> ActionId = new(string.Empty);
        [SerializeReference] public BlackboardVariable<float> SpeedMultiplier = new(1f);

        protected override Node.Status OnStart()
        {
            var bridge = HollowEnemyEngagedCondition.ResolveBridge(GameObject);
            if (bridge == null)
            {
                return Node.Status.Failure;
            }

            var commandKind = (EnemyBehaviorCommandKind)Mathf.Clamp(CommandKind.Value, 0, (int)EnemyBehaviorCommandKind.StartCreatureSignalAction);
            if (commandKind != EnemyBehaviorCommandKind.None &&
                commandKind != EnemyBehaviorCommandKind.Hold &&
                commandKind != EnemyBehaviorCommandKind.FacePlayer &&
                !bridge.CanStartCommand(commandKind, ActionId.Value))
            {
                return Node.Status.Failure;
            }

            bridge.SetOutputCommand(commandKind, ActionId.Value, SpeedMultiplier.Value, $"unity_behavior_start_{ActionId.Value}");
            return Node.Status.Success;
        }
    }
}
