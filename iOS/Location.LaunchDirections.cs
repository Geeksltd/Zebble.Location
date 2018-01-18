namespace Zebble.Device
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using CoreLocation;
    using Foundation;
    using MapKit;

    partial class Location
    {
        static async Task DoLaunchDirections(NavigationAddress destination)
        {
            await Thread.UI.Run(async () =>
            {
                CLLocationCoordinate2D? coords;

                if (destination.HasGeoLocation())
                    coords = new CLLocationCoordinate2D(destination.Latitude.Value, destination.Longitude.Value);
                else
                    coords = await FindCoordinates(destination);

                using (var mark = new MKPlacemark(coords.Value, default(NSDictionary)))
                using (var mapItem = new MKMapItem(mark) { Name = destination.Name.OrEmpty() })
                    MKMapItem.OpenMaps(new[] { mapItem });
            });
        }

        static async Task<CLLocationCoordinate2D?> FindCoordinates(NavigationAddress address)
        {
            CLPlacemark[] placemarks = null;
            var placemarkAddress =
            new MKPlacemarkAddress
            {
                City = address.City.OrEmpty(),
                Country = address.Country.OrEmpty(),
                CountryCode = address.CountryCode.OrEmpty(),
                State = address.State.OrEmpty(),
                Street = address.Street.OrEmpty(),
                Zip = address.Zip.OrEmpty()
            };

            try
            {
                using (var coder = new CLGeocoder())
                    placemarks = await coder.GeocodeAddressAsync(placemarkAddress.Dictionary);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to obtain geo Location from address: " + ex);
            }

            var result = placemarks?.FirstOrDefault()?.Location?.Coordinate;

            if (result == null)
                throw new Exception("Failed to obtain geo Location from address.");

            return result;
        }
    }
}