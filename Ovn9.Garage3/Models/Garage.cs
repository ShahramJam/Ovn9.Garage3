using Ovn9.Garage3.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Ovn9.Garage3.Models;

public sealed class Garage<T> : IEnumerable<T> where T : IVehicle
{
    private readonly List<T> _vehicles = [];

    public Garage(int capacity, IEnumerable<T>? initialVehicles = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Garagekapaciteten måste vara större än noll.");

        Capacity = capacity;
        if (initialVehicles is null)
            return;

        foreach (var vehicle in initialVehicles)
        {
            if (!TryAddVehicle(vehicle, out var error))
                throw new ArgumentException($"Kunde inte initiera garaget: {error}", nameof(initialVehicles));
        }
    }

    public int Capacity { get; }
    public int Count => _vehicles.Count;
    public long UsedParkingSpaceUnits => _vehicles.Sum(vehicle => (long)vehicle.ParkingSpaceUnitsRequired);
    public decimal UsedParkingSpaces => UsedParkingSpaceUnits / 3m;
    public decimal AvailableSpaces => (Capacity * 3L - UsedParkingSpaceUnits) / 3m;

    public bool CanFitVehicle(T vehicle) => vehicle is not null
        && vehicle.ParkingSpaceUnitsRequired > 0
        && vehicle.ParkingSpaceUnitsRequired <= Capacity * 3L - UsedParkingSpaceUnits;

    public bool AddVehicle(T vehicle) => TryAddVehicle(vehicle, out _);

    public bool TryAddVehicle(T vehicle, out string error)
    {
        if (vehicle is null)
        {
            error = "Ett fordon måste anges.";
            return false;
        }

        if (vehicle.ParkingSpaceUnitsRequired <= 0)
        {
            error = "Ett fordon måste kräva ett positivt parkeringsutrymme.";
            return false;
        }

        if (!CanFitVehicle(vehicle))
        {
            error = "Det finns inte tillräckligt med parkeringsutrymme för fordonet.";
            return false;
        }

        if (_vehicles.Any(existing => string.Equals(
                existing.RegistrationNumber, vehicle.RegistrationNumber, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Ett fordon med det registreringsnumret är redan parkerat.";
            return false;
        }

        _vehicles.Add(vehicle);
        error = string.Empty;
        return true;
    }

    public bool RemoveVehicle(string registrationNumber)
    {
        var vehicle = FindVehicle(registrationNumber);
        return vehicle is not null && _vehicles.Remove(vehicle);
    }

    public bool TryRemoveVehicle(string registrationNumber, out T? vehicle)
    {
        vehicle = FindVehicle(registrationNumber);
        return vehicle is not null && _vehicles.Remove(vehicle);
    }

    public T? FindVehicle(string registrationNumber)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            return default;

        return _vehicles.FirstOrDefault(vehicle => string.Equals(
            vehicle.RegistrationNumber, registrationNumber.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<T> SearchVehicles(
        string? color = null,
        int? numberOfWheels = null,
        Type? vehicleType = null,
        string? vehicleProperty = null,
        string? propertyValue = null)
    {
        if (numberOfWheels < 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfWheels), "Antalet hjul får inte vara negativt.");
        if (vehicleType is not null && !typeof(IVehicle).IsAssignableFrom(vehicleType))
            throw new ArgumentException("Söktypen måste implementera IVehicle.", nameof(vehicleType));
        if ((vehicleProperty is null) != (propertyValue is null))
            throw new ArgumentException("Både en fordonsegenskap och dess värde måste anges.");

        IEnumerable<T> results = _vehicles;
        if (!string.IsNullOrWhiteSpace(color))
            results = results.Where(vehicle => string.Equals(vehicle.Color, color.Trim(), StringComparison.OrdinalIgnoreCase));
        if (numberOfWheels.HasValue)
            results = results.Where(vehicle => vehicle.NumberOfWheels == numberOfWheels.Value);
        if (vehicleType is not null)
            results = results.Where(vehicle => vehicleType.IsInstanceOfType(vehicle));
        if (vehicleProperty is not null)
            results = results.Where(vehicle => MatchesProperty(vehicle, vehicleProperty, propertyValue!));

        return results.ToList();
    }

    public IReadOnlyDictionary<string, int> GetVehicleTypeCounts() =>
        _vehicles.GroupBy(vehicle => SwedishText.GetVehicleTypeName(vehicle.GetType()))
            .ToDictionary(group => group.Key, group => group.Count());

    public IEnumerator<T> GetEnumerator() => _vehicles.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static bool MatchesProperty(T vehicle, string propertyName, string expectedValue)
    {
        var actualValue = (vehicle, propertyName.Trim().ToLowerInvariant()) switch
        {
            (Airplane airplane, "numberofengines") => airplane.NumberOfEngines.ToString(CultureInfo.InvariantCulture),
            (Motorcycle motorcycle, "cylindervolume") => motorcycle.CylinderVolume.ToString("G", CultureInfo.InvariantCulture),
            (Car car, "fueltype") => SwedishText.GetFuelTypeName(car.FuelType),
            (Bus bus, "numberofseats") => bus.NumberOfSeats.ToString(CultureInfo.InvariantCulture),
            (Bus bus, "fueltype") => SwedishText.GetFuelTypeName(bus.FuelType),
            (Boat boat, "length") => boat.Length.ToString("G", CultureInfo.InvariantCulture),
            _ => null
        };

        return actualValue is not null
            && string.Equals(actualValue, expectedValue.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
