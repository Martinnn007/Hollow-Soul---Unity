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
            LastDamageReduction = defenseController != null ? defenseController.LastDamageReduction : 0;
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

        public int LastDamageReduction { get; }

        public string StatusText => RoomState == RoomObjectiveState.Cleared ? "Room Clear" : "In Combat";

        public string DefenseSummary => IsGuarding ? $"DEF {Defense} | Shield Up" : $"DEF {Defense}";
    }
}
