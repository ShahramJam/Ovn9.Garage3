using Ovn9.Garage3.Abstractions;
using System;
using System.Globalization;

namespace Ovn9.Garage3.Models;

public abstract class Vehicle : IVehicle
{
    protected Vehicle(string registrationNumber, string color, int numberOfWheels)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Registreringsnummer måste anges.", nameof(registrationNumber));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Färg måste anges.", nameof(color));
        if (numberOfWheels < 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfWheels), "Antalet hjul får inte vara negativt.");

        RegistrationNumber = registrationNumber.Trim();
        Color = color.Trim();
        NumberOfWheels = numberOfWheels;
    }

    public string RegistrationNumber { get; }
    public string Color { get; }
    public int NumberOfWheels { get; }
    public virtual int ParkingSpaceUnitsRequired => 3;
    public decimal ParkingSpacesRequired => ParkingSpaceUnitsRequired / 3m;

    public override string ToString()
    {
        var parkingSpaceLabel = ParkingSpacesRequired == 1m ? "parkeringsplats" : "parkeringsplatser";
        return $"{SwedishText.GetVehicleTypeName(GetType())}: {RegistrationNumber}, {Color}, {NumberOfWheels} hjul, {ParkingSpacesRequired.ToString(CultureInfo.InvariantCulture)} {parkingSpaceLabel}";
    }
}
