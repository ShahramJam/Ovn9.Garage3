using Ovn9.Garage3.Models;
using System.Text.Json.Serialization;

namespace Ovn9.Garage3.Abstractions;

// Typidentifieraren gör att JSON-serialiseringen kan återskapa fordonets konkreta undertyp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$vehicleType")]
[JsonDerivedType(typeof(Airplane), "airplane")]
[JsonDerivedType(typeof(Motorcycle), "motorcycle")]
[JsonDerivedType(typeof(Car), "car")]
[JsonDerivedType(typeof(Bus), "bus")]
[JsonDerivedType(typeof(Boat), "boat")]
public interface IVehicle
{
    string RegistrationNumber { get; }
    string Color { get; }
    int NumberOfWheels { get; }
    int ParkingSpaceUnitsRequired { get; }
}
