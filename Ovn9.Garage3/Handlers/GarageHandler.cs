using Microsoft.Extensions.Logging;
using Ovn9.Garage3.Abstractions;
using Ovn9.Garage3.Common;
using Ovn9.Garage3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ovn9.Garage3.Handlers;

// Samlar garageuppgifter som behövs för att visa profil, kapacitet och beläggning
public sealed record GarageInfo(string Name, GarageKind Kind, int Capacity, int VehicleCount, decimal UsedSpaces, decimal AvailableSpaces);

// Bär resultatet och meddelandet från en spar- eller inläsningsoperation
public sealed record GarageFileOperationResult(bool Succeeded, string Message);

// Samordnar garagehantering, fordonsregler och sparning/inläsning av data
public sealed class GarageHandler : IGarageHandler
{
    private readonly Dictionary<string, GarageEntry> _garages = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<GarageHandler> _logger;
    private string? _activeGarageName;

    public GarageHandler(ILogger<GarageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public bool InitializeGarage(int capacity, out string message) =>
        CreateGarage("Huvudgaraget", GarageKind.General, capacity, out message);

    public bool CreateGarage(string name, GarageKind kind, int capacity, out string message)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            message = "Garagets namn måste anges.";
            return false;
        }
        if (!Enum.IsDefined(kind))
        {
            message = "Okänd garageprofil.";
            return false;
        }
        if (capacity <= 0)
        {
            message = "Kapaciteten måste vara större än noll.";
            return false;
        }
        if (_garages.ContainsKey(name.Trim()))
        {
            message = "Ett garage med det namnet finns redan.";
            return false;
        }

