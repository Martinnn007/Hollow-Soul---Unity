using Unity.Behavior;
using UnityEngine;

namespace Hollow.Combat
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Unity Behavior Pilot Graph", fileName = "EnemyUnityBehaviorPilotGraph")]
    public sealed class EnemyUnityBehaviorPilotGraphDefinition : ScriptableObject
    {
        private static readonly string[] MigratedSpawnKinds =
        {
            "spawnEnemyNormal",
            "spawnEnemyFlying",
            "spawnEnemyFast",
            "spawnEnemyHeavy",
            "spawnEnemyCharger",
            "spawnEnemySplitter",
            "spawnEnemyRat",
            "spawnEnemySpider",
            "spawnEnemyHollowBird",
            "spawnEnemyHollowBeast",
            "spawnEnemySkeletonSword",
            "spawnEnemySkeletonSpear",
            "spawnEnemyKnight",
            "spawnEnemyGiant",
            "spawnEnemyTurret",
            "spawnEnemySpittingPod",
            "spawnEnemyHollowArcher",
            "spawnEnemyPowderGunner",
            "spawnEnemyKnifeThrower",
            "spawnEnemyRepeaterTurret",
            "spawnEnemyClockworkSentry",
            "spawnEnemyStarforgedOctantSentry",
            "spawnEnemyCrimsonRailSpider",
            "spawnEnemyAzureMinigunTurret",
            "spawnEnemyHollowAcolyte",
            "spawnEnemyWraith",
            "spawnEnemySoulEater",
            "spawnEnemyCurseBinder",
            "spawnEnemyGraveLantern"
        };

        [SerializeField] private string graphId = "unity_behavior_pilot";
        [SerializeField] private string displayName = "Unity Behavior Pilot";
        [SerializeField] private string ownerSpawnKind = "family:critters";
        [SerializeField] private EnemyUnityBehaviorPilotKind pilotKind = EnemyUnityBehaviorPilotKind.CritterFamily;
        [SerializeField] private BehaviorGraph behaviorGraph;
        [SerializeField] private int schemaVersion = EnemyUnityBehaviorBlackboardSchema.SchemaVersion;
        [SerializeField] private bool requiresOfficialBehaviorGraph = true;
        [SerializeField] private EnemyUnityBehaviorFallbackPolicy fallbackPolicy = EnemyUnityBehaviorFallbackPolicy.EmergencyOnly;
        [SerializeField] private string[] requiredBlackboardInputs = EnemyUnityBehaviorBlackboardSchema.CopyRequiredInputs();
        [SerializeField] private string[] requiredBlackboardOutputs = EnemyUnityBehaviorBlackboardSchema.CopyRequiredOutputs();
        [SerializeField] private string notes = "Official Unity Behavior graph slot; emergency fallback keeps the pilot playable if the graph is missing or invalid.";

        public string GraphId => string.IsNullOrWhiteSpace(graphId) ? "unity_behavior_pilot" : graphId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? GraphId : displayName;

        public string OwnerSpawnKind => ownerSpawnKind ?? string.Empty;

        public EnemyUnityBehaviorPilotKind PilotKind => pilotKind;

        public BehaviorGraph BehaviorGraph => behaviorGraph;

        public int SchemaVersion => schemaVersion;

        public bool RequiresOfficialBehaviorGraph => requiresOfficialBehaviorGraph;

        public EnemyUnityBehaviorFallbackPolicy FallbackPolicy => fallbackPolicy;

        public bool AllowsEmergencyFallback => fallbackPolicy == EnemyUnityBehaviorFallbackPolicy.EmergencyOnly;

        public System.Collections.Generic.IReadOnlyList<string> RequiredBlackboardInputs => requiredBlackboardInputs ?? EnemyUnityBehaviorBlackboardSchema.RequiredInputNames;

        public System.Collections.Generic.IReadOnlyList<string> RequiredBlackboardOutputs => requiredBlackboardOutputs ?? EnemyUnityBehaviorBlackboardSchema.RequiredOutputNames;

        public string Notes => notes ?? string.Empty;

        public static System.Collections.Generic.IReadOnlyList<string> MigratedUnityBehaviorSpawnKinds => MigratedSpawnKinds;

        public void Configure(
            string nextGraphId,
            string nextDisplayName,
            string nextOwnerSpawnKind,
            EnemyUnityBehaviorPilotKind nextPilotKind,
            BehaviorGraph nextBehaviorGraph,
            string nextNotes)
        {
            ConfigureHardened(
                nextGraphId,
                nextDisplayName,
                nextOwnerSpawnKind,
                nextPilotKind,
                nextBehaviorGraph,
                EnemyUnityBehaviorFallbackPolicy.EmergencyOnly,
                true,
                nextNotes);
        }

        public void ConfigureHardened(
            string nextGraphId,
            string nextDisplayName,
            string nextOwnerSpawnKind,
            EnemyUnityBehaviorPilotKind nextPilotKind,
            BehaviorGraph nextBehaviorGraph,
            EnemyUnityBehaviorFallbackPolicy nextFallbackPolicy,
            bool nextRequiresOfficialBehaviorGraph,
            string nextNotes)
        {
            graphId = string.IsNullOrWhiteSpace(nextGraphId) ? "unity_behavior_pilot" : nextGraphId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? graphId : nextDisplayName;
            ownerSpawnKind = nextOwnerSpawnKind ?? string.Empty;
            pilotKind = nextPilotKind;
            behaviorGraph = nextBehaviorGraph;
            schemaVersion = EnemyUnityBehaviorBlackboardSchema.SchemaVersion;
            requiresOfficialBehaviorGraph = nextRequiresOfficialBehaviorGraph;
            fallbackPolicy = nextFallbackPolicy;
            requiredBlackboardInputs = EnemyUnityBehaviorBlackboardSchema.CopyRequiredInputs();
            requiredBlackboardOutputs = EnemyUnityBehaviorBlackboardSchema.CopyRequiredOutputs();
            notes = nextNotes ?? string.Empty;
        }

        public static bool IsPilotSpawnKind(string spawnKind)
        {
            return PilotKindFor(spawnKind) != EnemyUnityBehaviorPilotKind.None;
        }

        public static EnemyUnityBehaviorPilotKind PilotKindFor(string spawnKind)
        {
            return spawnKind switch
            {
                "spawnEnemyRat" or
                    "spawnEnemySpider" or
                    "spawnEnemyHollowBird" or
                    "spawnEnemyHollowBeast" => EnemyUnityBehaviorPilotKind.CritterFamily,
                "spawnEnemyNormal" or
                    "spawnEnemyFlying" or
                    "spawnEnemyFast" or
                    "spawnEnemyHeavy" or
                    "spawnEnemyCharger" or
                    "spawnEnemySplitter" => EnemyUnityBehaviorPilotKind.ChaserFamily,
                "spawnEnemySkeletonSword" or
                    "spawnEnemySkeletonSpear" or
                    "spawnEnemyKnight" or
                    "spawnEnemyGiant" => EnemyUnityBehaviorPilotKind.WeaponUserFamily,
                "spawnEnemyTurret" or
                    "spawnEnemySpittingPod" or
                    "spawnEnemyHollowArcher" or
                    "spawnEnemyPowderGunner" or
                    "spawnEnemyKnifeThrower" or
                    "spawnEnemyRepeaterTurret" or
                    "spawnEnemyClockworkSentry" or
                    "spawnEnemyStarforgedOctantSentry" or
                    "spawnEnemyCrimsonRailSpider" or
                    "spawnEnemyAzureMinigunTurret" => EnemyUnityBehaviorPilotKind.RangedFirearmFamily,
                "spawnEnemyHollowAcolyte" or
                    "spawnEnemyWraith" or
                    "spawnEnemySoulEater" or
                    "spawnEnemyCurseBinder" or
                    "spawnEnemyGraveLantern" => EnemyUnityBehaviorPilotKind.MagicGhostFamily,
                _ => EnemyUnityBehaviorPilotKind.None
            };
        }

        public static string FamilyDisplayName(EnemyUnityBehaviorPilotKind kind)
        {
            return kind switch
            {
                EnemyUnityBehaviorPilotKind.Rat => "Critter / Rat",
                EnemyUnityBehaviorPilotKind.SkeletonSword => "Weapon User / Skeleton Sword",
                EnemyUnityBehaviorPilotKind.CritterFamily => "Critters",
                EnemyUnityBehaviorPilotKind.ChaserFamily => "Chasers",
                EnemyUnityBehaviorPilotKind.WeaponUserFamily => "Weapon Users",
                EnemyUnityBehaviorPilotKind.RangedFirearmFamily => "Ranged + Firearm",
                EnemyUnityBehaviorPilotKind.MagicGhostFamily => "Magic + Ghost",
                _ => "None"
            };
        }

        public static EnemyUnityBehaviorPilotGraphDefinition CreateRuntimeDefault(string spawnKind)
        {
            var definition = CreateInstance<EnemyUnityBehaviorPilotGraphDefinition>();
            var kind = PilotKindFor(spawnKind);
            var readable = FamilyDisplayName(kind);
            definition.Configure(
                $"m105_{spawnKind}_unity_behavior",
                $"M105 Unity Behavior {readable}",
                spawnKind,
                kind,
                null,
                "Runtime M105 family graph contract. Assign an official Unity BehaviorGraph asset; emergency fallback is used only if that graph is missing or invalid.");
            definition.hideFlags = HideFlags.HideAndDontSave;
            return definition;
        }
    }
}
