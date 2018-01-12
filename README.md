## Location
Location is a plugin for Zebble applications which make developers able to get current location of user or track the location of them and show it on a map. Also, it provide a way to show directions on all platforms. Location implemented for Android, IOS and UWP platforms and it is available on NuGet.
### How to use Location in Zebble?
You need to install this plugin from NuGet to each platform that you want to use it. After that, you can use it to get location or track a device and find the routs and directions.
#### Methods:
* ``IsEnabled``
* ``IsSupported``
* ``GetCurrentPosition``
* ``StartTracking``
* ``LaunchDirections``

##### IsEnabled
To ensure about enabling location on the device.
```csharp
if (!await LocationService.Location.IsEnabled())
{
     await Alert.Show("Geo location is not enabled on your device.");
     return;  
}
```
##### IsSupported
To check location supported by the device.
```csharp
if (await LocationService.Location.IsSupported())
{
    await Alert.Show("Geo location is not supported on your device.");
    return;
} 
```
##### GetCurrentPosition
To get the device (user) current geo-location, altitude, speed, etc.
```csharp
var position = await LocationService.Location.GetCurrentPosition (desiredAccuracy, timeout);
```
##### StartTracking
To make application able to track device (user) location and get the update location of it.
```csharp
var settings = new LocationTrackingSettings { ....  };

LocationService.Location.StartTracking(settings);
```
##### LaunchDirections
To show route or direction with specific address of the location.
```csharp
var destination = new NavigationAddress
{
    Zip = "SM4 5BE",
    City = "London",
    Country = "England"
};

await LocationService.Location.LaunchDirections(destination);
```
#### Events:
* ``PositionChanged``
* ``PositionError``

##### PositionChanged
When you start tracking, the system will raise an event to notify you of any changes.
```csharp
LocationService.Location.PositionChanged += HandlePositionChanged;
void HandlePositionChanged(Zebble.Services.GeoPosition newPosition)
{
     // use the newPosition as you want....
}
```
##### PositionError
When the user location is temporarily not available system will raise this event.
```csharp
LocationService.Location.PositionError += HandlePositionError;
void HandlePositionError(Exception error)
{
     // ....
}
```
