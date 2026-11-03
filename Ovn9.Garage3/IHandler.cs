using Ovn9.Garage3.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ovn9.Garage3;

public interface IGarageHandler
{
    bool InitializeGarage(int capacity, out string message);
    bool CreateGarage(string name, GarageKind kind, int capacity, out string message);
    bool SelectGarage(string name, out string message);
    IReadOnlyList<GarageInfo> GetGarages();
    GarageInfo GetActiveGarageInfo();
    IReadOnlyList<Type> GetVehicleTypesThatFit();
    bool TryParkVehicle(IVehicle vehicle, out string message);
    bool TryRemoveVehicle(string registrationNumber, out IVehicle? vehicle, out string message);
    IVehicle? FindVehicle(string registrationNumber);
    IReadOnlyList<IVehicle> GetVehicles();
    IReadOnlyDictionary<string, int> GetVehicleTypeCounts();
    IReadOnlyList<IVehicle> SearchVehicles(string? color = null, int? numberOfWheels = null, Type? vehicleType = null, string? vehicleProperty = null, string? propertyValue = null);
    Task<GarageFileOperationResult> SaveAsync(string filePath);
    Task<GarageFileOperationResult> LoadAsync(string filePath);
}
