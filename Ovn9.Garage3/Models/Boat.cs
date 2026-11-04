using System;

namespace Ovn9.Garage3.Models;

// Representerar en båt med längd och särskilt parkeringsbehov
public sealed class Boat : Vehicle
{
    public Boat(string registrationNumber, string color, int numberOfWheels, double length)
        : base(registrationNumber, color, numberOfWheels)
    {
        if (length <= 0 || double.IsNaN(length) || double.IsInfinity(length))
            throw new ArgumentOutOfRangeException(nameof(length), "Längden måste vara ett positivt tal.");
        Length = length;
    }

    public double Length { get; }
    public override int ParkingSpaceUnitsRequired => 6;

    public override string ToString() => $"{base.ToString()}, {Length} m";
}
