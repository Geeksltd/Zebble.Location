﻿namespace Zebble.Device
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    partial class Location
    {
        static async Task DoLaunchDirections(NavigationAddress destination)
        {
            string query;

            if (destination.HasGeoLocation())
                query = $"collection=point.{destination.Latitude}_{destination.Longitude}_{destination.Name}";
            else
                query = "where=" + destination.GetAddressParts().Select(x => x).ToString(",");

            var successful = await Windows.System.Launcher.LaunchUriAsync(new Uri("bingmaps:?" + query));

            if (!successful) throw new Exception("Failed to launch BingMaps");
        }
    }
}
