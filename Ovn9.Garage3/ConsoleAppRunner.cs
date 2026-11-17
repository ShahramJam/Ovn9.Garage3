using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ovn9.Garage3.Abstractions;
using Ovn9.Garage3.Common;
using Ovn9.Garage3.Models;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Ovn9.Garage3;

// Hanterar konsolens menyflöde och skickar användarens val vidare till garagehanteraren
public sealed class ConsoleAppRunner : IAppRunner
{
    private const string DefaultSavePath = "garage-data.json";
    private readonly IGarageHandler _handler;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsoleAppRunner> _logger;

    public ConsoleAppRunner(IGarageHandler handler, IConfiguration configuration, ILogger<ConsoleAppRunner> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync()
    {
        const int defaultCapacity = 10;
        var capacity = defaultCapacity;
        var configuredCapacity = _configuration["Garage:DefaultCapacity"];
        if (configuredCapacity is not null && (!int.TryParse(configuredCapacity, out capacity) || capacity <= 0))
        {
            Console.WriteLine("Standardkapaciteten måste vara ett positivt heltal. Värdet 10 används.");
            capacity = defaultCapacity;
        }

        if (!_handler.CreateGarage("Huvudgaraget", GarageKind.General, capacity, out var message))
        {
            _logger.LogError("Det gick inte att initiera standardgaraget: {Orsak}", message);
            Console.WriteLine(message);
            return;
        }

        Console.WriteLine($"Garaget 3.0 - {message}");
        while (true)
        {
            var active = _handler.GetActiveGarageInfo();
            Console.WriteLine($"\nAktivt garage: {active.Name} ({SwedishText.GetGarageKindName(active.Kind)}) - {active.UsedSpaces}/{active.Capacity} parkeringsplatser upptagna, {active.AvailableSpaces} lediga");
            Console.WriteLine("1. Visa fordon   2. Fordonsantal   3. Parkera fordon   4. Ta bort fordon\n5. Hitta fordon   6. Sök fordon      7. Skapa garage    8. Byt garage\n9. Visa garage    10. Spara garage    11. Läs in garage\n0. Avsluta");
            Console.Write("Välj: ");
            switch (Console.ReadLine())
            {
                case "1": ListVehicles(); break;
                case "2": ListCounts(); break;
                case "3": ParkVehicle(); break;
                case "4": RemoveVehicle(); break;
                case "5": FindVehicle(); break;
                case "6": SearchVehicles(); break;
                case "7": CreateGarage(); break;
                case "8": SwitchGarage(); break;
                case "9": ListGarages(); break;
                case "10": await SaveGaragesAsync().ConfigureAwait(false); break;
                case "11": await LoadGaragesAsync().ConfigureAwait(false); break;
                case "0":
                    return;
                case null:
                    return;
                default:
                    Console.WriteLine("Välj ett av alternativen i listan.");
                    break;
            }
        }
    }

    private void ListVehicles()
    {
        var vehicles = _handler.GetVehicles();
        if (vehicles.Count == 0)
        {
            Console.WriteLine("Garaget är tomt.");
            return;
        }
        foreach (var vehicle in vehicles)
            Console.WriteLine(vehicle);
    }

    private void ListCounts()
    {
        var counts = _handler.GetVehicleTypeCounts();
        if (counts.Count == 0)
        {
            Console.WriteLine("Garaget är tomt.");
            return;
        }
        foreach (var (type, count) in counts)
            Console.WriteLine($"{type}: {count}");
    }

    private void ParkVehicle()
    {
        var availableTypes = _handler.GetVehicleTypesThatFit();
        if (availableTypes.Count == 0)
        {
            Console.WriteLine("Ingen tillåten fordonstyp får plats i det här garaget.");
            return;
        }
        Console.WriteLine("Fordonstyper som får plats:");
        for (var index = 0; index < availableTypes.Count; index++)
            Console.WriteLine($"{index + 1}. {SwedishText.GetVehicleTypeName(availableTypes[index])}");
        var selection = ReadPositiveInt("Välj fordonstyp: ");
        if (selection is null)
            return;
        if (selection > availableTypes.Count)
        {
            Console.WriteLine("Den fordonstypen är inte tillgänglig.");
            return;
        }

        var registration = ReadRequired("Registreringsnummer: ");
        var color = ReadRequired("Färg: ");
        var wheels = ReadPositiveInt("Antal hjul (0 om inga): ", allowZero: true);
        if (registration is null || color is null || wheels is null)
            return;

        try
        {
            var selectedType = availableTypes[selection.Value - 1];
            // Fordonstyperna kräver olika tilläggsuppgifter, därför skapas den valda konkreta typen här.
            Vehicle vehicle = selectedType == typeof(Airplane)
                ? new Airplane(registration, color, wheels.Value, ReadPositiveInt("Antal motorer: ") ?? 0)
                : selectedType == typeof(Motorcycle)
                    ? new Motorcycle(registration, color, wheels.Value, ReadPositiveDouble("Cylindervolym (kubikcentimeter): ") ?? 0)
                    : selectedType == typeof(Car)
                        ? new Car(registration, color, wheels.Value, ReadFuelType())
                        : selectedType == typeof(Bus)
                            ? new Bus(registration, color, wheels.Value, ReadPositiveInt("Antal sittplatser: ") ?? 0, ReadFuelType())
                            : new Boat(registration, color, wheels.Value, ReadPositiveDouble("Längd (m): ") ?? 0);

            if (_handler.TryParkVehicle(vehicle, out var message))
                Console.WriteLine($"{vehicle.RegistrationNumber} parkerades.");
            else
                Console.WriteLine(message);
        }
        catch (ArgumentException exception)
        {

            Console.WriteLine($"Ogiltiga fordonsuppgifter: {exception.Message}");
        }
    }

    private void RemoveVehicle()
    {
        var registration = ReadRequired("Registreringsnummer: ");
        if (registration is not null)
        {
            _handler.TryRemoveVehicle(registration, out _, out var message);
            Console.WriteLine(message);
        }
    }

    private void FindVehicle()
    {
        var registration = ReadRequired("Registreringsnummer: ");
        if (registration is null)
            return;
        var vehicle = _handler.FindVehicle(registration);
        Console.WriteLine(vehicle is null ? "Inget fordon med det registreringsnumret hittades." : vehicle.ToString());
    }

    private void SearchVehicles()
    {
        Console.Write("Färg (lämna tomt för alla): ");
        var color = Console.ReadLine();
        var wheels = ReadOptionalInt("Antal hjul (lämna tomt för alla): ");
        if (wheels == int.MinValue)
            return;

        Console.WriteLine("Fordonstyp: lämna tomt för alla, 1. Flygplan, 2. Motorcykel, 3. Bil, 4. Buss, 5. Båt");
        Console.Write("Typ: ");
        var type = Console.ReadLine();
        // null betyder alla typer; typeof(void) markerar ett menyval som inte kunde tolkas.
        var vehicleType = type switch
        {
            "1" => typeof(Airplane),
            "2" => typeof(Motorcycle),
            "3" => typeof(Car),
            "4" => typeof(Bus),
            "5" => typeof(Boat),
            "" or null => null,
            _ => typeof(void)
        };
        if (vehicleType == typeof(void))
        {
            Console.WriteLine("Okänd fordonstyp.");
            return;
        }

        Console.WriteLine("Valfri fordonsegenskap: lämna tomt för att inte filtrera, 1. Antal motorer, 2. Cylindervolym, 3. Bränsle, 4. Antal sittplatser, 5. Längd");
        Console.Write("Egenskap: ");
        var propertyName = Console.ReadLine() switch
        {
            "1" => "NumberOfEngines",
            "2" => "CylinderVolume",
            "3" => "FuelType",
            "4" => "NumberOfSeats",
            "5" => "Length",
            "" or null => null,
            _ => string.Empty
        };
        if (propertyName == string.Empty)
        {
            Console.WriteLine("Okänd fordonsegenskap.");
            return;
        }
        var propertyValue = propertyName is null ? null : ReadRequired("Egenskapsvärde (använd punkt som decimaltecken): ");
        if (propertyName is not null && propertyValue is null)
            return;

        var results = _handler.SearchVehicles(color, wheels, vehicleType, propertyName, propertyValue);
        if (results.Count == 0)
        {
            Console.WriteLine("Inga fordon matchade sökningen.");
            return;
        }
        foreach (var vehicle in results)
            Console.WriteLine(vehicle);
    }

    private void CreateGarage()
    {
        var name = ReadRequired("Garagets namn: ");
        if (name is null)
            return;
        Console.WriteLine("Profil: 1. Allmänt  2. Hangar (flygplan)  3. Motorcykelgarage  4. Båtbrygga");
        var kind = Console.ReadLine() switch
        {
            "1" => GarageKind.General,
            "2" => GarageKind.Hangar,
            "3" => GarageKind.Motorcycle,
            "4" => GarageKind.BoatDock,
            _ => (GarageKind?)null
        };
        if (kind is null)
        {
            Console.WriteLine("Okänd garageprofil.");
            return;
        }
        var capacity = ReadPositiveInt("Kapacitet i parkeringsplatser: ");
        if (capacity is null)
            return;
        if (_handler.CreateGarage(name, kind.Value, capacity.Value, out var message))
            _handler.SelectGarage(name, out _);
        Console.WriteLine(message);
    }

    private void SwitchGarage()
    {
        var name = ReadRequired("Garagets namn: ");
        if (name is not null)
        {
            _handler.SelectGarage(name, out var message);
            Console.WriteLine(message);
        }
    }

    private void ListGarages()
    {
        foreach (var garage in _handler.GetGarages())
            Console.WriteLine($"{garage.Name} ({SwedishText.GetGarageKindName(garage.Kind)}): {garage.VehicleCount} fordon, {garage.UsedSpaces}/{garage.Capacity} parkeringsplatser upptagna");
    }

    private async Task SaveGaragesAsync()
    {
        var path = ReadOptionalPath("Sökväg till fil att spara", GetConfiguredSavePath());
        if (path is not null)
        {
            var result = await _handler.SaveAsync(path).ConfigureAwait(false);
            Console.WriteLine(result.Message);
        }
    }

    private async Task LoadGaragesAsync()
    {
        var path = ReadOptionalPath("Sökväg till fil att läsa in", GetConfiguredSavePath());
        if (path is not null)
        {
            var result = await _handler.LoadAsync(path).ConfigureAwait(false);
            Console.WriteLine(result.Message);
        }
    }

    private string GetConfiguredSavePath() => _configuration["Garage:SaveFilePath"] ?? DefaultSavePath;

    private static string? ReadOptionalPath(string prompt, string defaultPath)
    {
        Console.Write($"{prompt} (tryck på Enter för {defaultPath}): ");
        var input = Console.ReadLine();
        return input is null ? null : string.IsNullOrWhiteSpace(input) ? defaultPath : input.Trim();
    }

    private static string? ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var value = Console.ReadLine();
            if (value is null)
                return null;
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
            Console.WriteLine("Ett värde måste anges.");
        }
    }

    private static int? ReadPositiveInt(string prompt, bool allowZero = false)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (input is null)
                return null;
            if (int.TryParse(input, out var value) && (allowZero ? value >= 0 : value > 0))
                return value;
            Console.WriteLine(allowZero ? "Ange ett heltal som är noll eller större." : "Ange ett positivt heltal.");
        }
    }

    private static double? ReadPositiveDouble(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (input is null)
                return null;
            // InvariantCulture kräver punkt som decimaltecken oavsett datorns inställningar; IsFinite avvisar NaN och oändlighet.
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && value > 0 && double.IsFinite(value))
                return value;
            Console.WriteLine("Ange ett positivt tal med punkt som decimaltecken.");
        }
    }

    private static int? ReadOptionalInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            // int.MinValue skiljer avslutad inmatning från ett tomt filter; negativa hjultal godkänns ändå inte.
            if (input is null)
                return int.MinValue;
            if (string.IsNullOrWhiteSpace(input))
                return null;
            if (int.TryParse(input, out var value) && value >= 0)
                return value;
            Console.WriteLine("Ange ett heltal som är noll eller större, eller lämna tomt.");
        }
    }

    private static FuelType ReadFuelType()
    {
        while (true)
        {
            Console.Write("Bränsle (1. Bensin, 2. Diesel, 3. El): ");
            switch (Console.ReadLine())
            {
                case "1": return FuelType.Gasoline;
                case "2": return FuelType.Diesel;
                case "3": return FuelType.Electric;
                case null: throw new ArgumentException("Inmatningen avslutades innan en bränsletyp valdes.");
                default: Console.WriteLine("Välj 1, 2 eller 3."); break;
            }
        }
    }
}
