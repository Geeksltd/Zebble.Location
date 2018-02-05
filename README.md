[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Location/master/Shared/NuGet/Icon.png "Zebble.Sensors"


## Zebble.Location

![logo]

A Zebble plugin to access location of device in Zebble applications.

[![NuGet](https://img.shields.io/nuget/v/Zebble.Location.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.Location/)

> This plugin make developers able to get current location of user or track the location of them and show it on a map. Also, it provide a way to show directions on all platforms. Location implemented for Android, IOS and UWP platforms.
### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.Location/](https://www.nuget.org/packages/Zebble.Location/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>
### Api Usage

Call `Zebble.Device.Location` from any project to gain access to APIs.

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
<br>


### Events
| Event             | Type                                          | Android | iOS | Windows |
| :-----------      | :-----------                                  | :------ | :-- | :------ |
| PositionChanged  | AsyncEvent<Services.GeoPosition&gt;    | x       | x   | x       |
| PositionError            | AsyncEvent<Exception&gt;    | x       | x   | x       |


<br>


### Methods
| Method       | Return Type  | Parameters                          | Android | iOS | Windows |
| :----------- | :----------- | :-----------                        | :------ | :-- | :------ |
| IsEnabled         | Task<bool&gt;| - | x       | x   | x       |
| IsSupported         | Task<bool&gt;| - | x       | x   | x       |
| GetCurrentPosition         | Task<Services.GeoPosition&gt;| desiredAccuracy -> double<bt> timeout -> int<br> silently -> bool<br> errorAction -> OnError| x       | x   | x       |
| StartTracking     | Task<bool&gt;| settings -> LocationTrackingSettings<br> silently -> bool<br> errorAction -> OnError| x       | x   | x       |
| LaunchDirections  | Task<bool&gt;| destination -> NavigationAddress<br> errorAction -> OnError| x       | x   | x       |