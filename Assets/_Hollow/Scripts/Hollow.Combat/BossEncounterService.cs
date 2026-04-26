namespace Hollow.Combat
{
    public sealed class BossEncounterService
    {
        public BossEncounterState State { get; private set; } = BossEncounterState.None;

        public void PreparePlaceholderEncounter()
        {
            State = BossEncounterState.Prepared;
        }

        public void StartPlaceholderEncounter()
        {
            State = BossEncounterState.Active;
        }

        public void MarkCleared()
        {
            State = BossEncounterState.Cleared;
        }
    }
}
