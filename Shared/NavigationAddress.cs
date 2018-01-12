namespace Zebble
{
    using System;
    using System.Linq;

    public partial class NavigationAddress
    {
        public string Name, Street, City, State, Zip, Country, CountryCode;
        public double? Latitude, Longitude;

        internal bool HasGeoLocation() => Latitude.HasValue && Longitude.HasValue;

        internal string[] GetAddressParts()
        {
            return new[] { Name, Street, City, State, Zip, Country, CountryCode }.Trim().ToArray();
        }
    }
}