using System;

namespace Ovn9.Garage3.Models;

public sealed class Airplane : Vehicle
{
    public Airplane(string registrationNumber, string color, int numberOfWheels, int numberOfEngines)
        : base(registrationNumber, color, numberOfWheels)
    {
        if (numberOfEngines <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfEngines), "An airplane must have at least one engine.");
        NumberOfEngines = numberOfEngines;
    }

    public int NumberOfEngines { get; }
    public override int ParkingSpaceUnitsRequired => 9;

    public override string ToString() => $"{base.ToString()}, {NumberOfEngines} engine(s)";
}
