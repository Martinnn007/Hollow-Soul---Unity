using Hollow.Editor.EnemyAuthoring;

namespace Hollow.Editor.EnemyAiBrainStudio
{
    public static class EnemyAiBrainStudioLocalization
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
            "Overview",
            "Individual",
            "Templates",
            "Score Lab",
            "Threat & LOD",
            "Live Trace",
            "Validation"
        };

        public static readonly string[] TabsPolish =
        {
            "Przeglad",
            "Indywidualne",
            "Szablony",
            "Score Lab",
            "Threat i LOD",
            "Live trace",
            "Walidacja"
        };

        public static string[] Tabs => CurrentLanguage == EnemyAuthoringLanguage.Polish
            ? TabsPolish
            : TabsEnglish;
    }
}
