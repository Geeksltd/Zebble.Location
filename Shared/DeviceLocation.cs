namespace Zebble
{
    using System;
    using System.Threading.Tasks;

    public static class LocationService
    {
        public static readonly DeviceLocation Location = new DeviceLocation();
    }

    public partial class DeviceLocation
    {
        public const string UNAVAILABLE_ERROR = "DeviceLocation data is unavailable.";
        public const string UNAUTHORISED_ERROR = "Access to DeviceLocation data was denied.";
        public const string TIMEOUT_ERROR = "Getting DeviceLocation timed out.";

        public const double TEN_METERS = 10;
        public const int FIVE_SECONDS = 5 * 1000;

        public bool IsTracking { get; private set; }

        public readonly AsyncEvent<Services.GeoPosition> PositionChanged = new AsyncEvent<Services.GeoPosition>(ConcurrentEventRaisePolicy.Queue);
        public readonly AsyncEvent<Exception> PositionError = new AsyncEvent<Exception>();

        /// <summary>Returns the current user position.</summary>
        /// <param name="desiredAccuracy">In meters</param>
        /// <param name="timeout">Milliseconds</param>
        /// <param name="silently">If set to true, then the position will only be returned if it's available and request is previously granted but there will be no user interaction (this can only be used in combination with OnError.Ignore or Throw). If set to false (default) then if necessary, the user will be prompted for granting permission, and in any case the specified error action will apply when there i any problem (GPS not supported or enabled on the device, permission denied, general error, etc.)</param>
        public async Task<Services.GeoPosition> GetCurrentPosition(double desiredAccuracy = TEN_METERS, int timeout = FIVE_SECONDS, bool silently = false, OnError errorAction = OnError.Alert)
        {
            if (silently && errorAction != OnError.Ignore && errorAction != OnError.Throw)
                throw new Exception("If you want to get the DeviceLocation silently, ErrorAction must also be Ignore or Throw.");

            if (!(await LocationService.Location.IsSupported()))
            {
                await errorAction.Apply("Geo DeviceLocation is not supported on your device.");
                return null;
            }

            if (Device.Platform != DevicePlatform.IOS && !await LocationService.Location.IsEnabled())
            {
                var result = await Device.Permissions.Check(DevicePermission.Location);

                if (result != PermissionResult.Granted)
                {
                    await errorAction.Apply("Geo DeviceLocation is not enabled on your device.");
                    return null;
                }
            }

            if (silently && !await DevicePermission.Location.IsGranted())
            {
                await errorAction.Apply("Permission is not already granted to access the current DeviceLocation.");
                return null;
            }

            if (!await DevicePermission.Location.IsRequestGranted())
            {
                await errorAction.Apply("Permission was not granted to access your current DeviceLocation.");
                return null;
            }

            try
            {
                return await Device.UIThread.Run(() => TryGetCurrentPosition(desiredAccuracy, timeout));
            }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to get current position.");
                return null;
            }
        }

        /// <summary>Starts tracking the user's DeviceLocation.</summary>
        /// <param name="silently">If set to true, then the tracking will start if DeviceLocation permission is already granted but there will be no user interaction (this can only be used in combination with OnError.Ignore or Throw). If set to false (default) then if necessary, the user will be prompted for granting permission, and in any case the specified error action will apply when there i any problem (GPS not supported or enabled on the device, permission denied, general error, etc.)</param>
        public async Task<bool> StartTracking(LocationTrackingSettings settings = null, bool silently = false, OnError errorAction = OnError.Alert)
        {
            if (silently && errorAction != OnError.Ignore && errorAction != OnError.Throw)
                throw new Exception("If you want to track the DeviceLocation silently, ErrorAction must be Ignore or Throw.");

            if (silently && !await DevicePermission.Location.IsGranted())
            {
                await errorAction.Apply("Permission is not already granted to access the current DeviceLocation.");
                return false;
            }

            if (!await DevicePermission.Location.IsRequestGranted())
            {
                await errorAction.Apply("Permission was not granted to access your current DeviceLocation.");
                return false;
            }

            if (IsTracking) await StopTracking();

            if (settings == null) settings = new LocationTrackingSettings();

            try
            {
                await DoStartTracking(settings);
                return true;
            }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to start tracking your DeviceLocation.");
                return false;
            }
        }

        /// <summary>
        /// Will launch the external directions application and return whether it was successful.
        /// </summary>
        public async Task<bool> LaunchDirections(NavigationAddress destination, OnError errorAction = OnError.Toast)
        {
            try
            {
                await DoLaunchDirections(destination);
                return true;
            }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Launching navigation directions failed");
                return false;
            }
        }
    }

}
