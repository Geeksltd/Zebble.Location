namespace Zebble.Device
{
    using System;

    /// <summary>Currently used for iOS only, and ignored in other operating systems.</summary>
    public class LocationTrackingSettings
    {
        /// <summary>
        /// The requested minimum time interval between location updates, in milliseconds. If your application requires updates infrequently, set this value so that location services can conserve power by calculating location only when needed.
        /// </summary>
        public TimeSpan ReportInterval { get; set; } = 1.Seconds();

        /// <summary>The minimum distance of movement needed (in meters) relative to the coordinate from the last change event to report an update.</summary>
        public float MovementThreshold { get; set; } = 1f;

        /// <summary>Whether background location updates are allowed (iOS 9+).</summary>
        public bool AllowBackgroundUpdates { get; set; } = false;

        /// <summary>Whether location updates should be paused automatically when the location is unlikely to change (iOS 6+). True by default.</summary>
        public bool AutoPauseWhenSteady { get; set; } = true;

        /// <summary>
        /// The purpose of tracking. This is used by the OS to determine when to auto-pause location updates (iOS 6+).
        /// </summary>
        public LocationTrackingPurpose Purpose { get; set; } = LocationTrackingPurpose.Other;

        /// <summary>Whether the location manager should only listen for significant changes in location, rather than continuous listening (iOS 4+)./// </summary>
        public bool IgnoreSmallChanges { get; set; } = false;

        /// <summary> Whether the location manager should defer location updates until an energy efficient time arrives, or distance and time criteria are met (iOS 6+).</summary>
        public bool DeferLocationUpdates { get; set; } = false;

        /// <summary> If deferring location updates, the minimum time that should elapse before updates are delivered (iOS 6+). Set to null for indefinite wait. Default:  5 minutes</summary>
        public TimeSpan? DeferralTime { get; set; } = TimeSpan.FromMinutes(5);
    }
}