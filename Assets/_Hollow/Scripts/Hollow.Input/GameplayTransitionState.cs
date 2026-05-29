using System;

namespace Hollow.Input
{
    public static class GameplayTransitionState
    {
        private static int lockDepth;

        public static bool IsLocked => lockDepth > 0;

        public static int LockDepth => lockDepth;

        public static IDisposable AcquireLock()
        {
            lockDepth++;
            return new TransitionLockHandle();
        }

        public static void ReleaseLock()
        {
            if (lockDepth > 0)
            {
                lockDepth--;
            }
        }

        public static void ResetForTests()
        {
            lockDepth = 0;
        }

        private sealed class TransitionLockHandle : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                ReleaseLock();
            }
        }
    }
}
