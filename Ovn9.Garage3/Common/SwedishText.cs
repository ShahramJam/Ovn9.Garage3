using Ovn9.Garage3.Models;
using System;

namespace Ovn9.Garage3.Common;

// Översätter fordonstyper, garageprofiler och bränsletyper till svenska visningsnamn.
internal static class SwedishText
{
    internal static string GetVehicleTypeName(Type vehicleType) => vehicleType == typeof(Airplane) ? "Flygplan"
        : vehicleType == typeof(Motorcycle) ? "Motorcykel"
        : vehicleType == typeof(Car) ? "Bil"
        : vehicleType == typeof(Bus) ? "Buss"
        : vehicleType == typeof(Boat) ? "Båt"
        : vehicleType == typeof(Vehicle) ? "Fordon"
        : vehicleType.Name;

    internal static string GetGarageKindName(GarageKind kind) => kind switch
    {
        GarageKind.General => "Allmänt",
        GarageKind.Hangar => "Hangar",
        GarageKind.Motorcycle => "Motorcykelgarage",
        GarageKind.BoatDock => "Båtbrygga",
        _ => "Okänd"
    };

    internal static string GetFuelTypeName(FuelType fuelType) => fuelType switch
    {
        FuelType.Gasoline => "Bensin",
        FuelType.Diesel => "Diesel",
        FuelType.Electric => "El",
        _ => "Okänd"
    };
}