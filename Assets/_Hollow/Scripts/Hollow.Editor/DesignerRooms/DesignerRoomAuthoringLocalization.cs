using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEditor;

namespace Hollow.Editor.DesignerRooms
{
    public enum DesignerRoomAuthoringLanguage
    {
        English = 0,
        Polish = 1
    }

    public static class DesignerRoomAuthoringLocalization
    {
        private const string LanguagePreferenceKey = "Hollow.DesignerRooms.RoomAuthoring.Language";

        private static readonly string[] EnglishPanels = { "Palette", "Selection", "Validation", "Export", "Preview" };
        private static readonly string[] PolishPanels = { "Paleta", "Zaznaczenie", "Walidacja", "Eksport", "Podgląd" };

        public static DesignerRoomAuthoringLanguage CurrentLanguage
        {
            get => (DesignerRoomAuthoringLanguage)EditorPrefs.GetInt(LanguagePreferenceKey, (int)DesignerRoomAuthoringLanguage.English);
            set => EditorPrefs.SetInt(LanguagePreferenceKey, (int)value);
        }

        public static bool IsPolish => CurrentLanguage == DesignerRoomAuthoringLanguage.Polish;

        public static string[] PanelLabels => IsPolish ? PolishPanels : EnglishPanels;

        public static string T(string english, string polish)
        {
            return IsPolish ? polish : english;
        }

        public static string MarkerKindLabel(DesignerRoomSceneMarkerKind markerKind)
        {
            if (!IsPolish)
            {
                return ObjectNames.NicifyVariableName(markerKind.ToString());
            }

            return markerKind switch
            {
                DesignerRoomSceneMarkerKind.RoomRoot => "Korzeń pokoju",
                DesignerRoomSceneMarkerKind.Folder => "Folder",
                DesignerRoomSceneMarkerKind.FloorRegion => "Obszar podłogi",
                DesignerRoomSceneMarkerKind.DoorPort => "Drzwi",
                DesignerRoomSceneMarkerKind.SafeStart => "Bezpieczny start",
                DesignerRoomSceneMarkerKind.EnemySpawn => "Spawn wroga",
                DesignerRoomSceneMarkerKind.ItemSpawn => "Spawn przedmiotu",
                DesignerRoomSceneMarkerKind.Obstacle => "Przeszkoda",
                DesignerRoomSceneMarkerKind.Hazard => "Zagrożenie",
                DesignerRoomSceneMarkerKind.InteractiveObject => "Obiekt interaktywny",
                DesignerRoomSceneMarkerKind.HoleTile => "Dziura",
                _ => markerKind.ToString()
            };
        }