        _garages.Add(name.Trim(), new GarageEntry(kind, new Garage<IVehicle>(capacity)));
        _activeGarageName ??= name.Trim();
        message = $"Garaget '{name.Trim()}' skapades med profilen {SwedishText.GetGarageKindName(kind)} och kapaciteten {capacity}.";
        return true;
    }

    public bool SelectGarage(string name, out string message)
    {
        if (string.IsNullOrWhiteSpace(name) || !_garages.TryGetValue(name.Trim(), out _))
        {
            message = "Inget garage med det namnet hittades.";
            return false;
        }

        // Hämta den ursprungliga nyckeln så att visningen behåller garagets registrerade skiftläge.
        _activeGarageName = _garages.Keys.First(key => string.Equals(key, name.Trim(), StringComparison.OrdinalIgnoreCase));
        message = $"Arbetar nu i '{_activeGarageName}'.";
        return true;
    }

    public IReadOnlyList<GarageInfo> GetGarages() => _garages
        .Select(pair => ToInfo(pair.Key, pair.Value))
        .ToList();

    public GarageInfo GetActiveGarageInfo()
    {
        var (name, entry) = GetActiveEntry();
        return ToInfo(name, entry);
    }

    public IReadOnlyList<Type> GetVehicleTypesThatFit()
    {
        var (_, entry) = GetActiveEntry();
        return GetAllowedVehicleTypes(entry.Kind)
            .Where(type => GetParkingUnits(type) <= entry.Garage.Capacity * 3L - entry.Garage.UsedParkingSpaceUnits)
            .ToList();
    }

    public bool TryParkVehicle(IVehicle vehicle, out string message)
    {
        var (garageName, entry) = GetActiveEntry();
        if (vehicle is null)
        {
            message = "Ett fordon måste anges.";
            return false;
        }
        if (!IsAllowed(entry.Kind, vehicle.GetType()))
        {
            message = $"Fordonstypen {SwedishText.GetVehicleTypeName(vehicle.GetType())} är inte tillåten i garageprofilen {SwedishText.GetGarageKindName(entry.Kind)}.";
            return false;
        }

        var parked = entry.Garage.TryAddVehicle(vehicle, out message);

        return parked;
    }

    public bool TryRemoveVehicle(string registrationNumber, out IVehicle? vehicle, out string message)
    {
        var (garageName, entry) = GetActiveEntry();
        var removed = entry.Garage.TryRemoveVehicle(registrationNumber, out vehicle);
        message = removed
            ? $"Fordonet med registreringsnummer {vehicle!.RegistrationNumber} togs bort."
            : "Inget fordon med det registreringsnumret hittades.";

        return removed;
    }

    public IVehicle? FindVehicle(string registrationNumber) => GetActiveEntry().Entry.Garage.FindVehicle(registrationNumber);

    public IReadOnlyList<IVehicle> GetVehicles() => GetActiveEntry().Entry.Garage.ToList();

    public IReadOnlyDictionary<string, int> GetVehicleTypeCounts() => GetActiveEntry().Entry.Garage.GetVehicleTypeCounts();

    public IReadOnlyList<IVehicle> SearchVehicles(
        string? color = null,
        int? numberOfWheels = null,
        Type? vehicleType = null,
        string? vehicleProperty = null,
        string? propertyValue = null) =>
        GetActiveEntry().Entry.Garage.SearchVehicles(color, numberOfWheels, vehicleType, vehicleProperty, propertyValue);

    public async Task<GarageFileOperationResult> SaveAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new(false, "En sökväg till sparfilen måste anges.");
        }
        if (_garages.Count == 0)
        {
            return new(false, "Det finns inga garage att spara.");
        }

        try
        {
            var document = new GarageStoreDocument
            {
                ActiveGarageName = _activeGarageName!,
                Garages = _garages.Select(pair => new GarageStoreEntry
                {
                    Name = pair.Key,
                    Kind = pair.Value.Kind,
                    Capacity = pair.Value.Garage.Capacity,
                    Vehicles = pair.Value.Garage.ToList()
                }).ToList()
            };
            var json = JsonSerializer.Serialize(document, GarageJson.Options);
            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
            return new(true, $"Sparade {_garages.Count} garage i filen '{filePath}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            _logger.LogError(exception, "Det gick inte att spara garagen i filen {Filsokvag}.", filePath);
            return new(false, $"Kunde inte spara garagen: {exception.Message}");
        }
    }

    public async Task<GarageFileOperationResult> LoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new(false, "En sökväg till filen måste anges.");
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize<GarageStoreDocument>(json, GarageJson.Options);
            if (document?.Garages is null || document.Garages.Count == 0)
                throw new InvalidDataException("Sparfilen innehåller inga garage.");

            // Ersätt inte befintliga garage förrän hela sparfilen har validerats.
            var restored = new Dictionary<string, GarageEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var savedGarage in document.Garages)
            {
                if (savedGarage is null || string.IsNullOrWhiteSpace(savedGarage.Name) || !Enum.IsDefined(savedGarage.Kind))
                    throw new InvalidDataException("Ett sparat garage har ett ogiltigt namn eller en ogiltig profil.");
                if (savedGarage.Vehicles is null)
                    throw new InvalidDataException($"Garaget '{savedGarage.Name}' saknar en giltig fordonslista.");
                if (restored.ContainsKey(savedGarage.Name.Trim()))
                    throw new InvalidDataException($"Garagenamnet '{savedGarage.Name}' förekommer flera gånger.");

                var garage = new Garage<IVehicle>(savedGarage.Capacity, savedGarage.Vehicles);
                if (garage.Any(vehicle => !IsAllowed(savedGarage.Kind, vehicle.GetType())))
                    throw new InvalidDataException($"Garaget '{savedGarage.Name}' innehåller ett fordon som inte är tillåtet enligt profilen.");
                restored.Add(savedGarage.Name.Trim(), new GarageEntry(savedGarage.Kind, garage));
            }

            var activeName = restored.Keys.FirstOrDefault(name => string.Equals(name, document.ActiveGarageName, StringComparison.OrdinalIgnoreCase));
            if (activeName is null)
                throw new InvalidDataException("Det aktiva garaget i sparfilen finns inte.");

            _garages.Clear();
            foreach (var pair in restored)
                _garages.Add(pair.Key, pair.Value);
            _activeGarageName = activeName;
            return new(true, $"Läste in {_garages.Count} garage från '{filePath}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException or InvalidDataException)
        {
            _logger.LogError(exception, "Det gick inte att läsa in garagen från filen {Filsokvag}.", filePath);
            return new(false, $"Kunde inte läsa in garagen: {exception.Message}");
        }
    }

    private (string Name, GarageEntry Entry) GetActiveEntry()
    {
        if (_activeGarageName is null || !_garages.TryGetValue(_activeGarageName, out var entry))
            throw new InvalidOperationException("Inget garage har initierats.");
        return (_activeGarageName, entry);
    }

    private static GarageInfo ToInfo(string name, GarageEntry entry) => new(
        name, entry.Kind, entry.Garage.Capacity, entry.Garage.Count,
        entry.Garage.UsedParkingSpaces, entry.Garage.AvailableSpaces);

    private static IReadOnlyList<Type> GetAllowedVehicleTypes(GarageKind kind) => kind switch
    {
        GarageKind.General => [typeof(Airplane), typeof(Motorcycle), typeof(Car), typeof(Bus), typeof(Boat)],
        GarageKind.Hangar => [typeof(Airplane)],
        GarageKind.Motorcycle => [typeof(Motorcycle)],
        GarageKind.BoatDock => [typeof(Boat)],
        _ => []
    };

    private static bool IsAllowed(GarageKind kind, Type vehicleType) => GetAllowedVehicleTypes(kind).Contains(vehicleType);

    // Värdena måste motsvara fordonens krav så att förhandskontrollen stämmer med själva parkeringen.
    private static int GetParkingUnits(Type vehicleType) => vehicleType == typeof(Airplane) ? 9
        : vehicleType == typeof(Boat) ? 6
        : vehicleType == typeof(Motorcycle) ? 1
        : 3;

    // Knyter ihop en garageprofil med dess fordonslista.
    private sealed record GarageEntry(GarageKind Kind, Garage<IVehicle> Garage);
}

// Beskriver hela garagefilen, inklusive vilket garage som var aktivt när den sparades.
internal sealed class GarageStoreDocument
{
    public string ActiveGarageName { get; set; } = string.Empty;
    public List<GarageStoreEntry> Garages { get; set; } = [];
}

// Beskriver ett enskilt garage och dess fordon i sparfilen.
internal sealed class GarageStoreEntry
{
    public string Name { get; set; } = string.Empty;
    public GarageKind Kind { get; set; }
    public int Capacity { get; set; }
    public List<IVehicle> Vehicles { get; set; } = [];
}

// Håller de gemensamma inställningarna för JSON-serialisering av garage.
internal static class GarageJson
{
    internal static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
}
