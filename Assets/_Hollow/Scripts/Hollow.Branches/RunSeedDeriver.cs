using System;

namespace Hollow.Branches
{
    public static class RunSeedDeriver
    {
        public static int PrologueBranchSeed(int runSeed, int worldIndex)
        {
            return StablePositiveHash($"{Normalize(runSeed)}|world:{Math.Max(1, worldIndex)}|prologue");
        }

        public static int BranchSlotSeed(int runSeed, int worldIndex, int slotIndex)
        {
            return StablePositiveHash($"{Normalize(runSeed)}|world:{Math.Max(1, worldIndex)}|branch:{Math.Max(0, slotIndex)}");
        }

        public static int ShopSeed(int runSeed, int worldIndex, int refreshIndex)
        {
            return StablePositiveHash($"{Normalize(runSeed)}|world:{Math.Max(1, worldIndex)}|shop:{Math.Max(0, refreshIndex)}");
        }

        private static int Normalize(int seed)
        {
            if (seed == int.MinValue)
            {
                return int.MaxValue;
            }

            var positive = Math.Abs(seed);
            return positive == 0 ? 1 : positive;
        }

        private static int StablePositiveHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                var positive = (int)(hash & 0x7fffffff);
                return positive == 0 ? 1 : positive;
            }
        }
    }
}
