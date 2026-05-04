# Hollow Designer Room Authoring - Szybki przewodnik PL

Dla Martina, Rafała i Pawła. To krótki workflow do edycji scen `Assets/_Hollow/Scenes/DesignerRooms` bez wchodzenia w Play Mode.

## 1. Otwórz i zadokuj narzędzie

1. Otwórz scenę z `Assets/_Hollow/Scenes/DesignerRooms`.
2. Wejdź w `Hollow > Designer Rooms > Room Authoring`.
3. Złap okno `Room Authoring` za pasek tytułu.
4. Upuść je tam, gdzie ma być zadokowane:
   - obok `Inspector`, jeśli ma być wąskim panelem,
   - obok `Scene`, jeśli ma być większym panelem edycji,
   - albo zostaw jako pływające okno podczas ustawiania markerów.
5. W razie potrzeby zapisz layout: `Window > Layouts > Save Layout`.

## 2. Przełącz język narzędzia

- W prawym górnym rogu okna `Room Authoring` jest przełącznik `EN / PL`.
- Kliknij `PL`, żeby zmienić główny interfejs narzędzia na polski.
- Przełącznik zapamiętuje wybór w Unity EditorPrefs.
- Identyfikatory runtime, ścieżki eksportu i nazwy techniczne pozostają bez zmian, żeby eksport był bezpieczny.

## 3. Nawigacja po pokoju

- Używaj standardowych kontrolek Unity Scene View.
- Prawy przycisk myszy + WASD pozwala latać kamerą.
- Środkowy przycisk myszy przesuwa widok.
- Scroll przybliża i oddala.
- Kliknij marker w Scene View albo Hierarchy, żeby go edytować.
- Użyj `Dopasuj widok z góry`, żeby szybko ustawić czysty widok top-down.

## 4. Dodawanie obiektów na siatkę

1. Otwórz zakładkę `Paleta`.
2. Wybierz `Typ znacznika`, np. `Spawn wroga`, `Przeszkoda`, `Zagrożenie`, `Drzwi` albo `Spawn przedmiotu`.
3. Wybierz `Typ runtime`, np. `Szczur`, `Szkielet z włócznią`, `Kolce` albo `Skrzynia`.
4. Kliknij `Uzbrój dodawanie`.
5. Kliknij miejsce w Scene View.
6. Marker trafi do właściwego folderu, np. `EnemySpawns`, `DoorPorts` albo `Obstacles`.
7. Jeśli trzeba, kliknij `Przyciągnij zaznaczone`.

Zwykłe markery przyciągają się do siatki 1m. Drzwi przyciągają się do poprawnej krawędzi pokoju.

## 5. Edycja markera

1. Zaznacz marker w Scene View albo Hierarchy.
2. Otwórz zakładkę `Zaznaczenie`.
3. Możesz edytować:
   - `Id znacznika`
   - `Typ znacznika`
   - `Typ runtime`
   - `Własna nazwa wyświetlana`
   - `Pokaż etykietę w scenie`
   - kierunek, stan i numer wejścia dla drzwi
4. Dla spawnów wrogów panel pokazuje HP, inteligencję, nastawienie, zmysły, dystans i skrót ataków.

Użyj `Zablokuj warstwę`, jeśli chcesz uniknąć przypadkowego przesunięcia markera.

## 6. Podgląd modeli i światła

1. Otwórz zakładkę `Podgląd`.
2. Zostaw `Oświetlenie podglądu` włączone, jeśli chcesz widzieć pokój w lepszym świetle.
3. Kliknij `Podgląd wizualny: WYŁ.`, żeby go włączyć.
4. Ustaw Scene View w tryb `Shaded`, `Lit` albo `Textured`.
5. Po przesunięciu markerów kliknij `Odśwież podgląd`.
6. Kliknij `Podgląd wizualny: WŁ.` albo `Wyczyść podgląd`, żeby usunąć preview.

Podgląd pojawia się jako `RuntimePreview_DO_NOT_EXPORT`. Jest tymczasowy, nie ma markerów authoringowych i nie eksportuje się do JSON.

## 7. Menu Designer Rooms

- `Room Authoring`: otwiera główne okno edycji.
- `Snap Selected`: przyciąga zaznaczone markery do siatki lub krawędzi drzwi.
- `Snap All In Active Scene`: przyciąga wszystkie edytowalne markery w otwartej scenie.
- `Build Visual Preview`: buduje tymczasowy podgląd prefabów i materiałów.
- `Clear Visual Preview`: usuwa tymczasowy podgląd.
- `Diff Active Scene Against Source`: pokazuje zmiany względem źródłowego approved JSON.
- `Refresh Active Scene From Source JSON`: odtwarza markery ze źródłowego szablonu. Używaj ostrożnie, bo usuwa obecne zmiany markerów.
- `Export Active DesignerRoom Scene`: waliduje i eksportuje otwartą scenę do nowego draftu JSON.
- `Export All DesignerRooms`: eksportuje wszystkie sceny z `Assets/_Hollow/Scenes/DesignerRooms`.

## 8. Walidacja i eksport

1. Otwórz zakładkę `Walidacja`.
2. Kliknij `Sprawdź aktywną scenę DesignerRoom`.
3. Napraw błędy, np. brak safe startu, brak spawnu wroga, duplikaty ID, niepoprawne drzwi albo markery poza siatką.
4. Otwórz zakładkę `Eksport`.
5. Kliknij `Eksportuj aktywną scenę DesignerRoom`.

Eksport trafia tutaj:

`Assets/_Hollow/Data/Rooms/DesignerDrafts/ManualSceneExports/`

Zatwierdzone szablony źródłowe nie są nadpisywane.

## 9. Praktyczne wskazówki

- Najpierw ustaw spawny wrogów, potem strojenie kamieni, hazardów i przedmiotów.
- Pokój powinien mieć minimum jeden safe start i jeden spawn wroga.
- Nie stawiaj safe startu ani spawnów wrogów na kamieniach, dziurach lub kolcach.
- Używaj overlayu `Przejścia`, żeby zobaczyć blokady, dziury i hazardy.
- Używaj overlayu `Zasięg wroga`, żeby sprawdzić wzrok, słuch i dystans wybranego wroga.
- Używaj `Podgląd wizualny` do oceny skali, światła, czytelności i fallback prefabów.
- Przed eksportem użyj `Porównaj ze źródłem`, jeśli chcesz szybko zobaczyć, co się zmieniło.

## 10. Zasada handoffu

Edycje scen są draftami authoringowymi. Po eksporcie sprawdź wygenerowany JSON przed awansem do zatwierdzonej puli Designer Rooms.
