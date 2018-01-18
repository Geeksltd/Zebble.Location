namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Android.Locations;
    using Android.OS;

    partial class Location
    {
        internal class GeoLocationSingleListener : Java.Lang.Object, ILocationListener
        {
            readonly object LocationSync = new object();
            Android.Locations.Location BestLocation;
            Action OnFinished;
            float DesiredAccuracy;
            internal TaskCompletionSource<Services.GeoPosition> CompletionSource = new TaskCompletionSource<Services.GeoPosition>();
            HashSet<string> ActiveProviders = new HashSet<string>();

            public GeoLocationSingleListener(float desiredAccuracy, int timeout, IEnumerable<string> activeProviders, Action onFinished)
            {
                DesiredAccuracy = desiredAccuracy;
                OnFinished = onFinished;
                ActiveProviders = new HashSet<string>(activeProviders);

                Task.Delay(timeout.Milliseconds())
                 .ContinueWith(t =>
                 {
                     if (BestLocation == null) CompletionSource?.TrySetResult(null);
                     else Finish(BestLocation);
                 }).RunInParallel();
            }

            void ILocationListener.OnLocationChanged(Android.Locations.Location location)
            {
                if (location.Accuracy <= DesiredAccuracy)
                {
                    Finish(location);
                    return;
                }

                lock (LocationSync)
                {
                    if (BestLocation == null || location.Accuracy <= BestLocation.Accuracy)
                        BestLocation = location;
                }
            }

            public void OnProviderDisabled(string provider)
            {
                lock (ActiveProviders)
                {
                    if (ActiveProviders.Remove(provider) && ActiveProviders.Count == 0)
                        CompletionSource.TrySetException(new Exception(UNAVAILABLE_ERROR));
                }
            }

            public void OnProviderEnabled(string provider)
            {
                lock (ActiveProviders)
                    ActiveProviders.Add(provider);
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

            void Finish(Android.Locations.Location location)
            {
                var position = location.ToGeoPosition();
                OnFinished?.Invoke();
                CompletionSource.TrySetResult(position);
            }
        }
    }
}