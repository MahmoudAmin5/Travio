using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Travio.Core.Setting;

namespace Travio.Core.Services.Hotelbeds
{
    /// <summary>
    /// A custom <see cref="DelegatingHandler"/> that intercepts every outgoing HTTP request
    /// to the Hotelbeds APITUDE API and injects the required authentication headers.
    ///
    /// Hotelbeds uses a custom authentication scheme where every request must include:
    ///   • Api-key: The API key from configuration.
    ///   • X-Signature: SHA-256 hash of (ApiKey + SharedSecret + CurrentUnixTimestampInSeconds).
    ///   • Accept: application/json
    ///
    /// This handler is registered as a message handler on the typed HttpClient via
    /// <c>.AddHttpMessageHandler&lt;HotelbedsAuthHandler&gt;()</c> in DI configuration.
    ///
    /// The signature is generated fresh for every request because it includes a timestamp,
    /// which means it expires — Hotelbeds rejects requests with stale signatures.
    /// </summary>
    public class HotelbedsAuthHandler : DelegatingHandler
    {
        private readonly HotelbedsSettings _settings;

        /// <summary>
        /// Initializes the handler with Hotelbeds configuration injected via the Options Pattern.
        /// </summary>
        /// <param name="options">
        /// The <see cref="IOptions{HotelbedsSettings}"/> containing API key, shared secret, and base URL.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if options or its value is null.</exception>
        public HotelbedsAuthHandler(IOptions<HotelbedsSettings> options)
        {
            _settings = options?.Value
                ?? throw new ArgumentNullException(nameof(options), "HotelbedsSettings must be configured.");
        }

        /// <summary>
        /// Intercepts the outgoing request pipeline to inject Hotelbeds authentication headers
        /// before the request is sent to the server.
        /// </summary>
        /// <param name="request">The outgoing HTTP request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP response from the downstream handler.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // ── Generate the X-Signature ──────────────────────────────────────────
            // Formula: SHA256( ApiKey + SharedSecret + UnixTimestampInSeconds )
            // The timestamp ensures signatures expire, preventing replay attacks.
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signatureRaw = $"{_settings.ApiKey}{_settings.SharedSecret}{unixTimestamp}";
            var signatureHash = ComputeSha256Hash(signatureRaw);

            // ── Inject required Hotelbeds headers ─────────────────────────────────
            request.Headers.Remove("Api-key");       // Prevent duplicates on retry
            request.Headers.Remove("X-Signature");
            request.Headers.Remove("Accept");

            request.Headers.Add("Api-key", _settings.ApiKey);
            request.Headers.Add("X-Signature", signatureHash);
            request.Headers.Add("Accept", "application/json");

            // ── Delegate to the next handler in the pipeline ──────────────────────
            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Computes a lowercase hexadecimal SHA-256 hash of the given input string.
        /// </summary>
        /// <param name="input">The raw string to hash.</param>
        /// <returns>The hash as a lowercase hex string (64 characters).</returns>
        private static string ComputeSha256Hash(string input)
        {
            // Convert the input string to bytes using UTF-8 encoding
            var inputBytes = Encoding.UTF8.GetBytes(input);

            // Compute the SHA-256 hash
            var hashBytes = SHA256.HashData(inputBytes);

            // Convert the hash to a lowercase hex string
            // Each byte → 2 hex chars, e.g., 0xAB → "ab"
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
