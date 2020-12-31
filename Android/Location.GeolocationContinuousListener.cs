namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Android.Locations;
    using Android.OS;
    using Olive;

    partial class Location
    {
        internal class GeoLocationContinuousListener : Java.Lang.Object, ILocationListener
        {
            HashSet<string> ActiveProviders = new HashSet<string>();
            LocationManager Manager;
            string ActiveProvider;
            Android.Locations.Location LatestLocation;
            TimeSpan ReportIntervals;

            public readonly AsyncEvent<Exception> PositionError = new AsyncEvent<Exception>();
            public readonly AsyncEvent<GeoPosition> PositionChanged = new AsyncEvent<GeoPosition>(ConcurrentEventRaisePolicy.Queue);

            public GeoLocationContinuousListener(LocationManager manager, TimeSpan reportIntervals, IList<string> providers)
            {
                Manager = manager;
                ReportIntervals = reportIntervals;

                foreach (var p in providers)
                    if (manager.IsProviderEnabled(p)) ActiveProviders.Add(p);
            }

            public void OnLocationChanged(Android.Locations.Location location)
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
                PositionChanged.RaiseOn(Zebble.Thread.Pool, position);
            }

            public void OnProviderDisabled(string provider)
            {
                if (provider == LocationManager.PassiveProvider)
                    return;

                lock (ActiveProviders)
                {
                    if (ActiveProviders.Remove(provider) && ActiveProviders.None())
                        PositionError.RaiseOn(Zebble.Thread.Pool, new Exception(UNAVAILABLE_ERROR));
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

            static TimeSpan ToTime(long time) => new TimeSpan(TimeSpan.TicksPerMillisecond * time);
        }
    }
}