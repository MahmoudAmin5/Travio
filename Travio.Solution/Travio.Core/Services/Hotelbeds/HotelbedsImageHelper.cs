namespace Travio.Core.Services.Hotelbeds
{
    /// <summary>
    /// Static helper for constructing high-resolution Hotelbeds GIATA image URLs.
    /// 
    /// Hotelbeds returns raw relative paths (e.g., "12/123456/123456a.jpg").
    /// Appending them to the default base URL produces tiny 320px images.
    /// This helper constructs the correct CDN URLs at the desired resolution tier:
    ///   - /bigger/   → 800px wide  (search result cards, thumbnails)
    ///   - /original/ → max resolution (hotel detail galleries, fullscreen)
    /// </summary>
    public static class HotelbedsImageHelper
    {
        private const string CdnBase = "https://photos.hotelbeds.com/giata";

        /// <summary>
        /// Constructs an 800px-wide thumbnail URL suitable for search result cards.
        /// </summary>
        /// <param name="rawPath">The raw image path from the Content API (e.g., "12/123456/123456a.jpg").</param>
        /// <returns>Full HTTPS URL at /bigger/ resolution, or empty string if path is null/empty.</returns>
        public static string GetThumbnailUrl(string? rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return string.Empty;
            return $"{CdnBase}/original/{rawPath.TrimStart('/')}";
        }

        /// <summary>
        /// Constructs a max-resolution gallery URL suitable for hotel detail pages.
        /// </summary>
        /// <param name="rawPath">The raw image path from the Content API (e.g., "12/123456/123456a.jpg").</param>
        /// <returns>Full HTTPS URL at /original/ resolution, or empty string if path is null/empty.</returns>
        public static string GetGalleryImageUrl(string? rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return string.Empty;
            return $"{CdnBase}/original/{rawPath.TrimStart('/')}";
        }
    }
}
