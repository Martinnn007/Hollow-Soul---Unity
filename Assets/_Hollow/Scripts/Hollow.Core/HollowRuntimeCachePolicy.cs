namespace Hollow.Core
{
    public sealed class HollowRuntimeCachePolicy
    {
        public static readonly HollowRuntimeCachePolicy Default = new();

        public HollowRuntimeCachePolicy(
            int maxBranchGraphEntries = 8,
            int maxBranchPlanEntries = 24,
            int maxRoomAssetEntries = 64,
            int maxRoomDescriptorEntries = 96,
            int maxPredictivePreloadRooms = 6)
        {
            MaxBranchGraphEntries = ClampPositive(maxBranchGraphEntries);
            MaxBranchPlanEntries = ClampPositive(maxBranchPlanEntries);
            MaxRoomAssetEntries = ClampPositive(maxRoomAssetEntries);
            MaxRoomDescriptorEntries = ClampPositive(maxRoomDescriptorEntries);
            MaxPredictivePreloadRooms = ClampPositive(maxPredictivePreloadRooms);
        }

        public int MaxBranchGraphEntries { get; }

        public int MaxBranchPlanEntries { get; }

        public int MaxRoomAssetEntries { get; }

        public int MaxRoomDescriptorEntries { get; }

        public int MaxPredictivePreloadRooms { get; }

        private static int ClampPositive(int value)
        {
            return value < 1 ? 1 : value;
        }
    }
}
