namespace Zebble.Device
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Android.Content;
    using Android.Locations;
    using Android.OS;

    partial class Location
    {
        static string[] Providers;
        static LocationManager Manager;
        static GeoLocationContinuousListener Listener;
        static GeoPosition LastPosition;

        static Location()
        {
            Init();
        }

        internal static void Init()
        {
            Manager = UIRuntime.GetService<LocationManager>(Context.LocationService);
            Providers = Manager.GetProviders(enabledOnly: false).Where(s => s != LocationManager.PassiveProvider).ToArray();
        }

        static string[] EnabledProviders => Providers.Where(Manager.IsProviderEnabled).ToArray();

        public static Task<bool> IsSupported() => Task.FromResult(Providers.Length > 0);

        public static Task<bool> IsEnabled() => Task.FromResult(EnabledProviders.Any());

        static async Task<GeoPosition> TryGetCurrentPosition(double desiredAccuracy, int timeout)
        {
            if (!IsTracking)
                return await ObtainCurrentPosition(desiredAccuracy, timeout);

            var source = new TaskCompletionSource<GeoPosition>();

            // We're already listening, just use the current listener
            if (LastPosition == null)
            {
                Task gotPosition(GeoPosition p)
                {
                    source.TrySetResult(p);
                    PositionChanged.RemoveHandler(gotPosition);
                    return Task.CompletedTask;
                }

                PositionChanged.Handle(gotPosition);
            }
            else source.TrySetResult(LastPosition);

            return await source.Task;
        }

        static async Task<GeoPosition> ObtainCurrentPosition(double desiredAccuracy, int timeout)
        {
            var source = new TaskCompletionSource<GeoPosition>();

            GeoLocationSingleListener singleListener = null;
            singleListener = new GeoLocationSingleListener((float)desiredAccuracy, timeout, EnabledProviders,
                onFinished: () =>
                {
                    for (int i = 0; i < Providers.Length; ++i)
                        Manager.RemoveUpdates(singleListener);
                });

            try
            {
                var looper = Looper.MyLooper() ?? Looper.MainLooper;

                int enabled = EnabledProviders.Count();

                foreach (var provider in Providers)
                    Manager.RequestLocationUpdates(provider, 0, 0, singleListener, looper);

                if (enabled == 0)
                {
                    for (int i = 0; i < Providers.Length; ++i)
                        Manager.RemoveUpdates(singleListener);

                    source.SetException(new Exception(UNAVAILABLE_ERROR));
                    return await source.Task;
                }
            }
            catch (Java.Lang.SecurityException ex)
            {
                source.SetException(new Exception(UNAUTHORISED_ERROR, ex));
                return await source.Task;
            }

            return await singleListener.CompletionSource.Task;
        }

        static Task DoStartTracking(LocationTrackingSettings settings)
        {
            Listener = new GeoLocationContinuousListener(Manager, settings.ReportInterval, Providers);
            Listener.PositionChanged.Handle(x => OnListenerPositionChanged(x));
            Listener.PositionError.Handle(OnListenerPositionError);

            var looper = Looper.MyLooper() ?? Looper.MainLooper;
            for (var i = 0; i < Providers.Length; ++i)
                Manager.RequestLocationUpdates(Providers[i], (long)settings.ReportInterval.TotalMilliseconds, settings.MovementThreshold, Listener, looper);

            return Task.CompletedTask;
        }

        public static Task<bool> StopTracking()
        {
            if (Listener == null) return Task.FromResult(result: true);

            Listener.PositionChanged.RemoveHandler(OnListenerPositionChanged);
            Listener.PositionError.RemoveHandler(OnListenerPositionError);

            for (int i = 0; i < Providers.Length; ++i)
                Manager.RemoveUpdates(Listener);

            Listener = null;
            return Task.FromResult(result: true);
        }

        static Task OnListenerPositionChanged(GeoPosition position)
        {
            if (!IsTracking) return Task.CompletedTask;

            LastPosition = position;
            return PositionChanged.RaiseOn(Thread.Pool, position);
        }

        static Task OnListenerPositionError(Exception error) => StopTracking().ContinueWith(x => PositionError.Raise(error));
    }
}