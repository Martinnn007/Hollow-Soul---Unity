using Hollow.Core;

namespace Hollow.Persistence
{
    public static class TransientSessionGuard
    {
        public static bool CanPersist(RuntimeSessionMode sessionMode, bool hasProfile)
        {
            return sessionMode == RuntimeSessionMode.ProfileBacked && hasProfile;
        }
    }
}
