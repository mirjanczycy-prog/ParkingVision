namespace ParkingVision.Core.Localization;

/// <summary>
/// UI strings, one table for all languages (pl, en). Key -> (Pl, En). Plural keys use the suffixes |one |few |many
/// (a missing |few falls back to |many). To add a language: add a field to the tuple (or split into per-language tables),
/// extend <c>Loc.Normalize</c> and the language picker; tests in LocalizationTests keep the table consistent.
/// </summary>
public static class Strings
{
    public static readonly string[] Languages = { "pl", "en" };

    public static readonly IReadOnlyDictionary<string, (string Pl, string En)> Table = new Dictionary<string, (string Pl, string En)>
    {
        // tabs
        ["tab_map"] = ("Mapa", "Map"),
        ["tab_list"] = ("Lista", "List"),
        ["tab_settings"] = ("Ustawienia", "Settings"),

        // map page
        ["search_placeholder"] = ("Gdzie chcesz zaparkować?", "Where do you want to park?"),
        ["my_location"] = ("Moja lokalizacja", "My location"),
        ["map_details"] = ("Szczegóły", "Details"),
        ["map_offline"] = ("Mapa wymaga połączenia z internetem. Lista działa bez mapy.", "The map needs an internet connection. The list works without it."),

        // filters
        ["filter_all"] = ("Wszystkie", "All"),
        ["filter_car"] = ("Auto", "Car"),
        ["filter_moto"] = ("Motocykl", "Motorcycle"),
        ["filter_disabled"] = ("Dla niepełnosprawnych", "Accessible"),
        ["filter_ev"] = ("Elektryczne", "Electric"),

        // list page
        ["list_title"] = ("Wolne miejsca", "Free spaces"),
        ["list_empty"] = ("Brak stref w tym promieniu. Zmień lokalizację albo zwiększ promień w ustawieniach.", "No zones within this radius. Change the location or increase the radius in settings."),

        // status line
        ["status_searching"] = ("Szukam wolnych miejsc…", "Looking for free spaces…"),
        ["status_no_location"] = ("Brak dostępu do lokalizacji. Wpisz adres albo włącz lokalizację demo w ustawieniach.", "Location access is unavailable. Enter an address or turn on the demo location in settings."),
        ["status_none_in_radius"] = ("Brak stref w promieniu {0} m. Zwiększ promień w ustawieniach.", "No zones within {0} m. Increase the radius in settings."),
        ["status_zones_found|one"] = ("{0} strefa w pobliżu, aktualizacja {1}", "{0} zone nearby, updated {1}"),
        ["status_zones_found|few"] = ("{0} strefy w pobliżu, aktualizacja {1}", "{0} zones nearby, updated {1}"),
        ["status_zones_found|many"] = ("{0} stref w pobliżu, aktualizacja {1}", "{0} zones nearby, updated {1}"),
        ["status_server_error"] = ("Nie można połączyć się z serwerem ({0}). Sprawdź adres w ustawieniach.", "Cannot reach the server ({0}). Check the address in settings."),
        ["status_address_not_found"] = ("Nie znaleziono takiego adresu. Spróbuj podać ulicę i miasto.", "Address not found. Try a street and city."),
        ["status_search_unavailable"] = ("Wyszukiwanie adresu jest niedostępne na tym urządzeniu.", "Address search is not available on this device."),

        // availability confidence badges
        ["badge_live"] = ("Na żywo", "Live"),
        ["badge_mixed"] = ("Częściowo na żywo", "Partly live"),
        ["badge_estimated"] = ("Szacunek", "Estimate"),
        ["badge_nodata"] = ("Brak danych", "No data"),

        // zone card
        ["total_of|one"] = ("z {0} miejsca", "of {0} spot"),
        ["total_of|many"] = ("z {0} miejsc", "of {0} spots"),

        // relative time
        ["ago_none"] = ("brak aktualizacji", "no update"),
        ["ago_now"] = ("przed chwilą", "just now"),
        ["ago_sec"] = ("{0} s temu", "{0} s ago"),
        ["ago_min"] = ("{0} min temu", "{0} min ago"),
        ["ago_hour"] = ("ponad godzinę temu", "over an hour ago"),

        // where the number comes from (A-008)
        ["explain_live"] = ("Wszystkie miejsca w tej strefie widzą kamery.", "Cameras cover every space in this zone."),
        ["explain_mixed"] = ("Kamery widzą {0} z {1} miejsc. Dla pozostałych {2} szacujemy zajętość z biletów z parkomatów.", "Cameras cover {0} of {1} spaces. For the remaining {2} we estimate occupancy from parking meter tickets."),
        ["explain_estimated"] = ("Brak obrazu z kamer. Liczba wolnych miejsc to szacunek z biletów z parkomatów.", "No camera view. The number of free spaces is an estimate from parking meter tickets."),
        ["explain_nodata"] = ("Dla tej strefy nie mamy teraz żadnych danych.", "We have no data for this zone right now."),

        // zone detail
        ["detail_free_of|one"] = ("wolnych z {0} miejsca", "free of {0} spot"),
        ["detail_free_of|many"] = ("wolnych z {0} miejsc", "free of {0} spots"),
        ["detail_updated"] = ("Aktualizacja: {0}", "Updated: {0}"),
        ["detail_spaces"] = ("Miejsca", "Spaces"),
        ["legend_free"] = ("✓ wolne", "✓ free"),
        ["legend_taken"] = ("✕ zajęte", "✕ taken"),
        ["legend_unknown"] = ("? brak danych", "? no data"),
        ["detail_meters"] = ("Parkomaty w strefie", "Parking meters in this zone"),
        ["parkomat_info|one"] = ("{0}, obsługuje {1} miejsce", "{0}, serves {1} space"),
        ["parkomat_info|few"] = ("{0}, obsługuje {1} miejsca", "{0}, serves {1} spaces"),
        ["parkomat_info|many"] = ("{0}, obsługuje {1} miejsc", "{0}, serves {1} spaces"),
        ["nav_button"] = ("Nawiguj do strefy", "Navigate to zone"),
        ["pay_soon"] = ("Opłać postój (wkrótce)", "Pay for parking (coming soon)"),
        ["detail_not_found"] = ("Nie znaleziono strefy.", "Zone not found."),
        ["detail_error"] = ("Nie można pobrać danych. Sprawdź połączenie z serwerem.", "Cannot load data. Check the server connection."),

        // settings
        ["set_server"] = ("Adres serwera", "Server address"),
        ["set_server_hint"] = ("Symulator iOS: http://localhost:5080. Emulator Androida: http://10.0.2.2:5080. Telefon: adres IP komputera w sieci, np. http://192.168.0.12:5080.", "iOS simulator: http://localhost:5080. Android emulator: http://10.0.2.2:5080. Phone: your computer's IP address on the network, e.g. http://192.168.0.12:5080."),
        ["set_test"] = ("Sprawdź połączenie", "Test connection"),
        ["set_radius"] = ("Promień wyszukiwania: {0} m", "Search radius: {0} m"),
        ["set_refresh"] = ("Odświeżanie co {0} s", "Refresh every {0} s"),
        ["set_demo"] = ("Użyj lokalizacji demo", "Use demo location"),
        ["set_demo_hint"] = ("Dane demonstracyjne dotyczą fikcyjnych stref w centrum Krakowa, więc prawdziwa lokalizacja zwykle ich nie obejmie.", "Demo data covers fictional zones in central Kraków, so your real location usually won't include them."),
        ["set_language"] = ("Język", "Language"),
        ["set_save"] = ("Zapisz", "Save"),
        ["set_saved"] = ("Zapisano.", "Saved."),
        ["set_checking"] = ("Sprawdzam…", "Checking…"),
        ["set_connected"] = ("Połączono z serwerem.", "Connected to the server."),
        ["set_not_connected"] = ("Brak połączenia. Sprawdź adres i czy API działa.", "No connection. Check the address and that the API is running."),
    };
}
