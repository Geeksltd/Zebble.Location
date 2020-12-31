namespace Zebble.Device
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using CoreLocation;
    using Foundation;

    partial class Location
    {
        internal class GeoLocationSingleUpdateDelegate : CLLocationManagerDelegate
        {
            GeoPosition Result = new GeoPosition();

            double DesiredAccuracy;
            public TaskCompletionSource<GeoPosition> TaskSource;
            CLLocationManager Manager;

            public GeoLocationSingleUpdateDelegate(CLLocationManager manager, double desiredAccuracy, int timeout)
            {
                Manager = manager;
                TaskSource = new TaskCompletionSource<GeoPosition>(manager);
                DesiredAccuracy = desiredAccuracy;

                Timer timer = null;
                timer = new Timer(s =>
                {
                    TaskSource.TrySetResult(Result);

                    StopListening();
                    timer.Dispose();
                }, null, timeout, 0);
            }

            public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
            {
                if (status == CLAuthorizationStatus.Denied || status == CLAuthorizationStatus.Restricted)
                {
                    StopListening();
                    TaskSource.TrySetException(new Exception(UNAUTHORISED_ERROR));
                }
            }

            public override void Failed(CLLocationManager manager, NSError error)
            {
                switch ((CLError)(int)error.Code)
                {
                    case CLError.Network:
                    case CLError.LocationUnknown:
                        StopListening();
                        TaskSource.SetException(new Exception(UNAVAILABLE_ERROR));
                        break;
                    default: break;
                }
            }

            public override bool ShouldDisplayHeadingCalibration(CLLocationManager _) => true;

            public override void UpdatedLocation(CLLocationManager manager, CLLocation newLocation, CLLocation oldLocation)
            {
                if (newLocation.HorizontalAccuracy < 0) return;
                if (Result?.Accuracy > newLocation.HorizontalAccuracy) return;

                Result = new GeoPosition
                {
                    Accuracy = newLocation.HorizontalAccuracy,
                    Altitude = newLocation.Altitude,
                    AltitudeAccuracy = newLocation.VerticalAccuracy,
                    Latitude = newLocation.Coordinate.Latitude,
                    Longitude = newLocation.Coordinate.Longitude,
                    Speed = newLocation.Speed
                };

                try
                {
                    if (Result.Accuracy <= DesiredAccuracy)
                        TaskSource.TrySetResult(Result);
                }
                catch { }
            }

            void StopListening() => Manager.StopUpdatingLocation();
        }
    }
}