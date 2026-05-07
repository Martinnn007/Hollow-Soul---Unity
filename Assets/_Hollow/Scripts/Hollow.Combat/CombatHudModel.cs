namespace Hollow.Combat
{
    public readonly struct CombatHudModel
    {
        public CombatHudModel(int playerHealth, int playerMaxHealth, int enemiesRemaining, RoomObjectiveState roomState)
            : this(playerHealth, playerMaxHealth, enemiesRemaining, roomState, "Developer Sample", "None", "Shots:0")
        {
        }

        public CombatHudModel(
            int playerHealth,
            int playerMaxHealth,
            int enemiesRemaining,
            RoomObjectiveState roomState,
            string difficultyName,
            string archetypeSummary,
            string projectileSummary)
            : this(playerHealth, playerMaxHealth, enemiesRemaining, roomState, difficultyName, archetypeSummary, projectileSummary, null)
        {
        }

        public CombatHudModel(
            int playerHealth,
            int playerMaxHealth,
            int enemiesRemaining,
            RoomObjectiveState roomState,
            string difficultyName,
            string archetypeSummary,
            string projectileSummary,
            PlayerDefenseController defenseController)
            : this(playerHealth, playerMaxHealth, enemiesRemaining, roomState, difficultyName, archetypeSummary, projectileSummary, defenseController, RoomCombatEncounterContext.Empty)
        {
        }

        public CombatHudModel(
            int playerHealth,
            int playerMaxHealth,
            int enemiesRemaining,
            RoomObjectiveState roomState,
            string difficultyName,
            string archetypeSummary,
            string projectileSummary,
            PlayerDefenseController defenseController,
            RoomCombatEncounterContext encounterContext)
            : this(playerHealth, playerMaxHealth, enemiesRemaining, roomState, difficultyName, archetypeSummary, projectileSummary, defenseController, encounterContext, null)
        {
        }

        public CombatHudModel(
            int playerHealth,
            int playerMaxHealth,
            int enemiesRemaining,
            RoomObjectiveState roomState,
            string difficultyName,
            string archetypeSummary,
            string projectileSummary,
            PlayerDefenseController defenseController,
            RoomCombatEncounterContext encounterContext,
            PlayerWeaponController weaponController)
        {
            PlayerHealth = playerHealth;
            PlayerMaxHealth = playerMaxHealth;
            EnemiesRemaining = enemiesRemaining;
            RoomState = roomState;
            DifficultyName = difficultyName;
            ArchetypeSummary = archetypeSummary;
            ProjectileSummary = projectileSummary;
            Defense = defenseController != null ? defenseController.Defense : 0;
            IsGuarding = defenseController != null && defenseController.IsGuarding;
            IsInParryWindow = defenseController != null && defenseController.IsInParryWindow;
            LastGuardResult = defenseController != null ? defenseController.LastGuardResult : ShieldGuardResult.None;
            LastDamageReduction = defenseController != null ? defenseController.LastDamageReduction : 0;
            DirectorDebugLine = encounterContext != null ? encounterContext.DirectorDebugLine : "Director: --";
            RollDebugLine = weaponController != null ? weaponController.RollDebugLine : "Roll: --";
            RangedDrawDebugLine = weaponController != null ? weaponController.RangedDrawDebugLine : "Bow draw: --";
        }

        public int PlayerHealth { get; }

        public int PlayerMaxHealth { get; }

        public int EnemiesRemaining { get; }

        public RoomObjectiveState RoomState { get; }

        public string DifficultyName { get; }

        public string ArchetypeSummary { get; }

        public string ProjectileSummary { get; }

        public int Defense { get; }

        public bool IsGuarding { get; }

        public bool IsInParryWindow { get; }

        public ShieldGuardResult LastGuardResult { get; }

        public int LastDamageReduction { get; }

        public string DirectorDebugLine { get; }

        public string RollDebugLine { get; }

        public string RangedDrawDebugLine { get; }

        public string StatusText => RoomState == RoomObjectiveState.Cleared ? "Room Clear" : "In Combat";

        public string DefenseSummary
        {
            get
            {
                if (IsInParryWindow)
                {
                    return $"DEF {Defense} | Parry";
                }

                if (IsGuarding)
                {
                    return $"DEF {Defense} | Shield";
                }

                return LastGuardResult == ShieldGuardResult.PerfectParry
                    ? $"DEF {Defense} | Parried"
                    : $"DEF {Defense}";
            }
        }
    }
}