        public static string DisplayNameForRuntimeKind(string runtimeKind)
        {
            if (!IsPolish)
            {
                return DesignerRoomSceneAuthoringUtility.DisplayNameForRuntimeKind(runtimeKind);
            }

            return runtimeKind switch
            {
                RoomDesignerMarkerKinds.Enemy => "Dowolny wróg",
                RoomDesignerMarkerKinds.EnemyNormal => "Zwykły ścigacz",
                RoomDesignerMarkerKinds.EnemyFlying => "Latający ścigacz",
                RoomDesignerMarkerKinds.EnemyFast => "Szybki ścigacz",
                RoomDesignerMarkerKinds.EnemyHeavy => "Ciężki ścigacz",
                RoomDesignerMarkerKinds.EnemyCharger => "Popielny szarżownik",
                RoomDesignerMarkerKinds.EnemyTurret => "Kościana wieżyczka",
                RoomDesignerMarkerKinds.EnemySplitter => "Husk Splitter",
                RoomDesignerMarkerKinds.EnemySpittingPod => "Plujący strąk",
                RoomDesignerMarkerKinds.EnemyRat => "Szczur",
                RoomDesignerMarkerKinds.EnemySpider => "Pająk",
                RoomDesignerMarkerKinds.EnemyHollowBird => "Hollow Bird",
                RoomDesignerMarkerKinds.EnemyHollowBeast => "Hollow Beast",
                RoomDesignerMarkerKinds.EnemySkeletonSword => "Szkielet z mieczem",
                RoomDesignerMarkerKinds.EnemySkeletonSpear => "Szkielet z włócznią",
                RoomDesignerMarkerKinds.EnemyKnight => "Rycerz",
                RoomDesignerMarkerKinds.EnemyGiant => "Gigant",
                RoomDesignerMarkerKinds.EnemyHollowArcher => "Hollow Archer",
                RoomDesignerMarkerKinds.EnemyPowderGunner => "Strzelec prochowy",
                RoomDesignerMarkerKinds.EnemyKnifeThrower => "Miotacz noży",
                RoomDesignerMarkerKinds.EnemyRepeaterTurret => "Wieżyczka szybkostrzelna",
                RoomDesignerMarkerKinds.EnemyClockworkSentry => "Mechaniczny strażnik",
                RoomDesignerMarkerKinds.EnemyHollowAcolyte => "Hollow Acolyte",
                RoomDesignerMarkerKinds.EnemyWraith => "Widmo",
                RoomDesignerMarkerKinds.EnemySoulEater => "Pożeracz dusz",
                RoomDesignerMarkerKinds.EnemyCurseBinder => "Zaklinacz klątw",
                RoomDesignerMarkerKinds.EnemyGraveLantern => "Grobowa latarnia",
                RoomDesignerMarkerKinds.SafeStart => "Bezpieczny start",
                RoomDesignerMarkerKinds.RoomReward => "Nagroda pokoju",
                RoomDesignerMarkerKinds.ChestSpawn => "Skrzynia",
                RoomDesignerMarkerKinds.StandardBarrel => "Beczka",
                RoomDesignerMarkerKinds.ExplosiveBarrel => "Wybuchowa beczka",
                RoomDesignerCellKinds.Ground => "Podłoga",
                RoomDesignerCellKinds.Hole => "Dziura",
                RoomDesignerCellKinds.Rock => "Kamień",
                RoomDesignerCellKinds.Spike => "Kolce",
                RoomDesignerDoorKinds.Door => "Drzwi",
                RoomDesignerDoorKinds.Secret => "Tajne drzwi",
                RoomDesignerDoorKinds.Available => "Dostępny port drzwi",
                RoomDesignerDoorKinds.Inactive => "Nieaktywne drzwi",
                _ => string.IsNullOrWhiteSpace(runtimeKind) ? "Brak" : runtimeKind
            };
        }

        public static string OptionLabel(string option)
        {
            if (!IsPolish)
            {
                return option;
            }

            return option switch
            {
                "north" => "północ",
                "south" => "południe",
                "east" => "wschód",
                "west" => "zachód",
                _ => DisplayNameForRuntimeKind(option)
            };
        }

        public static string MarkerLabel(DesignerRoomSceneMarker marker)
        {
            if (marker == null || !IsPolish)
            {
                return marker != null ? marker.Label : string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(marker.DisplayName))
            {
                return marker.DisplayName;
            }

            return marker.MarkerKind switch
            {
                DesignerRoomSceneMarkerKind.RoomRoot => string.IsNullOrWhiteSpace(marker.SourceRoomId) ? "Korzeń pokoju" : marker.SourceRoomId,
                DesignerRoomSceneMarkerKind.FloorRegion => "Obszar podłogi",
                DesignerRoomSceneMarkerKind.DoorPort => $"Drzwi {OptionLabel(marker.DoorDirection)}_{marker.DoorLaneIndex}",
                DesignerRoomSceneMarkerKind.SafeStart => "Bezpieczny start",
                DesignerRoomSceneMarkerKind.EnemySpawn => DisplayNameForRuntimeKind(marker.RuntimeKind),
                DesignerRoomSceneMarkerKind.ItemSpawn => DisplayNameForRuntimeKind(marker.RuntimeKind),
                DesignerRoomSceneMarkerKind.Obstacle => "Kamień",
                DesignerRoomSceneMarkerKind.Hazard => "Kolce",
                DesignerRoomSceneMarkerKind.InteractiveObject => DisplayNameForRuntimeKind(marker.RuntimeKind),
                DesignerRoomSceneMarkerKind.HoleTile => "Dziura",
                _ => marker.Label
            };
        }
    }
}
