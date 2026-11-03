using System;

namespace Ovn9.Garage3.Models;

public sealed class Car : Vehicle
{
    public Car(string registrationNumber, string color, int numberOfWheels, FuelType fuelType)
        : base(registrationNumber, color, numberOfWheels)
    {
        if (!Enum.IsDefined(fuelType))
            throw new ArgumentOutOfRangeException(nameof(fuelType), "Ogiltig bränsletyp.");
        FuelType = fuelType;
    }

    public FuelType FuelType { get; }

    public override string ToString() => $"{base.ToString()}, {SwedishText.GetFuelTypeName(FuelType)}";
}
