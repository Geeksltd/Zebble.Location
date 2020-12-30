[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Location/master/icon.png "Zebble.Location"


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
#### GetCurrentPosition

To get the device (user) current geo-location, altitude, speed, etc.

```csharp
var position = await LocationService.Location.GetCurrentPosition (desiredAccuracy, timeout);
```

##### Using the result

The result is an instance of the following class:

```csharp
public class GeoPosition
{
     // Always provided:
     public double Latitude;
     public double Longitude;
     public double Accuracy;

     // May or may not be provided.
     public double? Altitude;
     public double? AltitudeAccuracy;
     public double? Speed;
}
```

As you can see the if you want to use Altitude and Speed properties, your code should handle the scenario of them not being returned.

#### Tracking User Location

If you need to track the user's location (instead of getting just the current one) you should not constantly poll the current location, because that can be very inefficient and drain the battery quickly. 

The reason is that the device may not be moving for long periods of time, or also small changes in location may not be important to your app. For example, if your app is to track lorry drivers around the city, changes below 50m may be ignored in the interest of efficiency.
To make application able to track device (user) location and get the update location of it.

##### StartTracking

Instead of active polling, you should use the built-in feature provided in smartphones for reacting to changes in the location. This way the operating system will notify your app if an important location change has occured, to then only process that for your app's purpose. To start tracking in Zebble you should specify the tracking parameters and then invoke the **StartTracking()** method.

```csharp
var settings = new LocationTrackingSettings { ....  };

LocationService.Location.StartTracking(settings);
```

##### Receiving updates

When you start tracking, the system will raise an event to notify you of any changes. You should handle that event and use it for your app's purposes.

```csharp
LocationService.Location.PositionChanged += HandlePositionChanged;
void HandlePositionChanged(Zebble.Services.GeoPosition newPosition)
{
     // use the newPosition as you want....
}
```

#### Tracking settings

You can specify any of the following settings.

- **Report Interval** (default: 1 second): The requested minimum time interval between location updates, in milliseconds. If your application requires updates infrequently, set this value so that location services can conserve power by calculating location only when needed.
- **Movement Threshold** (default: 1 meter): The minimum distance of movement needed (in meters) relative to the coordinate from the last change event to report an update.

##### iOS-only settings

- **Allow Background Updates** (default: false): Whether background location updates are allowed (iOS 9+).

In **Info.plist** add the following:

```xml
<key>UIBackgroundModes</key>
<array>
    <string>location</string>
</array>
```

- **Auto Pause When Steady** (default: true):  Whether location updates should be paused automatically when the location is unlikely to change (iOS 6+).
- **Purpose** (enum): The purpose of tracking. This is used by the OS to determine when to auto-pause location updates (iOS 6+).
- **Ignore Small Changes** (default: false):  Whether the location manager should only listen for significant changes in location, rather than continuous listening (iOS 4+).
- **Defer Location Updates** (default: false): Whether the location manager should defer location updates until an energy efficient time arrives, or distance and time criteria are met (iOS 6+).
- **Deferral Time** (default: 5 mins): If deferring location updates, the minimum time that should elapse before updates are delivered (iOS 6+). Set to null for indefinite wait.

##### PositionError

In many apps, if the user location is temporarily not available you may want to ignore it. For instance, there might not be GPS signals available, etc.

But if tracking location accurately is vital in your app and you don't want to ignore error cases, you can handle the **PositionError** event.

```csharp
LocationService.Location.PositionError += HandlePositionError;
void HandlePositionError(Exception error)
{
     // ....
}
```

#### LaunchDirections

You can also open the built-in map application on the device to show directions to any given address. For example if you have some addressing in your database, you can add a button in your app which calls the Launch directions method with the address data. This means that you do not need to build any UI for a map and directions yourself and this works like any external link.

```csharp
var destination = new NavigationAddress
{
    Zip = "SM4 5BE",
    City = "London",
    Country = "England"
};

await LocationService.Location.LaunchDirections(destination);
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
