using System;

namespace Ovn9.Garage3.Models;

public sealed class Bus : Vehicle
{
    public Bus(string registrationNumber, string color, int numberOfWheels, int numberOfSeats, FuelType fuelType)
        : base(registrationNumber, color, numberOfWheels)
    {
        if (numberOfSeats <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfSeats), "A bus must have at least one seat.");
        if (!Enum.IsDefined(fuelType))
            throw new ArgumentOutOfRangeException(nameof(fuelType));
        NumberOfSeats = numberOfSeats;
        FuelType = fuelType;
    }

    public int NumberOfSeats { get; }
    public FuelType FuelType { get; }

    public override string ToString() => $"{base.ToString()}, {NumberOfSeats} seat(s), {FuelType}";
}
