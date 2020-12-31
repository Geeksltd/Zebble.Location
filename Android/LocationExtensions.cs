namespace Zebble.Device
{
    static class LocationExtensions
    {
        public static GeoPosition ToGeoPosition(this Android.Locations.Location location)
        {
            var result = new GeoPosition
            {
                Longitude = location.Longitude,
                Latitude = location.Latitude
            };

            if (location.HasAccuracy) result.Accuracy = location.Accuracy;
            if (location.HasAltitude) result.Altitude = location.Altitude;
            if (location.HasSpeed) result.Speed = location.Speed;

            return result;
        }
    }
}
