using System;
using System.Globalization;

namespace Ovn9.Garage3.Models;

public abstract class Vehicle : IVehicle
{
    protected Vehicle(string registrationNumber, string color, int numberOfWheels)
    {
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Registration number is required.", nameof(registrationNumber));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Color is required.", nameof(color));
        if (numberOfWheels < 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfWheels), "Number of wheels cannot be negative.");

        RegistrationNumber = registrationNumber.Trim();
        Color = color.Trim();
        NumberOfWheels = numberOfWheels;
    }

    public string RegistrationNumber { get; }
    public string Color { get; }
    public int NumberOfWheels { get; }
    public virtual int ParkingSpaceUnitsRequired => 3;
    public decimal ParkingSpacesRequired => ParkingSpaceUnitsRequired / 3m;

    public override string ToString() =>
        $"{GetType().Name}: {RegistrationNumber}, {Color}, {NumberOfWheels} wheel(s), {ParkingSpacesRequired.ToString(CultureInfo.InvariantCulture)} parking space(s)";
}
