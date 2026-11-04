using Ovn9.Garage3.Common;
using System;

namespace Ovn9.Garage3.Models;

// Representerar en buss med sittplatser och bränsletyp
public sealed class Bus : Vehicle
{
    public Bus(string registrationNumber, string color, int numberOfWheels, int numberOfSeats, FuelType fuelType)
        : base(registrationNumber, color, numberOfWheels)
    {
        if (numberOfSeats <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfSeats), "En buss måste ha minst en sittplats.");
        if (!Enum.IsDefined(fuelType))
            throw new ArgumentOutOfRangeException(nameof(fuelType), "Ogiltig bränsletyp.");
        NumberOfSeats = numberOfSeats;
        FuelType = fuelType;
    }

    public int NumberOfSeats { get; }
    public FuelType FuelType { get; }

    public override string ToString() => $"{base.ToString()}, {NumberOfSeats} {(NumberOfSeats == 1 ? "sittplats" : "sittplatser")}, {SwedishText.GetFuelTypeName(FuelType)}";
}
