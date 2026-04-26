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
        {
            PlayerHealth = playerHealth;
            PlayerMaxHealth = playerMaxHealth;
            EnemiesRemaining = enemiesRemaining;
            RoomState = roomState;
            DifficultyName = difficultyName;
            ArchetypeSummary = archetypeSummary;
            ProjectileSummary = projectileSummary;
        }

        public int PlayerHealth { get; }

        public int PlayerMaxHealth { get; }

        public int EnemiesRemaining { get; }

        public RoomObjectiveState RoomState { get; }

        public string DifficultyName { get; }

        public string ArchetypeSummary { get; }

        public string ProjectileSummary { get; }

        public string StatusText => RoomState == RoomObjectiveState.Cleared ? "Room Clear" : "In Combat";
    }
}
