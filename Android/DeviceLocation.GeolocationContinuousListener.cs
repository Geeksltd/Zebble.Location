namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Android.Locations;
    using Android.OS;

    partial class DeviceLocation
    {
        internal class GeoLocationContinuousListener : Java.Lang.Object, ILocationListener
        {
            HashSet<string> ActiveProviders = new HashSet<string>();
            LocationManager Manager;
            string ActiveProvider;
            Location LatestLocation;
            TimeSpan ReportIntervals;

            public readonly AsyncEvent<Exception> PositionError = new AsyncEvent<Exception>();
            public readonly AsyncEvent<Services.GeoPosition> PositionChanged = new AsyncEvent<Services.GeoPosition>(ConcurrentEventRaisePolicy.Queue);

            public GeoLocationContinuousListener(LocationManager manager, TimeSpan reportIntervals, IList<string> providers)
            {
                Manager = manager;
                ReportIntervals = reportIntervals;

                foreach (var p in providers)
                    if (manager.IsProviderEnabled(p)) ActiveProviders.Add(p);
            }

            public void OnLocationChanged(Location location)
            {
                if (location.Provider != ActiveProvider)
                {
                    if (ActiveProvider != null && Manager.IsProviderEnabled(ActiveProvider))
                    {
                        var lapsed = ToTime(location.Time) - ToTime(LatestLocation.Time);

                        if (Manager.GetProvider(location.Provider).Accuracy > Manager.GetProvider(ActiveProvider).Accuracy
                          && lapsed < ReportIntervals.Add(ReportIntervals))
                        {
                            location.Dispose();
                            return;
                        }
                    }

                    ActiveProvider = location.Provider;
                }

                Interlocked.Exchange(ref LatestLocation, location)?.Dispose();

                var position = location.ToGeoPosition();
                PositionChanged.RaiseOn(Device.ThreadPool, position);
            }

            public void OnProviderDisabled(string provider)
            {
                if (provider == LocationManager.PassiveProvider)
                    return;

                lock (ActiveProviders)
                {
                    if (ActiveProviders.Remove(provider) && ActiveProviders.Count == 0)
                        PositionError.RaiseOn(Device.ThreadPool, new Exception(UNAVAILABLE_ERROR));
                }
            }

            public void OnProviderEnabled(string provider)
            {
                if (provider != LocationManager.PassiveProvider)
                    lock (ActiveProviders) ActiveProviders.Add(provider);
            }

            public void OnStatusChanged(string provider, Availability status, Bundle extras)
            {
                switch (status)
                {
                    case Availability.Available: OnProviderEnabled(provider); break;
                    case Availability.OutOfService: OnProviderDisabled(provider); break;
                    default: break;
                }
            }

            TimeSpan ToTime(long time) => new TimeSpan(TimeSpan.TicksPerMillisecond * time);
        }
    }
}