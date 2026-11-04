using System;

namespace Ovn9.Garage3.Models;

// Representerar en motorcykel med cylindervolym och ett tredjedels parkeringsutrymme
public sealed class Motorcycle : Vehicle
{
    public Motorcycle(string registrationNumber, string color, int numberOfWheels, double cylinderVolume)
        : base(registrationNumber, color, numberOfWheels)
    {
        if (cylinderVolume <= 0 || double.IsNaN(cylinderVolume) || double.IsInfinity(cylinderVolume))
            throw new ArgumentOutOfRangeException(nameof(cylinderVolume), "Cylindervolymen måste vara ett positivt tal.");
        CylinderVolume = cylinderVolume;
    }

    public double CylinderVolume { get; }
    public override int ParkingSpaceUnitsRequired => 1;

    public override string ToString() => $"{base.ToString()}, {CylinderVolume} kubikcentimeter";
}
