using UnityEngine;

namespace Hollow.Core.App
{
    public sealed class GameClock
    {
        public float DeltaTime { get; private set; }

        public float TimeScale { get; private set; } = 1f;

        public bool IsPaused { get; private set; }

        public void Tick(float unscaledDeltaTime)
        {
            DeltaTime = IsPaused ? 0f : Mathf.Max(0f, unscaledDeltaTime) * TimeScale;
        }

        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
        }

        public void SetTimeScale(float timeScale)
        {
            TimeScale = Mathf.Max(0f, timeScale);
        }
    }
}
