using Ovn9.Garage3.Models;

namespace Ovn9.Garage3.Tests;

public class GarageTests
{
    [Fact]
    public void AddVehicle_WhenCapacityAvailable_AddsVehicle()
    {
        var garage = new Garage<IVehicle>(1);
        var car = new Car("ABC123", "Black", 4, FuelType.Gasoline);

        var added = garage.AddVehicle(car);

        Assert.True(added);
        Assert.Equal(1, garage.Count);
        Assert.Same(car, Assert.Single(garage));
    }

    [Fact]
    public void AddVehicle_WhenGarageIsFull_RejectsVehicle()
    {
        var garage = new Garage<IVehicle>(1, [new Car("ABC123", "Black", 4, FuelType.Gasoline)]);

        var added = garage.AddVehicle(new Motorcycle("XYZ789", "Red", 2, 650));

        Assert.False(added);
        Assert.Equal(1, garage.Count);
    }

    [Fact]
    public void AddVehicle_WhenRegistrationDiffersOnlyByCase_RejectsDuplicate()
    {
        var garage = new Garage<IVehicle>(2, [new Car("ABC123", "Black", 4, FuelType.Gasoline)]);

        var added = garage.AddVehicle(new Motorcycle("abc123", "Red", 2, 650));

        Assert.False(added);
    }

    [Fact]
    public void FindVehicle_WhenRegistrationDiffersByCase_ReturnsVehicle()
    {
        var car = new Car("ABC123", "Black", 4, FuelType.Gasoline);
        var garage = new Garage<IVehicle>(1, [car]);

        var found = garage.FindVehicle("aBc123");

        Assert.Same(car, found);
    }

    [Fact]
    public void RemoveVehicle_WhenRegistrationDiffersByCase_RemovesVehicle()
    {
        var garage = new Garage<IVehicle>(1, [new Car("ABC123", "Black", 4, FuelType.Gasoline)]);

        var removed = garage.RemoveVehicle("abc123");

        Assert.True(removed);
        Assert.Empty(garage);
    }

    [Fact]
    public void SearchVehicles_WhenColorAndWheelCountAreProvided_ReturnsVehiclesMatchingBoth()
    {
        var matchingCar = new Car("ABC123", "Black", 4, FuelType.Gasoline);
        var garage = new Garage<IVehicle>(3,
        [
            matchingCar,
            new Bus("BUS123", "Black", 6, 40, FuelType.Diesel),
            new Car("RED123", "Red", 4, FuelType.Gasoline)
        ]);

        var matches = garage.SearchVehicles("black", 4);

        Assert.Same(matchingCar, Assert.Single(matches));
    }

    [Fact]
    public void Constructor_WhenInitialVehiclesExceedCapacity_ThrowsArgumentException()
    {
        var vehicles = new IVehicle[]
        {
            new Car("ABC123", "Black", 4, FuelType.Gasoline),
            new Motorcycle("XYZ789", "Red", 2, 650)
        };

        Assert.Throws<ArgumentException>(() => new Garage<IVehicle>(1, vehicles));
    }

    [Fact]
    public void Constructor_WhenCapacityIsNotPositive_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Garage<IVehicle>(0));
    }
}
