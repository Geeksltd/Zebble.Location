namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using CoreLocation;
    using Foundation;

    public partial class DeviceLocation
    {
        bool IsDeferringUpdates;
        CLLocationManager Manager;
        LocationTrackingSettings CurrentTrackingSettings;

        void OnDeferredUpdatedFinished(object sender, NSErrorEventArgs e)
        {
            IsDeferringUpdates = false;
        }

        public Task<bool> IsSupported() => Task.FromResult(result: true); // all iOS devices support at least wifi geoDeviceLocation

        public async Task<bool> IsEnabled() => await Device.Permissions.Check(DevicePermission.Location) == PermissionResult.Granted;

        async Task<Services.GeoPosition> TryGetCurrentPosition(double desiredAccuracy, int timeout)
        {
            TaskCompletionSource<Services.GeoPosition> tcs;

            if (!IsTracking)
            {
                var manager = await CreateManager();
                if (manager == null) return null;

                if (await Device.Permissions.Check(DevicePermission.BackgroundLocation) == PermissionResult.Granted)
                    manager.AllowsBackgroundLocationUpdates = true;

                // We need a single update.
                if (Device.OS.IsAtLeastiOS(6))
                    manager.PausesLocationUpdatesAutomatically = false;

                tcs = new TaskCompletionSource<Services.GeoPosition>(manager);
                using (var singleListener = new GeoLocationSingleUpdateDelegate(manager, desiredAccuracy, timeout))
                {
                    manager.Delegate = singleListener;
                    manager.StartUpdatingLocation();
                    var result = await singleListener.TaskSource.Task;
                }
            }

            tcs = new TaskCompletionSource<Services.GeoPosition>();

            Task gotError(Exception ex)
            {
                tcs.TrySetException(ex);
                PositionError.RemoveHandler(gotError);
                return Task.CompletedTask;
            }

            PositionError.Handle(gotError);

            Task gotPosition(Services.GeoPosition position)
            {
                tcs.TrySetResult(position);
                PositionChanged.RemoveHandler(gotPosition);
                return Task.CompletedTask;
            }

            PositionChanged.Handle(gotPosition);
            return await tcs.Task;
        }

        bool CanDeferLocationUpdate => Device.OS.IsAtLeastiOS(6);

        async Task DoStartTracking(LocationTrackingSettings settings)
        {
            CurrentTrackingSettings = settings;

            Manager = await CreateManager();

            if (Device.OS.IsAtLeastiOS(9))
                Manager.AllowsBackgroundLocationUpdates = settings.AllowBackgroundUpdates;

            if (Device.OS.IsAtLeastiOS(6))
            {
                Manager.PausesLocationUpdatesAutomatically = settings.AutoPauseWhenSteady;

                switch (settings.Purpose)
                {
                    case LocationTrackingPurpose.AutomotiveNavigation:
                        Manager.ActivityType = CLActivityType.AutomotiveNavigation;
                        break;
                    case LocationTrackingPurpose.OtherNavigation:
                        Manager.ActivityType = CLActivityType.OtherNavigation;
                        break;
                    case LocationTrackingPurpose.Fitness:
                        Manager.ActivityType = CLActivityType.Fitness;
                        break;
                    default:
                        Manager.ActivityType = CLActivityType.Other;
                        break;
                }
            }

            if (CanDeferLocationUpdate && settings.DeferLocationUpdates)
                settings.MovementThreshold = (float)CLLocationDistance.FilterNone;

            IsTracking = true;
            Manager.DesiredAccuracy = CLLocation.AccuracyBest;
            Manager.DistanceFilter = settings.MovementThreshold;

            if (settings.IgnoreSmallChanges) Manager.StartMonitoringSignificantLocationChanges();
            else Manager.StartUpdatingLocation();
        }

        public Task<bool> StopTracking()
        {
            if (!IsTracking || Manager == null) return Task.FromResult(result: true);

            IsTracking = false;

            if (CanDeferLocationUpdate) Manager.DisallowDeferredLocationUpdates();

            Manager.StopMonitoringSignificantLocationChanges();
            Manager.StopUpdatingLocation();

            return Task.FromResult(result: true);
        }

        async Task<CLLocationManager> CreateManager()
        {
            if (Manager != null) return Manager;

            if (await Device.Permissions.Request(DevicePermission.Location) != PermissionResult.Granted) return null;

            await Device.UIThread.Run(() =>
            {
                Manager = new CLLocationManager();
                Manager.AuthorizationChanged += OnAuthorizationChanged;
                Manager.Failed += OnFailed;

                if (Device.OS.IsAtLeastiOS(6))
                    Manager.LocationsUpdated += OnLocationsUpdated;
                else
                    Manager.UpdatedLocation += async (s, e) => await UpdatePosition(e.NewLocation);

                Manager.DeferredUpdatesFinished += OnDeferredUpdatedFinished;
            });

            return Manager;
        }

        async void OnLocationsUpdated(object sender, CLLocationsUpdatedEventArgs e)
        {
            foreach (var Location in e.Locations) await UpdatePosition(Location);

            // defer future DeviceLocation updates if requested
            if ((CurrentTrackingSettings?.DeferLocationUpdates == true) && !IsDeferringUpdates && CanDeferLocationUpdate)
            {
                Manager.AllowDeferredLocationUpdatesUntil(
                    CurrentTrackingSettings.MovementThreshold,
                    CurrentTrackingSettings.DeferralTime?.TotalSeconds ?? CLLocationManager.MaxTimeInterval);

                IsDeferringUpdates = true;
            }
        }

        async Task UpdatePosition(CLLocation location)
        {
            var result = new Services.GeoPosition
            {
                Accuracy = location.HorizontalAccuracy,
                Latitude = location.Coordinate.Latitude,
                Longitude = location.Coordinate.Longitude,
                Altitude = location.Altitude,
                AltitudeAccuracy = location.VerticalAccuracy,
                Speed = location.Speed
            };

            if (location.VerticalAccuracy == -1)
            {
                result.Altitude = null;
                result.AltitudeAccuracy = null;
            }

            if (location.Speed == -1) result.Speed = null;

            await PositionChanged?.RaiseOn(Device.ThreadPool, result);
            location.Dispose();
        }

        async void OnFailed(object sender, NSErrorEventArgs e)
        {
            var error = (CLError)(int)e.Error.Code;

            if (error == CLError.Network)
                await PositionError.RaiseOn(Device.ThreadPool, new Exception(UNAVAILABLE_ERROR));

            if (error == CLError.Denied)
                await PositionError.RaiseOn(Device.ThreadPool, new Exception(UNAUTHORISED_ERROR));
        }

        void OnAuthorizationChanged(object sender, CLAuthorizationChangedEventArgs e)
        {
            if (e.Status == CLAuthorizationStatus.Denied || e.Status == CLAuthorizationStatus.Restricted)
                PositionError.RaiseOn(Device.ThreadPool, new Exception(UNAUTHORISED_ERROR));
        }
    }
}