using Microsoft.Extensions.Configuration;
using Ovn9.Garage3.Models;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Ovn9.Garage3;

public sealed class ConsoleUI : IUI
{
    private const string DefaultSavePath = "garage-data.json";
    private readonly IGarageHandler _handler;
    private readonly IConfiguration _configuration;

    public ConsoleUI(IGarageHandler handler, IConfiguration configuration)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task RunAsync()
    {
        var capacity = 10;
        var configuredCapacity = _configuration["Garage:DefaultCapacity"];
        if (configuredCapacity is not null && (!int.TryParse(configuredCapacity, out capacity) || capacity <= 0))
        {
            Console.WriteLine("Garage:DefaultCapacity must be a positive whole number. Using default capacity 10.");
            capacity = 10;
        }

        if (!_handler.CreateGarage("Main Garage", GarageKind.General, capacity, out var message))
        {
            Console.WriteLine(message);
            return;
        }

        Console.WriteLine($"Garage 3.0 - {message}");
        while (true)
        {
            var active = _handler.GetActiveGarageInfo();
            Console.WriteLine($"\nActive garage: {active.Name} ({active.Kind}) - {active.UsedSpaces}/{active.Capacity} spaces used, {active.AvailableSpaces} available");
            Console.WriteLine("1. List vehicles   2. Vehicle counts   3. Park vehicle   4. Remove vehicle\n5. Find vehicle    6. Search vehicles   7. Create garage   8. Switch garage\n9. List garages    10. Save garages     11. Load garages\n0. Exit");
            Console.Write("Choose: ");
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
                case "0": return;
                case null: return;
                default: Console.WriteLine("Please choose a listed option."); break;
            }
        }
    }

    private void ListVehicles()
    {
        var vehicles = _handler.GetVehicles();
        if (vehicles.Count == 0)
        {
            Console.WriteLine("The garage is empty.");
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
            Console.WriteLine("The garage is empty.");
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
            Console.WriteLine("No vehicle type is currently allowed and fits in this garage.");
            return;
        }
        Console.WriteLine("Vehicle types that fit:");
        for (var index = 0; index < availableTypes.Count; index++)
            Console.WriteLine($"{index + 1}. {availableTypes[index].Name}");
        var selection = ReadPositiveInt("Choose vehicle type: ");
        if (selection is null)
            return;
        if (selection > availableTypes.Count)
        {
            Console.WriteLine("That vehicle type is not available.");
            return;
        }

        var registration = ReadRequired("Registration number: ");
        var color = ReadRequired("Color: ");
        var wheels = ReadPositiveInt("Number of wheels (0 if none): ", allowZero: true);
        if (registration is null || color is null || wheels is null)
            return;

        try
        {
            var selectedType = availableTypes[selection.Value - 1];
            IVehicle vehicle = selectedType == typeof(Airplane)
                ? new Airplane(registration, color, wheels.Value, ReadPositiveInt("Number of engines: ") ?? 0)
                : selectedType == typeof(Motorcycle)
                    ? new Motorcycle(registration, color, wheels.Value, ReadPositiveDouble("Cylinder volume (cc): ") ?? 0)
                    : selectedType == typeof(Car)
                        ? new Car(registration, color, wheels.Value, ReadFuelType())
                        : selectedType == typeof(Bus)
                            ? new Bus(registration, color, wheels.Value, ReadPositiveInt("Number of seats: ") ?? 0, ReadFuelType())
                            : new Boat(registration, color, wheels.Value, ReadPositiveDouble("Length (m): ") ?? 0);

            if (_handler.TryParkVehicle(vehicle, out var message))
                Console.WriteLine($"{vehicle.RegistrationNumber} was parked.");
            else
                Console.WriteLine(message);
        }
        catch (ArgumentException exception)
        {
            Console.WriteLine($"Invalid vehicle details: {exception.Message}");
        }
    }

    private void RemoveVehicle()
    {
        var registration = ReadRequired("Registration number: ");
        if (registration is not null)
        {
            _handler.TryRemoveVehicle(registration, out _, out var message);
            Console.WriteLine(message);
        }
    }

    private void FindVehicle()
    {
        var registration = ReadRequired("Registration number: ");
        if (registration is null)
            return;
        var vehicle = _handler.FindVehicle(registration);
        Console.WriteLine(vehicle is null ? "No vehicle with that registration number was found." : vehicle.ToString());
    }

    private void SearchVehicles()
    {
        Console.Write("Color (blank for any): ");
        var color = Console.ReadLine();
        var wheels = ReadOptionalInt("Number of wheels (blank for any): ");
        if (wheels == int.MinValue)
            return;

        Console.WriteLine("Vehicle type: blank for any, 1. Airplane, 2. Motorcycle, 3. Car, 4. Bus, 5. Boat");
        Console.Write("Type: ");
        var type = Console.ReadLine();
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
            Console.WriteLine("Unknown vehicle type.");
            return;
        }

        Console.WriteLine("Optional vehicle property: blank for none, 1. NumberOfEngines, 2. CylinderVolume, 3. FuelType, 4. NumberOfSeats, 5. Length");
        Console.Write("Property: ");
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
            Console.WriteLine("Unknown vehicle property.");
            return;
        }
        var propertyValue = propertyName is null ? null : ReadRequired("Property value (numeric values use a period): ");
        if (propertyName is not null && propertyValue is null)
            return;

        var results = _handler.SearchVehicles(color, wheels, vehicleType, propertyName, propertyValue);
        if (results.Count == 0)
        {
            Console.WriteLine("No matching vehicles were found.");
            return;
        }
        foreach (var vehicle in results)
            Console.WriteLine(vehicle);
    }

    private void CreateGarage()
    {
        var name = ReadRequired("Garage name: ");
        if (name is null)
            return;
        Console.WriteLine("Profile: 1. General  2. Hangar (airplanes)  3. Motorcycle garage  4. Boat dock");
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
            Console.WriteLine("Unknown garage profile.");
            return;
        }
        var capacity = ReadPositiveInt("Capacity in parking spaces: ");
        if (capacity is null)
            return;
        if (_handler.CreateGarage(name, kind.Value, capacity.Value, out var message))
            _handler.SelectGarage(name, out _);
        Console.WriteLine(message);
    }

    private void SwitchGarage()
    {
        var name = ReadRequired("Garage name: ");
        if (name is not null)
        {
            _handler.SelectGarage(name, out var message);
            Console.WriteLine(message);
        }
    }

    private void ListGarages()
    {
        foreach (var garage in _handler.GetGarages())
            Console.WriteLine($"{garage.Name} ({garage.Kind}): {garage.VehicleCount} vehicle(s), {garage.UsedSpaces}/{garage.Capacity} spaces used");
    }

    private async Task SaveGaragesAsync()
    {
        var path = ReadOptionalPath("Save file", GetConfiguredSavePath());
        if (path is not null)
        {
            var result = await _handler.SaveAsync(path).ConfigureAwait(false);
            Console.WriteLine(result.Message);
        }
    }

    private async Task LoadGaragesAsync()
    {
        var path = ReadOptionalPath("Load file", GetConfiguredSavePath());
        if (path is not null)
        {
            var result = await _handler.LoadAsync(path).ConfigureAwait(false);
            Console.WriteLine(result.Message);
        }
    }

    private string GetConfiguredSavePath() => _configuration["Garage:SaveFilePath"] ?? DefaultSavePath;

    private static string? ReadOptionalPath(string prompt, string defaultPath)
    {
        Console.Write($"{prompt} path (Enter for {defaultPath}): ");
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
            Console.WriteLine("A value is required.");
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
            Console.WriteLine(allowZero ? "Enter a non-negative whole number." : "Enter a positive whole number.");
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
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && value > 0 && double.IsFinite(value))
                return value;
            Console.WriteLine("Enter a positive number using a period as the decimal separator.");
        }
    }

    private static int? ReadOptionalInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            if (input is null)
                return int.MinValue;
            if (string.IsNullOrWhiteSpace(input))
                return null;
            if (int.TryParse(input, out var value) && value >= 0)
                return value;
            Console.WriteLine("Enter a non-negative whole number or leave blank.");
        }
    }

    private static FuelType ReadFuelType()
    {
        while (true)
        {
            Console.Write("Fuel (1. Gasoline, 2. Diesel, 3. Electric): ");
            switch (Console.ReadLine())
            {
                case "1": return FuelType.Gasoline;
                case "2": return FuelType.Diesel;
                case "3": return FuelType.Electric;
                case null: throw new ArgumentException("Input ended before a fuel type was selected.");
                default: Console.WriteLine("Choose 1, 2, or 3."); break;
            }
        }
    }
}
