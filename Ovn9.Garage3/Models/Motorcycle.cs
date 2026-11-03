using System;

namespace Ovn9.Garage3.Models;

public sealed class Motorcycle : Vehicle
{
    public Motorcycle(string registrationNumber, string color, int numberOfWheels, double cylinderVolume)
        : base(registrationNumber, color, numberOfWheels)
    {
        if (cylinderVolume <= 0 || double.IsNaN(cylinderVolume) || double.IsInfinity(cylinderVolume))
            throw new ArgumentOutOfRangeException(nameof(cylinderVolume), "Cylinder volume must be a positive number.");
        CylinderVolume = cylinderVolume;
    }

    public double CylinderVolume { get; }
    public override int ParkingSpaceUnitsRequired => 1;

    public override string ToString() => $"{base.ToString()}, {CylinderVolume} cc";
}
