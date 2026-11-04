using Ovn9.Garage3.Abstractions;
using Ovn9.Garage3.Common;
using System;
using System.Globalization;

namespace Ovn9.Garage3.Models;

// Gemensam basklass för fordonens grundegenskaper, validering och textrepresentation
public abstract class Vehicle : IVehicle
{
    protected Vehicle(string registrationNumber, string color, int numberOfWheels)
    {

        if (!Validator.IsValidRegistrationNumber(registrationNumber))
        {
            throw new ArgumentException(
                "Registreringsnumret måste bestå av tre bokstäver följt av tre siffror, till exempel ABc123.",
                nameof(registrationNumber));
        }
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
