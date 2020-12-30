namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Android.App;
    using Android.Content;
    using Android.Locations;
    using System.Linq;
    using Olive;

    partial class Location
    {
        const string BASE_URL = "http://" + "maps.google.com/maps?daddr=";

        static string ToUrl(NavigationAddress address)
        {
            if (address.HasGeoLocation())
                return $"{address.Latitude},{address.Longitude}{address.Name.WithWrappers(" (", ")")}";
            else
            {
                var addr = address.GetAddressParts();
                var location = GetLocationFromAddress(addr.ToString(","));
                return location.Latitude + "," + location.Longitude;
            }
        }

        static Address GetLocationFromAddress(string strAddress)
        {
            using (var coder = new Geocoder(UIRuntime.CurrentActivity))
            {
                var address = coder.GetFromLocationName(strAddress, 5).ToList();
                return address?[0];
            }
        }

        static Task DoLaunchDirections(NavigationAddress destination)
        {
            var url = $"{BASE_URL}{ToUrl(destination)}";

            if (Attempt(url, "com.google.android.apps.maps", "com.google.android.maps.MapsActivity"))
                return Task.CompletedTask;

            if (Attempt(url)) return Task.CompletedTask;

            if (destination.HasGeoLocation())
                url = "geo:{0},{1}?q={0},{1}{2}".FormatWith(
                    destination.Latitude, destination.Longitude, destination.Name.WithWrappers("(", ")"));
            else
                url = "geo:0,0?q=" + destination.GetAddressParts().ToString(" ");

            if (Attempt(url)) return Task.CompletedTask;

            throw new Exception(" There is no recognized map application.");
        }

        static bool Attempt(string url, string packageName = null, string className = null)
        {
            var uri = Android.Net.Uri.Parse(url);

            var intent = new Intent(Intent.ActionView, uri);
            if (className.HasValue()) intent.SetClassName(packageName, className);

            try
            {
                intent.SetFlags(ActivityFlags.ClearTop);
                intent.SetFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(intent);
                return true;
            }
            catch (ActivityNotFoundException) { return false; }
        }
    }
}