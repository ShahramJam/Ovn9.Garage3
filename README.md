# Garage 3.0

Console garage application with multiple named garage profiles, vehicle-specific searches, capacity-aware parking, and JSON persistence.

## Run

Run the `Ovn9.Garage3` project. The main menu lets you create and switch garages, list/search/park/remove vehicles, and save or load all garages. A garage profile limits accepted vehicles: General accepts all, Hangar accepts airplanes, Motorcycle accepts motorcycles, and BoatDock accepts boats.

Parking use is counted in thirds of a space: motorcycles use 1/3, cars and buses use 1, boats use 2, and airplanes use 3. The parking menu only offers vehicle types that are allowed and currently fit.

## Configuration and saved data

Edit `Ovn9.Garage3/appsettings.json` to configure the initial garage capacity and default save/load path:

```json
{
  "Garage": {
	"DefaultCapacity": 10,
	"SaveFilePath": "garage-data.json"
  }
}
```

Saving and loading are available in the console menu. The JSON file retains vehicle subclasses, garage profiles, capacities, and the active garage.
The application writes logs to the console using .NET's built-in `Microsoft.Extensions.Logging` console provider.

## Tests

Run the `Ovn9.Garage3.Tests` project to test garage capacity and search, vehicle space requirements, garage profiles, switching, and persistence.
