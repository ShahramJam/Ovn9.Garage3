using Microsoft.Extensions.Logging.Abstractions;
using Ovn9.Garage3.Abstractions;
using Ovn9.Garage3.Common;
using Ovn9.Garage3.Handlers;
using Ovn9.Garage3.Models;

namespace Ovn9.Garage3.Tests;

// Samlar automatiserade tester för parkering, sökning, garagehantering
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
    public void ToString_WhenVehicleIsCar_UsesSwedishLabels()
    {
        var car = new Car("CAR001", "Svart", 4, FuelType.Gasoline);
        var garage = new Garage<IVehicle>(1, [car]);

        Assert.Equal("Bil: CAR001, Svart, 4 hjul, 1 parkeringsplats, Bensin", car.ToString());
        Assert.Equal(1, garage.GetVehicleTypeCounts()["Bil"]);
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

    [Fact]
    public void AddVehicle_WhenParkingSpaceIsFractional_UsesThirdsWithoutRoundingErrors()
    {
        var garage = new Garage<IVehicle>(1);

        Assert.True(garage.AddVehicle(new Motorcycle("MOT001", "Red", 2, 650)));
        Assert.True(garage.AddVehicle(new Motorcycle("MOT002", "Blue", 2, 500)));
        Assert.True(garage.AddVehicle(new Motorcycle("MOT003", "Black", 2, 400)));
        Assert.False(garage.AddVehicle(new Motorcycle("MOT004", "White", 2, 300)));
        Assert.Equal(1m, garage.UsedParkingSpaces);
        Assert.Equal(0m, garage.AvailableSpaces);
    }

    [Fact]
    public void AddVehicle_WhenVehicleNeedsMultipleSpaces_RejectsVehicleThatDoesNotFit()
    {
        var garage = new Garage<IVehicle>(2);

        var parked = garage.AddVehicle(new Boat("BOA001", "White", 0, 7.5));
        var airplaneParked = garage.AddVehicle(new Airplane("AIR001", "Blue", 3, 2));

        Assert.True(parked);
        Assert.False(airplaneParked);
        Assert.Equal(0m, garage.AvailableSpaces);
    }

    [Fact]
    public void SearchVehicles_WhenVehiclePropertyIsProvided_FiltersSubclassProperty()
    {
        var dieselBus = new Bus("BUS001", "Yellow", 6, 40, FuelType.Diesel);
        var garage = new Garage<IVehicle>(4,
        [
            dieselBus,
            new Bus("BUS002", "Yellow", 6, 20, FuelType.Gasoline),
            new Car("CAR001", "Yellow", 4, FuelType.Diesel)
        ]);

        var matches = garage.SearchVehicles("yellow", 6, typeof(Bus), "FuelType", "diesel");

        Assert.Same(dieselBus, Assert.Single(matches));
    }

    [Fact]
    public void GetVehicleTypesThatFit_WhenGarageHasLimitedSpace_ReturnsOnlyFittingAllowedTypes()
    {
        var handler = new GarageHandler(NullLogger<GarageHandler>.Instance);
        handler.CreateGarage("Road", GarageKind.General, 1, out _);
        handler.TryParkVehicle(new Motorcycle("MOT001", "Red", 2, 650), out _);
        handler.TryParkVehicle(new Motorcycle("MOT002", "Blue", 2, 500), out _);

        var fittingTypes = handler.GetVehicleTypesThatFit();

        Assert.Equal([typeof(Motorcycle)], fittingTypes);
    }

    [Fact]
    public void TryParkVehicle_WhenVehicleDoesNotMatchGarageProfile_RejectsVehicle()
    {
        var handler = new GarageHandler(NullLogger<GarageHandler>.Instance);
        handler.CreateGarage("Hangar", GarageKind.Hangar, 3, out _);

        var parked = handler.TryParkVehicle(new Car("CAR001", "Black", 4, FuelType.Gasoline), out var message);

        Assert.False(parked);
        Assert.Contains("inte tillåten", message);
    }

    [Fact]
    public void SelectGarage_WhenChangingGarage_UsesOnlySelectedGarageVehicles()
    {
        var handler = new GarageHandler(NullLogger<GarageHandler>.Instance);
        handler.CreateGarage("Road", GarageKind.General, 2, out _);
        Assert.True(handler.TryParkVehicle(new Car("CAR001", "Black", 4, FuelType.Gasoline), out _));
        handler.CreateGarage("Hangar", GarageKind.Hangar, 3, out _);
        Assert.True(handler.SelectGarage("Hangar", out _));
        Assert.True(handler.TryParkVehicle(new Airplane("AIR001", "White", 3, 2), out _));
        Assert.True(handler.SelectGarage("Road", out _));

        var activeVehicles = handler.GetVehicles();

        Assert.IsType<Car>(Assert.Single(activeVehicles));
        Assert.Equal("Road", handler.GetActiveGarageInfo().Name);
    }

    [Fact]
    public async Task SaveAndLoad_WhenGaragesContainMultipleVehicleTypes_RestoresGarageAndActiveSelection()
    {
        var path = Path.Combine(Path.GetTempPath(), $"garage-{Guid.NewGuid():N}.json");
        try
        {
            var original = new GarageHandler(NullLogger<GarageHandler>.Instance);
            original.CreateGarage("Road", GarageKind.General, 3, out _);
            Assert.True(original.TryParkVehicle(new Motorcycle("MOT001", "Red", 2, 650), out _));
            original.CreateGarage("Hangar", GarageKind.Hangar, 3, out _);
            Assert.True(original.SelectGarage("Hangar", out _));
            Assert.True(original.TryParkVehicle(new Airplane("AIR001", "White", 3, 2), out _));
            var saveResult = await original.SaveAsync(path);
            Assert.True(saveResult.Succeeded, saveResult.Message);

            var restored = new GarageHandler(NullLogger<GarageHandler>.Instance);
            var loadResult = await restored.LoadAsync(path);

            Assert.True(loadResult.Succeeded, loadResult.Message);
            Assert.Equal("Hangar", restored.GetActiveGarageInfo().Name);
            Assert.IsType<Airplane>(Assert.Single(restored.GetVehicles()));
            Assert.Equal(2, restored.GetGarages().Count);
            restored.SelectGarage("Road", out _);
            Assert.IsType<Motorcycle>(Assert.Single(restored.GetVehicles()));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task Load_WhenSaveFileIsInvalid_PreservesExistingGarages()
    {
        var path = Path.Combine(Path.GetTempPath(), $"garage-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "not valid json");
            var handler = new GarageHandler(NullLogger<GarageHandler>.Instance);
            handler.CreateGarage("Keep", GarageKind.General, 1, out _);

            var loadResult = await handler.LoadAsync(path);

            Assert.False(loadResult.Succeeded);
            Assert.Contains("Kunde inte läsa in", loadResult.Message);
            Assert.Equal("Keep", Assert.Single(handler.GetGarages()).Name);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
