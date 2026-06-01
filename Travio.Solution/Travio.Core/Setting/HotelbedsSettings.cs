namespace Travio.Core.Setting
{
    /// <summary>
    /// Strongly-typed configuration for the Hotelbeds APITUDE API.
    /// Bound from appsettings.json section "HotelbedsSettings" via the Options Pattern.
    /// NEVER hardcode API keys — they are injected at runtime from configuration.
    /// </summary>
    public class HotelbedsSettings
    {
        /// <summary>
        /// The API key provided by Hotelbeds for authenticating requests.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The shared secret used alongside the API key to generate the X-Signature hash.
        /// </summary>
        public string SharedSecret { get; set; } = string.Empty;

        /// <summary>
        /// The base URL of the Hotelbeds Booking API (e.g., "https://api.test.hotelbeds.com/hotel-api/1.0/").
        /// Used for: availability, checkrates, bookings.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// The base URL of the Hotelbeds Content API (e.g., "https://api.test.hotelbeds.com/hotel-content-api/1.0/").
        /// Used for: hotel details, images, descriptions, facilities.
        /// This is a DIFFERENT API than the Booking API but uses the same authentication.
        /// </summary>
        public string ContentApiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// The base URL for Hotelbeds hotel images (e.g., "http://photos.hotelbeds.com/giata/").
        /// Image paths from the Content API are appended to this URL to form full image URLs.
        /// </summary>
        public string ImageBaseUrl { get; set; } = "http://photos.hotelbeds.com/giata/";
    }
}
