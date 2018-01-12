namespace Zebble
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Devices.Geolocation;
    using Windows.Foundation;
    using Zebble.NativeImpl;

    partial class DeviceLocation
    {
        Geolocator locator;

        public async Task<bool> IsSupported() => await GetGeolocatorStatus() != PositionStatus.NotAvailable;

        public async Task<bool> IsEnabled() => new[] { PositionStatus.NotAvailable, PositionStatus.Disabled }.Lacks(await GetGeolocatorStatus());

        async Task<Services.GeoPosition> TryGetCurrentPosition(double _, int timeout)
        {
            if (EnvironmentSimulator.Location != null) return EnvironmentSimulator.Location;

            var pos = Locator.GetGeopositionAsync(TimeSpan.Zero, timeout.Milliseconds());

            var source = new TaskCompletionSource<Services.GeoPosition>();
            var found = false;

            Task.Delay(timeout.Milliseconds())
                 .ContinueWith(t =>
                 {
                     if (!found) source?.TrySetException(new Exception(TIMEOUT_ERROR));
                 }).RunInParallel();

            pos.Completed = (op, s) =>
            {
                found = true;
                switch (s)
                {
                    case AsyncStatus.Canceled: source.SetCanceled(); break;
                    case AsyncStatus.Completed: source.TrySetResult(ToGeoPosition(op.GetResults())); break;
                    case AsyncStatus.Error: source.SetException(op.ErrorCode); break;
                    default: break;
                }
            };

            return await source.Task;
        }

        Task DoStartTracking(LocationTrackingSettings settings)
        {
            IsTracking = true;

            Locator.ReportInterval = (uint)settings.ReportInterval.TotalMilliseconds;
            Locator.MovementThreshold = settings.MovementThreshold;
            Locator.PositionChanged += OnLocatorPositionChanged;
            Locator.StatusChanged += OnLocatorStatusChanged;

            return Task.CompletedTask;
        }

        public Task<bool> StopTracking()
        {
            if (!IsTracking) return Task.FromResult(result: true);

            Locator.PositionChanged -= OnLocatorPositionChanged;
            Locator.StatusChanged -= OnLocatorStatusChanged;
            IsTracking = false;

            return Task.FromResult(result: true);
        }

        async void OnLocatorStatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            switch (args.Status)
            {
                case PositionStatus.NoData:
                    await StopTracking();
                    await Device.ThreadPool.Run(() => PositionError.Raise(new Exception(UNAVAILABLE_ERROR)));
                    return;

                case PositionStatus.Disabled:
                    await StopTracking();
                    await Device.ThreadPool.Run(() => PositionError.Raise(new Exception(UNAUTHORISED_ERROR)));
                    return;

                default: return;
            }
        }

        async void OnLocatorPositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            var pos = ToGeoPosition(args.Position);
            await Device.ThreadPool.Run(() => PositionChanged.Raise(pos));
        }

        Geolocator Locator
        {
            get
            {
                if (locator == null)
                {
                    locator = new Geolocator();
                    locator.StatusChanged += OnLocatorStatusChanged;
                }

                return locator;
            }
        }

        async Task<PositionStatus> GetGeolocatorStatus()
        {
            var result = Locator.LocationStatus;
            while (result == PositionStatus.Initializing)
            {
                await Task.Delay(100);
                result = Locator.LocationStatus;
            }

            return result;
        }

        static Services.GeoPosition ToGeoPosition(Geoposition position)
        {
            return new Services.GeoPosition
            {
                Latitude = position.Coordinate.Point.Position.Latitude,
                Longitude = position.Coordinate.Point.Position.Longitude,
                Accuracy = position.Coordinate.Accuracy,
                Altitude = position.Coordinate.Point.Position.Altitude,
                Speed = position.Coordinate.Speed,
                AltitudeAccuracy = position.Coordinate.AltitudeAccuracy
            };
        }

        internal class Timeout
        {
            readonly CancellationTokenSource Canceller = new CancellationTokenSource();

            public Timeout(TimeSpan timeout, Action onIimeup)
            {
                Task.Delay(timeout, Canceller.Token)
                    .ContinueWith(t =>
                    {
                        if (!t.IsCanceled) onIimeup?.Invoke();
                    });
            }

            public void Cancel() => Canceller.Cancel();
        }

    }
}
