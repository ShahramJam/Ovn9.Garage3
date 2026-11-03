using Ovn9.Garage3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ovn9.Garage3;

public sealed record GarageInfo(string Name, GarageKind Kind, int Capacity, int VehicleCount, decimal UsedSpaces, decimal AvailableSpaces);
public sealed record GarageFileOperationResult(bool Succeeded, string Message);

public sealed class GarageHandler : IGarageHandler
{
    private readonly Dictionary<string, GarageEntry> _garages = new(StringComparer.OrdinalIgnoreCase);
    private string? _activeGarageName;

    public bool InitializeGarage(int capacity, out string message) =>
        CreateGarage("Main Garage", GarageKind.General, capacity, out message);

    public bool CreateGarage(string name, GarageKind kind, int capacity, out string message)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            message = "Garage name is required.";
            return false;
        }
        if (!Enum.IsDefined(kind))
        {
            message = "Unknown garage profile.";
            return false;
        }
        if (capacity <= 0)
        {
            message = "Capacity must be greater than zero.";
            return false;
        }
        if (_garages.ContainsKey(name.Trim()))
        {
            message = "A garage with that name already exists.";
            return false;
        }

        _garages.Add(name.Trim(), new GarageEntry(kind, new Garage<IVehicle>(capacity)));
        _activeGarageName ??= name.Trim();
        message = $"{kind} '{name.Trim()}' created with capacity {capacity}.";
        return true;
    }

    public bool SelectGarage(string name, out string message)
    {
        if (string.IsNullOrWhiteSpace(name) || !_garages.TryGetValue(name.Trim(), out _))
        {
            message = "No garage with that name was found.";
            return false;
        }

        _activeGarageName = _garages.Keys.First(key => string.Equals(key, name.Trim(), StringComparison.OrdinalIgnoreCase));
        message = $"Now working in '{_activeGarageName}'.";
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
        var (_, entry) = GetActiveEntry();
        if (vehicle is null)
        {
            message = "A vehicle is required.";
            return false;
        }
        if (!IsAllowed(entry.Kind, vehicle.GetType()))
        {
            message = $"{vehicle.GetType().Name} vehicles are not allowed in this {entry.Kind} garage.";
            return false;
        }

        return entry.Garage.TryAddVehicle(vehicle, out message);
    }

    public bool TryRemoveVehicle(string registrationNumber, out IVehicle? vehicle, out string message)
    {
        var garage = GetActiveEntry().Entry.Garage;
        var removed = garage.TryRemoveVehicle(registrationNumber, out vehicle);
        message = removed
            ? $"Vehicle {vehicle!.RegistrationNumber} was removed."
            : "No vehicle with that registration number was found.";
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
            return new(false, "A save file path is required.");
        if (_garages.Count == 0)
            return new(false, "There are no garages to save.");

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
            return new(true, $"Saved {_garages.Count} garage(s) to '{filePath}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return new(false, $"Could not save garages: {exception.Message}");
        }
    }

    public async Task<GarageFileOperationResult> LoadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return new(false, "A load file path is required.");

        try
        {
            var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize<GarageStoreDocument>(json, GarageJson.Options);
            if (document?.Garages is null || document.Garages.Count == 0)
                throw new InvalidDataException("The save file contains no garages.");

            var restored = new Dictionary<string, GarageEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var savedGarage in document.Garages)
            {
                if (savedGarage is null || string.IsNullOrWhiteSpace(savedGarage.Name) || !Enum.IsDefined(savedGarage.Kind))
                    throw new InvalidDataException("A saved garage has an invalid name or profile.");
                if (savedGarage.Vehicles is null)
                    throw new InvalidDataException($"Garage '{savedGarage.Name}' has no valid vehicle list.");
                if (restored.ContainsKey(savedGarage.Name.Trim()))
                    throw new InvalidDataException($"Duplicate garage name '{savedGarage.Name}'.");

                var garage = new Garage<IVehicle>(savedGarage.Capacity, savedGarage.Vehicles);
                if (garage.Any(vehicle => !IsAllowed(savedGarage.Kind, vehicle.GetType())))
                    throw new InvalidDataException($"Garage '{savedGarage.Name}' contains a vehicle not allowed by its profile.");
                restored.Add(savedGarage.Name.Trim(), new GarageEntry(savedGarage.Kind, garage));
            }

            var activeName = restored.Keys.FirstOrDefault(name => string.Equals(name, document.ActiveGarageName, StringComparison.OrdinalIgnoreCase));
            if (activeName is null)
                throw new InvalidDataException("The saved active garage does not exist.");

            _garages.Clear();
            foreach (var pair in restored)
                _garages.Add(pair.Key, pair.Value);
            _activeGarageName = activeName;
            return new(true, $"Loaded {_garages.Count} garage(s) from '{filePath}'.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException or InvalidDataException)
        {
            return new(false, $"Could not load garages: {exception.Message}");
        }
    }

    private (string Name, GarageEntry Entry) GetActiveEntry()
    {
        if (_activeGarageName is null || !_garages.TryGetValue(_activeGarageName, out var entry))
            throw new InvalidOperationException("No garage has been initialized.");
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

    private static int GetParkingUnits(Type vehicleType) => vehicleType == typeof(Airplane) ? 9
        : vehicleType == typeof(Boat) ? 6
        : vehicleType == typeof(Motorcycle) ? 1
        : 3;

    private sealed record GarageEntry(GarageKind Kind, Garage<IVehicle> Garage);
}

internal sealed class GarageStoreDocument
{
    public string ActiveGarageName { get; set; } = string.Empty;
    public List<GarageStoreEntry> Garages { get; set; } = [];
}

internal sealed class GarageStoreEntry
{
    public string Name { get; set; } = string.Empty;
    public GarageKind Kind { get; set; }
    public int Capacity { get; set; }
    public List<IVehicle> Vehicles { get; set; } = [];
}

internal static class GarageJson
{
    internal static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
}
