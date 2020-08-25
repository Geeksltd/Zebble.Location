namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;

    public static partial class Location
    {
        const string UNAVAILABLE_ERROR = "Device Location data is unavailable.";
        const string UNAUTHORISED_ERROR = "Access to Device Location data was denied.";
        const string TIMEOUT_ERROR = "Getting Device Location timed out.";

        const double TEN_METERS = 10;
        const int FIVE_SECONDS = 5 * 1000;

        public static bool IsTracking { get; private set; }

        public static readonly AsyncEvent<Services.GeoPosition> PositionChanged = new AsyncEvent<Services.GeoPosition>(ConcurrentEventRaisePolicy.Queue);
        public static readonly AsyncEvent<Exception> PositionError = new AsyncEvent<Exception>();

        /// <summary>Returns the current user position.</summary>
        /// <param name="desiredAccuracy">In meters</param>
        /// <param name="timeout">Milliseconds</param>
        /// <param name="silently">If set to true, then the position will only be returned if it's available and request is previously granted but there will be no user interaction (this can only be used in combination with OnError.Ignore or Throw). If set to false (default) then if necessary, the user will be prompted for granting permission, and in any case the specified error action will apply when there i any problem (GPS not supported or enabled on the device, permission denied, general error, etc.)</param>
        public static async Task<Services.GeoPosition> GetCurrentPosition(double desiredAccuracy = TEN_METERS, int timeout = FIVE_SECONDS, bool silently = false, OnError errorAction = OnError.Alert)
        {
            if (silently && errorAction != OnError.Ignore && errorAction != OnError.Throw)
                throw new Exception("If you want to get the DeviceLocation silently, ErrorAction must also be Ignore or Throw.");

            await AskForPermission();

            if (!(await IsSupported()))
            {
                await errorAction.Apply("Geo DeviceLocation is not supported on your device.");
                return null;
            }

            if (OS.Platform != DevicePlatform.IOS && !await IsEnabled())
            {
                var result = await Device.Permissions.Check(Permission.Location);

                if (result != PermissionResult.Granted)
                {
                    await errorAction.Apply("Geo DeviceLocation is not enabled on your device.");
                    return null;
                }
            }

            if (silently && !await Permission.Location.IsGranted())
            {
                await errorAction.Apply("Permission is not already granted to access the current DeviceLocation.");
                return null;
            }

            if (!await Permission.Location.IsRequestGranted())
            {
                await errorAction.Apply("Permission was not granted to access your current DeviceLocation.");
                return null;
            }

            try
            {
                return await Thread.UI.Run(() => TryGetCurrentPosition(desiredAccuracy, timeout));
            }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to get current position.");
                return null;
            }
        }

        static async Task AskForPermission()
        {
            if (await Permission.Location.Request() == PermissionResult.Granted)
            {
#if ANDROID
                Init();
#endif
            }
        }

        /// <summary>Starts tracking the user's DeviceLocation.</summary>
        /// <param name="silently">If set to true, then the tracking will start if DeviceLocation permission is already granted but there will be no user interaction (this can only be used in combination with OnError.Ignore or Throw). If set to false (default) then if necessary, the user will be prompted for granting permission, and in any case the specified error action will apply when there i any problem (GPS not supported or enabled on the device, permission denied, general error, etc.)</param>
        public static async Task<bool> StartTracking(LocationTrackingSettings settings = null, bool silently = false, OnError errorAction = OnError.Alert)
        {
            if (silently && errorAction != OnError.Ignore && errorAction != OnError.Throw)
                throw new Exception("If you want to track the DeviceLocation silently, ErrorAction must be Ignore or Throw.");

            await AskForPermission();

            if (silently && !await Permission.Location.IsGranted())
            {
                await errorAction.Apply("Permission is not already granted to access the current DeviceLocation.");
                return false;
            }

            if (!await Permission.Location.IsRequestGranted())
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
        public static async Task<bool> LaunchDirections(NavigationAddress destination, OnError errorAction = OnError.Toast)
        {
            try
            {
                await AskForPermission();

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
