using UnityEngine;

namespace Hollow.Core.App
{
    [DefaultExecutionOrder(-1000)]
    public sealed class HollowBootstrap : MonoBehaviour
    {
        public static HollowBootstrap Instance { get; private set; }

        public AppStateMachine AppStateMachine { get; private set; }

        public GameClock GameClock { get; private set; }

        public GameEventBus EventBus { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            AppStateMachine = new AppStateMachine();
            GameClock = new GameClock();
            EventBus = new GameEventBus();
        }

        private void Update()
        {
            GameClock?.Tick(Time.unscaledDeltaTime);
        }
    }
}
