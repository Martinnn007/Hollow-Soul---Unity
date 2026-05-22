using UnityEngine;

namespace Hollow.Persistence
{
    [DefaultExecutionOrder(-900)]
    public sealed class ProfileSessionHost : MonoBehaviour
    {
        public static ProfileSessionHost Instance { get; private set; }

        public IProfileStore ProfileStore { get; private set; }

        public IRunSaveStore RunSaveStore { get; private set; }

        public IChallengeResultStore ChallengeResultStore { get; private set; }

        public SelectedProfileContext SelectedProfileContext { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            var jsonStore = new JsonProfileStore();
            ProfileStore = jsonStore;
            RunSaveStore = jsonStore;
            ChallengeResultStore = jsonStore;
            SelectedProfileContext = new SelectedProfileContext();
        }

        public void ConfigureForTests(IProfileStore profileStore, SelectedProfileContext selectedProfileContext)
        {
            Instance = this;
            ProfileStore = profileStore;
            RunSaveStore = profileStore as IRunSaveStore;
            ChallengeResultStore = profileStore as IChallengeResultStore;
            SelectedProfileContext = selectedProfileContext ?? new SelectedProfileContext();
        }
    }
}
