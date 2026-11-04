using System;

namespace Ovn9.Garage3.Models;

// Representerar ett flygplan med motorinformation och särskilt parkeringsbehov
public sealed class Airplane : Vehicle
{
    public Airplane(string registrationNumber, string color, int numberOfWheels, int numberOfEngines)
        : base(registrationNumber, color, numberOfWheels)
    {
        if (numberOfEngines <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfEngines), "Ett flygplan måste ha minst en motor.");
        NumberOfEngines = numberOfEngines;
    }

    public int NumberOfEngines { get; }
    public override int ParkingSpaceUnitsRequired => 9;

    public override string ToString() => $"{base.ToString()}, {NumberOfEngines} {(NumberOfEngines == 1 ? "motor" : "motorer")}";
}
