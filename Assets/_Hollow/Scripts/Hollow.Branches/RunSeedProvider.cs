using System;

namespace Hollow.Branches
{
    public static class RunSeedProvider
    {
        private static Func<int> seedFactoryOverride;

        public static int CreateSeed()
        {
            if (seedFactoryOverride != null)
            {
                return Normalize(seedFactoryOverride());
            }

            return Normalize(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
        }

        public static IDisposable OverrideForTests(Func<int> seedFactory)
        {
            var previous = seedFactoryOverride;
            seedFactoryOverride = seedFactory;
            return new OverrideScope(() => seedFactoryOverride = previous);
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

        private sealed class OverrideScope : IDisposable
        {
            private readonly Action restore;
            private bool disposed;

            public OverrideScope(Action restore)
            {
                this.restore = restore;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                restore?.Invoke();
            }
        }
    }
}
