namespace Hollow.Editor.EnemyAuthoring
{
    public enum EnemyAuthoringLanguage
    {
        English = 0,
        Polish = 1
    }

    public static class EnemyAuthoringLocalization
    {
        public static EnemyAuthoringLanguage CurrentLanguage { get; set; }

        public static readonly string[] PanelLabelsEnglish =
        {
            "Roster",
            "Stats & Senses",
            "Attacks",
            "Actions",
            "Spacing",
            "Behavior Tree",
            "Visuals",
            "Live Tuning",
            "Validation & Apply"
        };

        public static readonly string[] PanelLabelsPolish =
        {
            "Lista",
            "Staty i zmysly",
            "Ataki",
            "Akcje",
            "Dystans",
            "Drzewo AI",
            "Wizualia",
            "Live tuning",
            "Walidacja i zapis"
        };

        public static string[] PanelLabels => CurrentLanguage == EnemyAuthoringLanguage.Polish
            ? PanelLabelsPolish
            : PanelLabelsEnglish;

        public static string T(string english, string polish)
        {
            return CurrentLanguage == EnemyAuthoringLanguage.Polish ? polish : english;
        }
    }
}
