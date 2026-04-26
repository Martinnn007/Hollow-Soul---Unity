namespace Hollow.Persistence
{
    public interface IRunSaveStore
    {
        bool TryLoadActiveRun(ProfileSlotId slotId, out RunSaveSnapshot snapshot);

        void SaveActiveRun(ProfileSlotId slotId, RunSaveSnapshot snapshot);

        void ClearActiveRun(ProfileSlotId slotId);

        void CompleteActiveRun(ProfileSlotId slotId, RunCompletionSummary summary);
    }
}
