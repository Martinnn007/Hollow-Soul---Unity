namespace Hollow.Persistence
{
    public sealed class SelectedProfileContext
    {
        public ProfileSlotSummary SelectedProfile { get; private set; }

        public bool HasSelection => SelectedProfile != null && !SelectedProfile.IsEmpty;

        public void Select(ProfileSlotSummary profile)
        {
            SelectedProfile = profile;
        }

        public void Clear()
        {
            SelectedProfile = null;
        }
    }
}
