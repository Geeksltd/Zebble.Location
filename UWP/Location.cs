namespace Zebble.Device
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Devices.Geolocation;
    using Windows.Foundation;
    using Olive;
    using Thread = Zebble.Thread;

    partial class Location
    {
        static Geolocator locator;

        public static async Task<bool> IsSupported() => await GetGeolocatorStatus() != PositionStatus.NotAvailable;

        public static async Task<bool> IsEnabled() => new[] { PositionStatus.NotAvailable, PositionStatus.Disabled }.Lacks(await GetGeolocatorStatus());

        static async Task<GeoPosition> TryGetCurrentPosition(double _, int timeout)
        {
            if (EnvironmentSimulator.Location != null) return EnvironmentSimulator.Location;

            var pos = Locator.GetGeopositionAsync(TimeSpan.Zero, timeout.Milliseconds());

            var source = new TaskCompletionSource<GeoPosition>();
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

        static Task DoStartTracking(LocationTrackingSettings settings)
        {
            IsTracking = true;

            Locator.ReportInterval = (uint)settings.ReportInterval.TotalMilliseconds;
            Locator.MovementThreshold = settings.MovementThreshold;
            Locator.PositionChanged += OnLocatorPositionChanged;
            Locator.StatusChanged += OnLocatorStatusChanged;

            return Task.CompletedTask;
        }

        public static Task<bool> StopTracking()
        {
            if (!IsTracking) return Task.FromResult(result: true);

            Locator.PositionChanged -= OnLocatorPositionChanged;
            Locator.StatusChanged -= OnLocatorStatusChanged;
            IsTracking = false;

            return Task.FromResult(result: true);
        }

        static async void OnLocatorStatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            switch (args.Status)
            {
                case PositionStatus.NoData:
                    await StopTracking();
                    await Thread.Pool.Run(() => PositionError.Raise(new Exception(UNAVAILABLE_ERROR)));
                    return;

                case PositionStatus.Disabled:
                    await StopTracking();
                    await Thread.Pool.Run(() => PositionError.Raise(new Exception(UNAUTHORISED_ERROR)));
                    return;

                default: return;
            }
        }

        static async void OnLocatorPositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            var pos = ToGeoPosition(args.Position);
            await Thread.Pool.Run(() => PositionChanged.Raise(pos));
        }

        static Geolocator Locator
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

        static async Task<PositionStatus> GetGeolocatorStatus()
        {
            var result = Locator.LocationStatus;
            while (result == PositionStatus.Initializing)
            {
                await Task.Delay(100);
                result = Locator.LocationStatus;
            }

            return result;
        }

        static GeoPosition ToGeoPosition(Geoposition position)
        {
            return new GeoPosition
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

            public Timeout(TimeSpan timeout, Action onTimeup)
            {
                Task.Delay(timeout, Canceller.Token)
                    .ContinueWith(t =>
                    {
                        if (!t.IsCanceled) onTimeup?.Invoke();
                    });
            }

            public void Cancel() => Canceller.Cancel();
        }
    }
}
