namespace Zebble.Device
{
    /// <summary>Used to determine when to automatically pause location updates, which depends on the amount of expected change to warrant re-enabling location updates.</summary>
    public enum LocationTrackingPurpose
    {
        /// <summary>GPS is used for automobile navigation.</summary>
        AutomotiveNavigation,

        /// <summary>GPS is used to track movements for other navigation such as boat, train, or plane.</summary>
        OtherNavigation,

        /// <summary>GPS is used for pedestrian activity.</summary>
        Fitness,

        Other,
    }
}
