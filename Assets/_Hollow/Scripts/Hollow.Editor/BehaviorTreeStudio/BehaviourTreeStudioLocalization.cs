using Hollow.Editor.EnemyAuthoring;

namespace Hollow.Editor.BehaviorTreeStudio
{
    public static class BehaviourTreeStudioLocalization
    {
        public static EnemyAuthoringLanguage CurrentLanguage
        {
            get => EnemyAuthoringLocalization.CurrentLanguage;
            set => EnemyAuthoringLocalization.CurrentLanguage = value;
        }

        public static string T(string english, string polish)
        {
            return EnemyAuthoringLocalization.T(english, polish);
        }

        public static readonly string[] TabsEnglish =
        {
            "Graph",
            "Templates",
            "Validation",
            "Sandbox",
            "Live Trace",
            "Diff"
        };

        public static readonly string[] TabsPolish =
        {
            "Graf",
            "Szablony",
            "Walidacja",
            "Sandbox",
            "Live trace",
            "Diff"
        };

        public static string[] Tabs => CurrentLanguage == EnemyAuthoringLanguage.Polish
            ? TabsPolish
            : TabsEnglish;
    }
}
